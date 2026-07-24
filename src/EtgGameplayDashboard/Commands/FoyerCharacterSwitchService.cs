// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Logging;
using EtgGameplayDashboard.Core.Input;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class FoyerCharacterSwitchService
    {
        private const float PendingSelectionTimeoutSeconds = 5f;
        private static readonly string[] KnownCharacterLabels =
        {
            "Marine",
            "Hunter",
            "Pilot",
            "Convict",
            "Cultist",
            "Robot",
            "Bullet",
            "Paradox",
            "Gunslinger",
        };

        private FoyerCharacterSelectFlag _pendingSelectionFlag;
        private float _pendingSelectionStartedAt;
        private readonly ManualLogSource _logger;
        private readonly Func<bool> _performanceLoggingEnabledProvider;
        private readonly Func<bool> _characterSwitchVerboseLoggingEnabledProvider;
        private readonly PlayerInputOwnershipService _playerInputOwnershipService;
        private string _lastPilotOptionDiagnostic;

        public FoyerCharacterSwitchService(
            ManualLogSource logger,
            Func<bool> performanceLoggingEnabledProvider,
            Func<bool> characterSwitchVerboseLoggingEnabledProvider,
            PlayerInputOwnershipService playerInputOwnershipService)
        {
            _logger = logger;
            _performanceLoggingEnabledProvider = performanceLoggingEnabledProvider;
            _characterSwitchVerboseLoggingEnabledProvider = characterSwitchVerboseLoggingEnabledProvider;
            _playerInputOwnershipService = playerInputOwnershipService;
        }

        public FoyerCharacterOption[] GetCharacterOptions()
        {
            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                ClearPendingSelection();
                return new FoyerCharacterOption[0];
            }

            RefreshPendingSelectionState(foyer);

            FoyerCharacterSelectFlag[] flags = GetCharacterFlagsForFoyer(foyer);
            List<FoyerCharacterOption> options = new List<FoyerCharacterOption>();
            string selectedLabel = GetSelectedLabel(foyer);
            for (int i = 0; i < KnownCharacterLabels.Length; i++)
            {
                string label = KnownCharacterLabels[i];
                FoyerCharacterSelectFlag flag = FindFlagForLabel(flags, label);
                bool isSelected = !string.IsNullOrEmpty(selectedLabel) &&
                    string.Equals(selectedLabel, label, StringComparison.OrdinalIgnoreCase);
                bool isPending = (object)_pendingSelectionFlag != null &&
                    (object)_pendingSelectionFlag == (object)flag;
                bool isSelectable = !_pendingSelectionFlag &&
                    (isSelected || ((object)flag != null && flag.CanBeSelected()));
                FoyerCharacterOption option = new FoyerCharacterOption(label, isSelectable, isSelected, isPending, flag, IsUnlockableCharacter(label));
                options.Add(option);
                if (string.Equals(label, "Pilot", StringComparison.OrdinalIgnoreCase))
                {
                    LogPilotOptionStateIfChanged(option);
                }
            }

            options.Sort(CompareOptions);
            return options.ToArray();
        }

        public string GetAvailabilityStatus()
        {
            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return GuiText.Get("gui.characters.availability.breach_only");
            }

            FoyerCharacterOption[] options = GetCharacterOptions();
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

        public GrantCommandExecutionResult SwitchCharacter(FoyerCharacterOption option)
        {
            return SwitchCharacterOnly(option);
        }

        public GrantCommandExecutionResult UnlockCharacter(FoyerCharacterOption option)
        {
            if (option == null)
            {
                LogCharacterSwitchDiagnostic("Switch rejected: option is null.");
                return GrantCommandExecutionResult.Localized(false, "result.characters.option_missing");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.breach_only_unlock");
            }

            if (string.Equals(option.Label, "Robot", StringComparison.OrdinalIgnoreCase))
            {
                // Robot is intentionally excluded from unlock mode.
                // In this panel, Robot follows switch-only behavior for reliability.
                return GrantCommandExecutionResult.Localized(false, "result.characters.robot_unlock_forbidden");
            }

            if (!IsUnlockableCharacter(option.Label))
            {
                return CreateCharacterResult(false, "result.characters.no_manual_unlock", option.Label);
            }

            FoyerCharacterSelectFlag refreshedFlag = FindFlagForLabel(GetCharacterFlagsForFoyer(foyer), option.Label);
            if ((object)refreshedFlag != null && refreshedFlag.CanBeSelected())
            {
                return CreateCharacterResult(true, "result.characters.already_unlocked", option.Label);
            }

            string unlockFailureMessage;
            if (!TryUnlockCharacter(option, out unlockFailureMessage))
            {
                return new GrantCommandExecutionResult(
                    false,
                    !string.IsNullOrEmpty(unlockFailureMessage)
                        ? unlockFailureMessage
                        : GuiText.Get("result.characters.unlock_failed", GuiText.GetCharacterLabel(option.Label)),
                    !string.IsNullOrEmpty(unlockFailureMessage)
                        ? unlockFailureMessage
                        : GuiText.GetEnglish("result.characters.unlock_failed", GuiText.GetEnglishCharacterLabel(option.Label)));
            }

            return CreateCharacterResult(true, "result.characters.unlock_success", option.Label);
        }

        public GrantCommandExecutionResult SwitchCharacterOnly(FoyerCharacterOption option)
        {
            return SwitchCharacterOnly(option, false);
        }

        public GrantCommandExecutionResult SwitchCharacterOnly(FoyerCharacterOption option, bool switchSecondaryPlayer)
        {
            long startedAtTimestamp = IsPerformanceLoggingEnabled() ? Stopwatch.GetTimestamp() : 0L;
            LogCharacterSwitchDiagnostic(
                "Switch requested. Target=" +
                (switchSecondaryPlayer ? "P2" : "P1") +
                ", Label=" +
                (option != null ? option.Label : "<null>") +
                ", State=" +
                DescribeGameManagerPlayers() +
                ".");
            if (option == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.option_missing");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                LogCharacterSwitchDiagnostic("Switch rejected: active Foyer was not found. Label=" + option.Label + ".");
                return GrantCommandExecutionResult.Localized(false, "result.characters.breach_only_switch");
            }

            RefreshPendingSelectionState(foyer);

            if ((object)_pendingSelectionFlag != null)
            {
                LogCharacterSwitchDiagnostic("Switch rejected: pending selection exists. Pending=" + DescribeFlag(_pendingSelectionFlag) + ".");
                return GrantCommandExecutionResult.Localized(false, "result.characters.selection_in_progress");
            }

            if (Foyer.IsCurrentlyPlayingCharacterSelect)
            {
                LogCharacterSwitchDiagnostic("Switch rejected: native character select is currently playing.");
                return GrantCommandExecutionResult.Localized(false, "result.characters.selection_in_progress");
            }

            if (!switchSecondaryPlayer &&
                (option.IsSelected ||
                ((object)option.Flag != null && (object)foyer.CurrentSelectedCharacterFlag == (object)option.Flag))
                )
            {
                LogCharacterSwitchDiagnostic("Switch rejected: requested character is already selected. Option=" + DescribeOption(option) + ".");
                return CreateCharacterResult(false, "result.characters.already_selected", option.Label);
            }

            // Switch-only mode must avoid the native character-select callback flow,
            // because that flow can trigger currency costs for some selections.
            string forceSwitchFailureMessage;
            if (TryForceSwitchCharacterInBreach(foyer, option.Label, switchSecondaryPlayer, out forceSwitchFailureMessage))
            {
                LogPerformanceInfo(
                    "Character switch timing. Label=" +
                    option.Label +
                    ", Succeeded=true, DurationMs=" +
                    GetElapsedMilliseconds(startedAtTimestamp).ToString("0.00") +
                    ".");
                return CreateCharacterResult(true, "result.characters.switch_success", option.Label);
            }

            LogPerformanceInfo(
                "Character switch timing. Label=" +
                option.Label +
                ", Succeeded=false, DurationMs=" +
                GetElapsedMilliseconds(startedAtTimestamp).ToString("0.00") +
                ", Detail=" +
                (!string.IsNullOrEmpty(forceSwitchFailureMessage) ? forceSwitchFailureMessage : "force switch failed") +
                ".");

            return new GrantCommandExecutionResult(
                false,
                !string.IsNullOrEmpty(forceSwitchFailureMessage)
                    ? forceSwitchFailureMessage
                    : GuiText.Get("result.characters.force_switch_failed"),
                !string.IsNullOrEmpty(forceSwitchFailureMessage)
                    ? forceSwitchFailureMessage
                    : GuiText.GetEnglish("result.characters.force_switch_failed"));
        }

        private static GrantCommandExecutionResult CreateCharacterResult(bool succeeded, string key, string characterLabel)
        {
            return new GrantCommandExecutionResult(
                succeeded,
                GuiText.Get(key, GuiText.GetCharacterLabel(characterLabel)),
                GuiText.GetEnglish(key, GuiText.GetEnglishCharacterLabel(characterLabel)));
        }

        private bool IsPerformanceLoggingEnabled()
        {
            return _performanceLoggingEnabledProvider != null && _performanceLoggingEnabledProvider();
        }

        private bool IsCharacterSwitchVerboseLoggingEnabled()
        {
            return _characterSwitchVerboseLoggingEnabledProvider != null && _characterSwitchVerboseLoggingEnabledProvider();
        }

        private void LogCharacterSwitchDiagnostic(string message)
        {
            if (!IsCharacterSwitchVerboseLoggingEnabled() || _logger == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Character switch diagnostic. " + message));
        }

        private void LogPilotOptionStateIfChanged(FoyerCharacterOption option)
        {
            if (!IsCharacterSwitchVerboseLoggingEnabled())
            {
                return;
            }

            string diagnostic = DescribeOption(option);
            if (string.Equals(_lastPilotOptionDiagnostic, diagnostic, StringComparison.Ordinal))
            {
                return;
            }

            _lastPilotOptionDiagnostic = diagnostic;
            LogCharacterSwitchDiagnostic("Pilot option state changed. " + diagnostic + ".");
        }

        private static string DescribeOption(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return "Option=<null>";
            }

            return "Label=" + option.Label +
                   ", Selectable=" + option.IsSelectable +
                   ", Selected=" + option.IsSelected +
                   ", Pending=" + option.IsPending +
                   ", CanUnlock=" + option.CanUnlock +
                   ", Flag=" + DescribeFlag(option.Flag);
        }

        private static string DescribeFlag(FoyerCharacterSelectFlag flag)
        {
            if ((object)flag == null)
            {
                return "<null>";
            }

            try
            {
                return "Id=" + flag.GetInstanceID() +
                       ", Name=" + flag.name +
                       ", Path=" + (flag.CharacterPrefabPath ?? "<null>") +
                       ", Coop=" + flag.IsCoopCharacter +
                       ", Eevee=" + flag.IsEevee +
                       ", Gunslinger=" + flag.IsGunslinger +
                       ", Alternate=" + flag.IsAlternateCostume +
                       ", CanBeSelected=" + flag.CanBeSelected();
            }
            catch (Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

        private void LogPerformanceInfo(string message)
        {
            if (!IsPerformanceLoggingEnabled())
            {
                return;
            }

            if (_logger != null && !string.IsNullOrEmpty(message))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Performance(message));
            }
        }

        private static double GetElapsedMilliseconds(long startedAtTimestamp)
        {
            if (startedAtTimestamp == 0L)
            {
                return 0d;
            }

            return (Stopwatch.GetTimestamp() - startedAtTimestamp) * 1000d / Stopwatch.Frequency;
        }
    }
}
