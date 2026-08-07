// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using UnityEngine;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Owns the command panel's transient GUI focus and player-input cleanup lifecycle.
    /// </summary>
    internal sealed class CommandPanelLifecycleCoordinator
    {
        private const string PanelInputOverrideReason = "etg_gameplay_dashboard_command_panel";
        private readonly Func<bool> _isVisibleProvider;
        private readonly Func<PlayerController> _currentPlayerProvider;
        private readonly Func<string> _pageProvider;
        private readonly Action<string> _healthLog;
        private readonly Action<string> _shortcutLog;
        private PlayerController _panelInputOverridePlayer;
        private bool _releaseGuiFocusPending;

        public CommandPanelLifecycleCoordinator(
            Func<bool> isVisibleProvider,
            Func<PlayerController> currentPlayerProvider,
            Func<string> pageProvider,
            Action<string> healthLog,
            Action<string> shortcutLog)
        {
            _isVisibleProvider = isVisibleProvider;
            _currentPlayerProvider = currentPlayerProvider;
            _pageProvider = pageProvider;
            _healthLog = healthLog;
            _shortcutLog = shortcutLog;
        }

        public void SyncInputOverride()
        {
            PlayerController currentPlayer = _currentPlayerProvider();
            if (!_isVisibleProvider())
            {
                ClearInputOverride();
                return;
            }

            if ((object)_panelInputOverridePlayer != null &&
                !ReferenceEquals(_panelInputOverridePlayer, currentPlayer))
            {
                _panelInputOverridePlayer.ClearInputOverride(PanelInputOverrideReason);
                LogHealth(
                    "Cleared command panel input override from stale player instance. PreviousPlayerId=" +
                    _panelInputOverridePlayer.GetInstanceID() +
                    ".");
                _panelInputOverridePlayer = null;
            }

            // Panel navigation reads D-pad input directly. Do not put the player into NoInput here:
            // ETG's input override also blocks the controller left stick used for gameplay movement.
            ClearInputOverride();
        }

        public void ClearInputOverride()
        {
            if ((object)_panelInputOverridePlayer == null)
            {
                return;
            }

            _panelInputOverridePlayer.ClearInputOverride(PanelInputOverrideReason);
            LogHealth(
                "Cleared command panel input override. PlayerId=" +
                _panelInputOverridePlayer.GetInstanceID() +
                ", CurrentInputState=" +
                _panelInputOverridePlayer.CurrentInputState +
                ", IsInputOverridden=" +
                _panelInputOverridePlayer.IsInputOverridden +
                ".");
            _panelInputOverridePlayer = null;
        }

        public void RequestGuiFocusRelease()
        {
            LogShortcut(
                "Queued GUI focus release. Visible=" +
                _isVisibleProvider() +
                ", Page=" +
                _pageProvider() +
                ", KeyboardControl=" +
                GUIUtility.keyboardControl +
                ", HotControl=" +
                GUIUtility.hotControl +
                ".");
            _releaseGuiFocusPending = true;
        }

        public void ReleaseGuiFocusIfPending()
        {
            if (!_releaseGuiFocusPending)
            {
                return;
            }

            _releaseGuiFocusPending = false;
            LogShortcut(
                "Releasing GUI focus. Visible=" +
                _isVisibleProvider() +
                ", Page=" +
                _pageProvider() +
                ", KeyboardControlBefore=" +
                GUIUtility.keyboardControl +
                ", HotControlBefore=" +
                GUIUtility.hotControl +
                ".");
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
        }

        private void LogHealth(string message)
        {
            if (_healthLog != null)
            {
                _healthLog(message);
            }
        }

        private void LogShortcut(string message)
        {
            if (_shortcutLog != null)
            {
                _shortcutLog(message);
            }
        }
    }
}
