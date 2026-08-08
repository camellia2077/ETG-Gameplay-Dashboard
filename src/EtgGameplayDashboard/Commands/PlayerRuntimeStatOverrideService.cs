// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal sealed class PlayerRuntimeStatOverrideService
    {
        private readonly Dictionary<PlayerController, PlayerModifierState> _states = new Dictionary<PlayerController, PlayerModifierState>();

        public int GetDamageMultiplier(PlayerController player)
        {
            return GetMultiplierTarget(player, true);
        }

        public float DamageMultiplier { get { return GetFirstMultiplierTarget(true); } }
        public float MovementMultiplier { get { return GetFirstMultiplierTarget(false); } }

        public int GetMovementMultiplier(PlayerController player)
        {
            return GetMultiplierTarget(player, false);
        }

        public void SetDamageMultiplier(PlayerController player, int value)
        {
            SetMultiplierTarget(player, value, true);
        }

        public void SetMovementMultiplier(PlayerController player, int value)
        {
            SetMultiplierTarget(player, value, false);
        }

        public void SetCoolnessValue(PlayerController player, int value)
        {
            SetTargetValue(player, value, true);
        }

        public void SetCurseValue(PlayerController player, int value)
        {
            SetTargetValue(player, value, false);
        }

        public static void SetMagnificenceValue(PlayerController player, int value)
        {
            if ((object)player == null || (object)player.stats == null)
            {
                return;
            }

            player.stats.AddFloorMagnificence(value - player.stats.Magnificence);
        }

        public void Update(PlayerController player)
        {
            if ((object)player == null || (object)player.stats == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            PlayerModifierState state = GetOrCreateState(player);
            bool changed = ApplyModifier(player, ref state.DamageModifier, PlayerStats.StatType.Damage, state.DamageTarget ?? 1f);
            changed = ApplyModifier(player, ref state.MovementModifier, PlayerStats.StatType.MovementSpeed, state.MovementTarget ?? 1f) || changed;
            changed = ApplyTargetModifier(player, ref state.CoolnessModifier, PlayerStats.StatType.Coolness, state.CoolnessTarget) || changed;
            changed = ApplyTargetModifier(player, ref state.CurseModifier, PlayerStats.StatType.Curse, state.CurseTarget) || changed;
            if (changed)
            {
                player.stats.RecalculateStats(player);
            }
        }

        public void Reset(bool recalculateStats)
        {
            foreach (KeyValuePair<PlayerController, PlayerModifierState> pair in _states)
            {
                if (!recalculateStats)
                {
                    continue;
                }

                RemoveModifier(pair.Key, pair.Value.DamageModifier);
                RemoveModifier(pair.Key, pair.Value.MovementModifier);
                RemoveModifier(pair.Key, pair.Value.CoolnessModifier);
                RemoveModifier(pair.Key, pair.Value.CurseModifier);
                if ((object)pair.Key != null && (object)pair.Key.stats != null)
                {
                    pair.Key.stats.RecalculateStats(pair.Key);
                }
            }

            _states.Clear();
        }

        private PlayerModifierState GetOrCreateState(PlayerController player)
        {
            PlayerModifierState state;
            if (!_states.TryGetValue(player, out state))
            {
                state = new PlayerModifierState();
                _states.Add(player, state);
            }

            return state;
        }

        private int GetMultiplierTarget(PlayerController player, bool damage)
        {
            PlayerModifierState state;
            if ((object)player != null && _states.TryGetValue(player, out state))
            {
                int? target = damage ? state.DamageTarget : state.MovementTarget;
                return target.HasValue ? target.Value : 1;
            }

            return 1;
        }

        private float GetFirstMultiplierTarget(bool damage)
        {
            foreach (KeyValuePair<PlayerController, PlayerModifierState> pair in _states)
            {
                int? target = damage ? pair.Value.DamageTarget : pair.Value.MovementTarget;
                return target.HasValue ? target.Value : 1f;
            }

            return 1f;
        }

        private void SetMultiplierTarget(PlayerController player, int value, bool damage)
        {
            if ((object)player == null)
            {
                return;
            }

            PlayerModifierState state = GetOrCreateState(player);
            if (damage)
            {
                state.DamageTarget = value;
            }
            else
            {
                state.MovementTarget = value;
            }

            Update(player);
        }

        private void SetTargetValue(PlayerController player, int value, bool isCoolness)
        {
            if ((object)player == null)
            {
                return;
            }

            PlayerModifierState state = GetOrCreateState(player);
            if (isCoolness)
            {
                state.CoolnessTarget = value;
            }
            else
            {
                state.CurseTarget = value;
            }

            Update(player);
        }

        private static bool ApplyModifier(PlayerController player, ref StatModifier modifier, PlayerStats.StatType statType, float multiplier)
        {
            if (multiplier <= 0f)
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

            bool changed = modifier.amount != multiplier || modifier.modifyType != StatModifier.ModifyMethod.MULTIPLICATIVE || modifier.statToBoost != statType;
            modifier.amount = multiplier;
            modifier.modifyType = StatModifier.ModifyMethod.MULTIPLICATIVE;
            modifier.statToBoost = statType;
            if (!player.ownerlessStatModifiers.Contains(modifier))
            {
                player.ownerlessStatModifiers.Add(modifier);
                changed = true;
            }

            return changed;
        }

        private static bool ApplyTargetModifier(PlayerController player, ref StatModifier modifier, PlayerStats.StatType statType, int? targetValue)
        {
            if (!targetValue.HasValue)
            {
                bool hadModifier = modifier != null;
                RemoveModifier(player, modifier);
                modifier = null;
                return hadModifier;
            }

            if (modifier == null)
            {
                modifier = StatModifier.Create(statType, StatModifier.ModifyMethod.ADDITIVE, 0f);
                modifier.ignoredForSaveData = true;
                player.ownerlessStatModifiers.Add(modifier);
            }

            float valueWithoutThisModifier = player.stats.GetStatValue(statType) - modifier.amount;
            float desiredAmount = targetValue.Value - valueWithoutThisModifier;
            bool changed = modifier.amount != desiredAmount || modifier.modifyType != StatModifier.ModifyMethod.ADDITIVE || modifier.statToBoost != statType;
            modifier.amount = desiredAmount;
            modifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
            modifier.statToBoost = statType;
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
            public StatModifier CoolnessModifier;
            public StatModifier CurseModifier;
            public int? DamageTarget;
            public int? MovementTarget;
            public int? CoolnessTarget;
            public int? CurseTarget;
        }
    }
}
