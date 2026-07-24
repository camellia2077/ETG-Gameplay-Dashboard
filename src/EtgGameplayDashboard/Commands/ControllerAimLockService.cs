// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard
{
    internal sealed class ControllerAimLockService
    {
        // The setting is global because ETG's CameraController is global. The player's right-stick
        // aim must continue to reach PlayerController and Gun normally; only CameraController's
        // separate aim-look contribution is suppressed by the hook.
        private bool _isEnabled;
        private readonly Action<bool> _persistEnabledState;

        public ControllerAimLockService(bool initiallyEnabled, Action<bool> persistEnabledState)
        {
            _isEnabled = initiallyEnabled;
            _persistEnabledState = persistEnabledState;
        }

        public bool IsEnabled(PlayerController player)
        {
            return player != null && _isEnabled;
        }

        public bool IsControllerCameraAimLockActive()
        {
            // CameraController is global and follows P1, so camera suppression is based on the
            // primary player. Keyboard/mouse must retain vanilla mouse-look behavior even when
            // the persisted toggle remains enabled.
            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            if (player == null || !IsEnabled(player))
            {
                return false;
            }

            BraveInput input = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
            return input == null || !input.IsKeyboardAndMouse();
        }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            if (player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.controller_aim_lock.no_player");
            }

            _isEnabled = !_isEnabled;
            if (_persistEnabledState != null)
            {
                _persistEnabledState(_isEnabled);
            }

            return GrantCommandExecutionResult.Localized(
                true,
                _isEnabled
                    ? "result.controller_aim_lock.enable.success"
                    : "result.controller_aim_lock.disable.success");
        }

        public void Reset()
        {
            _isEnabled = false;
        }
    }
}
