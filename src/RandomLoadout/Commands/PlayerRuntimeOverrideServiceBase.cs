using System.Collections.Generic;
using BepInEx.Logging;

namespace RandomLoadout
{
    internal abstract class PlayerRuntimeOverrideServiceBase<TOverrideState>
        where TOverrideState : PlayerRuntimeOverrideServiceBase<TOverrideState>.PlayerRuntimeOverrideState
    {
        internal abstract class PlayerRuntimeOverrideState
        {
            public PlayerController Player;
            public bool IsRestoring;
        }

        private readonly Dictionary<int, TOverrideState> _overrides = new Dictionary<int, TOverrideState>();
        protected readonly ManualLogSource Logger;

        protected PlayerRuntimeOverrideServiceBase(ManualLogSource logger)
        {
            Logger = logger;
        }

        public void TrackOverride(PlayerController player)
        {
            if (!CanTrack(player))
            {
                return;
            }

            TOverrideState trackedOverride = GetOrCreateTrackedOverride(player);
            trackedOverride.Player = player;
            AttachHandlers(trackedOverride, player);
            CaptureTrackedState(trackedOverride, player);
        }

        public void Update(PlayerController player)
        {
            if (!CanTrack(player))
            {
                return;
            }

            TOverrideState trackedOverride;
            if (!_overrides.TryGetValue(player.GetInstanceID(), out trackedOverride))
            {
                return;
            }

            trackedOverride.Player = player;
            AttachHandlers(trackedOverride, player);
            TryRestoreTrackedState(trackedOverride, "poll");
        }

        public void Clear(PlayerController player)
        {
            if ((object)player == null)
            {
                return;
            }

            int playerId = player.GetInstanceID();
            TOverrideState trackedOverride;
            if (_overrides.TryGetValue(playerId, out trackedOverride))
            {
                DetachHandlers(trackedOverride);
                _overrides.Remove(playerId);
            }
        }

        public void Reset()
        {
            foreach (KeyValuePair<int, TOverrideState> pair in _overrides)
            {
                DetachHandlers(pair.Value);
            }

            _overrides.Clear();
        }

        protected static string DescribeGun(Gun gun)
        {
            if ((object)gun == null)
            {
                return "<none>";
            }

            return gun.PickupObjectId + ":" + gun.name + "#" + gun.GetInstanceID();
        }

        protected bool TryGetTrackedOverride(PlayerController player, out TOverrideState trackedOverride)
        {
            trackedOverride = null;
            if ((object)player == null)
            {
                return false;
            }

            return _overrides.TryGetValue(player.GetInstanceID(), out trackedOverride);
        }

        protected TOverrideState GetOrCreateTrackedOverride(PlayerController player)
        {
            int playerId = player.GetInstanceID();
            TOverrideState trackedOverride;
            if (!_overrides.TryGetValue(playerId, out trackedOverride))
            {
                trackedOverride = CreateState();
                _overrides.Add(playerId, trackedOverride);
            }

            return trackedOverride;
        }

        protected abstract bool CanTrack(PlayerController player);
        protected abstract TOverrideState CreateState();
        protected abstract void CaptureTrackedState(TOverrideState trackedOverride, PlayerController player);
        protected abstract void AttachHandlers(TOverrideState trackedOverride, PlayerController player);
        protected abstract void DetachHandlers(TOverrideState trackedOverride);
        protected abstract void TryRestoreTrackedState(TOverrideState trackedOverride, string source);
    }
}
