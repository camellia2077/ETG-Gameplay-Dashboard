using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenCurrencyPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Currency;
            _focusInputField = false;
            _currencyPageFocusedControlId = "currency.max_health";

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Pickups menu opened."));
            }
        }

        private void DrawCurrencyPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("currency.back", _buttonStyle)))
            {
                CloseCurrencyPage();
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                GetLocalizedFallback("gui.command.currency.title", "Pickups", "拾取物"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GetLocalizedFallback("gui.command.currency.hint.choose", "Choose a pickup to add.", "选择要增加的拾取物。"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetLocalizedFallback("gui.command.currency.hint.run_only", "Applies to the current character or current run only.", "只影响当前角色或当前这一局。"),
                _hintStyle);

            const float rowHeight = 38f;
            const float rowGap = 8f;
            float top = panelRect.y + 92f;
            Rect contentRect = new Rect(panelRect.x + 14f, top, panelRect.width - 28f, panelRect.height - (top - panelRect.y) - 14f);
            DrawPickupActionRows(contentRect, top, rowHeight, rowGap, BuildCurrencyPickupRows(player, logger));
        }

        private void CloseCurrencyPage()
        {
            _currentPage = PanelPage.Command;
            _focusInputField = true;
        }

        private ControllerFocusEntry[] GetCurrencyPageFocusEntries()
        {
            return CurrencyPageFocusEntries;
        }

        private void ExecuteCurrencyPageFocusedControl(PlayerController player, ManualLogSource logger)
        {
            switch (_currencyPageFocusedControlId)
            {
                case "currency.back":
                    CloseCurrencyPage();
                    return;
                case "currency.max_health":
                    ExecuteAddMaxHealth(player, logger);
                    return;
                case "currency.armor":
                    ExecuteAddArmor(player, logger);
                    return;
                case "currency.blank":
                    ExecuteAddBlank(player, logger);
                    return;
                case "currency.key":
                    ExecuteAddKey(player, logger);
                    return;
                case "currency.rat_key":
                    ExecuteAddRatKey(player, logger);
                    return;
                case "currency.casings":
                    ExecuteAddCurrency(player, logger);
                    return;
                case "currency.hegemony":
                    ExecuteAddMetaCurrency(player, logger);
                    return;
                default:
                    return;
            }
        }

        private PickupActionRowDefinition[] BuildCurrencyPickupRows(PlayerController player, ManualLogSource logger)
        {
            string actionLabel = GetLocalizedFallback("gui.command.currency.button.spawn", "Spawn", "生成");
            return new[]
            {
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteHealthPickup,
                    GetLocalizedFallback("gui.command.currency.label.max_health", "Max HP (+1)", "血量上限（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.max_health", actionLabel, delegate { ExecuteAddMaxHealth(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteArmorPickup,
                    GetLocalizedFallback("gui.command.currency.label.armor", "Armor (+1)", "护甲（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.armor", actionLabel, delegate { ExecuteAddArmor(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteBlankPickup,
                    GetLocalizedFallback("gui.command.currency.label.blank", "Blank (+1)", "空响弹（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.blank", actionLabel, delegate { ExecuteAddBlank(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteKeyPickup,
                    GetLocalizedFallback("gui.command.currency.label.key", "Key (+1)", "钥匙（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.key", actionLabel, delegate { ExecuteAddKey(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteRatRewardKeyPickup,
                    GetLocalizedFallback("gui.command.currency.label.rat_key", "Rat Key (+1)", "老鼠钥匙（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.rat_key", actionLabel, delegate { ExecuteAddRatKey(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteCasingsPickup,
                    GetLocalizedFallback("gui.command.currency.label.casings", "Casings (+100)", "弹壳（+100）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.casings", actionLabel, delegate { ExecuteAddCurrency(player, logger); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteHegemonyPickup,
                    GetLocalizedFallback("gui.command.currency.label.hegemony", "Hegemony (+50)", "霸权币（+50）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.hegemony", actionLabel, delegate { ExecuteAddMetaCurrency(player, logger); }, _buttonStyle),
                    }),
            };
        }
    }
}
