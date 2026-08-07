// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private void LogJoystickButtonStateChanges()
        {
            for (int i = 0; i < _wasJoystickButtonPressed.Length; i++)
            {
                KeyCode buttonKeyCode = GetJoystickButtonKeyCode(i);
                bool isPressed = Input.GetKey(buttonKeyCode);
                if (isPressed == _wasJoystickButtonPressed[i])
                {
                    continue;
                }

                _wasJoystickButtonPressed[i] = isPressed;
                LogGamepadShortcutState(
                    "Observed joystick button state change. Button=" +
                    i +
                    ", Pressed=" +
                    isPressed +
                    ", Down=" +
                    Input.GetKeyDown(buttonKeyCode) +
                    ", Up=" +
                    Input.GetKeyUp(buttonKeyCode) +
                    ".");
            }
        }

        private void LogControllerStickStateChanges()
        {
            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            InControl.InputDevice activeDevice =
                (object)braveInput != null && braveInput.ActiveActions != null
                    ? braveInput.ActiveActions.Device
                    : null;
            if (activeDevice == null || activeDevice.DeviceClass != InControl.InputDeviceClass.Controller)
            {
                return;
            }

            float dpadX = activeDevice.DPadX != null ? activeDevice.DPadX.Value : 0f;
            float dpadY = activeDevice.DPadY != null ? activeDevice.DPadY.Value : 0f;
            float leftStickX = activeDevice.LeftStickX != null ? activeDevice.LeftStickX.Value : 0f;
            float leftStickY = activeDevice.LeftStickY != null ? activeDevice.LeftStickY.Value : 0f;
            float rightStickX = activeDevice.RightStickX != null ? activeDevice.RightStickX.Value : 0f;
            float rightStickY = activeDevice.RightStickY != null ? activeDevice.RightStickY.Value : 0f;

            LogNamedControllerAxisStateChange(
                "DPad",
                dpadX,
                dpadY,
                ref _lastLoggedControllerDpadHorizontalAxis,
                ref _lastLoggedControllerDpadVerticalAxis);
            LogNamedControllerAxisStateChange(
                "LeftStick",
                leftStickX,
                leftStickY,
                ref _lastLoggedControllerLeftStickHorizontalAxis,
                ref _lastLoggedControllerLeftStickVerticalAxis);
            LogNamedControllerAxisStateChange(
                "RightStick",
                rightStickX,
                rightStickY,
                ref _lastLoggedControllerRightStickHorizontalAxis,
                ref _lastLoggedControllerRightStickVerticalAxis);
        }

        private void LogDisabledKeyboardNavigationKeyAttempts()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.W, "Up");
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.A, "Left");
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.S, "Down");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.D, "Right");
            }
        }

        private void LogGameplayKeyboardInputState()
        {
            if (!IsCommandPanelGameplayInputVerboseLoggingEnabled())
            {
                _hasLoggedGameplayInputState = false;
                return;
            }

            bool isWPressed = Input.GetKey(KeyCode.W);
            bool isAPressed = Input.GetKey(KeyCode.A);
            bool isSPressed = Input.GetKey(KeyCode.S);
            bool isDPressed = Input.GetKey(KeyCode.D);
            PlayerController player = GetCurrentPlayer();
            bool isInputOverridden = (object)player != null && player.IsInputOverridden;
            string inputState = (object)player != null ? player.CurrentInputState.ToString() : "<none>";
            bool stateChanged = !_hasLoggedGameplayInputState ||
                _lastLoggedGameplayPanelVisible != _isVisible ||
                _lastLoggedGameplayW != isWPressed ||
                _lastLoggedGameplayA != isAPressed ||
                _lastLoggedGameplayS != isSPressed ||
                _lastLoggedGameplayD != isDPressed ||
                _lastLoggedGameplayInputOverridden != isInputOverridden ||
                !string.Equals(_lastLoggedGameplayInputState, inputState, System.StringComparison.Ordinal);
            if (!stateChanged)
            {
                return;
            }

            LogGamepadShortcutState(
                "Observed gameplay keyboard input state. PanelVisible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", W=" +
                isWPressed +
                ", A=" +
                isAPressed +
                ", S=" +
                isSPressed +
                ", D=" +
                isDPressed +
                ", PlayerId=" +
                ((object)player != null ? player.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture) : "<none>") +
                ", IsInputOverridden=" +
                isInputOverridden +
                ", CurrentInputState=" +
                inputState +
                ", CurrentFocus=" +
                GUIUtility.keyboardControl +
                ".");

            _hasLoggedGameplayInputState = true;
            _lastLoggedGameplayPanelVisible = _isVisible;
            _lastLoggedGameplayW = isWPressed;
            _lastLoggedGameplayA = isAPressed;
            _lastLoggedGameplayS = isSPressed;
            _lastLoggedGameplayD = isDPressed;
            _lastLoggedGameplayInputOverridden = isInputOverridden;
            _lastLoggedGameplayInputState = inputState;
        }

        private void LogControllerGameplayInputState()
        {
            if (!IsCommandPanelControllerGameplayInputVerboseLoggingEnabled())
            {
                _hasLoggedControllerGameplayInputState = false;
                return;
            }

            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            InControl.InputDevice activeDevice =
                (object)braveInput != null && braveInput.ActiveActions != null
                    ? braveInput.ActiveActions.Device
                    : null;
            string device = activeDevice == null
                ? "<none>"
                : activeDevice.DeviceClass + "/" + activeDevice.GetType().Name;
            float dpadX = activeDevice != null && activeDevice.DPadX != null ? activeDevice.DPadX.Value : 0f;
            float dpadY = activeDevice != null && activeDevice.DPadY != null ? activeDevice.DPadY.Value : 0f;
            float leftStickX = activeDevice != null && activeDevice.LeftStickX != null ? activeDevice.LeftStickX.Value : 0f;
            float leftStickY = activeDevice != null && activeDevice.LeftStickY != null ? activeDevice.LeftStickY.Value : 0f;
            float rightStickX = activeDevice != null && activeDevice.RightStickX != null ? activeDevice.RightStickX.Value : 0f;
            float rightStickY = activeDevice != null && activeDevice.RightStickY != null ? activeDevice.RightStickY.Value : 0f;
            PlayerController player = GetCurrentPlayer();
            bool isInputOverridden = (object)player != null && player.IsInputOverridden;
            string inputState = (object)player != null ? player.CurrentInputState.ToString() : "<none>";
            bool stateChanged = !_hasLoggedControllerGameplayInputState ||
                _lastLoggedControllerGameplayPanelVisible != _isVisible ||
                !string.Equals(_lastLoggedControllerGameplayDevice, device, System.StringComparison.Ordinal) ||
                _lastLoggedControllerGameplayInputOverridden != isInputOverridden ||
                !string.Equals(_lastLoggedControllerGameplayInputState, inputState, System.StringComparison.Ordinal) ||
                Mathf.Abs(_lastLoggedControllerGameplayDpadHorizontal - dpadX) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayDpadVertical - dpadY) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayLeftStickHorizontal - leftStickX) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayLeftStickVertical - leftStickY) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayRightStickHorizontal - rightStickX) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayRightStickVertical - rightStickY) > 0.01f;
            if (!stateChanged)
            {
                return;
            }

            LogGamepadShortcutState(
                "Observed gameplay controller input state. PanelVisible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", Device=" +
                device +
                ", DPad=" +
                dpadX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                "," +
                dpadY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                ", LeftStick=" +
                leftStickX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                "," +
                leftStickY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                ", RightStick=" +
                rightStickX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                "," +
                rightStickY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) +
                ", PlayerId=" +
                ((object)player != null ? player.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture) : "<none>") +
                ", IsInputOverridden=" +
                isInputOverridden +
                ", CurrentInputState=" +
                inputState +
                ", CurrentFocus=" +
                GUIUtility.keyboardControl +
                ".");

            _hasLoggedControllerGameplayInputState = true;
            _lastLoggedControllerGameplayPanelVisible = _isVisible;
            _lastLoggedControllerGameplayDevice = device;
            _lastLoggedControllerGameplayInputOverridden = isInputOverridden;
            _lastLoggedControllerGameplayInputState = inputState;
            _lastLoggedControllerGameplayDpadHorizontal = dpadX;
            _lastLoggedControllerGameplayDpadVertical = dpadY;
            _lastLoggedControllerGameplayLeftStickHorizontal = leftStickX;
            _lastLoggedControllerGameplayLeftStickVertical = leftStickY;
            _lastLoggedControllerGameplayRightStickHorizontal = rightStickX;
            _lastLoggedControllerGameplayRightStickVertical = rightStickY;
        }

        private void LogDisabledKeyboardNavigationKeyAttempt(KeyCode keyCode, string mappedDirection)
        {
            LogGamepadShortcutState(
                "Observed disabled keyboard navigation key press. Key=" +
                keyCode +
                ", MappedDirection=" +
                mappedDirection +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ".");
        }

        private void LogMouseButtonAttempts()
        {
            if (!IsCommandPanelCursorVerboseLoggingEnabled())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                LogMouseButtonAttempt(0, "Left");
            }

            if (Input.GetMouseButtonDown(1))
            {
                LogMouseButtonAttempt(1, "Right");
            }
        }

        private void LogMouseButtonAttempt(int buttonIndex, string buttonName)
        {
            Vector3 mousePosition = Input.mousePosition;
            LogGamepadShortcutState(
                "Observed mouse button press. Button=" +
                buttonName +
                ", ButtonIndex=" +
                buttonIndex +
                ", MouseX=" +
                mousePosition.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", MouseY=" +
                mousePosition.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ".");
        }

        private static KeyCode GetJoystickButtonKeyCode(int buttonIndex)
        {
            return KeyCode.JoystickButton0 + buttonIndex;
        }

        private static string DescribePlayerVitals(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<player:null>";
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return "<health:null>";
            }

            return
                "CurrentHealth=" +
                healthHaver.GetCurrentHealth().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                ", MaxHealth=" +
                healthHaver.GetMaxHealth().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                ", Armor=" +
                healthHaver.Armor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                ", Blanks=" +
                player.Blanks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void LogHealthDiagnosticStateChanges()
        {
            if (!IsCommandPanelHealthVerboseLoggingEnabled())
            {
                ResetHealthDiagnosticState();
                return;
            }

            PlayerController player = GetCurrentPlayer();
            if ((object)player == null || (object)player.healthHaver == null)
            {
                ResetHealthDiagnosticState();
                return;
            }

            HealthHaver healthHaver = player.healthHaver;
            Gun currentGun = player.CurrentGun;
            int gunId = (object)currentGun != null ? currentGun.GetInstanceID() : 0;
            string gunName = (object)currentGun != null ? currentGun.name : "<none>";
            float currentHealth = healthHaver.GetCurrentHealth();
            float maxHealth = healthHaver.GetMaxHealth();
            float armor = healthHaver.Armor;
            bool playerChanged = !ReferenceEquals(_lastHealthDiagnosticPlayer, player);
            bool gunChanged = playerChanged || gunId != _lastHealthDiagnosticGunId;
            bool vitalsChanged = playerChanged ||
                float.IsNaN(_lastHealthDiagnosticCurrentHealth) ||
                Mathf.Abs(currentHealth - _lastHealthDiagnosticCurrentHealth) > 0.001f ||
                Mathf.Abs(maxHealth - _lastHealthDiagnosticMaxHealth) > 0.001f ||
                Mathf.Abs(armor - _lastHealthDiagnosticArmor) > 0.001f;

            if (gunChanged || vitalsChanged)
            {
                LogCommandPanelHealthDiagnostic(
                    "Observed player health state change. PreviousCurrentHealth=" +
                    FormatDiagnosticFloat(_lastHealthDiagnosticCurrentHealth) +
                    ", PreviousMaxHealth=" +
                    FormatDiagnosticFloat(_lastHealthDiagnosticMaxHealth) +
                    ", PreviousArmor=" +
                    FormatDiagnosticFloat(_lastHealthDiagnosticArmor) +
                    ", CurrentCurrentHealth=" +
                    FormatDiagnosticFloat(currentHealth) +
                    ", CurrentMaxHealth=" +
                    FormatDiagnosticFloat(maxHealth) +
                    ", CurrentArmor=" +
                    FormatDiagnosticFloat(armor) +
                    ", PreviousGunId=" +
                    _lastHealthDiagnosticGunId +
                    ", PreviousGunName=" +
                    (_lastHealthDiagnosticGunName ?? "<none>") +
                    ", CurrentGunId=" +
                    gunId +
                    ", CurrentGunName=" +
                    gunName +
                    ", GunChanged=" +
                    gunChanged +
                    ", VitalsChanged=" +
                    vitalsChanged +
                    ", Visible=" +
                    _isVisible +
                    ", Page=" +
                    _currentPage +
                    ", PlayerFocus=" +
                    _commandPageFocusedControlId +
                    ", SettingsFocus=" +
                    _settingsPageFocusedControlId +
                    ".");
            }

            _lastHealthDiagnosticPlayer = player;
            _lastHealthDiagnosticCurrentHealth = currentHealth;
            _lastHealthDiagnosticMaxHealth = maxHealth;
            _lastHealthDiagnosticArmor = armor;
            _lastHealthDiagnosticGunId = gunId;
            _lastHealthDiagnosticGunName = gunName;
        }

        private void ResetHealthDiagnosticState()
        {
            _lastHealthDiagnosticPlayer = null;
            _lastHealthDiagnosticCurrentHealth = float.NaN;
            _lastHealthDiagnosticMaxHealth = float.NaN;
            _lastHealthDiagnosticArmor = float.NaN;
            _lastHealthDiagnosticGunId = -1;
            _lastHealthDiagnosticGunName = string.Empty;
        }

        private static string FormatDiagnosticFloat(float value)
        {
            return float.IsNaN(value)
                ? "<unset>"
                : value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void LogNamedControllerAxisStateChange(
            string inputName,
            float horizontal,
            float vertical,
            ref float lastHorizontal,
            ref float lastVertical)
        {
            bool didHorizontalChange =
                float.IsNaN(lastHorizontal) ||
                Mathf.Abs(horizontal - lastHorizontal) > 0.01f;
            bool didVerticalChange =
                float.IsNaN(lastVertical) ||
                Mathf.Abs(vertical - lastVertical) > 0.01f;
            if (!didHorizontalChange && !didVerticalChange)
            {
                return;
            }

            lastHorizontal = horizontal;
            lastVertical = vertical;
            LogGamepadShortcutState(
                "Observed controller input axis change. Input=" +
                inputName +
                ", Horizontal=" +
                horizontal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Vertical=" +
                vertical.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ".");
        }

        private void LogControllerNavigationAxisState(float horizontal, float vertical)
        {
            bool didHorizontalChange =
                float.IsNaN(_lastLoggedControllerHorizontalAxis) ||
                Mathf.Abs(horizontal - _lastLoggedControllerHorizontalAxis) > 0.01f;
            bool didVerticalChange =
                float.IsNaN(_lastLoggedControllerVerticalAxis) ||
                Mathf.Abs(vertical - _lastLoggedControllerVerticalAxis) > 0.01f;
            if (!didHorizontalChange && !didVerticalChange)
            {
                return;
            }

            _lastLoggedControllerHorizontalAxis = horizontal;
            _lastLoggedControllerVerticalAxis = vertical;
            LogGamepadShortcutState(
                "Observed controller navigation axis change. Horizontal=" +
                horizontal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Vertical=" +
                vertical.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", HorizontalLatched=" +
                _wasControllerHorizontalNavigationActive +
                ", VerticalLatched=" +
                _wasControllerVerticalNavigationActive +
                ".");
        }

        private void LogGamepadShortcutState(string message)
        {
            if (_inputLogHandler != null)
            {
                _inputLogHandler(message);
            }
        }

        private void Close()
        {
            _isVisible = false;
            ResetClosedPanelState();
        }

        private void SyncPanelInputOverride()
        {
            _commandPanelLifecycleCoordinator.SyncInputOverride();
        }

        private void ClearPanelInputOverride()
        {
            _commandPanelLifecycleCoordinator.ClearInputOverride();
        }

        private bool IsCommandPanelHealthVerboseLoggingEnabled()
        {
            return _commandPanelHealthVerboseLoggingEnabledProvider != null &&
                _commandPanelHealthVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelCursorVerboseLoggingEnabled()
        {
            return _commandPanelCursorVerboseLoggingEnabledProvider != null &&
                _commandPanelCursorVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelGameplayInputVerboseLoggingEnabled()
        {
            return _commandPanelGameplayInputVerboseLoggingEnabledProvider != null &&
                _commandPanelGameplayInputVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelControllerGameplayInputVerboseLoggingEnabled()
        {
            return _commandPanelControllerGameplayInputVerboseLoggingEnabledProvider != null &&
                _commandPanelControllerGameplayInputVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelShortcutVerboseLoggingEnabled()
        {
            return _commandPanelShortcutVerboseLoggingEnabledProvider != null &&
                _commandPanelShortcutVerboseLoggingEnabledProvider();
        }

        private void LogCommandPanelShortcutDiagnostic(string message)
        {
            if (!IsCommandPanelShortcutVerboseLoggingEnabled())
            {
                return;
            }

            LogGamepadShortcutState("Command panel shortcut diagnostic. " + message);
        }

        private void LogCommandPanelShortcutState(bool keyboardTogglePressed, bool controllerTogglePressed)
        {
            if (!IsCommandPanelShortcutVerboseLoggingEnabled())
            {
                _hasLoggedCommandPanelShortcutState = false;
                return;
            }

            KeyCode toggleKey = GetToggleKey();
            bool keyboardToggleHeld = Input.GetKey(toggleKey);
            GameManager gameManager = GameManager.Instance;
            string gameType = (object)gameManager != null ? gameManager.CurrentGameType.ToString() : "<null>";
            string primaryPlayer = (object)gameManager != null ? DescribeCommandPanelShortcutPlayer(gameManager.PrimaryPlayer) : "<none>";
            string secondaryPlayer = (object)gameManager != null ? DescribeCommandPanelShortcutPlayer(gameManager.SecondaryPlayer) : "<none>";
            bool stateChanged = !_hasLoggedCommandPanelShortcutState ||
                _lastLoggedCommandPanelKeyboardHeld != keyboardToggleHeld ||
                _lastLoggedCommandPanelKeyboardDown != keyboardTogglePressed ||
                _lastLoggedCommandPanelControllerDetected != controllerTogglePressed ||
                _lastLoggedCommandPanelVisible != _isVisible;
            if (!stateChanged)
            {
                return;
            }

            LogGamepadShortcutState(
                "Command panel shortcut sample. " +
                "ToggleKey=" + toggleKey +
                ", KeyboardHeld=" + keyboardToggleHeld +
                ", KeyboardDown=" + keyboardTogglePressed +
                ", ControllerDetected=" + controllerTogglePressed +
                ", ControllerShortcutEnabled=" + IsControllerShortcutEnabled() +
                ", ConfiguredControllerShortcut=" + GetConfiguredControllerShortcut() +
                ", Visible=" + _isVisible +
                ", GameType=" + gameType +
                ", P1=" + primaryPlayer +
                ", P2=" + secondaryPlayer +
                ".");

            _hasLoggedCommandPanelShortcutState = true;
            _lastLoggedCommandPanelKeyboardHeld = keyboardToggleHeld;
            _lastLoggedCommandPanelKeyboardDown = keyboardTogglePressed;
            _lastLoggedCommandPanelControllerDetected = controllerTogglePressed;
            _lastLoggedCommandPanelVisible = _isVisible;
        }

        private static string DescribeCommandPanelShortcutPlayer(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<null>";
            }

            try
            {
                return "Id=" + player.GetInstanceID() +
                       ",Name=" + player.name +
                       ",Active=" + player.gameObject.activeInHierarchy +
                       ",InputOverridden=" + player.IsInputOverridden;
            }
            catch (System.Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

    }
}
