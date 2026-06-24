namespace RandomLoadout
{
    internal sealed class PlayerActiveItemCapacityOverrideService
        : PlayerRuntimeOverrideServiceBase<PlayerActiveItemCapacityOverrideService.TrackedActiveItemCapacityOverride>
    {
        internal sealed class TrackedActiveItemCapacityOverride : PlayerRuntimeOverrideState
        {
            public int DesiredCapacity;
        }

        public PlayerActiveItemCapacityOverrideService(BepInEx.Logging.ManualLogSource logger)
            : base(logger)
        {
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

            trackedOverride.Player = null;
            trackedOverride.IsRestoring = false;
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
            Logger.LogWarning(
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

            player.maxActiveItemsHeld = desiredCapacity;
            player.UpdateInventoryMaxItems();
            trackedOverride.IsRestoring = false;

            Logger.LogInfo(
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
    }
}
