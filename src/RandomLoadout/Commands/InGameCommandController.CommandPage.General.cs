using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private static readonly ControllerFocusEntry[] GeneralCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.general.pickups", 2, 0),
            new ControllerFocusEntry("cmd.general.loadout", 2, 1),
            new ControllerFocusEntry("cmd.general.currency", 2, 2),
            new ControllerFocusEntry("cmd.general.teleport", 2, 3),
            new ControllerFocusEntry("cmd.general.characters", 3, 0),
            new ControllerFocusEntry("cmd.general.boss_rush", 3, 1),
            new ControllerFocusEntry("cmd.general.reveal_map", 3, 2),
            new ControllerFocusEntry("cmd.general.random_item", 3, 3),
            new ControllerFocusEntry("cmd.general.stats", 4, 0),
            new ControllerFocusEntry("cmd.general.pickup_info", 4, 1),
            new ControllerFocusEntry("cmd.general.pickup_info_config", 4, 2),
        };

        private void DrawGeneralContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float secondColumnX = contentRect.x + buttonWidth + ButtonGap;
            float thirdColumnX = secondColumnX + buttonWidth + ButtonGap;
            float fourthColumnX = thirdColumnX + buttonWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

            if (DrawControllerButton(new Rect(contentRect.x, firstRowY, buttonWidth, controlHeight), "cmd.general.pickups", GuiText.Get("gui.command.button.pickups"), _buttonStyle))
            {
                OpenPickupPage(logger);
            }

            if (DrawControllerButton(new Rect(secondColumnX, firstRowY, buttonWidth, controlHeight), "cmd.general.loadout", GuiText.Get("gui.command.button.loadout"), _buttonStyle))
            {
                OpenLoadoutEditorPage(logger);
            }

            if (DrawControllerButton(new Rect(thirdColumnX, firstRowY, buttonWidth, controlHeight), "cmd.general.currency", GuiText.Get("gui.command.button.currency"), _buttonStyle))
            {
                OpenCurrencyPage(logger);
            }

            if (DrawControllerButton(new Rect(fourthColumnX, firstRowY, buttonWidth, controlHeight), "cmd.general.teleport", GuiText.Get("gui.command.button.teleport"), _buttonStyle))
            {
                ToggleTeleportPanel();
            }

            if (DrawControllerButton(new Rect(contentRect.x, secondRowY, buttonWidth, controlHeight), "cmd.general.characters", GuiText.Get("gui.command.button.characters"), _buttonStyle))
            {
                OpenCharacterPage(logger);
            }

            if (DrawControllerButton(new Rect(secondColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.boss_rush", GuiText.Get("gui.command.button.boss_rush"), _buttonStyle))
            {
                OpenBossRushPage(logger);
            }

            GUIStyle revealMapButtonStyle = IsRevealMapActive() ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(thirdColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.reveal_map", GetLocalizedFallback("gui.command.button.reveal_map", "Reveal Map", "地图全开"), revealMapButtonStyle))
            {
                ExecuteRevealCurrentFloorMap(player, logger);
            }

            if (DrawControllerButton(new Rect(fourthColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.random_item", GuiText.Get("gui.command.button.random"), _buttonStyle))
            {
                ExecuteRandom(player, logger);
            }

            string statsButtonLabel = _showPlayerStatsPanel
                ? GuiText.Get("gui.command.button.stats_on")
                : GuiText.Get("gui.command.button.stats_off");
            GUIStyle statsButtonStyle = _showPlayerStatsPanel ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(contentRect.x, thirdRowY, buttonWidth, controlHeight), "cmd.general.stats", statsButtonLabel, statsButtonStyle))
            {
                SetPlayerStatsPanelShown(!_showPlayerStatsPanel);
            }

            string pickupInfoButtonLabel = _showPickupInfoOverlay
                ? GetLocalizedFallback("gui.command.button.pickup_info_on", "Pickup Info: On", "物品图鉴：开")
                : GetLocalizedFallback("gui.command.button.pickup_info_off", "Pickup Info: Off", "物品图鉴：关");
            GUIStyle pickupInfoButtonStyle = _showPickupInfoOverlay ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(secondColumnX, thirdRowY, buttonWidth, controlHeight), "cmd.general.pickup_info", pickupInfoButtonLabel, pickupInfoButtonStyle))
            {
                ExecuteTogglePickupInfoOverlay();
            }

            if (DrawControllerButton(new Rect(thirdColumnX, thirdRowY, buttonWidth, controlHeight), "cmd.general.pickup_info_config", GetLocalizedFallback("gui.command.button.pickup_info_config", "Pickup Info Config", "物品图鉴设置"), _buttonStyle))
            {
                OpenPickupInfoConfigPage();
            }
        }

        private CommandPageActionBinding[] GetGeneralCommandPageActionBindings(PlayerController player)
        {
            return new[]
            {
                new CommandPageActionBinding("cmd.general.pickups", delegate { OpenPickupPage(null); }),
                new CommandPageActionBinding("cmd.general.loadout", delegate { OpenLoadoutEditorPage(null); }),
                new CommandPageActionBinding("cmd.general.currency", delegate { OpenCurrencyPage(null); }),
                new CommandPageActionBinding("cmd.general.teleport", delegate { ToggleTeleportPanel(); }),
                new CommandPageActionBinding("cmd.general.characters", delegate { OpenCharacterPage(null); }),
                new CommandPageActionBinding("cmd.general.boss_rush", delegate { OpenBossRushPage(null); }),
                new CommandPageActionBinding("cmd.general.reveal_map", delegate { ExecuteRevealCurrentFloorMap(player, null); }),
                new CommandPageActionBinding("cmd.general.random_item", delegate { ExecuteRandom(player, null); }),
                new CommandPageActionBinding("cmd.general.stats", delegate { SetPlayerStatsPanelShown(!_showPlayerStatsPanel); }),
                new CommandPageActionBinding("cmd.general.pickup_info", delegate { ExecuteTogglePickupInfoOverlay(); }),
                new CommandPageActionBinding("cmd.general.pickup_info_config", delegate { OpenPickupInfoConfigPage(); }),
            };
        }

        private void ExecuteTogglePickupInfoOverlay()
        {
            SetPickupInfoOverlayShown(!_showPickupInfoOverlay);
            ShowStatus(
                _showPickupInfoOverlay
                    ? GetLocalizedFallback("result.pickup_info_overlay.enabled", "Pickup Info enabled. Show detailed item info when you approach dropped pickups.", "已开启物品图鉴。靠近掉落物时显示详细物品信息。")
                    : GetLocalizedFallback("result.pickup_info_overlay.disabled", "Pickup Info disabled.", "已关闭物品图鉴。"),
                false);
        }
    }
}
