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

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Currency menu opened."));
            }
        }

        private void DrawCurrencyPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                GetLocalizedFallback("gui.currency.title", "Player Resources", "人物资源"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GetLocalizedFallback("gui.currency.hint.choose", "Choose a player resource to add.", "选择要增加的人物资源。"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetLocalizedFallback("gui.currency.hint.run_only", "Applies to the current character or current run only.", "只影响当前角色或当前这一局。"),
                _hintStyle);

            float left = panelRect.x + 14f;
            float top = panelRect.y + 92f;
            Rect addKeyButtonRect = new Rect(left, top, CurrencyActionButtonWidth, 34f);
            Rect addRatKeyButtonRect = new Rect(addKeyButtonRect.xMax + ButtonGap, top, CurrencyActionButtonWidth, 34f);
            Rect addCurrencyButtonRect = new Rect(left, addKeyButtonRect.yMax + ButtonGap, CurrencyActionButtonWidth, 34f);
            Rect addMetaCurrencyButtonRect = new Rect(addCurrencyButtonRect.xMax + ButtonGap, addCurrencyButtonRect.y, CurrencyActionButtonWidth, 34f);
            Rect addBlankButtonRect = new Rect(left, addCurrencyButtonRect.yMax + ButtonGap, CurrencyActionButtonWidth, 34f);
            Rect addMaxHealthButtonRect = new Rect(addBlankButtonRect.xMax + ButtonGap, addBlankButtonRect.y, CurrencyActionButtonWidth, 34f);
            if (GUI.Button(addKeyButtonRect, GetLocalizedFallback("gui.currency.button.key", "+1 Key", "+1 钥匙"), _buttonStyle))
            {
                ExecuteAddKey(player, logger);
            }

            if (GUI.Button(addRatKeyButtonRect, GetLocalizedFallback("gui.currency.button.rat_key", "+1 Rat Key", "+1 老鼠钥匙"), _buttonStyle))
            {
                ExecuteAddRatKey(player, logger);
            }

            if (GUI.Button(addCurrencyButtonRect, GetLocalizedFallback("gui.currency.button.casings", "+50 Casings", "+50 弹壳"), _buttonStyle))
            {
                // Dungeon run currency (casings).
                ExecuteAddCurrency(player, logger);
            }

            if (GUI.Button(addMetaCurrencyButtonRect, GetLocalizedFallback("gui.currency.button.hegemony", "+50 Hegemony", "+50 霸权币"), _buttonStyle))
            {
                // Breach hub meta currency (hegemony credits).
                ExecuteAddMetaCurrency(player, logger);
            }

            if (GUI.Button(addBlankButtonRect, GetLocalizedFallback("gui.currency.button.blank", "+1 Blank", "+1 空包弹"), _buttonStyle))
            {
                ExecuteAddBlank(player, logger);
            }

            if (GUI.Button(addMaxHealthButtonRect, GetLocalizedFallback("gui.currency.button.max_health", "+1 Max HP", "+1 血量上限"), _buttonStyle))
            {
                ExecuteAddMaxHealth(player, logger);
            }
        }
    }
}
