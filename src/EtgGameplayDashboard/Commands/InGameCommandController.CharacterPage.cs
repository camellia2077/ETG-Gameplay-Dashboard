// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using BepInEx.Logging;
using EtgGameplayDashboard.Core;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private void DrawCharacterPage(Rect panelRect, FoyerCharacterOption[] characterOptions, string availabilityMessage, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            Rect modeButtonRect = new Rect(backButtonRect.x - ButtonGap - CharacterModeButtonWidth, panelRect.y + 12f, CharacterModeButtonWidth, 30f);
            Rect targetButtonRect = new Rect(modeButtonRect.x - ButtonGap - ButtonWidth, panelRect.y + 12f, ButtonWidth, 30f);
            GUIStyle targetButtonStyle = _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer
                ? _enabledButtonStyle
                : _buttonStyle;
            if (GUI.Button(targetButtonRect, GetCharacterSwitchTargetButtonLabel(), GetControllerButtonStyle("characters.target", targetButtonStyle)))
            {
                ToggleCharacterSwitchTarget(logger);
            }

            if (GUI.Button(modeButtonRect, GetCharacterModeButtonLabel(), GetControllerButtonStyle("characters.mode", _buttonStyle)))
            {
                ToggleCharacterActionMode(logger);
            }

            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("characters.back", _buttonStyle)))
            {
                CloseCharacterPage();
                return;
            }

            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 12f, targetButtonRect.x - panelRect.x - 24f, 24f), GuiText.Get("gui.characters.title"), _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.characters.hint.apply_mode"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetCharacterModeHint() + " " + GetCharacterSwitchTargetHint(),
                _hintStyle);
            float availabilityHeight = GetCharacterAvailabilityHeight(availabilityMessage, panelRect.width);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 80f, panelRect.width - 28f, availabilityHeight),
                availabilityMessage,
                _wrappedHintStyle);

            if (characterOptions.Length == 0)
            {
                return;
            }

            DrawCharacterButtons(panelRect, characterOptions, logger, 80f + availabilityHeight + 4f);
        }

        private void DrawCharacterButtons(Rect panelRect, FoyerCharacterOption[] characterOptions, ManualLogSource logger, float topOffset)
        {
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + topOffset, panelRect.width - 28f, 20f),
                GuiText.Get("gui.characters.select"),
                _hintStyle);

            for (int i = 0; i < characterOptions.Length; i++)
            {
                FoyerCharacterOption option = characterOptions[i];
                int row = i / CharacterButtonsPerRow;
                int column = i % CharacterButtonsPerRow;
                float buttonX = panelRect.x + 14f + (column * (CharacterButtonWidth + ButtonGap));
                float buttonY = panelRect.y + topOffset + 24f + (row * (34f + ButtonGap));
                Rect buttonRect = new Rect(buttonX, buttonY, CharacterButtonWidth, 34f);

                bool wasEnabled = GUI.enabled;
                GUI.enabled = !option.IsPending;
                string localizedLabel = GuiText.GetCharacterLabel(option.Label);
                string buttonLabel = option.IsSelected ? localizedLabel + " *" : localizedLabel;
                if (option.IsLocked && option.CanUnlock)
                {
                    buttonLabel = localizedLabel + " ?";
                }
                if (option.IsPending)
                {
                    buttonLabel = localizedLabel + " ...";
                }

                if (GUI.Button(buttonRect, buttonLabel, GetControllerButtonStyle(GetCharacterOptionControlId(i), _buttonStyle)))
                {
                    ExecuteSwitchCharacter(option, logger);
                }

                GUI.enabled = wasEnabled;
            }
        }

        private void DrawStatusOverlay(float panelHeight)
        {
            if (string.IsNullOrEmpty(_statusMessage) || Time.unscaledTime > _statusExpiresAt)
            {
                return;
            }

            Rect panelRect = GetMainPanelRect(panelHeight);
            float statusWidth = Mathf.Min(StatusMaxWidth, GetScaledScreenWidth() - 24f);
            GUIStyle style = GetStatusStyle();
            float statusHeight = Mathf.Max(StatusMinHeight, style.CalcHeight(new GUIContent(_statusMessage), statusWidth));
            Rect statusRect = new Rect(
                (GetScaledScreenWidth() - statusWidth) * 0.5f,
                panelRect.y - StatusGap - statusHeight,
                statusWidth,
                statusHeight);

            GUI.Box(statusRect, _statusMessage, style);
        }

        private GUIStyle GetStatusStyle()
        {
            switch (_statusSeverity)
            {
                case StatusSeverity.Failure:
                    return _statusErrorStyle;
                case StatusSeverity.Warning:
                    return _statusWarningStyle;
                case StatusSeverity.Information:
                    return _statusInformationStyle;
                case StatusSeverity.Success:
                default:
                    return _statusSuccessStyle;
            }
        }

        private float GetPanelHeight(FoyerCharacterOption[] characterOptions, string characterAvailability)
        {
            if (_currentPage != PanelPage.Characters)
            {
                return BasePanelHeight;
            }

            int buttonCount = characterOptions != null ? characterOptions.Length : 0;
            int rows = buttonCount > 0 ? ((buttonCount + CharacterButtonsPerRow - 1) / CharacterButtonsPerRow) : 0;
            return GetCharacterHeaderHeight(characterAvailability) +
                   (rows * (34f + ButtonGap)) +
                   CharacterPanelFooterHeight;
        }

        private float GetCharacterHeaderHeight(string availabilityMessage)
        {
            return CharacterPanelBaseHeaderHeight + GetCharacterAvailabilityHeight(availabilityMessage, PanelWidth);
        }

        private float GetCharacterAvailabilityHeight(string availabilityMessage, float panelWidth)
        {
            return _wrappedHintStyle != null
                ? _wrappedHintStyle.CalcHeight(new GUIContent(availabilityMessage ?? string.Empty), panelWidth - 28f)
                : 40f;
        }

        private void CloseCharacterPage()
        {
            _currentPage = PanelPage.Command;
            _focusInputField = true;
            ResetCharacterPageCache();
        }

        private ControllerFocusEntry[] GetCharacterPageFocusEntries(FoyerCharacterOption[] characterOptions)
        {
            int optionCount = characterOptions != null ? characterOptions.Length : 0;
            ControllerFocusEntry[] entries = new ControllerFocusEntry[3 + optionCount];
            entries[0] = new ControllerFocusEntry("characters.target", 0, 0);
            entries[1] = new ControllerFocusEntry("characters.mode", 0, 1);
            entries[2] = new ControllerFocusEntry("characters.back", 0, 2);

            for (int index = 0; index < optionCount; index++)
            {
                entries[index + 3] = new ControllerFocusEntry(
                    GetCharacterOptionControlId(index),
                    1 + (index / CharacterButtonsPerRow),
                    index % CharacterButtonsPerRow);
            }

            return entries;
        }

        private void ExecuteCharacterPageFocusedControl(ManualLogSource logger)
        {
            if (string.Equals(_characterPageFocusedControlId, "characters.target", StringComparison.Ordinal))
            {
                ToggleCharacterSwitchTarget(logger);
                return;
            }

            if (string.Equals(_characterPageFocusedControlId, "characters.mode", StringComparison.Ordinal))
            {
                ToggleCharacterActionMode(logger);
                return;
            }

            if (string.Equals(_characterPageFocusedControlId, "characters.back", StringComparison.Ordinal))
            {
                CloseCharacterPage();
                return;
            }

            int optionIndex = GetCharacterOptionIndexFromControlId(_characterPageFocusedControlId);
            if (optionIndex < 0 || optionIndex >= _cachedCharacterOptions.Length)
            {
                return;
            }

            FoyerCharacterOption option = _cachedCharacterOptions[optionIndex];
            if (option.IsPending)
            {
                return;
            }

            ExecuteSwitchCharacter(option, logger);
        }

        private static string GetCharacterOptionControlId(int optionIndex)
        {
            return "characters.option." + optionIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int GetCharacterOptionIndexFromControlId(string controlId)
        {
            const string prefix = "characters.option.";
            if (string.IsNullOrEmpty(controlId) || !controlId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return -1;
            }

            int optionIndex;
            return int.TryParse(controlId.Substring(prefix.Length), out optionIndex) ? optionIndex : -1;
        }
    }
}
