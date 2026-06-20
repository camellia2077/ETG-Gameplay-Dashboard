using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void DrawCommandPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            const float controlHeight = 34f;
            const float categoryButtonWidth = 92f;
            const float categoryButtonHeight = 28f;
            const float contentButtonWidth = 132f;
            Rect languageButtonRect = new Rect(panelRect.x + panelRect.width - LanguageButtonWidth - 14f, panelRect.y + 12f, LanguageButtonWidth, 30f);
            Rect aboutButtonRect = new Rect(languageButtonRect.x - ButtonGap - ButtonWidth, languageButtonRect.y, ButtonWidth, 30f);

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, aboutButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.command.title"),
                _titleStyle);
            if (GUI.Button(aboutButtonRect, GuiText.Get("gui.command.button.about"), _buttonStyle))
            {
                OpenAboutPage();
            }

            if (GUI.Button(languageButtonRect, GetLanguageButtonLabel(), _buttonStyle))
            {
                ExecuteToggleLanguage(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.input"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.toggle"),
                _hintStyle);

            GUI.SetNextControlName(InputControlName);
            float textFieldWidth = panelRect.width - 54f - (ButtonWidth * 2f) - ButtonGap - 12f;
            Rect textFieldRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, textFieldWidth, controlHeight);
            Rect grantButtonRect = new Rect(textFieldRect.xMax + 12f, textFieldRect.y, ButtonWidth, controlHeight);
            Rect randomButtonRect = new Rect(grantButtonRect.xMax + ButtonGap, textFieldRect.y, ButtonWidth, controlHeight);
            _inputText = GUI.TextField(textFieldRect, _inputText, 256, _textFieldStyle);

            if (_focusInputField)
            {
                GUI.FocusControl(InputControlName);
                _focusInputField = false;
            }

            bool shouldSubmit = false;
            Event currentEvent = Event.current;
            if (currentEvent != null &&
                currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                shouldSubmit = true;
                currentEvent.Use();
            }

            if (GUI.Button(grantButtonRect, GuiText.Get("gui.command.button.grant"), _buttonStyle))
            {
                shouldSubmit = true;
            }

            if (GUI.Button(randomButtonRect, GuiText.Get("gui.command.button.random"), _buttonStyle))
            {
                ExecuteRandom(player, logger);
            }

            float categoryTop = panelRect.y + 132f;
            float segmentLeft = panelRect.x + 14f;
            Rect generalCategoryButtonRect = new Rect(segmentLeft, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect combatCategoryButtonRect = new Rect(generalCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect playerCategoryButtonRect = new Rect(combatCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            DrawCommandCategoryButton(generalCategoryButtonRect, CommandMenuCategory.General, GuiText.Get("gui.command.category.general"));
            DrawCommandCategoryButton(combatCategoryButtonRect, CommandMenuCategory.Combat, GuiText.Get("gui.command.category.combat"));
            DrawCommandCategoryButton(playerCategoryButtonRect, CommandMenuCategory.Player, GuiText.Get("gui.command.category.player"));

            Rect contentRect = new Rect(panelRect.x + 14f, generalCategoryButtonRect.yMax + 14f, panelRect.width - 28f, panelRect.height - (generalCategoryButtonRect.yMax - panelRect.y) - 56f);
            DrawCommandCategoryContent(contentRect, contentButtonWidth, controlHeight, player, logger);

            if (shouldSubmit)
            {
                Submit(player, logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + panelRect.height - 28f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.submit"),
                _hintStyle);
        }

        private float GetCommandPanelHeight()
        {
            const float contentTopOffset = 174f;
            const float controlHeight = 34f;
            const float footerReserveHeight = 42f;
            int rowCount = GetCommandCategoryRowCount();
            float contentHeight = (rowCount * controlHeight) + (Mathf.Max(0, rowCount - 1) * ButtonGap);
            return Mathf.Max(BasePanelHeight, contentTopOffset + contentHeight + footerReserveHeight);
        }

        private int GetCommandCategoryRowCount()
        {
            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.Combat:
                    return 3;
                case CommandMenuCategory.General:
                case CommandMenuCategory.Player:
                default:
                    return 2;
            }
        }

        private void DrawCommandCategoryButton(Rect rect, CommandMenuCategory category, string label)
        {
            GUIStyle style = _commandMenuCategory == category ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _commandMenuCategory = category;
            }
        }

        private void DrawCommandCategoryContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float secondColumnX = contentRect.x + buttonWidth + ButtonGap;
            float thirdColumnX = secondColumnX + buttonWidth + ButtonGap;
            float fourthColumnX = thirdColumnX + buttonWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

            if (_commandMenuCategory == CommandMenuCategory.General)
            {
                if (GUI.Button(new Rect(contentRect.x, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.pickups"), _buttonStyle))
                {
                    OpenPickupPage(logger);
                }

                if (GUI.Button(new Rect(secondColumnX, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.loadout"), _buttonStyle))
                {
                    OpenLoadoutEditorPage(logger);
                }

                if (GUI.Button(new Rect(thirdColumnX, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.currency"), _buttonStyle))
                {
                    OpenCurrencyPage(logger);
                }

                if (GUI.Button(new Rect(fourthColumnX, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.teleport"), _buttonStyle))
                {
                    _showTeleportPanel = !_showTeleportPanel;
                }

                if (GUI.Button(new Rect(contentRect.x, secondRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.characters"), _buttonStyle))
                {
                    OpenCharacterPage(logger);
                }

                if (GUI.Button(new Rect(secondColumnX, secondRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.boss_rush"), _buttonStyle))
                {
                    OpenBossRushPage(logger);
                }

                return;
            }

            if (_commandMenuCategory == CommandMenuCategory.Combat)
            {
                DrawCombatSettings(contentRect, controlHeight, player, logger);

                return;
            }

            if (GUI.Button(new Rect(contentRect.x, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.heal_half"), _buttonStyle))
            {
                ExecuteHealHalfHeart(player, logger);
            }

            if (GUI.Button(new Rect(secondColumnX, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.add_armor"), _buttonStyle))
            {
                ExecuteAddArmor(player, logger);
            }

            if (GUI.Button(new Rect(thirdColumnX, firstRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.full_heal"), _buttonStyle))
            {
                ExecuteFullHeal(player, logger);
            }

            if (GUI.Button(new Rect(contentRect.x, secondRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.clear_curse"), _buttonStyle))
            {
                ExecuteClearCurse(player, logger);
            }

            if (GUI.Button(new Rect(secondColumnX, secondRowY, buttonWidth, controlHeight), GuiText.Get("gui.command.button.refill_blanks"), _buttonStyle))
            {
                ExecuteRefillBlanks(player, logger);
            }

            string statsButtonLabel = _showPlayerStatsPanel
                ? GuiText.Get("gui.command.button.stats_on")
                : GuiText.Get("gui.command.button.stats_off");
            if (GUI.Button(new Rect(thirdColumnX, secondRowY, buttonWidth, controlHeight), statsButtonLabel, _buttonStyle))
            {
                _showPlayerStatsPanel = !_showPlayerStatsPanel;
            }
        }

        private void DrawCombatSettings(Rect contentRect, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float settingColumnWidth = 270f;
            const float settingLabelWidth = 146f;
            const float settingButtonWidth = 116f;
            float secondSettingColumnX = contentRect.x + settingColumnWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

            DrawCombatSettingRow(
                new Rect(contentRect.x, firstRowY, settingColumnWidth, controlHeight),
                GetLocalizedFallback("gui.command.setting.rapid", "Hold Rapid", "\u6309\u4f4f\u8fde\u53d1"),
                GetOnOffStatusLabel(_rapidFireToggleService != null && _rapidFireToggleService.IsEnabledFor(player)),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleRapidFire(player, logger); });

            DrawCombatSettingRow(
                new Rect(secondSettingColumnX, firstRowY, settingColumnWidth, controlHeight),
                GetLocalizedFallback("gui.command.setting.auto_reload", "Auto Reload", "\u81ea\u52a8\u6362\u5f39"),
                GetAutoReloadStatusLabel(),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleAutoReload(logger); });

            DrawCombatSettingRow(
                new Rect(contentRect.x, secondRowY, settingColumnWidth, controlHeight),
                GetLocalizedFallback("gui.command.setting.no_ammo", "Ammo Consumption", "\u4e0d\u6d88\u8017\u5b50\u5f39"),
                GetOnOffStatusLabel(_noAmmoConsumptionToggleService != null && _noAmmoConsumptionToggleService.IsEnabled),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleNoAmmoConsumption(logger); });

            DrawCombatSettingRow(
                new Rect(secondSettingColumnX, secondRowY, settingColumnWidth, controlHeight),
                GetLocalizedFallback("gui.command.setting.invincible", "Invincibility", "\u65e0\u654c"),
                GetOnOffStatusLabel(_invincibilityToggleService != null && _invincibilityToggleService.IsEnabled),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleInvincibility(player, logger); });

            DrawCombatSettingRow(
                new Rect(contentRect.x, thirdRowY, settingColumnWidth, controlHeight),
                GetLocalizedFallback("gui.command.setting.ammonomicon_animation", "Ammo Book Animation", "\u67aa\u68b0\u767e\u79d1\u52a8\u753b"),
                GetOnOffStatusLabel(AmmonomiconAnimationToggleService.IsOpenAnimationEnabled),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleAmmonomiconOpenAnimation(logger); });

            if (GUI.Button(new Rect(secondSettingColumnX + settingLabelWidth, thirdRowY, settingButtonWidth, controlHeight), GuiText.Get("gui.command.button.full_ammo"), _buttonStyle))
            {
                ExecuteRefillCurrentGunAmmo(player, logger);
            }
        }

        private void DrawCombatSettingRow(Rect rowRect, string label, string statusLabel, float labelWidth, float buttonWidth, System.Action onClick)
        {
            GUI.Label(new Rect(rowRect.x, rowRect.y + 7f, labelWidth, 20f), label, _hintStyle);
            if (GUI.Button(new Rect(rowRect.x + labelWidth, rowRect.y, buttonWidth, rowRect.height), statusLabel, _buttonStyle) && onClick != null)
            {
                onClick();
            }
        }

        private string GetAutoReloadButtonLabel()
        {
            if (_autoReloadToggleService == null)
            {
                return GuiText.Get("gui.command.button.auto_reload_off");
            }

            switch (_autoReloadToggleService.Mode)
            {
                case AutoReloadMode.Instant:
                    return GuiText.Get("gui.command.button.auto_reload_instant");
                case AutoReloadMode.Animated:
                    return GuiText.Get("gui.command.button.auto_reload_animated");
                default:
                    return GuiText.Get("gui.command.button.auto_reload_off");
            }
        }

        private string GetAutoReloadStatusLabel()
        {
            if (_autoReloadToggleService == null)
            {
                return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }

            switch (_autoReloadToggleService.Mode)
            {
                case AutoReloadMode.Instant:
                    return GetLocalizedFallback("gui.command.status.instant", "Instant", "\u77ac\u95f4");
                case AutoReloadMode.Animated:
                    return GetLocalizedFallback("gui.command.status.animated", "Animated", "\u52a8\u753b");
                default:
                    return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }
        }

        private static string GetOnOffStatusLabel(bool isEnabled)
        {
            return isEnabled
                ? GetLocalizedFallback("gui.command.status.on", "ON", "\u5f00")
                : GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
        }

        private static string GetLocalizedFallback(string key, string englishFallback, string simplifiedChineseFallback)
        {
            string value = GuiText.Get(key);
            if (!string.Equals(value, key, System.StringComparison.Ordinal))
            {
                return value;
            }

            return string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase)
                ? simplifiedChineseFallback
                : englishFallback;
        }

        private string GetLanguageButtonLabel()
        {
            string language = GetConfiguredLanguage();
            if (string.Equals(language, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("gui.command.button.language_zh");
            }

            if (string.Equals(language, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("gui.command.button.language_en");
            }

            return GuiText.Get("gui.command.button.language_auto");
        }

        private void ExecuteToggleLanguage(ManualLogSource logger)
        {
            if (_languageSetter == null)
            {
                return;
            }

            string nextLanguage = GetNextLanguage(GetConfiguredLanguage());
            _languageSetter(nextLanguage);
            _lastGuiLanguageCode = string.Empty;
            ResetPickupBrowserState();
            ResetCharacterPageCache();
            ShowStatus(GuiText.Get("result.language.changed", GetLanguageDisplayName(nextLanguage)), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(GuiText.GetEnglish("result.language.changed", GetEnglishLanguageDisplayName(nextLanguage))));
            }
        }

        private string GetConfiguredLanguage()
        {
            return _languageProvider != null ? GuiText.NormalizeLanguageOverride(_languageProvider()) : "auto";
        }

        private static string GetNextLanguage(string currentLanguage)
        {
            if (string.Equals(currentLanguage, "auto", System.StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            if (string.Equals(currentLanguage, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            return "auto";
        }

        private static string GetLanguageDisplayName(string language)
        {
            if (string.Equals(language, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("label.language.zh");
            }

            if (string.Equals(language, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("label.language.en");
            }

            return GuiText.Get("label.language.auto");
        }

        private static string GetEnglishLanguageDisplayName(string language)
        {
            if (string.Equals(language, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.GetEnglish("label.language.zh");
            }

            if (string.Equals(language, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.GetEnglish("label.language.en");
            }

            return GuiText.GetEnglish("label.language.auto");
        }
    }
}
