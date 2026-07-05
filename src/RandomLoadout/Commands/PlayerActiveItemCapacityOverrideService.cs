using UnityEngine;

namespace RandomLoadout
{
    internal sealed class PlayerActiveItemCapacityOverrideService
        : PlayerRuntimeOverrideServiceBase<PlayerActiveItemCapacityOverrideService.TrackedActiveItemCapacityOverride>
    {
        private readonly System.Func<bool> _verboseLoggingEnabledProvider;

        internal sealed class TrackedActiveItemCapacityOverride : PlayerRuntimeOverrideState
        {
            public int DesiredCapacity;
            public StatModifier CapacityModifier;
            public int BaseCapacity;
        }

        public PlayerActiveItemCapacityOverrideService(BepInEx.Logging.ManualLogSource logger, System.Func<bool> verboseLoggingEnabledProvider)
            : base(logger)
        {
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
        }

        public void EnsureCapacity(PlayerController player, int minimumCapacity)
        {
            if (!CanTrack(player))
            {
                return;
            }

            base.TrackOverride(player);

            TrackedActiveItemCapacityOverride trackedOverride;
            if (!TryGetTrackedOverride(player, out trackedOverride) || trackedOverride == null)
            {
                return;
            }

            int requiredCapacity = GetRequiredCapacity(player, minimumCapacity);
            if (requiredCapacity > trackedOverride.DesiredCapacity)
            {
                trackedOverride.DesiredCapacity = requiredCapacity;
            }

            TryRestoreTrackedState(trackedOverride, "ensure_capacity");
        }

        protected override bool CanTrack(PlayerController player)
        {
            return (object)player != null && player.activeItems != null;
        }

        protected override TrackedActiveItemCapacityOverride CreateState()
        {
            return new TrackedActiveItemCapacityOverride();
        }

        protected override void CaptureTrackedState(TrackedActiveItemCapacityOverride trackedOverride, PlayerController player)
        {
            int observedCapacity = GetRequiredCapacity(player, player.maxActiveItemsHeld);
            if (observedCapacity > trackedOverride.DesiredCapacity)
            {
                trackedOverride.DesiredCapacity = observedCapacity;
            }

            EnsureCapacityModifierReference(trackedOverride, player);
            trackedOverride.BaseCapacity = GetBaseCapacity(player, trackedOverride);
        }

        protected override void AttachHandlers(TrackedActiveItemCapacityOverride trackedOverride, PlayerController player)
        {
        }

        protected override void DetachHandlers(TrackedActiveItemCapacityOverride trackedOverride)
        {
            if (trackedOverride == null)
            {
                return;
            }

            RemoveCapacityModifier(trackedOverride);
            trackedOverride.Player = null;
            trackedOverride.IsRestoring = false;
            trackedOverride.CapacityModifier = null;
        }

        protected override void TryRestoreTrackedState(TrackedActiveItemCapacityOverride trackedOverride, string source)
        {
            if (trackedOverride == null ||
                trackedOverride.IsRestoring ||
                (object)trackedOverride.Player == null)
            {
                return;
            }

            PlayerController player = trackedOverride.Player;
            PlayerStats stats = player.stats;
            if ((object)stats == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            EnsureCapacityModifierReference(trackedOverride, player);
            int currentCapacity = player.maxActiveItemsHeld;
            int desiredCapacity = GetRequiredCapacity(player, trackedOverride.DesiredCapacity);
            if (currentCapacity > desiredCapacity)
            {
                desiredCapacity = currentCapacity;
            }

            trackedOverride.DesiredCapacity = desiredCapacity;
            if (currentCapacity >= desiredCapacity)
            {
                return;
            }

            trackedOverride.IsRestoring = true;
            LogVerboseWarning(
                RandomLoadoutLog.Command(
                    "Detected active-item capacity rollback. PlayerId=" +
                    player.GetInstanceID() +
                    ", CurrentCapacity=" +
                    currentCapacity +
                    ", DesiredCapacity=" +
                    desiredCapacity +
                    ", ActiveItemCount=" +
                    GetActiveItemCount(player) +
                    ", CurrentGun=" +
                    DescribeGun(player.CurrentGun) +
                    ", Source=" +
                    source +
                    ". Restoring tracked active-item capacity override."));

            ApplyCapacityModifier(trackedOverride, player, desiredCapacity);
            stats.RecalculateStats(player);
            player.UpdateInventoryMaxItems();
            trackedOverride.IsRestoring = false;

            LogVerbose(
                RandomLoadoutLog.Command(
                    "Restored tracked active-item capacity override. PlayerId=" +
                    player.GetInstanceID() +
                    ", CurrentCapacity=" +
                    player.maxActiveItemsHeld +
                    ", ActiveItemCount=" +
                    GetActiveItemCount(player) +
                    ", CurrentGun=" +
                    DescribeGun(player.CurrentGun) +
                    ", Source=" +
                    source +
                    "."));
        }

        private static int GetRequiredCapacity(PlayerController player, int minimumCapacity)
        {
            int activeItemCount = GetActiveItemCount(player);
            if (minimumCapacity < activeItemCount)
            {
                return activeItemCount;
            }

            return minimumCapacity;
        }

        private static int GetActiveItemCount(PlayerController player)
        {
            return (object)player != null && player.activeItems != null
                ? player.activeItems.Count
                : 0;
        }

        private static void EnsureCapacityModifierReference(TrackedActiveItemCapacityOverride trackedOverride, PlayerController player)
        {
            if (trackedOverride == null || (object)player == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            if (trackedOverride.CapacityModifier != null && player.ownerlessStatModifiers.Contains(trackedOverride.CapacityModifier))
            {
                return;
            }

            trackedOverride.CapacityModifier = FindCapacityModifier(player.ownerlessStatModifiers);
        }

        private static StatModifier FindCapacityModifier(System.Collections.Generic.List<StatModifier> modifiers)
        {
            if (modifiers == null)
            {
                return null;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier != null &&
                    modifier.statToBoost == PlayerStats.StatType.AdditionalItemCapacity &&
                    modifier.modifyType == StatModifier.ModifyMethod.ADDITIVE)
                {
                    return modifier;
                }
            }

            return null;
        }

        private static void ApplyCapacityModifier(TrackedActiveItemCapacityOverride trackedOverride, PlayerController player, int desiredCapacity)
        {
            if (trackedOverride == null || (object)player == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            int baseCapacity = GetBaseCapacity(player, trackedOverride);
            trackedOverride.BaseCapacity = baseCapacity;
            int requiredAdditionalCapacity = desiredCapacity - baseCapacity;
            if (requiredAdditionalCapacity <= 0)
            {
                return;
            }

            EnsureCapacityModifierReference(trackedOverride, player);
            if (trackedOverride.CapacityModifier == null)
            {
                trackedOverride.CapacityModifier = StatModifier.Create(
                    PlayerStats.StatType.AdditionalItemCapacity,
                    StatModifier.ModifyMethod.ADDITIVE,
                    requiredAdditionalCapacity);
                trackedOverride.CapacityModifier.ignoredForSaveData = true;
                player.ownerlessStatModifiers.Add(trackedOverride.CapacityModifier);
                return;
            }

            if (trackedOverride.CapacityModifier.amount < requiredAdditionalCapacity)
            {
                trackedOverride.CapacityModifier.amount = requiredAdditionalCapacity;
            }
        }

        private static int GetBaseCapacity(TrackedActiveItemCapacityOverride trackedOverride)
        {
            if (trackedOverride == null || trackedOverride.BaseCapacity <= 0)
            {
                return 0;
            }

            return trackedOverride.BaseCapacity;
        }

        private static int GetBaseCapacity(PlayerController player, TrackedActiveItemCapacityOverride trackedOverride)
        {
            if ((object)player == null)
            {
                return 0;
            }

            int currentCapacity = player.maxActiveItemsHeld;
            StatModifier modifier = trackedOverride != null ? trackedOverride.CapacityModifier : null;
            if (modifier != null)
            {
                currentCapacity -= Mathf.RoundToInt(modifier.amount);
            }

            int trackedBaseCapacity = GetBaseCapacity(trackedOverride);
            if (trackedBaseCapacity > currentCapacity)
            {
                return trackedBaseCapacity;
            }

            return currentCapacity;
        }

        private static void RemoveCapacityModifier(TrackedActiveItemCapacityOverride trackedOverride)
        {
            if (trackedOverride == null ||
                trackedOverride.CapacityModifier == null ||
                (object)trackedOverride.Player == null ||
                trackedOverride.Player.ownerlessStatModifiers == null)
            {
                return;
            }

            trackedOverride.Player.ownerlessStatModifiers.Remove(trackedOverride.CapacityModifier);
            if ((object)trackedOverride.Player.stats != null)
            {
                trackedOverride.Player.stats.RecalculateStats(trackedOverride.Player);
            }

            trackedOverride.Player.UpdateInventoryMaxItems();
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

            Logger.LogInfo(message);
        }

        private void LogVerboseWarning(string message)
        {
            if (!IsVerboseLoggingEnabled())
            {
                return;
            }

            Logger.LogWarning(message);
        }
    }
}
