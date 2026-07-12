// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private static readonly ControllerFocusEntry[] CommandPageSharedFocusEntries =
        {
            new ControllerFocusEntry("cmd.about", 0, 0),
            new ControllerFocusEntry("cmd.settings", 0, 1),
            new ControllerFocusEntry("cmd.language", 0, 2),
            new ControllerFocusEntry("cmd.category.general", 1, 0),
            new ControllerFocusEntry("cmd.category.combat", 1, 1),
            new ControllerFocusEntry("cmd.category.player", 1, 2),
            new ControllerFocusEntry("cmd.category.room", 1, 3),
        };

        private void DrawCommandPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            const float controlHeight = 34f;
            const float categoryButtonWidth = 92f;
            const float categoryButtonHeight = 28f;
            const float contentButtonWidth = 132f;
            Rect languageButtonRect = new Rect(panelRect.x + panelRect.width - LanguageButtonWidth - 14f, panelRect.y + 12f, LanguageButtonWidth, 30f);
            Rect settingsButtonRect = new Rect(languageButtonRect.x - ButtonGap - ButtonWidth, languageButtonRect.y, ButtonWidth, 30f);
            Rect aboutButtonRect = new Rect(settingsButtonRect.x - ButtonGap - ButtonWidth, languageButtonRect.y, ButtonWidth, 30f);

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, aboutButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.command.title"),
                _titleStyle);
            if (DrawControllerButton(aboutButtonRect, "cmd.about", GuiText.Get("gui.command.button.about"), _buttonStyle))
            {
                OpenAboutPage();
            }

            if (DrawControllerButton(settingsButtonRect, "cmd.settings", GuiText.Get("gui.command.button.settings"), _buttonStyle))
            {
                OpenSettingsPage();
            }

            if (DrawControllerButton(languageButtonRect, "cmd.language", GetLanguageButtonLabel(), _buttonStyle))
            {
                ExecuteToggleLanguage(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.toggle", GetConfiguredToggleKeyName(), GetControllerShortcutDisplayName()),
                _hintStyle);

            float categoryTop = panelRect.y + 72f;
            float segmentLeft = panelRect.x + 14f;
            Rect generalCategoryButtonRect = new Rect(segmentLeft, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect combatCategoryButtonRect = new Rect(generalCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect playerCategoryButtonRect = new Rect(combatCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect roomCategoryButtonRect = new Rect(playerCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            DrawCommandCategoryButton(generalCategoryButtonRect, "cmd.category.general", CommandMenuCategory.General, GuiText.Get("gui.command.category.general"));
            DrawCommandCategoryButton(combatCategoryButtonRect, "cmd.category.combat", CommandMenuCategory.Combat, GuiText.Get("gui.command.category.combat"));
            DrawCommandCategoryButton(playerCategoryButtonRect, "cmd.category.player", CommandMenuCategory.Player, GuiText.Get("gui.command.category.player"));
            DrawCommandCategoryButton(roomCategoryButtonRect, "cmd.category.room", CommandMenuCategory.Room, GetLocalizedFallback("gui.command.category.room", "Room", "房间"));

            Rect contentRect = new Rect(panelRect.x + 14f, generalCategoryButtonRect.yMax + 14f, panelRect.width - 28f, panelRect.height - (generalCategoryButtonRect.yMax - panelRect.y) - 56f);
            DrawCommandCategoryContent(contentRect, contentButtonWidth, controlHeight, player, logger);

        }

        private float GetCommandPanelHeight()
        {
            const float contentTopOffset = 114f;
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
                case CommandMenuCategory.Player:
                    return _playerMenuSection == PlayerMenuSection.Stats ? 3 : 8;
                case CommandMenuCategory.Room:
                    return 5;
                case CommandMenuCategory.General:
                    return 3;
                default:
                    return 2;
            }
        }

        private void DrawCommandCategoryButton(Rect rect, string controlId, CommandMenuCategory category, string label)
        {
            GUIStyle style = _commandMenuCategory == category || IsControllerFocusActive("cmd", controlId)
                ? _pickupFilterActiveButtonStyle
                : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _commandMenuCategory = category;
            }
        }

        private void DrawCommandCategoryContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            if (_commandMenuCategory == CommandMenuCategory.General)
            {
                DrawGeneralContent(contentRect, buttonWidth, controlHeight, player, logger);
                return;
            }

            if (_commandMenuCategory == CommandMenuCategory.Combat)
            {
                DrawCombatSettings(contentRect, controlHeight, player, logger);

                return;
            }

            if (_commandMenuCategory == CommandMenuCategory.Room)
            {
                DrawRoomContent(contentRect, buttonWidth, controlHeight, player, logger);
                return;
            }
            DrawPlayerContent(contentRect, buttonWidth, controlHeight, player, logger);
        }

        private bool DrawControllerButton(Rect rect, string controlId, string label, GUIStyle normalStyle)
        {
            return GUI.Button(rect, label, GetControllerButtonStyle(controlId, normalStyle));
        }

        private GUIStyle GetControllerButtonStyle(string controlId, GUIStyle normalStyle)
        {
            if (!IsControllerFocusActive("cmd", controlId) &&
                !IsControllerFocusActive("settings", controlId) &&
                !IsControllerFocusActive("pickup_info_config", controlId) &&
                !IsControllerFocusActive("characters", controlId) &&
                !IsControllerFocusActive("loadout", controlId) &&
                !IsControllerFocusActive("pickups", controlId) &&
                !IsControllerFocusActive("currency", controlId))
            {
                return normalStyle;
            }

            if (normalStyle == _pickupFilterButtonStyle || normalStyle == _pickupFilterDisabledButtonStyle)
            {
                return _pickupFilterActiveButtonStyle;
            }

            if (normalStyle == _disabledToggleButtonStyle)
            {
                return _disabledToggleButtonStyle;
            }

            return _enabledButtonStyle;
        }

        private bool IsControllerFocusActive(string pagePrefix, string controlId)
        {
            if (_currentPage == PanelPage.Command && string.Equals(pagePrefix, "cmd", System.StringComparison.Ordinal))
            {
                return string.Equals(_commandPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Settings && string.Equals(pagePrefix, "settings", System.StringComparison.Ordinal))
            {
                return string.Equals(_settingsPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.PickupInfoConfig && string.Equals(pagePrefix, "pickup_info_config", System.StringComparison.Ordinal))
            {
                return string.Equals(_pickupInfoConfigFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Characters && string.Equals(pagePrefix, "characters", System.StringComparison.Ordinal))
            {
                return string.Equals(_characterPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.LoadoutEditor && string.Equals(pagePrefix, "loadout", System.StringComparison.Ordinal))
            {
                return string.Equals(_loadoutEditorFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Pickups && string.Equals(pagePrefix, "pickups", System.StringComparison.Ordinal))
            {
                return string.Equals(_pickupPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Currency && string.Equals(pagePrefix, "currency", System.StringComparison.Ordinal))
            {
                return string.Equals(_currencyPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            return false;
        }

        private ControllerFocusEntry[] GetCommandPageFocusEntries()
        {
            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.Combat:
                    return BuildCommandPageFocusEntries(CombatCommandPageFocusEntries);
                case CommandMenuCategory.Player:
                    return GetPlayerCommandPageFocusEntries();
                case CommandMenuCategory.Room:
                    return GetRoomCommandPageFocusEntries();
                case CommandMenuCategory.General:
                default:
                    return BuildCommandPageFocusEntries(GeneralCommandPageFocusEntries);
            }
        }

        private void ExecuteCommandPageFocusedControl()
        {
            PlayerController player = GetCurrentPlayer();
            if (ExecuteSharedCommandPageFocusedControl(player))
            {
                return;
            }

            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.General:
                    ExecuteGeneralCommandPageFocusedControl(player);
                    return;
                case CommandMenuCategory.Combat:
                    ExecuteCombatCommandPageFocusedControl(player);
                    return;
                case CommandMenuCategory.Player:
                    ExecutePlayerCommandPageFocusedControl(player);
                    return;
                case CommandMenuCategory.Room:
                    ExecuteRoomCommandPageFocusedControl(player);
                    return;
                default:
                    return;
            }
        }

        private bool ExecuteSharedCommandPageFocusedControl(PlayerController player)
        {
            return TryExecuteCommandPageAction(_commandPageFocusedControlId, player, GetSharedCommandPageActionBindings());
        }

        private void ExecuteGeneralCommandPageFocusedControl(PlayerController player)
        {
            TryExecuteCommandPageAction(_commandPageFocusedControlId, player, GetGeneralCommandPageActionBindings(player));
        }

        private void ExecutePlayerCommandPageFocusedControl(PlayerController player)
        {
            TryExecuteCommandPageAction(_commandPageFocusedControlId, player, GetPlayerCommandPageActionBindings(player));
        }

        private void ExecuteRoomCommandPageFocusedControl(PlayerController player)
        {
            TryExecuteCommandPageAction(_commandPageFocusedControlId, player, GetRoomCommandPageActionBindings(player));
        }

        private static bool TryExecuteCommandPageAction(string controlId, PlayerController player, params CommandPageActionBinding[] bindings)
        {
            if (string.IsNullOrEmpty(controlId) || bindings == null)
            {
                return false;
            }

            for (int index = 0; index < bindings.Length; index++)
            {
                if (!string.Equals(bindings[index].ControlId, controlId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (bindings[index].Action != null)
                {
                    bindings[index].Action(player);
                }

                return true;
            }

            return false;
        }

        private void CycleCommandCategory(int direction)
        {
            int currentIndex = 0;
            for (int i = 0; i < CommandMenuCategoryOrder.Length; i++)
            {
                if (CommandMenuCategoryOrder[i] == _commandMenuCategory)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + direction + CommandMenuCategoryOrder.Length) % CommandMenuCategoryOrder.Length;
            SetCommandMenuCategory(CommandMenuCategoryOrder[nextIndex]);
            _commandPageFocusedControlId = GetDefaultCommandFocusForCategory(_commandMenuCategory);
        }

        private void SetCommandMenuCategory(CommandMenuCategory category)
        {
            _commandMenuCategory = category;
        }

        private CommandPageActionBinding[] GetSharedCommandPageActionBindings()
        {
            return new[]
            {
                new CommandPageActionBinding("cmd.about", delegate { OpenAboutPage(); }),
                new CommandPageActionBinding("cmd.settings", delegate { OpenSettingsPage(); }),
                new CommandPageActionBinding("cmd.language", delegate { ExecuteToggleLanguage(null); }),
                new CommandPageActionBinding("cmd.category.general", delegate { SetCommandMenuCategory(CommandMenuCategory.General); }),
                new CommandPageActionBinding("cmd.category.combat", delegate { SetCommandMenuCategory(CommandMenuCategory.Combat); }),
                new CommandPageActionBinding("cmd.category.player", delegate { SetCommandMenuCategory(CommandMenuCategory.Player); }),
                new CommandPageActionBinding("cmd.category.room", delegate { SetCommandMenuCategory(CommandMenuCategory.Room); }),
                new CommandPageActionBinding("cmd.player.section.pickups", delegate { _playerMenuSection = PlayerMenuSection.Pickups; }),
                new CommandPageActionBinding("cmd.player.section.stats", delegate { _playerMenuSection = PlayerMenuSection.Stats; }),
                new CommandPageActionBinding("cmd.room.section.chest", delegate { _roomMenuSection = RoomMenuSection.Chest; }),
                new CommandPageActionBinding("cmd.room.section.neutral", delegate { _roomMenuSection = RoomMenuSection.Neutral; }),
                new CommandPageActionBinding("cmd.room.section.enemies", delegate
                {
                    if (IsExperimentalModeEnabled())
                    {
                        _roomMenuSection = RoomMenuSection.Enemies;
                    }
                }),
                new CommandPageActionBinding("cmd.room.section.state", delegate
                {
                    if (IsExperimentalModeEnabled())
                    {
                        _roomMenuSection = RoomMenuSection.State;
                    }
                }),
            };
        }

        private static string GetDefaultCommandFocusForCategory(CommandMenuCategory category)
        {
            switch (category)
            {
                case CommandMenuCategory.Combat:
                    return "cmd.combat.rapid";
                case CommandMenuCategory.Player:
                    return "cmd.player.section.pickups";
                case CommandMenuCategory.Room:
                    return "cmd.category.room";
                case CommandMenuCategory.General:
                default:
                    return "cmd.general.pickups";
            }
        }

        private struct CommandPageActionBinding
        {
            public CommandPageActionBinding(string controlId, System.Action<PlayerController> action)
            {
                ControlId = controlId ?? string.Empty;
                Action = action;
            }

            public string ControlId;

            public System.Action<PlayerController> Action;
        }

        private PlayerController GetCurrentPlayer()
        {
            GameManager gameManager = GameManager.Instance;
            return (object)gameManager != null ? gameManager.PrimaryPlayer : null;
        }

        private static ControllerFocusEntry[] BuildCommandPageFocusEntries(params ControllerFocusEntry[][] contentGroups)
        {
            int entryCount = CommandPageSharedFocusEntries.Length;
            for (int groupIndex = 0; groupIndex < contentGroups.Length; groupIndex++)
            {
                entryCount += contentGroups[groupIndex].Length;
            }

            ControllerFocusEntry[] entries = new ControllerFocusEntry[entryCount];
            int writeIndex = CopyFocusEntries(CommandPageSharedFocusEntries, entries, 0);
            for (int groupIndex = 0; groupIndex < contentGroups.Length; groupIndex++)
            {
                writeIndex = CopyFocusEntries(contentGroups[groupIndex], entries, writeIndex);
            }

            return entries;
        }

        private static int CopyFocusEntries(ControllerFocusEntry[] source, ControllerFocusEntry[] destination, int writeIndex)
        {
            for (int index = 0; index < source.Length; index++)
            {
                destination[writeIndex] = source[index];
                writeIndex++;
            }

            return writeIndex;
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

        private string GetAmmoModeStatusLabel()
        {
            if (_ammoModeToggleService == null)
            {
                return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }

            switch (_ammoModeToggleService.Mode)
            {
                case AmmoMode.NoConsume:
                    return GetLocalizedFallback("gui.command.status.ammo_mode.no_consume", "No Consume", "\u4e0d\u8017\u5f39");
                case AmmoMode.InfiniteReserve:
                    return GetLocalizedFallback("gui.command.status.ammo_mode.infinite_reserve", "Inf Reserve", "\u5907\u5f39\u65e0\u9650");
                case AmmoMode.Off:
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

        private static string GetNoConsumeActionLabel(bool isEnabled)
        {
            return isEnabled
                ? GetLocalizedFallback("gui.command.action.enable_no_consume", "Enable No Consume", "\u5f00\u542f\u4e0d\u6d88\u8017")
                : GetLocalizedFallback("gui.command.action.disable_no_consume", "Disable No Consume", "\u5173\u95ed\u4e0d\u6d88\u8017");
        }

        private string GetAmmonomiconFastOpenStatusLabel()
        {
            return IsAmmonomiconFastOpenEnabled()
                ? GetLocalizedFallback("gui.command.status.instant", "Instant", "\u77ac\u5f00")
                : GetLocalizedFallback("gui.command.status.animated", "Animated", "\u52a8\u753b");
        }

        private bool IsRapidFireEnabledFor(PlayerController player)
        {
            return _rapidFireToggleService != null && _rapidFireToggleService.IsEnabledFor(player);
        }

        private bool IsAutoReloadEnabled()
        {
            return _autoReloadToggleService != null && _autoReloadToggleService.Mode != AutoReloadMode.Off;
        }

        private bool IsAmmoModeEnabled()
        {
            return _ammoModeToggleService != null && _ammoModeToggleService.Mode != AmmoMode.Off;
        }

        private bool IsInvincibilityEnabled()
        {
            return _invincibilityToggleService != null && _invincibilityToggleService.IsEnabled;
        }

        private bool IsAmmonomiconFastOpenEnabled()
        {
            return _ammonomiconFastOpenToggleService != null && AmmonomiconFastOpenToggleService.IsFastOpenEnabled;
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

        private static string GetLocalizedFormattedFallback(string key, string englishFallback, string simplifiedChineseFallback, params object[] args)
        {
            string template = GetLocalizedFallback(key, englishFallback, simplifiedChineseFallback);
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args);
            }
            catch
            {
                return template;
            }
        }

        private bool IsExperimentalModeEnabled()
        {
            return _experimentalModeProvider != null && _experimentalModeProvider();
        }

        private void SetPlayerStatsPanelShown(bool isEnabled)
        {
            _showPlayerStatsPanel = isEnabled;
            if (_playerStatsPanelShownSetter != null)
            {
                _playerStatsPanelShownSetter(isEnabled);
            }
        }

        private void SetPickupInfoOverlayShown(bool isEnabled)
        {
            _showPickupInfoOverlay = isEnabled;
            if (_pickupInfoOverlayEnabledSetter != null)
            {
                _pickupInfoOverlayEnabledSetter(isEnabled);
            }
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
            HandleLanguageChanged();
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
