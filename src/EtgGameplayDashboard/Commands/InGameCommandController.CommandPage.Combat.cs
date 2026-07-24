// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private static readonly ControllerFocusEntry[] CombatCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.combat.rapid", 2, 0),
            new ControllerFocusEntry("cmd.combat.auto_reload", 2, 1),
            new ControllerFocusEntry("cmd.combat.ammo_mode", 3, 0),
            new ControllerFocusEntry("cmd.combat.invincible", 3, 1),
            new ControllerFocusEntry("cmd.combat.ammonomicon", 4, 0),
            new ControllerFocusEntry("cmd.combat.enemy_health_bars", 4, 1),
            new ControllerFocusEntry("cmd.combat.full_ammo", 5, 1),
            new ControllerFocusEntry("cmd.combat.boss_intro", 5, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.ModeControlId, 8, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.MultiplierControlId, 8, 1),
        };

        private static readonly ControllerFocusEntry[] CombatStandardCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.combat.rapid", 2, 0),
            new ControllerFocusEntry("cmd.combat.auto_reload", 2, 1),
            new ControllerFocusEntry("cmd.combat.ammo_mode", 3, 0),
            new ControllerFocusEntry("cmd.combat.invincible", 3, 1),
            new ControllerFocusEntry("cmd.combat.ammonomicon", 4, 0),
            new ControllerFocusEntry("cmd.combat.enemy_health_bars", 4, 1),
            new ControllerFocusEntry("cmd.combat.full_ammo", 5, 1),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.ModeControlId, 8, 0),
            new ControllerFocusEntry(KeyboardAimAssistUiDefinition.MultiplierControlId, 8, 1),
        };

        private void DrawCombatSettings(Rect contentRect, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float settingColumnWidth = 270f;
            const float settingLabelWidth = 154f;
            const float settingButtonWidth = 108f;
            SyncPersistedCombatTargetState(GetSelectedCommandTargetPlayer() ?? player);
            bool experimentalModeEnabled = IsExperimentalModeEnabled();
            float secondSettingColumnX = contentRect.x + settingColumnWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;
            float fourthRowY = thirdRowY + controlHeight + ButtonGap;

            DrawCombatSettingRows(
                new[]
                {
                    CreateRapidFireCombatSetting(
                        new Rect(contentRect.x, firstRowY, settingColumnWidth, controlHeight),
                        player,
                        logger),
                    CreateAutoReloadCombatSetting(
                        new Rect(secondSettingColumnX, firstRowY, settingColumnWidth, controlHeight),
                        logger),
                    CreateAmmoModeCombatSetting(
                        new Rect(contentRect.x, secondRowY, settingColumnWidth, controlHeight),
                        logger),
                    CreateInvincibilityCombatSetting(
                        new Rect(secondSettingColumnX, secondRowY, settingColumnWidth, controlHeight),
                        player,
                        logger),
                    CreateAmmonomiconCombatSetting(
                        new Rect(contentRect.x, thirdRowY, settingColumnWidth, controlHeight),
                        logger),
                    CreateEnemyHealthBarsCombatSetting(
                        new Rect(secondSettingColumnX, thirdRowY, settingColumnWidth, controlHeight),
                        player,
                        logger),
                    CreateBossIntroSkipCombatSetting(
                        new Rect(contentRect.x, fourthRowY, settingColumnWidth, controlHeight),
                        logger,
                        experimentalModeEnabled),
                    CreateControllerAimLockCombatSetting(
                        new Rect(contentRect.x, fourthRowY + controlHeight + ButtonGap, settingColumnWidth, controlHeight),
                        logger),
                },
                settingLabelWidth,
                settingButtonWidth);

            DrawKeyboardAimAssistSetting(
                new Rect(contentRect.x, fourthRowY + (controlHeight + ButtonGap) * 2f, contentRect.width, controlHeight),
                controlHeight,
                settingLabelWidth,
                settingButtonWidth,
                logger);

            if (DrawControllerButton(new Rect(secondSettingColumnX + settingLabelWidth, fourthRowY, settingButtonWidth, controlHeight), "cmd.combat.full_ammo", GuiText.Get("gui.command.button.full_ammo"), _buttonStyle))
            {
                ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteRefillCurrentGunAmmo(targetPlayer, logger); });
            }

        }

        private void SyncPersistedCombatTargetState(PlayerController player)
        {
            if ((object)player == null)
            {
                return;
            }

            if (_ammoModeToggleService != null && _ammoModeToggleService.Mode != AmmoMode.Off)
            {
                _ammoModeTargetStates.Add(player);
            }
            else
            {
                _ammoModeTargetStates.Remove(player);
            }

            bool autoReloadShouldBeVisible = _autoReloadToggleService != null &&
                _autoReloadToggleService.Mode != AutoReloadMode.Off &&
                !(_ammoModeToggleService != null && _ammoModeToggleService.Mode == AmmoMode.NoConsume);
            if (autoReloadShouldBeVisible)
            {
                _autoReloadTargetStates.Add(player);
            }
            else
            {
                _autoReloadTargetStates.Remove(player);
            }
        }

        private void ExecuteCombatCommandPageFocusedControl(PlayerController player)
        {
            TryExecuteCommandPageAction(_commandPageFocusedControlId, player, GetCombatCommandPageActionBindings(player));
        }

        private CommandPageActionBinding[] GetCombatCommandPageActionBindings(PlayerController player)
        {
            if (!IsExperimentalModeEnabled())
            {
                return new[]
                {
                    new CommandPageActionBinding("cmd.combat.rapid", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteToggleRapidFire(targetPlayer, null); }); }),
                    new CommandPageActionBinding("cmd.combat.auto_reload", delegate { ExecuteToggleAutoReload(null); }),
                    new CommandPageActionBinding("cmd.combat.ammo_mode", delegate { ExecuteCycleAmmoMode(null); }),
                    new CommandPageActionBinding("cmd.combat.invincible", delegate { ExecuteToggleInvincibilityForSelectedTargets(player, null); }),
                    new CommandPageActionBinding("cmd.combat.ammonomicon", delegate { ExecuteToggleAmmonomiconFastOpen(null); }),
                new CommandPageActionBinding("cmd.combat.enemy_health_bars", delegate { ExecuteToggleEnemyHealthBars(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding("cmd.combat.controller_aim_lock", delegate { ExecuteToggleControllerAimLock(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding(KeyboardAimAssistUiDefinition.ModeControlId, delegate { ExecuteToggleKeyboardAimAssist(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding(KeyboardAimAssistUiDefinition.MultiplierControlId, delegate { ExecuteCycleKeyboardAimAssistMultiplier(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding("cmd.combat.full_ammo", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteRefillCurrentGunAmmo(targetPlayer, null); }); }),
                };
            }

            return new[]
            {
                new CommandPageActionBinding("cmd.combat.rapid", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteToggleRapidFire(targetPlayer, null); }); }),
                new CommandPageActionBinding("cmd.combat.auto_reload", delegate { ExecuteToggleAutoReload(null); }),
                new CommandPageActionBinding("cmd.combat.ammo_mode", delegate { ExecuteCycleAmmoMode(null); }),
                new CommandPageActionBinding("cmd.combat.invincible", delegate { ExecuteToggleInvincibilityForSelectedTargets(player, null); }),
                new CommandPageActionBinding("cmd.combat.ammonomicon", delegate { ExecuteToggleAmmonomiconFastOpen(null); }),
                new CommandPageActionBinding("cmd.combat.enemy_health_bars", delegate { ExecuteToggleEnemyHealthBars(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding("cmd.combat.controller_aim_lock", delegate { ExecuteToggleControllerAimLock(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding(KeyboardAimAssistUiDefinition.ModeControlId, delegate { ExecuteToggleKeyboardAimAssist(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding(KeyboardAimAssistUiDefinition.MultiplierControlId, delegate { ExecuteCycleKeyboardAimAssistMultiplier(GetSelectedCommandTargetPlayer(), null); }),
                new CommandPageActionBinding("cmd.combat.boss_intro", delegate { ExecuteToggleBossIntroSkip(null); }),
                new CommandPageActionBinding("cmd.combat.full_ammo", delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteRefillCurrentGunAmmo(targetPlayer, null); }); }),
            };
        }

        private CombatSettingRow CreateRapidFireCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            PlayerController targetPlayer = GetSelectedCommandTargetPlayer() ?? player;
            bool isEnabled = IsRapidFireEnabledFor(targetPlayer);
            return new CombatSettingRow(
                rect,
                "cmd.combat.rapid",
                GetLocalizedFallback("gui.command.setting.rapid", "Hold Rapid", "\u6309\u4f4f\u8fde\u53d1"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController selectedPlayer) { ExecuteToggleRapidFire(selectedPlayer, logger); }); });
        }

        private CombatSettingRow CreateAutoReloadCombatSetting(Rect rect, ManualLogSource logger)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.auto_reload",
                GetLocalizedFallback("gui.command.setting.auto_reload", "Auto Reload", "\u81ea\u52a8\u6362\u5f39"),
                GetAutoReloadStatusLabel(GetSelectedCommandTargetPlayer()),
                IsAutoReloadEnabled(GetSelectedCommandTargetPlayer()),
                delegate { ExecuteToggleAutoReload(logger); });
        }

        private CombatSettingRow CreateAmmoModeCombatSetting(Rect rect, ManualLogSource logger)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.ammo_mode",
                GetLocalizedFallback("gui.command.setting.ammo_mode", "Ammo Mode", "\u5f39\u836f\u6a21\u5f0f"),
                GetAmmoModeStatusLabel(GetSelectedCommandTargetPlayer()),
                IsAmmoModeEnabled(GetSelectedCommandTargetPlayer()),
                delegate { ExecuteCycleAmmoMode(logger); });
        }

        private CombatSettingRow CreateInvincibilityCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            PlayerController targetPlayer = GetSelectedCommandTargetPlayer() ?? player;
            bool isEnabled = IsInvincibilityEnabled(targetPlayer);
            return new CombatSettingRow(
                rect,
                "cmd.combat.invincible",
                GetLocalizedFallback("gui.command.setting.invincible", "Invincibility", "\u65e0\u654c"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleInvincibilityForSelectedTargets(player, logger); });
        }

        private CombatSettingRow CreateAmmonomiconCombatSetting(Rect rect, ManualLogSource logger)
        {
            bool isEnabled = IsAmmonomiconFastOpenEnabled();
            return new CombatSettingRow(
                rect,
                "cmd.combat.ammonomicon",
                GetLocalizedFallback("gui.command.setting.ammonomicon", "Ammo Book", "\u56fe\u9274"),
                GetAmmonomiconFastOpenStatusLabel(),
                isEnabled,
                delegate { ExecuteToggleAmmonomiconFastOpen(logger); });
        }

        private CombatSettingRow CreateEnemyHealthBarsCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            bool isEnabled = IsEnemyHealthBarsEnabled();
            return new CombatSettingRow(
                rect,
                "cmd.combat.enemy_health_bars",
                GetLocalizedFallback("gui.command.setting.enemy_health_bars", "Enemy HP Bars", "敌人血条"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleEnemyHealthBars(GetSelectedCommandTargetPlayer(), logger); });
        }

        private CombatSettingRow CreateControllerAimLockCombatSetting(Rect rect, ManualLogSource logger)
        {
            PlayerController targetPlayer = GetSelectedCommandTargetPlayer();
            bool isEnabled = _controllerAimLockService != null && _controllerAimLockService.IsEnabled(targetPlayer);
            return new CombatSettingRow(
                rect,
                "cmd.combat.controller_aim_lock",
                GetLocalizedFallback("gui.command.setting.controller_aim_lock", "Controller Aim Lock", "手柄视角固定"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleControllerAimLock(targetPlayer, logger); });
        }

        private void DrawKeyboardAimAssistSetting(
            Rect rect,
            float controlHeight,
            float labelWidth,
            float buttonWidth,
            ManualLogSource logger)
        {
            PlayerController targetPlayer = GetSelectedCommandTargetPlayer();
            bool isEnabled = _keyboardAimAssistService != null && _keyboardAimAssistService.IsEnabled(targetPlayer);
            GUI.Label(
                new Rect(rect.x, rect.y + 2f, labelWidth, rect.height - 4f),
                GetLocalizedFallback(KeyboardAimAssistUiDefinition.SettingLabelKey, "Keyboard Aim Assist", "键鼠自瞄"),
                _combatSettingLabelStyle);

            float modeButtonWidth = Mathf.Max(buttonWidth, 150f);
            float multiplierButtonWidth = buttonWidth;
            float rightEdge = rect.xMax;
            float multiplierX = rightEdge - multiplierButtonWidth;
            float modeX = multiplierX - ButtonGap - modeButtonWidth;
            GUIStyle modeStyle = isEnabled ? _enabledButtonStyle : _buttonStyle;
            string modeStatus = _keyboardAimAssistService != null
                ? GuiText.Get(KeyboardAimAssistUiDefinition.GetModeStatusKey(_keyboardAimAssistService.Mode))
                : GuiText.Get("gui.command.status.keyboard_aim_assist.off");
            string multiplierStatus = _keyboardAimAssistService != null
                ? GuiText.Get(KeyboardAimAssistUiDefinition.GetMultiplierStatusKey(_keyboardAimAssistService.Multiplier))
                : "1.0x";

            if (DrawControllerButton(
                new Rect(modeX, rect.y, modeButtonWidth, controlHeight),
                KeyboardAimAssistUiDefinition.ModeControlId,
                modeStatus,
                modeStyle))
            {
                ExecuteToggleKeyboardAimAssist(targetPlayer, logger);
            }
            if (DrawControllerButton(
                new Rect(multiplierX, rect.y, multiplierButtonWidth, controlHeight),
                KeyboardAimAssistUiDefinition.MultiplierControlId,
                multiplierStatus,
                isEnabled ? _enabledButtonStyle : _buttonStyle))
            {
                ExecuteCycleKeyboardAimAssistMultiplier(targetPlayer, logger);
            }
        }

        private CombatSettingRow CreateBossIntroSkipCombatSetting(Rect rect, ManualLogSource logger, bool isExperimentalModeEnabled)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.boss_intro",
                GetLocalizedFallback("gui.command.setting.boss_intro_skip", "Skip Boss Intro", "跳过 Boss 开场"),
                GetOnOffStatusLabel(BossIntroSkipHooks.IsEnabled),
                BossIntroSkipHooks.IsEnabled,
                isExperimentalModeEnabled,
                delegate { ExecuteToggleBossIntroSkip(logger); });
        }

        private void DrawCombatSettingRows(CombatSettingRow[] rows, float labelWidth, float buttonWidth)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                DrawCombatSettingRow(rows[index], labelWidth, buttonWidth);
            }
        }

        private string GetCombatCursorColorDisplayName()
        {
            string colorId = _combatCursorColorProvider != null
                ? _combatCursorColorProvider()
                : CombatCursorColorCatalog.DisabledId;
            if (!CombatCursorColorCatalog.IsEnabled(colorId))
            {
                return GuiText.Get("gui.command.status.cursor_color.off");
            }

            return GuiText.Get(CombatCursorColorCatalog.Get(colorId).DisplayNameKey);
        }

        private void DrawCombatSettingRow(CombatSettingRow row, float labelWidth, float buttonWidth)
        {
            GUI.Label(
                new Rect(row.Rect.x, row.Rect.y + 2f, labelWidth, row.Rect.height - 4f),
                row.Label,
                _combatSettingLabelStyle);
            GUIStyle buttonStyle = !row.IsAvailable
                ? _pickupFilterDisabledButtonStyle
                : (row.IsActive ? _enabledButtonStyle : _buttonStyle);
            bool wasPressed = DrawControllerButton(
                new Rect(row.Rect.x + labelWidth, row.Rect.y, buttonWidth, row.Rect.height),
                row.ControlId,
                row.Status,
                buttonStyle);
            if (row.IsAvailable && wasPressed &&
                row.OnClick != null)
            {
                row.OnClick();
            }
        }

        private struct CombatSettingRow
        {
            public CombatSettingRow(Rect rect, string controlId, string label, string status, bool isActive, System.Action onClick)
                : this(rect, controlId, label, status, isActive, true, onClick)
            {
            }

            public CombatSettingRow(Rect rect, string controlId, string label, string status, bool isActive, bool isAvailable, System.Action onClick)
            {
                Rect = rect;
                ControlId = controlId ?? string.Empty;
                Label = label ?? string.Empty;
                Status = status ?? string.Empty;
                IsActive = isActive;
                IsAvailable = isAvailable;
                OnClick = onClick;
            }

            public Rect Rect;

            public string ControlId;

            public string Label;

            public string Status;

            public bool IsActive;

            public bool IsAvailable;

            public System.Action OnClick;
        }
    }
}
