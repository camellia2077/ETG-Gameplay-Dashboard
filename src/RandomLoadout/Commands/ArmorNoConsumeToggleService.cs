// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace RandomLoadout
{
    internal sealed class ArmorNoConsumeToggleService : NoConsumeToggleServiceBase
    {
        protected override string EnableResultKey
        {
            get { return "result.armor_no_consume.enable.success"; }
        }

        protected override string DisableResultKey
        {
            get { return "result.armor_no_consume.disable.success"; }
        }

        protected override bool TryPrepareForEnable(PlayerController player, out GrantCommandExecutionResult failureResult)
        {
            if (player == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
                return false;
            }

            HealthHaver healthHaver = player.healthHaver;
            if (healthHaver == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.health_not_ready");
                return false;
            }

            failureResult = null;
            return true;
        }

        protected override void OnEnabled(PlayerController player)
        {
            HealthHaver healthHaver = player.healthHaver;
            // Armor no-consume only has something to restore after damage if the player currently has
            // at least one armor point. When armor is still at zero, taking damage cannot enter the
            // normal "lose armor, then restore it" path, so we seed one armor up front as the baseline.
            if (healthHaver != null && healthHaver.Armor <= 0f)
            {
                healthHaver.Armor = 1f;
            }
        }

        protected override bool TryGetCurrentValue(PlayerController player, out float currentValue)
        {
            currentValue = 0f;
            if (player == null || player.healthHaver == null)
            {
                return false;
            }

            currentValue = player.healthHaver.Armor;
            return true;
        }

        protected override void SetCurrentValue(PlayerController player, float value)
        {
            if (player != null && player.healthHaver != null)
            {
                player.healthHaver.Armor = value;
            }
        }
    }
}
