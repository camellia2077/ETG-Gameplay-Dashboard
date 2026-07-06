// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed class InvincibilityToggleService
    {
        private const string PitImmunityOverrideKey = "RandomLoadout.Invincibility";

        private sealed class PlayerInvincibilityState
        {
            public PlayerInvincibilityState(
                bool originalIsVulnerable,
                bool originalPreventAllDamage,
                bool originalReceivesTouchDamage,
                bool originalImmuneToAllEffects,
                OverridableBool originalImmuneToPits)
            {
                OriginalIsVulnerable = originalIsVulnerable;
                OriginalPreventAllDamage = originalPreventAllDamage;
                OriginalReceivesTouchDamage = originalReceivesTouchDamage;
                OriginalImmuneToAllEffects = originalImmuneToAllEffects;
                OriginalImmuneToPits = originalImmuneToPits;
            }

            public bool OriginalIsVulnerable { get; private set; }

            public bool OriginalPreventAllDamage { get; private set; }

            public bool OriginalReceivesTouchDamage { get; private set; }

            public bool OriginalImmuneToAllEffects { get; private set; }

            public OverridableBool OriginalImmuneToPits { get; private set; }
        }

        private readonly Dictionary<PlayerController, PlayerInvincibilityState> _playerStates =
            new Dictionary<PlayerController, PlayerInvincibilityState>();

        private bool _isEnabled;

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            if (_isEnabled)
            {
                RestoreAll();
                _isEnabled = false;
                return GrantCommandExecutionResult.Localized(true, "result.invincible.disable.success");
            }

            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.invincible.no_player");
            }

            HealthHaver healthHaver;
            if (!TryGetHealthHaver(player, out healthHaver) || (object)healthHaver == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.invincible.no_health");
            }

            _isEnabled = true;
            ApplyToPlayer(player);
            return GrantCommandExecutionResult.Localized(true, "result.invincible.enable.success");
        }

        public void Update(PlayerController player)
        {
            if (!_isEnabled || (object)player == null)
            {
                return;
            }

            ApplyToPlayer(player);
        }

        public void Reset()
        {
            RestoreAll();
            _isEnabled = false;
        }

        private void ApplyToPlayer(PlayerController player)
        {
            HealthHaver healthHaver;
            if (!IsPlayerUsable(player) || !TryGetHealthHaver(player, out healthHaver) || (object)healthHaver == null)
            {
                return;
            }

            if (!_playerStates.ContainsKey(player))
            {
                _playerStates.Add(
                    player,
                    new PlayerInvincibilityState(
                        healthHaver.IsVulnerable,
                        healthHaver.PreventAllDamage,
                        player.ReceivesTouchDamage,
                        player.ImmuneToAllEffects,
                        player.ImmuneToPits));
            }

            healthHaver.IsVulnerable = false;
            healthHaver.PreventAllDamage = true;
            player.ReceivesTouchDamage = false;
            player.ImmuneToAllEffects = true;
            ApplyPitImmunity(player);
        }

        private void RestoreAll()
        {
            foreach (KeyValuePair<PlayerController, PlayerInvincibilityState> pair in _playerStates)
            {
                RestorePlayer(pair.Key, pair.Value);
            }

            _playerStates.Clear();
        }

        private void RestorePlayer(PlayerController player, PlayerInvincibilityState state)
        {
            // ETG scene transitions can leave tracked player references pointing at destroyed or
            // partially torn-down Unity objects. Restoring best-effort state is safer than letting
            // one stale player block invincibility from being disabled for the current run.
            if (!IsPlayerUsable(player) || state == null)
            {
                return;
            }

            HealthHaver healthHaver;
            if (TryGetHealthHaver(player, out healthHaver) && (object)healthHaver != null)
            {
                healthHaver.IsVulnerable = state.OriginalIsVulnerable;
                healthHaver.PreventAllDamage = state.OriginalPreventAllDamage;
            }

            player.ReceivesTouchDamage = state.OriginalReceivesTouchDamage;
            player.ImmuneToAllEffects = state.OriginalImmuneToAllEffects;
            if ((object)state.OriginalImmuneToPits == null)
            {
                player.ImmuneToPits = null;
                return;
            }

            state.OriginalImmuneToPits.RemoveOverride(PitImmunityOverrideKey);
            player.ImmuneToPits = state.OriginalImmuneToPits;
        }

        private static void ApplyPitImmunity(PlayerController player)
        {
            if ((object)player.ImmuneToPits == null)
            {
                player.ImmuneToPits = new OverridableBool(false);
            }

            player.ImmuneToPits.SetOverride(PitImmunityOverrideKey, true, null);
        }

        private static bool TryGetHealthHaver(PlayerController player, out HealthHaver healthHaver)
        {
            healthHaver = null;
            if (!IsPlayerUsable(player))
            {
                return false;
            }

            try
            {
                healthHaver = player.healthHaver;
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static bool IsPlayerUsable(PlayerController player)
        {
            return IsUnityObjectAlive(player) && IsUnityObjectAlive(player.gameObject);
        }

        private static bool IsUnityObjectAlive(UnityEngine.Object unityObject)
        {
            return (object)unityObject != null && unityObject != null;
        }
    }
}
