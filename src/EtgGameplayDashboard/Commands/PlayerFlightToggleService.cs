// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal sealed class PlayerFlightToggleService
    {
        private const string FlightOverrideKey = "EtgGameplayDashboard.Flight";
        private const string CleanupOverrideKey = "EtgGameplayDashboard.FlightCleanup";

        private readonly HashSet<PlayerController> _enabledPlayers = new HashSet<PlayerController>();
        private readonly PersistedPlayerToggleState _persistedState;

        public PlayerFlightToggleService(bool initiallyEnabled, System.Action<bool> persistEnabledState)
        {
            _persistedState = new PersistedPlayerToggleState(initiallyEnabled, persistEnabledState);
        }

        public bool IsEnabledFor(PlayerController player)
        {
            return IsPlayerUsable(player) && _enabledPlayers.Contains(player);
        }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            if (!IsPlayerUsable(player))
            {
                return GrantCommandExecutionResult.Localized(false, "result.flight.no_player");
            }

            if (IsEnabledFor(player))
            {
                Disable(player);
                _enabledPlayers.Remove(player);
                _persistedState.Set(_enabledPlayers.Count > 0);
                return GrantCommandExecutionResult.Localized(true, "result.flight.disable.success");
            }

            player.SetIsFlying(true, FlightOverrideKey, true, false);
            _enabledPlayers.Add(player);
            _persistedState.Set(true);
            return GrantCommandExecutionResult.Localized(true, "result.flight.enable.success");
        }

        public void Update(PlayerController player)
        {
            if (_persistedState.IsEnabled && IsPlayerUsable(player) && !_enabledPlayers.Contains(player))
            {
                _enabledPlayers.Add(player);
            }

            if (IsEnabledFor(player))
            {
                player.SetIsFlying(true, FlightOverrideKey, true, false);
            }
        }

        public void Reset()
        {
            foreach (PlayerController player in new List<PlayerController>(_enabledPlayers))
            {
                Disable(player);
            }

            _enabledPlayers.Clear();
        }

        private static void Disable(PlayerController player)
        {
            OverridableBool flying = PrivateFieldAccessor.GetPrivateObject<OverridableBool>(player, "m_isFlying");
            if (flying == null)
            {
                return;
            }

            flying.RemoveOverride(FlightOverrideKey);
            if (!IsPlayerUsable(player) || player.IsFlying)
            {
                return;
            }

            // Re-run ETG's transition side effects only when removing our override leaves the
            // player grounded. The temporary override is removed immediately afterward.
            player.SetIsFlying(false, CleanupOverrideKey, true, false);
            flying.RemoveOverride(CleanupOverrideKey);
        }

        private static bool IsPlayerUsable(PlayerController player)
        {
            return (object)player != null && player != null &&
                (object)player.gameObject != null && player.gameObject != null;
        }
    }

    /// <summary>
    /// Keeps a persisted default separate from the per-player runtime state owned by a toggle service.
    /// </summary>
    internal sealed class PersistedPlayerToggleState
    {
        private readonly System.Action<bool> _persist;

        public PersistedPlayerToggleState(bool initiallyEnabled, System.Action<bool> persist)
        {
            IsEnabled = initiallyEnabled;
            _persist = persist;
        }

        public bool IsEnabled { get; private set; }

        public void Set(bool enabled)
        {
            if (IsEnabled == enabled)
            {
                return;
            }

            IsEnabled = enabled;
            if (_persist != null)
            {
                _persist(enabled);
            }
        }
    }
}
