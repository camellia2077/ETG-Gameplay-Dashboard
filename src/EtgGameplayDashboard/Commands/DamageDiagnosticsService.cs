// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Logging;

namespace EtgGameplayDashboard
{
    internal sealed class DamageDiagnosticsService
    {
        private readonly ManualLogSource _logger;
        private readonly Func<bool> _verboseLoggingEnabledProvider;
        private readonly Func<float> _damageMultiplierProvider;
        private readonly Dictionary<PlayerController, Action<float, bool, HealthHaver>> _handlers =
            new Dictionary<PlayerController, Action<float, bool, HealthHaver>>();

        public DamageDiagnosticsService(
            ManualLogSource logger,
            Func<bool> verboseLoggingEnabledProvider,
            Func<float> damageMultiplierProvider)
        {
            _logger = logger;
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            _damageMultiplierProvider = damageMultiplierProvider;
        }

        public void Update(PlayerController player)
        {
            if (_verboseLoggingEnabledProvider == null || !_verboseLoggingEnabledProvider() || (object)player == null ||
                _handlers.ContainsKey(player))
            {
                return;
            }

            PlayerController capturedPlayer = player;
            Action<float, bool, HealthHaver> handler = delegate(float damageAmount, bool fatal, HealthHaver target)
            {
                LogDamage(capturedPlayer, damageAmount, fatal, target);
            };
            _handlers.Add(player, handler);
            player.OnAnyEnemyReceivedDamage = (Action<float, bool, HealthHaver>)Delegate.Combine(
                player.OnAnyEnemyReceivedDamage,
                handler);
            LogInfo("Damage diagnostics attached. Player=" + player.PlayerIDX + ".");
        }

        public void Reset()
        {
            foreach (KeyValuePair<PlayerController, Action<float, bool, HealthHaver>> pair in _handlers)
            {
                if ((object)pair.Key != null)
                {
                    pair.Key.OnAnyEnemyReceivedDamage = (Action<float, bool, HealthHaver>)Delegate.Remove(
                        pair.Key.OnAnyEnemyReceivedDamage,
                        pair.Value);
                }
            }
            _handlers.Clear();
        }

        private void LogDamage(PlayerController player, float damageAmount, bool fatal, HealthHaver target)
        {
            if (_logger == null || _verboseLoggingEnabledProvider == null || !_verboseLoggingEnabledProvider() ||
                (object)player == null || (object)target == null)
            {
                return;
            }

            Gun gun = player.CurrentGun;
            float playerDamageStat = player.stats != null
                ? player.stats.GetStatValue(PlayerStats.StatType.Damage)
                : 0f;
            float configuredDamageMultiplier = _damageMultiplierProvider != null ? _damageMultiplierProvider() : 1f;
            float projectileBaseDamage = 0f;
            string projectileSource = "none";
            if ((object)gun != null && gun.DefaultModule != null)
            {
                Projectile projectile = gun.DefaultModule.GetCurrentProjectile();
                if ((object)projectile != null)
                {
                    projectileBaseDamage = projectile.baseData.damage;
                    projectileSource = "current_projectile";
                }
            }

            string targetName = target.name;
            if (target.aiActor != null)
            {
                targetName = target.aiActor.GetActorName();
            }

            LogInfo(
                "Damage event: Player=" + player.PlayerIDX +
                ", Damage=" + Format(damageAmount) +
                ", Fatal=" + fatal +
                ", Target='" + targetName + "'" +
                ", TargetHealthAfter=" + Format(target.GetCurrentHealth()) +
                ", TargetMaxHealth=" + Format(target.GetMaxHealth()) +
                ", IsBoss=" + target.IsBoss +
                ", GunId=" + (gun != null ? gun.PickupObjectId.ToString(CultureInfo.InvariantCulture) : "0") +
                ", GunName='" + (gun != null ? gun.gunName : "<none>") + "'" +
                ", ConfiguredDamageMultiplier=" + Format(configuredDamageMultiplier) +
                ", PlayerDamageStat=" + Format(playerDamageStat) +
                ", ProjectileBaseDamage=" + Format(projectileBaseDamage) +
                ", ProjectileSource=" + projectileSource + ".");
        }

        private void LogInfo(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Damage(message));
            }
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
