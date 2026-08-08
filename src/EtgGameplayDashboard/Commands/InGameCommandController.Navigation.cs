// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private void HandleControllerNavigation()
        {
            if (!_isVisible)
            {
                ResetControllerNavigationAxes();
                return;
            }

            if (_isCapturingPickupShortcut || _isCapturingCommandPanelKey)
            {
                ResetControllerNavigationAxes();
                return;
            }

            bool isControllerBackPressed = IsPanelBackPressed();
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Detected controller back press. Page=" +
                    _currentPage +
                    ", CommandFocus=" +
                    _commandPageFocusedControlId +
                    ", SettingsFocus=" +
                    _settingsPageFocusedControlId +
                    ", ExperimentalDialog=" +
                    _showExperimentalModeConfirmDialog +
                    ".");
            }

            if (_showExperimentalModeConfirmDialog && _currentPage == PanelPage.Settings)
            {
                if (isControllerBackPressed)
                {
                    LogGamepadShortcutState("Controller back press dismissed the experimental mode confirmation dialog.");
                    _showExperimentalModeConfirmDialog = false;
                }

                if (IsPanelConfirmPressed())
                {
                    SetExperimentalModeEnabled(true, null);
                }

                ResetControllerNavigationAxes();
                return;
            }

            if (_showTeleportPanel)
            {
                HandleTeleportPanelControllerNavigation(isControllerBackPressed);
                return;
            }

            switch (_currentPage)
            {
                case PanelPage.Command:
                    HandleCommandPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Settings:
                    HandleSettingsPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.PickupInfoConfig:
                    HandlePickupInfoConfigPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Characters:
                    HandleCharacterPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.AdvancedTools:
                    HandleAdvancedToolsPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.ControllerHelp:
                    HandleControllerHelpPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.KeyboardHelp:
                    HandleKeyboardHelpPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.CursorColor:
                    HandleCursorColorPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.CommandInfo:
                    HandleCommandInfoPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Pickups:
                    HandlePickupPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Currency:
                    HandleCurrencyPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.LoadoutEditor:
                    HandleLoadoutEditorPageControllerNavigation(isControllerBackPressed);
                    return;
                default:
                    if (isControllerBackPressed)
                    {
                        LogGamepadShortcutState(
                            "Controller back press detected on a page without controller back handling. Page=" +
                            _currentPage +
                            ".");
                    }

                    ResetControllerNavigationAxes();
                    return;
            }
        }

        private void HandleCommandPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is closing the command page.");
                Close();
                return;
            }

            if (Input.GetKeyDown(GetJoystickButtonKeyCode(4)))
            {
                CycleCommandCategory(-1);
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _commandPageFocusedControlId;
                _commandPageFocusedControlId = MoveControllerFocus(GetCommandPageFocusEntries(), _commandPageFocusedControlId, navigationDirection.Value);
                if (IsCommandPanelHealthVerboseLoggingEnabled())
                {
                    LogGamepadShortcutState(
                        "Command page controller navigation moved focus. Direction=" +
                        navigationDirection.Value +
                        ", From=" +
                        previousControlId +
                        ", To=" +
                        _commandPageFocusedControlId +
                        ", PlayerVitals=" +
                        DescribePlayerVitals(GetCurrentPlayer()) +
                        ".");
                }
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteCommandPageFocusedControl();
            }
        }

        private void HandleSettingsPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from settings to the command page.");
                _currentPage = PanelPage.Command;
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _settingsPageFocusedControlId;
                _settingsPageFocusedControlId = MoveControllerFocus(GetSettingsPageFocusEntries(), _settingsPageFocusedControlId, navigationDirection.Value);
                LogGamepadShortcutState(
                    "Settings page controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _settingsPageFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteSettingsPageFocusedControl();
            }
        }

        private void HandlePickupInfoConfigPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from pickup info config to the command page.");
                _currentPage = PanelPage.Command;
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _pickupInfoConfigFocusedControlId;
                _pickupInfoConfigFocusedControlId = MoveControllerFocus(PickupInfoConfigPageFocusEntries, _pickupInfoConfigFocusedControlId, navigationDirection.Value);
                LogGamepadShortcutState(
                    "Pickup info config controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _pickupInfoConfigFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecutePickupInfoConfigPageFocusedControl();
            }
        }

        private void HandleCharacterPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from characters to the command page.");
                CloseCharacterPage();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _characterPageFocusedControlId;
                _characterPageFocusedControlId = MoveControllerFocus(
                    GetCharacterPageFocusEntries(_cachedCharacterOptions),
                    _characterPageFocusedControlId,
                    navigationDirection.Value);
                LogGamepadShortcutState(
                    "Character page controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _characterPageFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteCharacterPageFocusedControl(null);
            }
        }

        private void HandleControllerHelpPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed || IsPanelConfirmPressed())
            {
                LogGamepadShortcutState("Controller navigation is returning from controller help to settings.");
                OpenSettingsPage();
                return;
            }

            ResetControllerNavigationAxes();
        }

        private void HandleKeyboardHelpPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed || IsPanelConfirmPressed())
            {
                LogGamepadShortcutState("Keyboard navigation is returning from keyboard help to settings.");
                OpenSettingsPage();
                return;
            }

            ResetControllerNavigationAxes();
        }

        private void HandlePickupPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Controller back press detected on pickup browser. Mode=" +
                    _pickupBrowserMode +
                    ", Focus=" +
                    _pickupPageFocusedControlId +
                    ".");
                ReturnFromPickupPage();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _pickupPageFocusedControlId;
                _pickupPageFocusedControlId = MoveControllerFocus(
                    GetPickupPageFocusEntries(),
                    _pickupPageFocusedControlId,
                    navigationDirection.Value);
                LogGamepadShortcutState(
                    "Pickup browser controller navigation moved focus. Mode=" +
                    _pickupBrowserMode +
                    ", Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _pickupPageFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecutePickupPageFocusedControl(GetSelectedCommandTargetPlayer(), null);
            }
        }

        private void HandleCurrencyPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Controller back press detected on currency page. Focus=" +
                    _currencyPageFocusedControlId +
                    ".");
                HandleCurrencyBack();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _currencyPageFocusedControlId;
                _currencyPageFocusedControlId = MoveControllerFocus(
                    GetCurrencyPageFocusEntries(),
                    _currencyPageFocusedControlId,
                    navigationDirection.Value);
                if (IsCommandPanelHealthVerboseLoggingEnabled())
                {
                    LogGamepadShortcutState(
                        "Currency page controller navigation moved focus. Direction=" +
                        navigationDirection.Value +
                        ", From=" +
                        previousControlId +
                        ", To=" +
                        _currencyPageFocusedControlId +
                        ", PlayerVitals=" +
                        DescribePlayerVitals(GetCurrentPlayer()) +
                        ".");
                }
            }

            if (IsPanelConfirmPressed())
            {
                LogGamepadShortcutState(
                    "Controller confirm is activating currency page control. Focus=" +
                    _currencyPageFocusedControlId +
                    ".");
                ExecuteCurrencyPageFocusedControl(GetCurrentPlayer(), null);
            }
        }

        private void HandleLoadoutEditorPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Controller back press detected on loadout editor. Mode=" +
                    _loadoutEditorMode +
                    ", Focus=" +
                    _loadoutEditorFocusedControlId +
                    ".");
                HandleLoadoutEditorBackNavigation();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _loadoutEditorFocusedControlId;
                _loadoutEditorFocusedControlId = MoveControllerFocus(
                    GetLoadoutEditorFocusEntries(),
                    _loadoutEditorFocusedControlId,
                    navigationDirection.Value);
                LogGamepadShortcutState(
                    "Loadout editor controller navigation moved focus. Mode=" +
                    _loadoutEditorMode +
                    ", Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _loadoutEditorFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteLoadoutEditorFocusedControl(GetCurrentPlayer(), null);
            }
        }

        private void HandleTeleportPanelControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is closing the teleport panel.");
                CloseTeleportPanel();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                int previousIndex = _teleportSelectedIndex;
                switch (navigationDirection.Value)
                {
                    case ControllerNavDirection.Up:
                        _teleportSelectedIndex = (_teleportSelectedIndex + TeleportOptions.Length - 1) % TeleportOptions.Length;
                        break;
                    case ControllerNavDirection.Down:
                        _teleportSelectedIndex = (_teleportSelectedIndex + 1) % TeleportOptions.Length;
                        break;
                    default:
                        LogGamepadShortcutState(
                            "Ignored teleport panel horizontal navigation. Direction=" +
                            navigationDirection.Value +
                            ", SelectedIndex=" +
                            _teleportSelectedIndex +
                            ".");
                        return;
                }

                LogGamepadShortcutState(
                    "Teleport panel selection changed. Direction=" +
                    navigationDirection.Value +
                    ", FromIndex=" +
                    previousIndex +
                    ", ToIndex=" +
                    _teleportSelectedIndex +
                    ", Token=" +
                    TeleportOptions[_teleportSelectedIndex].CommandToken +
                    ".");
                return;
            }

            if (IsPanelConfirmPressed())
            {
                TeleportOption selectedOption = TeleportOptions[_teleportSelectedIndex];
                LogGamepadShortcutState(
                    "Controller confirm is activating teleport option. SelectedIndex=" +
                    _teleportSelectedIndex +
                    ", Token=" +
                    selectedOption.CommandToken +
                    ".");
                ExecuteTeleport(selectedOption, null);
            }
        }
    }
}
