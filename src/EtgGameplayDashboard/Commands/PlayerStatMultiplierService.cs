// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal sealed class PlayerStatMultiplierService
    {
        private static readonly float[] DamageMultipliers = { 1f, 2f, 5f, 10f, 25f, 50f, 100f };
        private static readonly float[] MovementMultipliers = { 1f, 1.5f, 2f, 3f };
        private readonly Dictionary<PlayerController, PlayerModifierState> _states = new Dictionary<PlayerController, PlayerModifierState>();
        private int _damageMultiplierIndex;
        private int _movementMultiplierIndex;

        public float DamageMultiplier { get { return DamageMultipliers[_damageMultiplierIndex]; } }
        public float MovementMultiplier { get { return MovementMultipliers[_movementMultiplierIndex]; } }

        public float CycleDamageMultiplier(PlayerController player)
        {
            _damageMultiplierIndex = (_damageMultiplierIndex + 1) % DamageMultipliers.Length;
            Update(player);
            return DamageMultiplier;
        }

        public float CycleMovementMultiplier(PlayerController player)
        {
            _movementMultiplierIndex = (_movementMultiplierIndex + 1) % MovementMultipliers.Length;
            Update(player);
            return MovementMultiplier;
        }

        public void Update(PlayerController player)
        {
            if ((object)player == null || (object)player.stats == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            PlayerModifierState state;
            if (!_states.TryGetValue(player, out state))
            {
                state = new PlayerModifierState();
                _states.Add(player, state);
            }

            bool changed = ApplyModifier(player, ref state.DamageModifier, PlayerStats.StatType.Damage, DamageMultiplier);
            changed = ApplyModifier(player, ref state.MovementModifier, PlayerStats.StatType.MovementSpeed, MovementMultiplier) || changed;
            if (changed)
            {
                player.stats.RecalculateStats(player);
            }
        }

        public void Reset()
        {
            foreach (KeyValuePair<PlayerController, PlayerModifierState> pair in _states)
            {
                RemoveModifier(pair.Key, pair.Value.DamageModifier);
                RemoveModifier(pair.Key, pair.Value.MovementModifier);
                if ((object)pair.Key != null && (object)pair.Key.stats != null)
                {
                    pair.Key.stats.RecalculateStats(pair.Key);
                }
            }

            _states.Clear();
            _damageMultiplierIndex = 0;
            _movementMultiplierIndex = 0;
        }

        private static bool ApplyModifier(PlayerController player, ref StatModifier modifier, PlayerStats.StatType statType, float multiplier)
        {
            if (multiplier <= 1f)
            {
                bool hadModifier = modifier != null;
                RemoveModifier(player, modifier);
                modifier = null;
                return hadModifier;
            }

            if (modifier == null)
            {
                modifier = StatModifier.Create(statType, StatModifier.ModifyMethod.MULTIPLICATIVE, multiplier);
                modifier.ignoredForSaveData = true;
                player.ownerlessStatModifiers.Add(modifier);
                return true;
            }

            bool changed = modifier.amount != multiplier;
            modifier.amount = multiplier;
            if (!player.ownerlessStatModifiers.Contains(modifier))
            {
                player.ownerlessStatModifiers.Add(modifier);
                changed = true;
            }

            return changed;
        }

        private static void RemoveModifier(PlayerController player, StatModifier modifier)
        {
            if ((object)player == null || modifier == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            player.ownerlessStatModifiers.Remove(modifier);
        }

        private sealed class PlayerModifierState
        {
            public StatModifier DamageModifier;
            public StatModifier MovementModifier;
        }
    }
}
