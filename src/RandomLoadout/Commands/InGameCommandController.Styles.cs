// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        internal void RefreshTheme()
        {
            _panelStyle = null;
            _playerStatsPanelStyle = null;
            _playerStatsRowStyle = null;
            _titleStyle = null;
            _hintStyle = null;
            _combatSettingLabelStyle = null;
            _playerStatsTextStyle = null;
            _wrappedHintStyle = null;
            _textFieldStyle = null;
            _buttonStyle = null;
            _cursorColorSelectedButtonStyle = null;
            _enabledButtonStyle = null;
            _pickupGrantButtonStyle = null;
            _disabledToggleButtonStyle = null;
            _commandCategoryButtonStyle = null;
            _commandCategoryFocusButtonStyle = null;
            _commandCategoryActiveButtonStyle = null;
            _commandContentButtonStyle = null;
            _commandContentFocusButtonStyle = null;
            _commandContentActiveButtonStyle = null;
            _commandContentActiveFocusButtonStyle = null;
            _headerActionButtonStyle = null;
            _headerActionFocusButtonStyle = null;
            _statusStyle = null;
            _statusSuccessStyle = null;
            _statusErrorStyle = null;
            _statusWarningStyle = null;
            _statusInformationStyle = null;
            _pickupRowStyle = null;
            _loadoutEditorRowStyle = null;
            _pickupBrowserRowStyle = null;
            _activePresetRowStyle = null;
            _pickupRowButtonStyle = null;
            _pickupPrimaryTextStyle = null;
            _pickupSecondaryTextStyle = null;
            _pickupSecondaryActiveTextStyle = null;
            _activePresetAccentTextStyle = null;
            _pickupFilterButtonStyle = null;
            _pickupFilterFocusButtonStyle = null;
            _pickupFilterActiveButtonStyle = null;
            _pickupFilterActiveFocusButtonStyle = null;
            _pickupFilterDisabledButtonStyle = null;
            _pickupIconBackgroundStyle = null;
            _pickupIconFallbackStyle = null;
            _modalOverlayStyle = null;
            _modalPanelStyle = null;
            _modalBodyStyle = null;
            _settingsInfoTextStyle = null;
            _controllerHelpTitleStyle = null;
            _controllerHelpTextStyle = null;
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeTripleBorderedTexture(
                PanelBackgroundColor,
                PanelBorderColor,
                DashboardTheme.PanelMiddleBorder,
                InnerBorderColor,
                5,
                7,
                5);
            _panelStyle.border = new RectOffset(17, 17, 17, 17);
            _panelStyle.padding = new RectOffset(12, 12, 12, 12);

            _playerStatsPanelStyle = new GUIStyle(GUI.skin.box);
            _playerStatsPanelStyle.normal.background = null;
            _playerStatsPanelStyle.border = new RectOffset(0, 0, 0, 0);
            _playerStatsPanelStyle.padding = new RectOffset(12, 12, 12, 12);

            _playerStatsRowStyle = new GUIStyle(GUI.skin.box);
            _playerStatsRowStyle.normal.background = null;
            _playerStatsRowStyle.border = new RectOffset(0, 0, 0, 0);
            _playerStatsRowStyle.padding = new RectOffset(0, 0, 0, 0);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.normal.textColor = PrimaryTextColor;
            _titleStyle.fontSize = 18;
            _titleStyle.fontStyle = FontStyle.Bold;

            _hintStyle = new GUIStyle(GUI.skin.label);
            _hintStyle.normal.textColor = SecondaryTextColor;
            _hintStyle.fontSize = 14;

            _combatSettingLabelStyle = new GUIStyle(_hintStyle);
            _combatSettingLabelStyle.fontSize = 16;
            _combatSettingLabelStyle.alignment = TextAnchor.MiddleLeft;
            _combatSettingLabelStyle.wordWrap = false;

            _playerStatsTextStyle = new GUIStyle(_hintStyle);
            _playerStatsTextStyle.normal.textColor = PlayerStatsTextColor;
            _playerStatsTextStyle.fontSize = 17;

            _wrappedHintStyle = new GUIStyle(_hintStyle);
            _wrappedHintStyle.wordWrap = true;

            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.normal.background = MakeTexture(1, 1, InputBackgroundColor);
            _textFieldStyle.focused.background = MakeTexture(1, 1, InputBackgroundColor);
            _textFieldStyle.normal.textColor = PrimaryTextColor;
            _textFieldStyle.focused.textColor = PrimaryTextColor;
            _textFieldStyle.border = new RectOffset(2, 2, 2, 2);
            _textFieldStyle.padding = new RectOffset(10, 10, 7, 7);
            _textFieldStyle.alignment = TextAnchor.MiddleLeft;
            _textFieldStyle.fontSize = 15;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.background = MakeInsetBorderedTexture(
                DashboardTheme.NonFunctionalButtonBackground,
                DashboardTheme.NonFunctionalButtonBorder,
                2,
                2);
            _buttonStyle.hover.background = MakeInsetBorderedTexture(
                ButtonHoverColor,
                DashboardTheme.NonFunctionalButtonSelectedBorder,
                2,
                2);
            _buttonStyle.active.background = MakeInsetBorderedTexture(
                ButtonActiveColor,
                DashboardTheme.NonFunctionalButtonSelectedBorder,
                2,
                2);
            _buttonStyle.normal.textColor = PrimaryTextColor;
            _buttonStyle.hover.textColor = PrimaryTextColor;
            _buttonStyle.active.textColor = PrimaryTextColor;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.fontSize = 14;

            // Cursor Color uses the theme's selected border as its enabled state.
            // Keep the ordinary button fills unchanged so this UI does not switch
            // to the theme's semantic Secondary fill when toggled on.
            _cursorColorSelectedButtonStyle = new GUIStyle(_buttonStyle);
            _cursorColorSelectedButtonStyle.normal.background = MakeInsetBorderedTexture(
                DashboardTheme.NonFunctionalButtonBackground,
                DashboardTheme.NonFunctionalButtonSelectedBorder,
                2,
                2);
            _cursorColorSelectedButtonStyle.hover.background = MakeInsetBorderedTexture(
                ButtonHoverColor,
                DashboardTheme.NonFunctionalButtonSelectedBorder,
                2,
                2);
            _cursorColorSelectedButtonStyle.active.background = MakeInsetBorderedTexture(
                ButtonActiveColor,
                DashboardTheme.NonFunctionalButtonSelectedBorder,
                2,
                2);

            _enabledButtonStyle = new GUIStyle(_buttonStyle);
            _enabledButtonStyle.normal.background = MakeInsetBorderedTexture(EnabledButtonBackgroundColor, ButtonSelectedBorderColor, 2, 2);
            _enabledButtonStyle.hover.background = MakeInsetBorderedTexture(EnabledButtonHoverColor, ButtonSelectedBorderColor, 2, 2);
            _enabledButtonStyle.active.background = MakeInsetBorderedTexture(EnabledButtonActiveColor, ButtonSelectedBorderColor, 2, 2);
            _enabledButtonStyle.normal.textColor = DashboardTheme.EnabledButtonText;
            _enabledButtonStyle.hover.textColor = DashboardTheme.EnabledButtonText;
            _enabledButtonStyle.active.textColor = DashboardTheme.EnabledButtonText;

            _pickupGrantButtonStyle = new GUIStyle(_buttonStyle);
            _pickupGrantButtonStyle.normal.background = MakeInsetBorderedTexture(
                EnabledButtonBackgroundColor,
                DashboardTheme.Outline,
                2,
                2);
            _pickupGrantButtonStyle.hover.background = MakeInsetBorderedTexture(
                EnabledButtonHoverColor,
                ButtonSelectedBorderColor,
                2,
                2);
            _pickupGrantButtonStyle.active.background = MakeInsetBorderedTexture(
                EnabledButtonActiveColor,
                ButtonSelectedBorderColor,
                2,
                2);
            Color pickupGrantTextColor = DashboardTheme.GetContrastingText(EnabledButtonBackgroundColor);
            _pickupGrantButtonStyle.normal.textColor = pickupGrantTextColor;
            _pickupGrantButtonStyle.hover.textColor = pickupGrantTextColor;
            _pickupGrantButtonStyle.active.textColor = pickupGrantTextColor;

            _disabledToggleButtonStyle = new GUIStyle(_buttonStyle);
            _disabledToggleButtonStyle.normal.background = MakeTexture(1, 1, ButtonBackgroundColor);
            _disabledToggleButtonStyle.hover.background = MakeTexture(1, 1, ButtonHoverColor);
            _disabledToggleButtonStyle.active.background = MakeTexture(1, 1, ButtonActiveColor);
            _disabledToggleButtonStyle.border = new RectOffset(0, 0, 0, 0);
            _disabledToggleButtonStyle.normal.textColor = PrimaryTextColor;
            _disabledToggleButtonStyle.hover.textColor = PrimaryTextColor;
            _disabledToggleButtonStyle.active.textColor = PrimaryTextColor;

            _commandCategoryButtonStyle = new GUIStyle(_buttonStyle);
            _commandCategoryButtonStyle.normal.background = MakeInsetBorderedTexture(CommandCategoryButtonBackgroundColor, CommandCategoryButtonBorderColor, 2, 2);
            _commandCategoryButtonStyle.hover.background = MakeInsetBorderedTexture(CommandCategoryHoverButtonBackgroundColor, CommandCategoryHoverButtonBorderColor, 2, 2);
            _commandCategoryButtonStyle.active.background = _commandCategoryButtonStyle.hover.background;
            _commandCategoryButtonStyle.normal.textColor = DashboardTheme.CommandCategoryNormalText;
            _commandCategoryButtonStyle.hover.textColor = DashboardTheme.CommandCategoryNormalText;
            _commandCategoryButtonStyle.active.textColor = DashboardTheme.CommandCategoryNormalText;

            _commandCategoryFocusButtonStyle = new GUIStyle(_commandCategoryButtonStyle);
            _commandCategoryFocusButtonStyle.normal.background = MakeInsetBorderedTexture(CommandCategoryActiveButtonBackgroundColor, CommandCategoryActiveButtonBorderColor, 2, 2);
            _commandCategoryFocusButtonStyle.hover.background = _commandCategoryFocusButtonStyle.normal.background;
            _commandCategoryFocusButtonStyle.active.background = _commandCategoryFocusButtonStyle.normal.background;
            _commandCategoryFocusButtonStyle.normal.textColor = DashboardTheme.CommandCategorySelectedText;
            _commandCategoryFocusButtonStyle.hover.textColor = DashboardTheme.CommandCategorySelectedText;
            _commandCategoryFocusButtonStyle.active.textColor = DashboardTheme.CommandCategorySelectedText;

            _commandCategoryActiveButtonStyle = new GUIStyle(_commandCategoryButtonStyle);
            _commandCategoryActiveButtonStyle.normal.background = MakeInsetBorderedTexture(CommandCategoryActiveButtonBackgroundColor, CommandCategoryActiveButtonBorderColor, 2, 2);
            _commandCategoryActiveButtonStyle.hover.background = _commandCategoryActiveButtonStyle.normal.background;
            _commandCategoryActiveButtonStyle.active.background = _commandCategoryActiveButtonStyle.normal.background;
            _commandCategoryActiveButtonStyle.normal.textColor = DashboardTheme.CommandCategorySelectedText;
            _commandCategoryActiveButtonStyle.hover.textColor = DashboardTheme.CommandCategorySelectedText;
            _commandCategoryActiveButtonStyle.active.textColor = DashboardTheme.CommandCategorySelectedText;

            _commandContentButtonStyle = new GUIStyle(_buttonStyle);
            _commandContentButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.NonFunctionalButtonBackground, DashboardTheme.NonFunctionalButtonBorder, 2, 2);
            _commandContentButtonStyle.hover.background = MakeInsetBorderedTexture(ButtonHoverColor, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _commandContentButtonStyle.active.background = MakeInsetBorderedTexture(ButtonActiveColor, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);

            _commandContentFocusButtonStyle = new GUIStyle(_commandContentButtonStyle);
            _commandContentFocusButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.ControllerFocusButtonBackground, DashboardTheme.NonFunctionalButtonBorder, 2, 2);
            _commandContentFocusButtonStyle.hover.background = _commandContentFocusButtonStyle.normal.background;
            _commandContentFocusButtonStyle.active.background = _commandContentFocusButtonStyle.normal.background;
            _commandContentFocusButtonStyle.normal.textColor = DashboardTheme.GetContrastingText(DashboardTheme.ControllerFocusButtonBackground);
            _commandContentFocusButtonStyle.hover.textColor = _commandContentFocusButtonStyle.normal.textColor;
            _commandContentFocusButtonStyle.active.textColor = _commandContentFocusButtonStyle.normal.textColor;

            _commandContentActiveButtonStyle = new GUIStyle(_commandContentButtonStyle);
            _commandContentActiveButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.NonFunctionalButtonBackground, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _commandContentActiveButtonStyle.hover.background = MakeInsetBorderedTexture(ButtonHoverColor, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _commandContentActiveButtonStyle.active.background = MakeInsetBorderedTexture(ButtonActiveColor, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);

            _commandContentActiveFocusButtonStyle = new GUIStyle(_commandContentActiveButtonStyle);
            _commandContentActiveFocusButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.ControllerFocusButtonBackground, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _commandContentActiveFocusButtonStyle.hover.background = _commandContentActiveFocusButtonStyle.normal.background;
            _commandContentActiveFocusButtonStyle.active.background = _commandContentActiveFocusButtonStyle.normal.background;
            _commandContentActiveFocusButtonStyle.normal.textColor = DashboardTheme.GetContrastingText(DashboardTheme.ControllerFocusButtonBackground);
            _commandContentActiveFocusButtonStyle.hover.textColor = _commandContentActiveFocusButtonStyle.normal.textColor;
            _commandContentActiveFocusButtonStyle.active.textColor = _commandContentActiveFocusButtonStyle.normal.textColor;

            _headerActionButtonStyle = new GUIStyle(_buttonStyle);
            _headerActionButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.HeaderActionBackground, DashboardTheme.HeaderActionBorder, 2, 2);
            _headerActionButtonStyle.hover.background = MakeInsetBorderedTexture(DashboardTheme.HeaderActionHoverBackground, DashboardTheme.HeaderActionHoverBorder, 2, 2);
            _headerActionButtonStyle.active.background = MakeInsetBorderedTexture(DashboardTheme.HeaderActionHoverBackground, DashboardTheme.HeaderActionHoverBorder, 2, 2);

            _headerActionFocusButtonStyle = new GUIStyle(_headerActionButtonStyle);
            _headerActionFocusButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.ControllerFocusButtonBackground, DashboardTheme.HeaderActionBorder, 2, 2);
            _headerActionFocusButtonStyle.hover.background = _headerActionFocusButtonStyle.normal.background;
            _headerActionFocusButtonStyle.active.background = _headerActionFocusButtonStyle.normal.background;
            _headerActionFocusButtonStyle.normal.textColor = DashboardTheme.GetContrastingText(DashboardTheme.ControllerFocusButtonBackground);
            _headerActionFocusButtonStyle.hover.textColor = _headerActionFocusButtonStyle.normal.textColor;
            _headerActionFocusButtonStyle.active.textColor = _headerActionFocusButtonStyle.normal.textColor;

            _statusStyle = new GUIStyle(GUI.skin.box);
            _statusStyle.normal.textColor = PrimaryTextColor;
            _statusStyle.alignment = TextAnchor.MiddleCenter;
            _statusStyle.fontSize = 14;
            _statusStyle.padding = new RectOffset(10, 10, 6, 6);
            _statusStyle.wordWrap = true;

            _statusSuccessStyle = new GUIStyle(_statusStyle);
            _statusSuccessStyle.normal.background = MakeTexture(1, 1, StatusSuccessBackgroundColor);
            _statusSuccessStyle.normal.textColor = Color.white;

            _statusErrorStyle = new GUIStyle(_statusStyle);
            _statusErrorStyle.normal.background = MakeTexture(1, 1, StatusFailureBackgroundColor);
            _statusErrorStyle.normal.textColor = Color.white;

            _statusWarningStyle = new GUIStyle(_statusStyle);
            _statusWarningStyle.normal.background = MakeTexture(1, 1, StatusWarningBackgroundColor);
            _statusWarningStyle.normal.textColor = Color.white;

            _statusInformationStyle = new GUIStyle(_statusStyle);
            _statusInformationStyle.normal.background = MakeTexture(1, 1, StatusInformationBackgroundColor);
            _statusInformationStyle.normal.textColor = Color.white;

            _pickupRowStyle = new GUIStyle(GUI.skin.box);
            _pickupRowStyle.normal.background = MakeTexture(1, 1, PanelBackgroundColor);
            _pickupRowStyle.border = new RectOffset(1, 1, 1, 1);

            _loadoutEditorRowStyle = new GUIStyle(GUI.skin.box);
            _loadoutEditorRowStyle.normal.background = MakeBorderedTexture(
                PanelBackgroundColor,
                ButtonBorderColor,
                1);
            _loadoutEditorRowStyle.border = new RectOffset(1, 1, 1, 1);

            _pickupBrowserRowStyle = new GUIStyle(GUI.skin.box);
            _pickupBrowserRowStyle.normal.background = MakeBorderedTexture(
                PanelBackgroundColor,
                DashboardTheme.ItemRowBorder,
                2);
            _pickupBrowserRowStyle.border = new RectOffset(2, 2, 2, 2);

            _activePresetRowStyle = new GUIStyle(_pickupRowStyle);
            _activePresetRowStyle.normal.background = MakeBorderedTexture(
                PanelBackgroundColor,
                ButtonSelectedBorderColor,
                1);
            _activePresetRowStyle.border = new RectOffset(1, 1, 1, 1);

            _pickupRowButtonStyle = new GUIStyle(GUI.skin.button);
            _pickupRowButtonStyle.normal.background = MakeTexture(1, 1, new Color(0f, 0f, 0f, 0f));
            _pickupRowButtonStyle.hover.background = MakeTexture(1, 1, DashboardTheme.PickupRowHoverBackground);
            _pickupRowButtonStyle.active.background = MakeTexture(1, 1, DashboardTheme.PickupRowActiveBackground);

            _pickupPrimaryTextStyle = new GUIStyle(GUI.skin.label);
            _pickupPrimaryTextStyle.normal.textColor = PrimaryTextColor;
            _pickupPrimaryTextStyle.fontSize = 14;
            _pickupPrimaryTextStyle.fontStyle = FontStyle.Bold;

            _pickupSecondaryTextStyle = new GUIStyle(GUI.skin.label);
            _pickupSecondaryTextStyle.normal.textColor = SecondaryTextColor;
            _pickupSecondaryTextStyle.fontSize = 11;

            _pickupSecondaryActiveTextStyle = new GUIStyle(_pickupSecondaryTextStyle);
            _pickupSecondaryActiveTextStyle.normal.textColor = PrimaryTextColor;

            _activePresetAccentTextStyle = new GUIStyle(_pickupPrimaryTextStyle);
            _activePresetAccentTextStyle.normal.textColor = SecondaryColor;

            _pickupFilterButtonStyle = new GUIStyle(_buttonStyle);
            _pickupFilterButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.NonFunctionalButtonBackground, DashboardTheme.NonFunctionalButtonBackground, 2, 2);
            _pickupFilterButtonStyle.hover.background = MakeInsetBorderedTexture(ButtonHoverColor, DashboardTheme.NonFunctionalButtonBackground, 2, 2);
            _pickupFilterButtonStyle.active.background = MakeInsetBorderedTexture(ButtonActiveColor, DashboardTheme.NonFunctionalButtonBackground, 2, 2);
            _pickupFilterButtonStyle.fontSize = 13;

            _pickupFilterFocusButtonStyle = new GUIStyle(_pickupFilterButtonStyle);
            _pickupFilterFocusButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.ControllerFocusButtonBackground, DashboardTheme.NonFunctionalButtonBackground, 2, 2);
            _pickupFilterFocusButtonStyle.hover.background = _pickupFilterFocusButtonStyle.normal.background;
            _pickupFilterFocusButtonStyle.active.background = _pickupFilterFocusButtonStyle.normal.background;
            _pickupFilterFocusButtonStyle.normal.textColor = DashboardTheme.GetContrastingText(DashboardTheme.ControllerFocusButtonBackground);
            _pickupFilterFocusButtonStyle.hover.textColor = _pickupFilterFocusButtonStyle.normal.textColor;
            _pickupFilterFocusButtonStyle.active.textColor = _pickupFilterFocusButtonStyle.normal.textColor;

            _pickupFilterActiveButtonStyle = new GUIStyle(_buttonStyle);
            _pickupFilterActiveButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.NonFunctionalButtonBackground, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _pickupFilterActiveButtonStyle.hover.background = MakeInsetBorderedTexture(ButtonHoverColor, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _pickupFilterActiveButtonStyle.active.background = MakeInsetBorderedTexture(ButtonActiveColor, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _pickupFilterActiveButtonStyle.fontSize = 13;

            _pickupFilterActiveFocusButtonStyle = new GUIStyle(_pickupFilterActiveButtonStyle);
            _pickupFilterActiveFocusButtonStyle.normal.background = MakeInsetBorderedTexture(DashboardTheme.ControllerFocusButtonBackground, DashboardTheme.NonFunctionalButtonSelectedBorder, 2, 2);
            _pickupFilterActiveFocusButtonStyle.hover.background = _pickupFilterActiveFocusButtonStyle.normal.background;
            _pickupFilterActiveFocusButtonStyle.active.background = _pickupFilterActiveFocusButtonStyle.normal.background;
            _pickupFilterActiveFocusButtonStyle.normal.textColor = DashboardTheme.GetContrastingText(DashboardTheme.ControllerFocusButtonBackground);
            _pickupFilterActiveFocusButtonStyle.hover.textColor = _pickupFilterActiveFocusButtonStyle.normal.textColor;
            _pickupFilterActiveFocusButtonStyle.active.textColor = _pickupFilterActiveFocusButtonStyle.normal.textColor;

            _pickupFilterDisabledButtonStyle = new GUIStyle(_buttonStyle);
            _pickupFilterDisabledButtonStyle.normal.background = MakeTexture(1, 1, DisabledButtonBackgroundColor);
            _pickupFilterDisabledButtonStyle.hover.background = _pickupFilterDisabledButtonStyle.normal.background;
            _pickupFilterDisabledButtonStyle.active.background = _pickupFilterDisabledButtonStyle.normal.background;
            _pickupFilterDisabledButtonStyle.border = new RectOffset(0, 0, 0, 0);
            _pickupFilterDisabledButtonStyle.normal.textColor = PanelBackgroundColor;
            _pickupFilterDisabledButtonStyle.hover.textColor = _pickupFilterDisabledButtonStyle.normal.textColor;
            _pickupFilterDisabledButtonStyle.active.textColor = _pickupFilterDisabledButtonStyle.normal.textColor;
            _pickupFilterDisabledButtonStyle.fontSize = 13;

            _pickupIconBackgroundStyle = new GUIStyle(GUI.skin.box);
            _pickupIconBackgroundStyle.normal.background = MakeTexture(1, 1, PanelBackgroundColor);
            _pickupIconBackgroundStyle.border = new RectOffset(0, 0, 0, 0);

            _pickupIconFallbackStyle = new GUIStyle(_pickupIconBackgroundStyle);
            _pickupIconFallbackStyle.normal.textColor = PrimaryTextColor;
            _pickupIconFallbackStyle.alignment = TextAnchor.MiddleCenter;
            _pickupIconFallbackStyle.fontStyle = FontStyle.Bold;

            _modalOverlayStyle = new GUIStyle(GUI.skin.box);
            _modalOverlayStyle.normal.background = MakeTexture(1, 1, DashboardTheme.ModalOverlay);
            _modalOverlayStyle.border = new RectOffset(0, 0, 0, 0);

            _modalPanelStyle = new GUIStyle(GUI.skin.box);
            _modalPanelStyle.normal.background = MakeTripleBorderedTexture(
                PanelBackgroundColor,
                PanelBorderColor,
                DashboardTheme.PanelMiddleBorder,
                InnerBorderColor,
                5,
                7,
                5);
            _modalPanelStyle.border = new RectOffset(17, 17, 17, 17);
            _modalPanelStyle.padding = new RectOffset(14, 14, 14, 14);

            _modalBodyStyle = new GUIStyle(_hintStyle);
            _modalBodyStyle.wordWrap = true;

            _settingsInfoTextStyle = new GUIStyle(_hintStyle);
            _settingsInfoTextStyle.normal.textColor = PrimaryTextColor;
            _settingsInfoTextStyle.wordWrap = true;

            _controllerHelpTitleStyle = new GUIStyle(_titleStyle);
            _controllerHelpTitleStyle.normal.textColor = PrimaryTextColor;

            _controllerHelpTextStyle = new GUIStyle(_hintStyle);
            _controllerHelpTextStyle.normal.textColor = PrimaryTextColor;

        }

        private Vector2 BeginCommandScrollView(Rect position, Vector2 scrollPosition, Rect viewRect)
        {
            return SharedScrollViewStyles.Begin(position, scrollPosition, viewRect);
        }

        private sealed class PickupBrowserEntry
        {
            public PickupBrowserEntry(
                EtgPickupCatalogEntry catalogEntry,
                IList<string> aliases,
                Func<int, string> pickupGameplayNameProvider)
            {
                CatalogEntry = catalogEntry;
                string gameplayDisplayName = pickupGameplayNameProvider != null && catalogEntry != null
                    ? pickupGameplayNameProvider(catalogEntry.PickupId)
                    : string.Empty;
                DisplayName = !string.IsNullOrEmpty(gameplayDisplayName)
                    ? gameplayDisplayName
                    : ResolveDisplayName(catalogEntry);
                Aliases = aliases != null ? ToArray(aliases) : new string[0];
                PreferredInput = Aliases.Length > 0
                    ? Aliases[0]
                    : (!string.IsNullOrEmpty(catalogEntry.InternalName)
                        ? catalogEntry.InternalName.ToLowerInvariant()
                        : catalogEntry.PickupId.ToString());
                CommandText = BuildCommandText(catalogEntry.Category, PreferredInput);
                MetadataLine = BuildMetadataLine(catalogEntry, Aliases, PreferredInput);
                SearchText = BuildSearchText(catalogEntry, Aliases, PreferredInput);
                IconFallbackLabel = GetCategoryInitial(catalogEntry.Category);
            }

            public EtgPickupCatalogEntry CatalogEntry { get; private set; }

            public string DisplayName { get; private set; }

            public string[] Aliases { get; private set; }

            public string PreferredInput { get; private set; }

            public string CommandText { get; private set; }

            public string MetadataLine { get; private set; }

            public string SearchText { get; private set; }

            public string IconFallbackLabel { get; private set; }

            private static string[] ToArray(IList<string> aliases)
            {
                string[] values = new string[aliases.Count];
                for (int index = 0; index < aliases.Count; index++)
                {
                    values[index] = aliases[index] ?? string.Empty;
                }

                return values;
            }

            private static string ResolveDisplayName(EtgPickupCatalogEntry catalogEntry)
            {
                if (catalogEntry == null)
                {
                    return string.Empty;
                }

                if (string.Equals(GuiText.CurrentLanguageCode, "en", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(catalogEntry.EnglishDisplayName))
                {
                    return catalogEntry.EnglishDisplayName;
                }

                if (!string.IsNullOrEmpty(catalogEntry.DisplayName))
                {
                    return catalogEntry.DisplayName;
                }

                return !string.IsNullOrEmpty(catalogEntry.EnglishDisplayName)
                    ? catalogEntry.EnglishDisplayName
                    : catalogEntry.InternalName;
            }

            private static string BuildCommandText(PickupCategory category, string preferredInput)
            {
                switch (category)
                {
                    case PickupCategory.Gun:
                        return "gun " + preferredInput;
                    case PickupCategory.Passive:
                        return "passive " + preferredInput;
                    case PickupCategory.Active:
                        return "active " + preferredInput;
                    default:
                        return preferredInput;
                }
            }

            private static string BuildMetadataLine(EtgPickupCatalogEntry catalogEntry, string[] aliases, string preferredInput)
            {
                string metadata = GuiText.GetCategoryLabel(catalogEntry.Category) + " | " + GuiText.Get("gui.pickups.metadata.id") + " " + catalogEntry.PickupId + " | " + preferredInput;
                if (aliases.Length > 1)
                {
                    metadata += " | " + GuiText.Get("gui.pickups.metadata.aliases") + ": " + string.Join(", ", aliases);
                }
                else if (aliases.Length == 1 && !string.Equals(aliases[0], preferredInput, StringComparison.OrdinalIgnoreCase))
                {
                    metadata += " | " + GuiText.Get("gui.pickups.metadata.alias") + ": " + aliases[0];
                }
                else if (!string.IsNullOrEmpty(catalogEntry.InternalName) &&
                         !string.Equals(catalogEntry.InternalName, preferredInput, StringComparison.OrdinalIgnoreCase))
                {
                    metadata += " | " + catalogEntry.InternalName;
                }

                return metadata;
            }

            private static string BuildSearchText(EtgPickupCatalogEntry catalogEntry, string[] aliases, string preferredInput)
            {
                string combined = catalogEntry.DisplayName + "|" +
                                  catalogEntry.EnglishDisplayName + "|" +
                                  catalogEntry.InternalName + "|" +
                                  catalogEntry.PickupId + "|" +
                                  preferredInput + "|" +
                                  string.Join("|", aliases);
                return NormalizeLookupValue(combined);
            }

            private static string GetCategoryInitial(PickupCategory category)
            {
                switch (category)
                {
                    case PickupCategory.Gun:
                        return "G";
                    case PickupCategory.Passive:
                        return "P";
                    case PickupCategory.Active:
                        return "A";
                    default:
                        return "?";
                }
            }
        }

        private struct PickupIconData
        {
            public static readonly PickupIconData Empty = new PickupIconData(null, Rect.zero);

            public PickupIconData(Texture texture, Rect textureCoords)
            {
                Texture = texture;
                TextureCoords = textureCoords;
            }

            public Texture Texture;

            public Rect TextureCoords;
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeBorderedTexture(Color fillColor, Color borderColor)
        {
            return MakeBorderedTexture(fillColor, borderColor, 1);
        }

        private static Texture2D MakeBorderedTexture(Color fillColor, Color borderColor, int borderThickness)
        {
            Texture2D texture = new Texture2D(8, 8);
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[64];
            int thickness = Mathf.Clamp(borderThickness, 1, 3);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorderPixel = x < thickness || x >= 8 - thickness || y < thickness || y >= 8 - thickness;
                    pixels[(y * 8) + x] = isBorderPixel ? borderColor : fillColor;
                }
            }

            texture.SetPixels(pixels);
            // Keep the bordered texture crisp when Unity stretches it with nine-slice rendering;
            // bilinear sampling would blend the border color into the solid fill.
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeInsetBorderedTexture(
            Color fillColor,
            Color borderColor,
            int inset,
            int borderThickness)
        {
            Texture2D texture = new Texture2D(16, 16);
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[256];
            int borderInset = Mathf.Clamp(inset, 1, 5);
            int thickness = Mathf.Clamp(borderThickness, 1, 4);
            int borderStart = borderInset;
            int borderEnd = 16 - borderInset;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isBorderPixel = x >= borderStart
                        && x < borderStart + thickness
                        && y >= borderStart
                        && y < borderEnd
                        || x < borderEnd
                        && x >= borderEnd - thickness
                        && y >= borderStart
                        && y < borderEnd
                        || y >= borderStart
                        && y < borderStart + thickness
                        && x >= borderStart
                        && x < borderEnd
                        || y < borderEnd
                        && y >= borderEnd - thickness
                        && x >= borderStart
                        && x < borderEnd;

                    pixels[(y * 16) + x] = isBorderPixel ? borderColor : fillColor;
                }
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeTripleBorderedTexture(
            Color fillColor,
            Color outerBorderColor,
            Color middleBorderColor,
            Color innerBorderColor,
            int outerBorderThickness,
            int middleBorderThickness,
            int innerBorderThickness)
        {
            Texture2D texture = new Texture2D(32, 32);
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[1024];
            int outerThickness = Mathf.Clamp(outerBorderThickness, 1, 4);
            int middleThickness = Mathf.Clamp(middleBorderThickness, 1, 8);
            int innerThickness = Mathf.Clamp(innerBorderThickness, 1, 4);
            int middleStart = outerThickness;
            int innerStart = middleStart + middleThickness;
            int innerEnd = 32 - innerStart;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isOuterBorder = x < outerThickness
                        || x >= 32 - outerThickness
                        || y < outerThickness
                        || y >= 32 - outerThickness;
                    bool isMiddleBorder = x >= middleStart
                        && x < middleStart + middleThickness
                        || x < 32 - middleStart
                        && x >= 32 - middleStart - middleThickness
                        || y >= middleStart
                        && y < middleStart + middleThickness
                        || y < 32 - middleStart
                        && y >= 32 - middleStart - middleThickness;
                    bool isInnerBorder = x >= innerStart
                        && x < innerStart + innerThickness
                        || x < innerEnd
                        && x >= innerEnd - innerThickness
                        || y >= innerStart
                        && y < innerStart + innerThickness
                        || y < innerEnd
                        && y >= innerEnd - innerThickness;

                    pixels[(y * 32) + x] = isOuterBorder
                        ? outerBorderColor
                        : (isMiddleBorder
                            ? middleBorderColor
                            : (isInnerBorder ? innerBorderColor : fillColor));
                }
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }
    }
}
