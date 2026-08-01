// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard
{
    internal sealed class ActiveItemNoCooldownToggleService
    {
        private readonly Action<bool> _persistEnabledState;
        private bool _isEnabled;

        public ActiveItemNoCooldownToggleService(bool initiallyEnabled, Action<bool> persistEnabledState)
        {
            _isEnabled = initiallyEnabled;
            _persistEnabledState = persistEnabledState;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            _isEnabled = !_isEnabled;
            if (_isEnabled)
            {
                ClearCooldowns(player);
            }

            if (_persistEnabledState != null)
            {
                _persistEnabledState(_isEnabled);
            }

            return GrantCommandExecutionResult.Localized(
                true,
                _isEnabled
                    ? "result.active_item_no_cooldown.enable.success"
                    : "result.active_item_no_cooldown.disable.success");
        }

        public void Update(PlayerController player)
        {
            if (!_isEnabled || player == null || player.activeItems == null)
            {
                return;
            }

            ClearCooldowns(player);
        }

        public void Reset()
        {
            _isEnabled = false;
        }

        private static void ClearCooldowns(PlayerController player)
        {
            if (player == null || player.activeItems == null)
            {
                return;
            }

            for (int index = 0; index < player.activeItems.Count; index++)
            {
                PlayerItem activeItem = player.activeItems[index];
                if (activeItem != null)
                {
                    activeItem.ClearCooldowns();
                }
            }
        }
    }
}
