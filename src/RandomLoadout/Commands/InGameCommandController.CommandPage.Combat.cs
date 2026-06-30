using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
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
            new ControllerFocusEntry("cmd.combat.full_ammo", 4, 1),
        };

        private void DrawCombatSettings(Rect contentRect, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float settingColumnWidth = 270f;
            const float settingLabelWidth = 146f;
            const float settingButtonWidth = 116f;
            float secondSettingColumnX = contentRect.x + settingColumnWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

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
                },
                settingLabelWidth,
                settingButtonWidth);

            if (DrawControllerButton(new Rect(secondSettingColumnX + settingLabelWidth, thirdRowY, settingButtonWidth, controlHeight), "cmd.combat.full_ammo", GuiText.Get("gui.command.button.full_ammo"), _buttonStyle))
            {
                ExecuteRefillCurrentGunAmmo(player, logger);
            }
        }

        private void ExecuteCombatCommandPageFocusedControl(PlayerController player)
        {
            TryExecuteCommandPageAction(_commandPageFocusedControlId, player, GetCombatCommandPageActionBindings(player));
        }

        private CommandPageActionBinding[] GetCombatCommandPageActionBindings(PlayerController player)
        {
            return new[]
            {
                new CommandPageActionBinding("cmd.combat.rapid", delegate { ExecuteToggleRapidFire(player, null); }),
                new CommandPageActionBinding("cmd.combat.auto_reload", delegate { ExecuteToggleAutoReload(null); }),
                new CommandPageActionBinding("cmd.combat.ammo_mode", delegate { ExecuteCycleAmmoMode(null); }),
                new CommandPageActionBinding("cmd.combat.invincible", delegate { ExecuteToggleInvincibility(player, null); }),
                new CommandPageActionBinding("cmd.combat.ammonomicon", delegate { ExecuteToggleAmmonomiconFastOpen(null); }),
                new CommandPageActionBinding("cmd.combat.full_ammo", delegate { ExecuteRefillCurrentGunAmmo(player, null); }),
            };
        }

        private CombatSettingRow CreateRapidFireCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            bool isEnabled = IsRapidFireEnabledFor(player);
            return new CombatSettingRow(
                rect,
                "cmd.combat.rapid",
                GetLocalizedFallback("gui.command.setting.rapid", "Hold Rapid", "\u6309\u4f4f\u8fde\u53d1"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleRapidFire(player, logger); });
        }

        private CombatSettingRow CreateAutoReloadCombatSetting(Rect rect, ManualLogSource logger)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.auto_reload",
                GetLocalizedFallback("gui.command.setting.auto_reload", "Auto Reload", "\u81ea\u52a8\u6362\u5f39"),
                GetAutoReloadStatusLabel(),
                IsAutoReloadEnabled(),
                delegate { ExecuteToggleAutoReload(logger); });
        }

        private CombatSettingRow CreateAmmoModeCombatSetting(Rect rect, ManualLogSource logger)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.ammo_mode",
                GetLocalizedFallback("gui.command.setting.ammo_mode", "Ammo Mode", "\u5f39\u836f\u6a21\u5f0f"),
                GetAmmoModeStatusLabel(),
                IsAmmoModeEnabled(),
                delegate { ExecuteCycleAmmoMode(logger); });
        }

        private CombatSettingRow CreateInvincibilityCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            bool isEnabled = IsInvincibilityEnabled();
            return new CombatSettingRow(
                rect,
                "cmd.combat.invincible",
                GetLocalizedFallback("gui.command.setting.invincible", "Invincibility", "\u65e0\u654c"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleInvincibility(player, logger); });
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

        private void DrawCombatSettingRows(CombatSettingRow[] rows, float labelWidth, float buttonWidth)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                DrawCombatSettingRow(rows[index], labelWidth, buttonWidth);
            }
        }

        private void DrawCombatSettingRow(CombatSettingRow row, float labelWidth, float buttonWidth)
        {
            GUI.Label(new Rect(row.Rect.x, row.Rect.y + 7f, labelWidth, 20f), row.Label, _hintStyle);
            GUIStyle buttonStyle = row.IsActive ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(row.Rect.x + labelWidth, row.Rect.y, buttonWidth, row.Rect.height), row.ControlId, row.Status, buttonStyle) && row.OnClick != null)
            {
                row.OnClick();
            }
        }

        private struct CombatSettingRow
        {
            public CombatSettingRow(Rect rect, string controlId, string label, string status, bool isActive, System.Action onClick)
            {
                Rect = rect;
                ControlId = controlId ?? string.Empty;
                Label = label ?? string.Empty;
                Status = status ?? string.Empty;
                IsActive = isActive;
                OnClick = onClick;
            }

            public Rect Rect;

            public string ControlId;

            public string Label;

            public string Status;

            public bool IsActive;

            public System.Action OnClick;
        }
    }
}
