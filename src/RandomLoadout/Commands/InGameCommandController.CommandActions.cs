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
        private void LogCommandExecutionResult(ManualLogSource logger, GrantCommandExecutionResult executionResult)
        {
            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                return;
            }

            logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
        }

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
            LogCommandPanelHealthDiagnostic("Executing add armor command. Before=" + DescribePlayerVitals(player) + ".");
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddArmor(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            LogCommandPanelHealthDiagnostic("Finished add armor command. Result=" + executionResult.Succeeded + ", After=" + DescribePlayerVitals(player) + ".");

            if (executionResult.Succeeded)
            {
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
            }
        }

        private void ExecuteToggleArmorNoConsume(PlayerController player, ManualLogSource logger)
        {
            if (_armorNoConsumeToggleService == null)
            {
                string unavailableMessage = GetLocalizedFallback(
                    "result.armor_no_consume.unavailable",
                    "Armor no-consume service is unavailable.",
                    "护甲不消耗服务当前不可用。");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GetLocalizedFallback(
                        "result.armor_no_consume.unavailable",
                        "Armor no-consume service is unavailable.",
                        "护甲不消耗服务当前不可用。")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _armorNoConsumeToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }

            _focusInputField = true;
        }

        private void ExecuteAddMaxHealth(PlayerController player, ManualLogSource logger)
        {
            LogCommandPanelHealthDiagnostic("Executing add max health command. Before=" + DescribePlayerVitals(player) + ".");
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddMaxHealth(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            LogCommandPanelHealthDiagnostic("Finished add max health command. Result=" + executionResult.Succeeded + ", After=" + DescribePlayerVitals(player) + ".");

            if (executionResult.Succeeded)
            {
                MarkRevealMapActivatedForCurrentScene();
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
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

        private void ExecuteToggleBlankNoConsume(PlayerController player, ManualLogSource logger)
        {
            if (_blankNoConsumeToggleService == null)
            {
                string unavailableMessage = GetLocalizedFallback(
                    "result.blank_no_consume.unavailable",
                    "Blank no-consume service is unavailable.",
                    "空响弹不消耗服务当前不可用。");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GetLocalizedFallback(
                        "result.blank_no_consume.unavailable",
                        "Blank no-consume service is unavailable.",
                        "空响弹不消耗服务当前不可用。")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _blankNoConsumeToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }

            _focusInputField = true;
        }

        private void ExecuteAddBlank(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddBlank(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
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
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
            }
        }

        private void ExecuteToggleKeyNoConsume(PlayerController player, ManualLogSource logger)
        {
            if (_keyNoConsumeToggleService == null)
            {
                string unavailableMessage = GetLocalizedFallback(
                    "result.key_no_consume.unavailable",
                    "Key no-consume service is unavailable.",
                    "钥匙不消耗服务当前不可用。");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GetLocalizedFallback(
                        "result.key_no_consume.unavailable",
                        "Key no-consume service is unavailable.",
                        "钥匙不消耗服务当前不可用。")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _keyNoConsumeToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }

            _focusInputField = true;
        }

        private void ExecuteAddRatKey(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddRatKey(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
            }
        }

        private void ExecuteAddCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddCurrency(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
            }
        }

        private void ExecuteAddLargeCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddLargeCurrency(player);
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

        private void ExecuteToggleCurrencyNoConsume(PlayerController player, ManualLogSource logger)
        {
            if (_currencyNoConsumeToggleService == null)
            {
                string unavailableMessage = GetLocalizedFallback(
                    "result.currency_no_consume.unavailable",
                    "Casings no-consume service is unavailable.",
                    "弹壳不消耗服务当前不可用。");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GetLocalizedFallback(
                        "result.currency_no_consume.unavailable",
                        "Casings no-consume service is unavailable.",
                        "弹壳不消耗服务当前不可用。")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _currencyNoConsumeToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }

            _focusInputField = true;
        }

        private void ExecuteAddMetaCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddMetaCurrency(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                LogCommandExecutionResult(logger, executionResult);
                _focusInputField = true;
            }
            else
            {
                LogCommandExecutionResult(logger, executionResult);
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

            if (_ammoModeToggleService != null && _ammoModeToggleService.Mode == AmmoMode.NoConsume)
            {
                string disabledMessage = GetLocalizedFallback(
                    "result.auto_reload.disabled_by_no_consume",
                    "Auto Reload is disabled because ammo is not consumed, so reload will not trigger. To avoid unknown bugs, it stays disabled.",
                    "自动换弹已禁用：因为子弹不消耗，不会触发换弹。为了避免未知 bug，这里保持禁用。");
                ShowStatus(disabledMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command("Auto Reload is disabled because ammo is not consumed, so reload will not trigger. To avoid unknown bugs, it stays disabled."));
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

        private void ExecuteCycleAmmoMode(ManualLogSource logger)
        {
            if (_ammoModeToggleService == null)
            {
                string unavailableMessage = GuiText.Get("result.no_ammo_consumption.unavailable");
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(GuiText.GetEnglish("result.no_ammo_consumption.unavailable")));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _ammoModeToggleService.Toggle();
            if (_ammoModeToggleService.Mode == AmmoMode.NoConsume && _autoReloadToggleService != null)
            {
                // No Consume prevents ammo from decreasing naturally, so the gun never reaches the
                // normal "empty clip then reload" flow. Disable Auto Reload here to avoid bugs
                // caused by combining it with a state that does not naturally consume bullets.
                _autoReloadToggleService.Disable();
            }

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

        private void ExecuteToggleAmmonomiconFastOpen(ManualLogSource logger)
        {
            if (_ammonomiconFastOpenToggleService == null)
            {
                return;
            }

            GrantCommandExecutionResult executionResult = _ammonomiconFastOpenToggleService.Toggle();
            if (_ammonomiconFastOpenEnabledSetter != null)
            {
                _ammonomiconFastOpenEnabledSetter(AmmonomiconFastOpenToggleService.IsFastOpenEnabled);
            }

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

        private void ExecuteRevealCurrentFloorMap(PlayerController player, ManualLogSource logger)
        {
            if (logger != null && ShouldLogMapTeleportVerbose())
            {
                logger.LogInfo(
                    RandomLoadoutLog.Command(
                        "Reveal map button pressed. " +
                        "Scene=" +
                        GetLoadedUnitySceneName() +
                        ", RevealMapActive=" +
                        IsRevealMapActive() +
                        "."));
            }

            GrantCommandExecutionResult executionResult = _roomDebugCommandService.RevealCurrentFloorMap(player, logger);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                MarkRevealMapActivatedForCurrentScene();
                MarkMapDirectTeleportActivatedForCurrentScene();
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
