// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class BossRushService
    {
        private static void LogInfoStatic(PlayerController player, string message)
        {
            if ((object)player != null)
            {
                Debug.Log("[EtgGameplayDashboard][BossRush] " + message + " Player=" + player.name + ".");
            }
        }

        private static string GetStateToken(BossRushState state)
        {
            switch (state)
            {
                case BossRushState.Starting:
                    return "starting";
                case BossRushState.LoadingFloor:
                    return "loading_floor";
                case BossRushState.TeleportingToBoss:
                    return "teleporting_to_boss";
                case BossRushState.InEncounter:
                    return "in_encounter";
                case BossRushState.AwaitingRewardClaim:
                    return "awaiting_reward_claim";
                case BossRushState.Transitioning:
                    return "transitioning";
                case BossRushState.ReturningToCharacterSelect:
                    return "returning_to_character_select";
                default:
                    return "idle";
            }
        }

        private void RaiseStatus(GrantCommandExecutionResult result)
        {
            if (result == null)
            {
                return;
            }

            if (StatusRaised != null)
            {
                StatusRaised(result);
            }

            if (_logger != null)
            {
                if (result.Succeeded)
                {
                    _logger.LogInfo(EtgGameplayDashboardLog.BossRush(result.LogMessage));
                }
                else
                {
                    _logger.LogWarning(EtgGameplayDashboardLog.BossRush(result.LogMessage));
                }
            }
        }

        private void LogInfo(string message)
        {
            if (_verboseLoggingEnabledProvider != null && !_verboseLoggingEnabledProvider())
            {
                return;
            }

            if (_logger != null)
            {
                _logger.LogInfo(EtgGameplayDashboardLog.BossRush(message));
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning(EtgGameplayDashboardLog.BossRush(message));
            }
        }
    }
}
