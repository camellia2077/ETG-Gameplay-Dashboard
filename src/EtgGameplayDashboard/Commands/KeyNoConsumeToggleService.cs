// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal sealed class KeyNoConsumeToggleService : NoConsumeToggleServiceBase
    {
        protected override string EnableResultKey
        {
            get { return "result.key_no_consume.enable.success"; }
        }

        protected override string DisableResultKey
        {
            get { return "result.key_no_consume.disable.success"; }
        }

        protected override bool TryPrepareForEnable(PlayerController player, out GrantCommandExecutionResult failureResult)
        {
            if (player == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
                return false;
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if (consumables == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
                return false;
            }

            failureResult = null;
            return true;
        }

        protected override bool TryGetCurrentValue(PlayerController player, out float currentValue)
        {
            currentValue = 0f;
            if (player == null || player.carriedConsumables == null)
            {
                return false;
            }

            currentValue = player.carriedConsumables.KeyBullets;
            return true;
        }

        protected override void SetCurrentValue(PlayerController player, float value)
        {
            if (player != null && player.carriedConsumables != null)
            {
                player.carriedConsumables.KeyBullets = UnityEngine.Mathf.RoundToInt(value);
            }
        }
    }
}
