// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal sealed class ProjectileModifierService
    {
        private const float ProjectileScaleLimit = 30f;
        private readonly float _originalProjectileScaleLimit;
        private readonly Dictionary<PlayerController, ProjectileModifierState> _states = new Dictionary<PlayerController, ProjectileModifierState>();

        public ProjectileModifierService()
        {
            _originalProjectileScaleLimit = Projectile.s_maxProjectileScale;
            Projectile.s_maxProjectileScale = ProjectileScaleLimit;
        }

        public int GetBulletSizeMultiplier(PlayerController player)
        {
            return GetStateValue(player, true);
        }

        public int GetBulletSpeedMultiplier(PlayerController player)
        {
            return GetStateValue(player, false);
        }

        public float GetReloadSpeedMultiplier(PlayerController player)
        {
            ProjectileModifierState state;
            if ((object)player != null && _states.TryGetValue(player, out state))
            {
                return state.ReloadSpeedTarget.HasValue ? state.ReloadSpeedTarget.Value : 1f;
            }

            return 1f;
        }

        public void SetBulletSizeMultiplier(PlayerController player, int value)
        {
            SetBulletValue(player, value, true);
        }

        public void SetBulletSpeedMultiplier(PlayerController player, int value)
        {
            SetBulletValue(player, value, false);
        }

        public void SetReloadSpeedMultiplier(PlayerController player, float value)
        {
            if ((object)player == null)
            {
                return;
            }

            ProjectileModifierState state = GetOrCreateState(player);
            state.ReloadSpeedTarget = value;
            Update(player);
        }

        public void SetAccuracyValue(PlayerController player, int value)
        {
            if ((object)player == null)
            {
                return;
            }

            ProjectileModifierState state = GetOrCreateState(player);
            state.AccuracyTarget = value;
            Update(player);
        }

        public void Update(PlayerController player)
        {
            if ((object)player == null || (object)player.stats == null || player.ownerlessStatModifiers == null)
            {
                return;
            }

            ProjectileModifierState state = GetOrCreateState(player);
            bool changed = ApplyModifier(player, ref state.BulletSizeModifier, PlayerStats.StatType.PlayerBulletScale, state.BulletSizeTarget ?? 1f);
            changed = ApplyModifier(player, ref state.BulletSpeedModifier, PlayerStats.StatType.ProjectileSpeed, state.BulletSpeedTarget ?? 1f) || changed;
            changed = ApplyModifier(player, ref state.ReloadSpeedModifier, PlayerStats.StatType.ReloadSpeed, 1f / (state.ReloadSpeedTarget ?? 1f)) || changed;
            changed = ApplyTargetModifier(player, ref state.AccuracyModifier, PlayerStats.StatType.Accuracy, state.AccuracyTarget) || changed;
            if (changed)
            {
                player.stats.RecalculateStats(player);
            }
        }

        public void Reset(bool recalculateStats)
        {
            foreach (KeyValuePair<PlayerController, ProjectileModifierState> pair in _states)
            {
                if (!recalculateStats)
                {
                    continue;
                }

                RemoveModifier(pair.Key, pair.Value.BulletSizeModifier);
                RemoveModifier(pair.Key, pair.Value.BulletSpeedModifier);
                RemoveModifier(pair.Key, pair.Value.ReloadSpeedModifier);
                RemoveModifier(pair.Key, pair.Value.AccuracyModifier);
                if ((object)pair.Key != null && (object)pair.Key.stats != null)
                {
                    pair.Key.stats.RecalculateStats(pair.Key);
                }
            }

            _states.Clear();
            Projectile.s_maxProjectileScale = _originalProjectileScaleLimit;
        }

        private int GetStateValue(PlayerController player, bool size)
        {
            ProjectileModifierState state;
            if ((object)player != null && _states.TryGetValue(player, out state))
            {
                int? target = size ? state.BulletSizeTarget : state.BulletSpeedTarget;
                return target.HasValue ? target.Value : 1;
            }

            return 1;
        }

        private void SetBulletValue(PlayerController player, int value, bool size)
        {
            if ((object)player == null)
            {
                return;
            }

            ProjectileModifierState state = GetOrCreateState(player);
            if (size)
            {
                state.BulletSizeTarget = value;
            }
            else
            {
                state.BulletSpeedTarget = value;
            }

            Update(player);
        }

        private ProjectileModifierState GetOrCreateState(PlayerController player)
        {
            ProjectileModifierState state;
            if (!_states.TryGetValue(player, out state))
            {
                state = new ProjectileModifierState();
                _states.Add(player, state);
            }

            return state;
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

        private sealed class ProjectileModifierState
        {
            public StatModifier BulletSizeModifier;
            public StatModifier BulletSpeedModifier;
            public StatModifier ReloadSpeedModifier;
            public StatModifier AccuracyModifier;
            public int? BulletSizeTarget;
            public int? BulletSpeedTarget;
            public float? ReloadSpeedTarget;
            public int? AccuracyTarget;
        }
    }
}
