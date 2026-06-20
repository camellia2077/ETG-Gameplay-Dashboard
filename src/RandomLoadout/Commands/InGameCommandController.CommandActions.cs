using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void Submit(PlayerController player, ManualLogSource logger)
        {
            GrantCommandParseResult parseResult = _parser.Parse(_inputText);
            if (!parseResult.Succeeded)
            {
                ShowStatus(GetParseErrorMessage(parseResult), true);
                logger.LogWarning(RandomLoadoutLog.Command(parseResult.ErrorMessage));
                return;
            }

            GrantCommandExecutionResult executionResult = _commandService.Execute(player, parseResult.Request);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _inputText = string.Empty;
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteRandom(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _commandService.ExecuteRandom(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteHealHalfHeart(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.HealHalfHeart(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteAddArmor(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddArmor(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteFullHeal(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.FullHeal(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteClearCurse(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.ClearCurse(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteRefillBlanks(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.RefillBlanks(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteRefillCurrentGunAmmo(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.RefillCurrentGunAmmo(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteAddKey(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddKey(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteAddCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddCurrency(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteAddMetaCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddMetaCurrency(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteToggleRapidFire(PlayerController player, ManualLogSource logger)
        {
            if (_rapidFireToggleService == null)
            {
                string unavailableMessage = GuiText.Get("result.rapid.unavailable");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GuiText.GetEnglish("result.rapid.unavailable")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _rapidFireToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteToggleAutoReload(ManualLogSource logger)
        {
            if (_autoReloadToggleService == null)
            {
                string unavailableMessage = GuiText.Get("result.auto_reload.unavailable");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GuiText.GetEnglish("result.auto_reload.unavailable")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _autoReloadToggleService.Toggle();
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteToggleInvincibility(PlayerController player, ManualLogSource logger)
        {
            if (_invincibilityToggleService == null)
            {
                string unavailableMessage = GuiText.Get("result.invincible.unavailable");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GuiText.GetEnglish("result.invincible.unavailable")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _invincibilityToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteToggleNoAmmoConsumption(ManualLogSource logger)
        {
            if (_noAmmoConsumptionToggleService == null)
            {
                string unavailableMessage = GuiText.Get("result.no_ammo_consumption.unavailable");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GuiText.GetEnglish("result.no_ammo_consumption.unavailable")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _noAmmoConsumptionToggleService.Toggle();
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteToggleAmmonomiconOpenAnimation(ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = AmmonomiconAnimationToggleService.Toggle();
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private static string GetParseErrorMessage(GrantCommandParseResult parseResult)
        {
            if (parseResult == null)
            {
                return GuiText.Get("result.error.resolve_failed");
            }

            switch (parseResult.ErrorCode)
            {
                case "InputRequired":
                    return GuiText.Get("parse.input_required");
                case "TargetValueRequired":
                    return GuiText.Get("parse.target_value_required");
                default:
                    return parseResult.ErrorMessage;
            }
        }
    }
}
