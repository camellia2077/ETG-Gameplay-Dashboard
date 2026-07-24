// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EtgGameplayDashboard
{
    // ETG can rebuild player health state while swapping weapons, which can discard
    // intentional runtime max-health changes. Track the latest intended health values
    // and re-apply them immediately when the game rolls them back.
    internal sealed class PlayerHealthOverrideService
        : PlayerRuntimeOverrideServiceBase<PlayerHealthOverrideService.TrackedHealthOverride>
    {
        private readonly System.Func<bool> _verboseLoggingEnabledProvider;
        private static readonly FieldInfo MaximumHealthField = typeof(HealthHaver).GetField(
            "maximumHealth",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CurrentHealthField = typeof(HealthHaver).GetField(
            "currentHealth",
            BindingFlags.Instance | BindingFlags.NonPublic);

        internal sealed class TrackedHealthOverride : PlayerRuntimeOverrideState
        {
            public float DesiredCurrentHealth;
            public float DesiredMaxHealth;
            public HealthHaver HealthHaver;
            public System.Action<Gun, Gun, bool> GunChangedHandler;
            public HealthHaver.OnHealthChangedEvent HealthChangedHandler;
        }

        public PlayerHealthOverrideService(BepInEx.Logging.ManualLogSource logger, System.Func<bool> verboseLoggingEnabledProvider)
            : base(logger)
        {
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
        }

        public void TrackOverride(PlayerController player, HealthHaver healthHaver)
        {
            if ((object)player == null || (object)healthHaver == null)
            {
                return;
            }

            base.TrackOverride(player);
        }

        internal bool TryPreserveMaxHealthDuringGunChange(HealthHaver healthHaver, ref float targetValue, ref float? amountOfHealthToGain)
        {
            TrackedHealthOverride trackedOverride;
            if (!TryGetTrackedOverrideByHealthHaver(healthHaver, out trackedOverride) || trackedOverride == null)
            {
                return false;
            }

            if (targetValue >= trackedOverride.DesiredMaxHealth)
            {
                return false;
            }

            targetValue = trackedOverride.DesiredMaxHealth;
            amountOfHealthToGain = null;
            LogVerbose(
                "Preserved tracked max health during gun change. PlayerId=" +
                (trackedOverride.Player != null ? trackedOverride.Player.GetInstanceID().ToString() : "<null>") +
                ", PreservedMaxHealth=" +
                trackedOverride.DesiredMaxHealth +
                ".");
            return true;
        }

        protected override bool CanTrack(PlayerController player)
        {
            return (object)player != null && (object)player.healthHaver != null;
        }

        protected override TrackedHealthOverride CreateState()
        {
            return new TrackedHealthOverride();
        }

        protected override void CaptureTrackedState(TrackedHealthOverride trackedOverride, PlayerController player)
        {
            HealthHaver healthHaver = player.healthHaver;
            trackedOverride.HealthHaver = healthHaver;
            trackedOverride.DesiredCurrentHealth = healthHaver.GetCurrentHealth();
            trackedOverride.DesiredMaxHealth = healthHaver.GetMaxHealth();
        }

        protected override void AttachHandlers(TrackedHealthOverride trackedOverride, PlayerController player)
        {
            HealthHaver healthHaver = player.healthHaver;
            if ((object)trackedOverride.Player != null && !ReferenceEquals(trackedOverride.Player, player) && trackedOverride.GunChangedHandler != null)
            {
                trackedOverride.Player.GunChanged -= trackedOverride.GunChangedHandler;
            }

            if ((object)trackedOverride.HealthHaver != null && !ReferenceEquals(trackedOverride.HealthHaver, healthHaver) && trackedOverride.HealthChangedHandler != null)
            {
                trackedOverride.HealthHaver.OnHealthChanged -= trackedOverride.HealthChangedHandler;
            }

            trackedOverride.HealthHaver = healthHaver;
            if (trackedOverride.GunChangedHandler == null)
            {
                trackedOverride.GunChangedHandler = delegate(Gun oldGun, Gun newGun, bool arg3)
                {
                    TryRestoreTrackedState(trackedOverride, "gun_changed");
                };
            }

            if (trackedOverride.HealthChangedHandler == null)
            {
                trackedOverride.HealthChangedHandler = delegate(float resultValue, float maxValue)
                {
                    LogVerbose(
                        "Observed tracked health changed callback. PlayerId=" +
                        (trackedOverride.Player != null ? trackedOverride.Player.GetInstanceID().ToString() : "<null>") +
                        ", ResultValue=" +
                        resultValue +
                        ", MaxValue=" +
                        maxValue +
                        ", DesiredCurrentHealth=" +
                        trackedOverride.DesiredCurrentHealth +
                        ", DesiredMaxHealth=" +
                        trackedOverride.DesiredMaxHealth +
                        ", Source=health_changed.");
                    TryRestoreTrackedState(trackedOverride, "health_changed");
                };
            }

            if (!HasGunChangedSubscription(player, trackedOverride.GunChangedHandler))
            {
                player.GunChanged += trackedOverride.GunChangedHandler;
            }

            if (!HasHealthChangedSubscription(healthHaver, trackedOverride.HealthChangedHandler))
            {
                healthHaver.OnHealthChanged += trackedOverride.HealthChangedHandler;
            }
        }

        protected override void DetachHandlers(TrackedHealthOverride trackedOverride)
        {
            if (trackedOverride == null)
            {
                return;
            }

            if ((object)trackedOverride.Player != null && trackedOverride.GunChangedHandler != null)
            {
                trackedOverride.Player.GunChanged -= trackedOverride.GunChangedHandler;
            }

            if ((object)trackedOverride.HealthHaver != null && trackedOverride.HealthChangedHandler != null)
            {
                trackedOverride.HealthHaver.OnHealthChanged -= trackedOverride.HealthChangedHandler;
            }

            trackedOverride.Player = null;
            trackedOverride.HealthHaver = null;
            trackedOverride.IsRestoring = false;
        }

        protected override void TryRestoreTrackedState(TrackedHealthOverride trackedOverride, string source)
        {
            if (trackedOverride == null ||
                trackedOverride.IsRestoring ||
                (object)trackedOverride.Player == null ||
                (object)trackedOverride.HealthHaver == null)
            {
                return;
            }

            HealthHaver healthHaver = trackedOverride.HealthHaver;
            float currentHealth = healthHaver.GetCurrentHealth();
            float maxHealth = healthHaver.GetMaxHealth();
            LogVerbose(
                "Evaluating tracked health override. PlayerId=" +
                trackedOverride.Player.GetInstanceID() +
                ", CurrentHealth=" +
                currentHealth +
                ", CurrentMaxHealth=" +
                maxHealth +
                ", DesiredCurrentHealth=" +
                trackedOverride.DesiredCurrentHealth +
                ", DesiredMaxHealth=" +
                trackedOverride.DesiredMaxHealth +
                ", CurrentGun=" +
                DescribeGun(trackedOverride.Player.CurrentGun) +
                ", Source=" +
                source +
                ".");
            if (maxHealth + 0.001f < trackedOverride.DesiredMaxHealth)
            {
                float desiredCurrentHealth = Mathf.Min(trackedOverride.DesiredCurrentHealth, trackedOverride.DesiredMaxHealth);
                trackedOverride.IsRestoring = true;
                LogVerboseWarning(
                    "Detected unexpected max-health rollback. PlayerId=" +
                    trackedOverride.Player.GetInstanceID() +
                    ", CurrentHealth=" +
                    currentHealth +
                    ", CurrentMaxHealth=" +
                    maxHealth +
                    ", DesiredHealth=" +
                    desiredCurrentHealth +
                    ", DesiredMaxHealth=" +
                    trackedOverride.DesiredMaxHealth +
                    ", CurrentGun=" +
                    DescribeGun(trackedOverride.Player.CurrentGun) +
                    ", Source=" +
                    source +
                    ". Restoring tracked health override.");

                // This is a defensive fallback for runtimes where the earlier gun-change hook did
                // not intercept the stat recalculation. Write the fields without raising another
                // OnHealthChanged event, otherwise the HUD can animate a second time while the
                // service repairs ETG's temporary base-health rollback.
                bool restoredSilently = TryRestoreHealthFieldsSilently(
                    healthHaver,
                    trackedOverride.DesiredMaxHealth,
                    desiredCurrentHealth);
                if (!restoredSilently)
                {
                    healthHaver.SetHealthMaximum(trackedOverride.DesiredMaxHealth, null, false);
                    healthHaver.ForceSetCurrentHealth(desiredCurrentHealth);
                    LogVerboseWarning(
                        "Silent health restore was unavailable; used event-raising fallback. " +
                        "PlayerId=" +
                        trackedOverride.Player.GetInstanceID() +
                        ".");
                }
                trackedOverride.IsRestoring = false;

                currentHealth = healthHaver.GetCurrentHealth();
                maxHealth = healthHaver.GetMaxHealth();
                LogVerbose(
                    "Restored tracked health override. Silent=" +
                    restoredSilently +
                    ", PlayerId=" +
                    trackedOverride.Player.GetInstanceID() +
                    ", CurrentHealth=" +
                    currentHealth +
                    ", CurrentMaxHealth=" +
                    maxHealth +
                    ", CurrentGun=" +
                    DescribeGun(trackedOverride.Player.CurrentGun) +
                    ", Source=" +
                    source +
                    ".");
            }

            trackedOverride.DesiredCurrentHealth = currentHealth;
            if (maxHealth > trackedOverride.DesiredMaxHealth)
            {
                trackedOverride.DesiredMaxHealth = maxHealth;
            }
        }

        private static bool TryRestoreHealthFieldsSilently(HealthHaver healthHaver, float desiredMaxHealth, float desiredCurrentHealth)
        {
            if ((object)healthHaver == null || MaximumHealthField == null || CurrentHealthField == null)
            {
                return false;
            }

            try
            {
                MaximumHealthField.SetValue(healthHaver, desiredMaxHealth);
                CurrentHealthField.SetValue(healthHaver, Mathf.Min(desiredCurrentHealth, healthHaver.GetMaxHealth()));
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static bool HasGunChangedSubscription(PlayerController player, System.Action<Gun, Gun, bool> handler)
        {
            if ((object)player == null || handler == null)
            {
                return false;
            }

            System.Reflection.FieldInfo field = typeof(PlayerController).GetField("GunChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            System.Delegate existingDelegate = field != null ? field.GetValue(player) as System.Delegate : null;
            return existingDelegate != null && System.Array.IndexOf(existingDelegate.GetInvocationList(), handler) >= 0;
        }

        private static bool HasHealthChangedSubscription(HealthHaver healthHaver, HealthHaver.OnHealthChangedEvent handler)
        {
            if ((object)healthHaver == null || handler == null)
            {
                return false;
            }

            System.Reflection.FieldInfo field = typeof(HealthHaver).GetField("OnHealthChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            System.Delegate existingDelegate = field != null ? field.GetValue(healthHaver) as System.Delegate : null;
            return existingDelegate != null && System.Array.IndexOf(existingDelegate.GetInvocationList(), handler) >= 0;
        }

        private bool IsVerboseLoggingEnabled()
        {
            return _verboseLoggingEnabledProvider != null && _verboseLoggingEnabledProvider();
        }

        private void LogVerbose(string message)
        {
            if (!IsVerboseLoggingEnabled())
            {
                return;
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command(message));
        }

        private void LogVerboseWarning(string message)
        {
            if (!IsVerboseLoggingEnabled())
            {
                return;
            }

            Logger.LogWarning(EtgGameplayDashboardLog.Command(message));
        }
    }
}
