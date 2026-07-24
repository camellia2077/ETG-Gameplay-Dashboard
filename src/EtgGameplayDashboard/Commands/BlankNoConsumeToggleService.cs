// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal sealed class BlankNoConsumeToggleService : NoConsumeToggleServiceBase
    {
        protected override string EnableResultKey
        {
            get { return "result.blank_no_consume.enable.success"; }
        }

        protected override string DisableResultKey
        {
            get { return "result.blank_no_consume.disable.success"; }
        }

        protected override bool TryPrepareForEnable(PlayerController player, out GrantCommandExecutionResult failureResult)
        {
            if (player == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
                return false;
            }

            failureResult = null;
            return true;
        }

        protected override bool TryGetCurrentValue(PlayerController player, out float currentValue)
        {
            currentValue = 0f;
            if (player == null)
            {
                return false;
            }

            currentValue = player.Blanks;
            return true;
        }

        protected override void SetCurrentValue(PlayerController player, float value)
        {
            if (player != null)
            {
                player.Blanks = UnityEngine.Mathf.RoundToInt(value);
            }
        }
    }
}
