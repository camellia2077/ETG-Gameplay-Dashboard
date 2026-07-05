using System;
using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private static readonly ControllerFocusEntry[] PlayerSectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.section.pickups", 2, 0),
            new ControllerFocusEntry("cmd.player.section.stats", 2, 1),
        };

        private static readonly ControllerFocusEntry[] PlayerPickupCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.heal_half", 3, 0),
            new ControllerFocusEntry("cmd.player.full_heal", 3, 1),
            new ControllerFocusEntry("cmd.player.add_max_health", 3, 2),
            new ControllerFocusEntry("cmd.player.add_armor", 4, 0),
            new ControllerFocusEntry("cmd.player.armor_no_consume", 4, 1),
            new ControllerFocusEntry("cmd.player.add_blank", 5, 0),
            new ControllerFocusEntry("cmd.player.blank_no_consume", 5, 1),
            new ControllerFocusEntry("cmd.player.add_key", 6, 0),
            new ControllerFocusEntry("cmd.player.key_no_consume", 6, 1),
            new ControllerFocusEntry("cmd.player.add_rat_key", 7, 0),
            new ControllerFocusEntry("cmd.player.add_currency_large", 8, 0),
            new ControllerFocusEntry("cmd.player.currency_no_consume", 8, 1),
        };

        private static readonly ControllerFocusEntry[] PlayerStatsCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.clear_curse", 3, 0),
        };

        private void DrawPlayerContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            Rect pickupsSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect statsSectionRect = new Rect(pickupsSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawPlayerSectionButton(pickupsSectionRect, "cmd.player.section.pickups", PlayerMenuSection.Pickups, GetLocalizedFallback("gui.command.player.section.pickups", "Pickups", "拾取物"));
            DrawPlayerSectionButton(statsSectionRect, "cmd.player.section.stats", PlayerMenuSection.Stats, GetLocalizedFallback("gui.command.player.section.stats", "Stats", "属性"));

            Rect sectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 12f, contentRect.width, contentRect.height - sectionButtonHeight - 12f);
            if (_playerMenuSection == PlayerMenuSection.Stats)
            {
                DrawPlayerStatsContent(sectionContentRect, buttonWidth, controlHeight, player, logger);
                return;
            }

            const float actionRowHeight = 38f;
            DrawPickupActionRows(sectionContentRect, sectionContentRect.y, actionRowHeight, ButtonGap, BuildPlayerPickupRows(player, logger));
        }

        private void DrawPlayerStatsContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            if (DrawControllerButton(new Rect(contentRect.x, contentRect.y, buttonWidth, controlHeight), "cmd.player.clear_curse", GuiText.Get("gui.command.button.clear_curse"), _buttonStyle))
            {
                ExecuteClearCurse(player, logger);
            }
        }

        private PickupActionRowDefinition[] BuildPlayerPickupRows(PlayerController player, ManualLogSource logger)
        {
            return new[]
            {
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteHealthPickup,
                    GetLocalizedFallback("gui.command.label.health", "Health", "血量"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.heal_half", GuiText.Get("gui.command.player.health.heal_half"), delegate { ExecuteHealHalfHeart(player, logger); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.full_heal", GuiText.Get("gui.command.player.health.full_heal"), delegate { ExecuteFullHeal(player, logger); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.add_max_health", GuiText.Get("gui.command.player.health.add_max"), delegate { ExecuteAddMaxHealth(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteArmorPickup,
                    GetLocalizedFallback("gui.command.label.armor", "Armor", "护甲"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_armor", GuiText.Get("gui.command.player.armor.add_one"), delegate { ExecuteAddArmor(player, logger); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.armor_no_consume", GetNoConsumeActionLabel(_armorNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleArmorNoConsume(player, logger); }, GetNoConsumeActionStyle(_armorNoConsumeToggleService.IsEnabled)),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteBlankPickup,
                    GetLocalizedFallback("gui.command.label.blank", "Blank", "空响弹"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_blank", GuiText.Get("gui.command.player.blank.add_one"), delegate { ExecuteAddBlank(player, logger); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.blank_no_consume", GetNoConsumeActionLabel(_blankNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleBlankNoConsume(player, logger); }, GetNoConsumeActionStyle(_blankNoConsumeToggleService.IsEnabled)),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteKeyPickup,
                    GetLocalizedFallback("gui.command.label.key", "Key", "钥匙"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_key", GetLocalizedFallback("gui.command.action.add_one", "+1", "+1"), delegate { ExecuteAddKey(player, logger); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.key_no_consume", GetNoConsumeActionLabel(_keyNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleKeyNoConsume(player, logger); }, GetNoConsumeActionStyle(_keyNoConsumeToggleService.IsEnabled)),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteRatRewardKeyPickup,
                    GetLocalizedFallback("gui.command.label.rat_key", "Rat Key", "老鼠钥匙"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_rat_key", GetLocalizedFallback("gui.command.action.add_one", "+1", "+1"), delegate { ExecuteAddRatKey(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteCasingsPickup,
                    GetLocalizedFallback("gui.command.label.casings", "Casings", "弹壳"),
                    new[]
                    {
                        new PickupActionButtonDefinition("cmd.player.add_currency_large", GetLocalizedFallback("gui.command.action.add_hundred", "+100", "+100"), delegate { ExecuteAddLargeCurrency(player, logger); }, _buttonStyle),
                        new PickupActionButtonDefinition("cmd.player.currency_no_consume", GetNoConsumeActionLabel(_currencyNoConsumeToggleService.IsEnabled), delegate { ExecuteToggleCurrencyNoConsume(player, logger); }, GetNoConsumeActionStyle(_currencyNoConsumeToggleService.IsEnabled)),
                    }),
            };
        }

        private void DrawPlayerSectionButton(Rect rect, string controlId, PlayerMenuSection section, string label)
        {
            GUIStyle style = _playerMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (DrawControllerButton(rect, controlId, label, style))
            {
                _playerMenuSection = section;
            }
        }

        private ControllerFocusEntry[] GetPlayerCommandPageFocusEntries()
        {
            if (_playerMenuSection == PlayerMenuSection.Stats)
            {
                return BuildCommandPageFocusEntries(PlayerSectionCommandPageFocusEntries, PlayerStatsCommandPageFocusEntries);
            }

            return BuildCommandPageFocusEntries(PlayerSectionCommandPageFocusEntries, PlayerPickupCommandPageFocusEntries);
        }

        private GUIStyle GetNoConsumeActionStyle(bool isEnabled)
        {
            return isEnabled ? _enabledButtonStyle : _buttonStyle;
        }

        private CommandPageActionBinding[] GetPlayerCommandPageActionBindings(PlayerController player)
        {
            return new[]
            {
                new CommandPageActionBinding("cmd.player.heal_half", delegate { ExecuteHealHalfHeart(player, null); }),
                new CommandPageActionBinding("cmd.player.full_heal", delegate { ExecuteFullHeal(player, null); }),
                new CommandPageActionBinding("cmd.player.add_max_health", delegate { ExecuteAddMaxHealth(player, null); }),
                new CommandPageActionBinding("cmd.player.add_armor", delegate { ExecuteAddArmor(player, null); }),
                new CommandPageActionBinding("cmd.player.armor_no_consume", delegate { ExecuteToggleArmorNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.player.add_blank", delegate { ExecuteAddBlank(player, null); }),
                new CommandPageActionBinding("cmd.player.add_key", delegate { ExecuteAddKey(player, null); }),
                new CommandPageActionBinding("cmd.player.key_no_consume", delegate { ExecuteToggleKeyNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.player.add_rat_key", delegate { ExecuteAddRatKey(player, null); }),
                new CommandPageActionBinding("cmd.player.add_currency_large", delegate { ExecuteAddLargeCurrency(player, null); }),
                new CommandPageActionBinding("cmd.player.currency_no_consume", delegate { ExecuteToggleCurrencyNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.player.blank_no_consume", delegate { ExecuteToggleBlankNoConsume(player, null); }),
                new CommandPageActionBinding("cmd.player.clear_curse", delegate { ExecuteClearCurse(player, null); }),
            };
        }
    }
}
