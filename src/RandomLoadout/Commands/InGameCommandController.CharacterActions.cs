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
        private void OpenCharacterPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Characters;
            if (_characterSwitchTarget == CharacterSwitchTarget.BothPlayers)
            {
                _characterSwitchTarget = CharacterSwitchTarget.PrimaryPlayer;
            }
            _focusInputField = false;
            _characterPageFocusedControlId = "characters.mode";
            RefreshCharacterPageData(true);

            if (logger == null)
            {
                return;
            }

            if (!string.Equals(_lastCharacterAvailabilityLog, _cachedCharacterAvailability, StringComparison.Ordinal))
            {
                _lastCharacterAvailabilityLog = _cachedCharacterAvailability;
                logger.LogInfo(RandomLoadoutLog.Command("Character page opened. " + _cachedCharacterAvailability));
            }
        }

        private void ExecuteSwitchCharacter(FoyerCharacterOption option, ManualLogSource logger)
        {
            if (logger != null && option != null && string.Equals(option.Label, "Pilot", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInfo(RandomLoadoutLog.Command(
                    "Pilot button activated. Mode=" + _characterActionMode +
                    ", Target=" + (_characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer ? "P2" : "P1") +
                    ", Selectable=" + option.IsSelectable +
                    ", Selected=" + option.IsSelected +
                    ", Pending=" + option.IsPending + "."));
            }

            GrantCommandExecutionResult executionResult = _characterActionMode == CharacterActionMode.Unlock
                ? _foyerCharacterSwitchService.UnlockCharacter(option)
                : _foyerCharacterSwitchService.SwitchCharacterOnly(option, _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            RefreshCharacterPageData(true);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private string GetCharacterModeButtonLabel()
        {
            return _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.Get("gui.characters.mode.unlock")
                : GuiText.Get("gui.characters.mode.switch_only");
        }

        private string GetCharacterModeHint()
        {
            return _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.Get("gui.characters.hint.unlock")
                : GuiText.Get("gui.characters.hint.switch_only");
        }

        private string GetCharacterSwitchTargetHint()
        {
            return GuiText.Get(
                "gui.characters.hint.target",
                GetCharacterSwitchTargetDisplayLabel());
        }

        private string GetCharacterSwitchTargetButtonLabel()
        {
            return GuiText.Get(
                "gui.command.button.character_switch_target",
                GetCharacterSwitchTargetDisplayLabel());
        }

        private string GetCharacterSwitchTargetDisplayLabel()
        {
            if (_characterSwitchTarget == CharacterSwitchTarget.BothPlayers)
            {
                return GuiText.Get("gui.characters.target.both");
            }

            return _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer ? "P2" : "P1";
        }

        private string GetEnglishCharacterSwitchTargetDisplayLabel()
        {
            if (_characterSwitchTarget == CharacterSwitchTarget.BothPlayers)
            {
                return GuiText.GetEnglish("gui.characters.target.both");
            }

            return _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer ? "P2" : "P1";
        }

        private void ToggleCharacterSwitchTarget(ManualLogSource logger)
        {
            bool allowBothPlayers = IsTargetSelectionPage();
            if (_characterSwitchTarget == CharacterSwitchTarget.PrimaryPlayer)
            {
                _characterSwitchTarget = CharacterSwitchTarget.SecondaryPlayer;
            }
            else if (_characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer && allowBothPlayers)
            {
                _characterSwitchTarget = CharacterSwitchTarget.BothPlayers;
            }
            else
            {
                _characterSwitchTarget = CharacterSwitchTarget.PrimaryPlayer;
            }

            string targetLabel = GetCharacterSwitchTargetDisplayLabel();
            string englishTargetLabel = GetEnglishCharacterSwitchTargetDisplayLabel();
            ShowStatus(GuiText.Get("result.characters.target_changed", targetLabel), false);

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(GuiText.GetEnglish("result.characters.target_changed", englishTargetLabel)));
            }
        }

        private bool IsTargetSelectionPage()
        {
            if (_currentPage == PanelPage.Pickups)
            {
                return _pickupBrowserMode == PickupBrowserMode.Grant;
            }

            if (_currentPage == PanelPage.Currency)
            {
                return true;
            }

            // Player commands are always scoped to exactly one player. Both remains
            // available for General pickup/currency grants, but is intentionally not
            // offered for player-specific toggles and stats.
            if (_currentPage == PanelPage.Command && _commandMenuCategory == CommandMenuCategory.Player)
            {
                return false;
            }

            return false;
        }

        private void ToggleCharacterActionMode(ManualLogSource logger)
        {
            _characterActionMode = _characterActionMode == CharacterActionMode.Unlock
                ? CharacterActionMode.SwitchOnly
                : CharacterActionMode.Unlock;

            string modeMessage = _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.Get("result.characters.mode_changed_unlock")
                : GuiText.Get("result.characters.mode_changed_switch_only");
            string logMessage = _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.GetEnglish("result.characters.mode_changed_unlock")
                : GuiText.GetEnglish("result.characters.mode_changed_switch_only");
            ShowStatus(modeMessage, false);

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(logMessage));
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            ShowStatus(message, isError ? StatusSeverity.Failure : StatusSeverity.Success);
        }

        private void ShowStatus(string message, StatusSeverity severity)
        {
            _statusMessage = GetStatusPrefix(severity) + " " + message;
            _statusSeverity = severity;
            _statusExpiresAt = Time.unscaledTime + StatusDurationSeconds;
        }

        private static string GetStatusPrefix(StatusSeverity severity)
        {
            switch (severity)
            {
                case StatusSeverity.Failure:
                    return "×";
                case StatusSeverity.Warning:
                    return "!";
                case StatusSeverity.Information:
                    return "i";
                case StatusSeverity.Success:
                default:
                    return "✓";
            }
        }

        private void RefreshCharacterPageData(bool forceRefresh)
        {
            if (_foyerCharacterSwitchService == null)
            {
                _cachedCharacterOptions = EmptyCharacterOptions;
                _cachedCharacterAvailability = GuiText.Get("gui.characters.availability.unavailable");
                return;
            }

            if (!forceRefresh && Time.unscaledTime < _nextCharacterPageRefreshAt)
            {
                return;
            }

            FoyerCharacterOption[] options = _foyerCharacterSwitchService.GetCharacterOptions();
            _cachedCharacterOptions = options ?? EmptyCharacterOptions;
            _cachedCharacterAvailability = BuildCharacterAvailabilityStatus(_cachedCharacterOptions);
            _nextCharacterPageRefreshAt = Time.unscaledTime + CharacterPageRefreshIntervalSeconds;
        }

        private static string BuildCharacterAvailabilityStatus(FoyerCharacterOption[] options)
        {
            if (options == null || options.Length == 0)
            {
                return GuiText.Get("gui.characters.availability.breach_only");
            }

            int availableCount = 0;
            int lockedCount = 0;
            for (int i = 0; i < options.Length; i++)
            {
                FoyerCharacterOption option = options[i];
                if (option.IsSelected || option.IsSelectable)
                {
                    availableCount++;
                }
                else if (option.CanUnlock)
                {
                    lockedCount++;
                }
            }

            return GuiText.Get("gui.characters.availability.summary", availableCount, lockedCount);
        }

        private void ResetCharacterPageCache()
        {
            _cachedCharacterOptions = EmptyCharacterOptions;
            _cachedCharacterAvailability = GuiText.Get("gui.characters.availability.breach_only");
            _nextCharacterPageRefreshAt = 0f;
            _characterPageFocusedControlId = "characters.mode";
        }
    }
}
