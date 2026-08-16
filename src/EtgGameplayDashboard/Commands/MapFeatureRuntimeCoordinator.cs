// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using BepInEx.Logging;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Owns floor-scoped map reveal and direct-teleport activation state.
    /// The command controller exposes the feature; this coordinator owns its runtime lifecycle.
    /// </summary>
    internal sealed class MapFeatureRuntimeCoordinator
    {
        private readonly RoomDebugCommandService _roomDebugCommandService;
        private readonly ManualLogSource _performanceLogger;
        private readonly Func<bool> _verboseLoggingProvider;
        private readonly Func<string> _sceneKeyProvider;
        private readonly Func<RoomHandler, string> _roomKeyProvider;
        private readonly Action<string> _transitionDiagnostic;
        private readonly Action<string> _shortcutLog;
        private readonly Action _resetDirectTeleportDiagnostics;
        private string _revealMapActivatedSceneName = string.Empty;
        private string _mapDirectTeleportActivatedSceneName = string.Empty;
        private string _autoRevealMapSceneName = string.Empty;
        private float _nextAutoRevealMapAttemptAt;
        private string _autoRevealMapReadyRoomKey = string.Empty;
        private int _autoRevealMapReadyRoomFrames;

        public MapFeatureRuntimeCoordinator(
            RoomDebugCommandService roomDebugCommandService,
            ManualLogSource performanceLogger,
            Func<bool> verboseLoggingProvider,
            Func<string> sceneKeyProvider,
            Func<RoomHandler, string> roomKeyProvider,
            Action<string> transitionDiagnostic,
            Action<string> shortcutLog,
            Action resetDirectTeleportDiagnostics)
        {
            _roomDebugCommandService = roomDebugCommandService;
            _performanceLogger = performanceLogger;
            _verboseLoggingProvider = verboseLoggingProvider;
            _sceneKeyProvider = sceneKeyProvider;
            _roomKeyProvider = roomKeyProvider;
            _transitionDiagnostic = transitionDiagnostic;
            _shortcutLog = shortcutLog;
            _resetDirectTeleportDiagnostics = resetDirectTeleportDiagnostics;
        }

        public bool RevealMapEnabled { get; set; }
        public bool RevealMapEveryFloor { get; set; }
        public bool PendingDungeonReveal { get; private set; }
        public string AutomaticRevealMapSceneName
        {
            get { return _autoRevealMapSceneName; }
            set
            {
                _autoRevealMapSceneName = value ?? string.Empty;
                _nextAutoRevealMapAttemptAt = 0f;
            }
        }

        public void Update()
        {
            UpdateFeatureActivationState();
            UpdateAutomaticRevealMap();
        }

        public void ClearActivationStatePreservingPendingDungeonReveal()
        {
            // Teleporting out of the foyer clears the foyer's active bindings, but
            // must preserve the one-shot request to reveal the first dungeon floor.
            ResetActivationState(true);
        }

        public void DisableRevealMap()
        {
            RevealMapEnabled = false;
            ResetActivationState(false);
            if (_resetDirectTeleportDiagnostics != null)
            {
                _resetDirectTeleportDiagnostics();
            }
        }

        private void ResetActivationState(bool preservePendingDungeonReveal)
        {
            _revealMapActivatedSceneName = string.Empty;
            _mapDirectTeleportActivatedSceneName = string.Empty;
            _autoRevealMapSceneName = string.Empty;
            _nextAutoRevealMapAttemptAt = 0f;
            if (!preservePendingDungeonReveal)
            {
                PendingDungeonReveal = false;
            }
            _autoRevealMapReadyRoomKey = string.Empty;
            _autoRevealMapReadyRoomFrames = 0;
            // Current Floor mode ends when changing dungeon floors, unless the
            // foyer request is still waiting to be consumed by the first floor.
            if (!RevealMapEveryFloor && !PendingDungeonReveal)
            {
                RevealMapEnabled = false;
            }
        }

        public bool IsRevealMapActive()
        {
            return IsActive(_revealMapActivatedSceneName);
        }

        public bool IsMapDirectTeleportActive()
        {
            return IsActive(_mapDirectTeleportActivatedSceneName);
        }

        public void HandleManualRevealCompleted()
        {
            string currentSceneName = GetSceneKey();
            RevealMapEnabled = true;
            _revealMapActivatedSceneName = currentSceneName;
            _mapDirectTeleportActivatedSceneName = currentSceneName;
            if (IsDungeonFloorScene(currentSceneName))
            {
                if (RevealMapEveryFloor)
                {
                    AutomaticRevealMapSceneName = currentSceneName;
                }
            }
            else if (IsFoyerScene(currentSceneName))
            {
                PendingDungeonReveal = true;
                AutomaticRevealMapSceneName = string.Empty;
            }

            if (_resetDirectTeleportDiagnostics != null)
            {
                _resetDirectTeleportDiagnostics();
            }
        }

        public string GetMapDirectTeleportActivationSceneName()
        {
            return _mapDirectTeleportActivatedSceneName;
        }

        public string GetRevealMapActivationSceneName()
        {
            return _revealMapActivatedSceneName;
        }

        private void UpdateFeatureActivationState()
        {
            if (string.IsNullOrEmpty(_revealMapActivatedSceneName) && string.IsNullOrEmpty(_mapDirectTeleportActivatedSceneName)) return;

            string currentSceneName = GetSceneKey();
            bool revealActive = IsSceneMatch(_revealMapActivatedSceneName, currentSceneName);
            bool teleportActive = IsSceneMatch(_mapDirectTeleportActivatedSceneName, currentSceneName);
            if (revealActive || teleportActive) return;

            if (ShouldLogVerbose())
            {
                RaiseTransitionDiagnostic("floor_scene_changed_before_reset");
                LogShortcut("Map feature activation reset. PreviousRevealMapScene=" + _revealMapActivatedSceneName +
                    ", PreviousMapDirectTeleportScene=" + _mapDirectTeleportActivatedSceneName +
                    ", CurrentScene=" + currentSceneName + ".");
            }

            ResetActivationState(PendingDungeonReveal);
            if (_resetDirectTeleportDiagnostics != null) _resetDirectTeleportDiagnostics();
        }

        private void UpdateAutomaticRevealMap()
        {
            if (!RevealMapEnabled)
            {
                _autoRevealMapSceneName = string.Empty;
                _autoRevealMapReadyRoomKey = string.Empty;
                _autoRevealMapReadyRoomFrames = 0;
                return;
            }

            string currentSceneName = GetSceneKey();
            bool isDungeonFloor = IsDungeonFloorScene(currentSceneName);
            bool shouldRevealPendingDungeonFloor = PendingDungeonReveal && isDungeonFloor;
            bool shouldRevealEveryDungeonFloor = RevealMapEveryFloor &&
                isDungeonFloor &&
                !string.Equals(currentSceneName, _autoRevealMapSceneName, StringComparison.Ordinal);
            if (string.IsNullOrEmpty(currentSceneName) ||
                (!shouldRevealPendingDungeonFloor && !shouldRevealEveryDungeonFloor) ||
                Time.time < _nextAutoRevealMapAttemptAt) return;

            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            RoomHandler currentRoom = player != null ? player.CurrentRoom : null;
            if ((object)player == null || (object)currentRoom == null || gameManager == null ||
                gameManager.Dungeon == null || gameManager.Dungeon.data == null || !Minimap.HasInstance)
            {
                ResetReadyRoom(Time.time + 0.5f);
                return;
            }

            if (player.IsInputOverridden || player.CurrentInputState != PlayerInputState.AllInput)
            {
                ResetReadyRoom(Time.time + 0.25f);
                return;
            }

            string currentRoomKey = GetRoomKey(currentRoom);
            if (!string.Equals(_autoRevealMapReadyRoomKey, currentRoomKey, StringComparison.Ordinal))
            {
                _autoRevealMapReadyRoomKey = currentRoomKey;
                _autoRevealMapReadyRoomFrames = 0;
            }

            _autoRevealMapReadyRoomFrames++;
            if (_autoRevealMapReadyRoomFrames < 10) return;

            RaiseTransitionDiagnostic("auto_reveal_before_request");
            GrantCommandExecutionResult executionResult = _roomDebugCommandService != null
                ? _roomDebugCommandService.RevealCurrentFloorMap(player, _performanceLogger)
                : GrantCommandExecutionResult.Localized(false, "result.map_reveal.unavailable");
            if (!executionResult.Succeeded)
            {
                RaiseTransitionDiagnostic("auto_reveal_failed");
                _nextAutoRevealMapAttemptAt = Time.time + 0.5f;
                return;
            }

            _autoRevealMapSceneName = currentSceneName;
            PendingDungeonReveal = false;
            _nextAutoRevealMapAttemptAt = 0f;
            _revealMapActivatedSceneName = currentSceneName;
            _mapDirectTeleportActivatedSceneName = currentSceneName;
            if (!RevealMapEveryFloor)
            {
                RevealMapEnabled = false;
                _autoRevealMapSceneName = string.Empty;
            }
            if (_resetDirectTeleportDiagnostics != null) _resetDirectTeleportDiagnostics();
            RaiseTransitionDiagnostic("auto_reveal_completed");
            if (_performanceLogger != null)
            {
                _performanceLogger.LogInfo(EtgGameplayDashboardLog.Command("Automatic Reveal Map completed for floor " + currentSceneName + "."));
            }
        }

        private void ResetReadyRoom(float nextAttemptAt)
        {
            _autoRevealMapReadyRoomKey = string.Empty;
            _autoRevealMapReadyRoomFrames = 0;
            _nextAutoRevealMapAttemptAt = nextAttemptAt;
        }

        private bool IsActive(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && IsSceneMatch(sceneName, GetSceneKey());
        }

        private string GetSceneKey()
        {
            return _sceneKeyProvider != null ? (_sceneKeyProvider() ?? string.Empty) : string.Empty;
        }

        private string GetRoomKey(RoomHandler room)
        {
            return _roomKeyProvider != null ? (_roomKeyProvider(room) ?? string.Empty) : string.Empty;
        }

        private bool ShouldLogVerbose()
        {
            return _verboseLoggingProvider != null && _verboseLoggingProvider();
        }

        private static bool IsFoyerScene(string sceneName)
        {
            GameManager gameManager = GameManager.Instance;
            return (object)gameManager != null && gameManager.IsFoyer ||
                string.Equals(sceneName, "foyer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDungeonFloorScene(string sceneName)
        {
            GameManager gameManager = GameManager.Instance;
            return !string.IsNullOrEmpty(sceneName) &&
                !string.Equals(sceneName, "LoadingDungeon", StringComparison.OrdinalIgnoreCase) &&
                !IsFoyerScene(sceneName) &&
                (object)gameManager != null &&
                !gameManager.IsFoyer &&
                gameManager.Dungeon != null &&
                gameManager.Dungeon.data != null;
        }

        private void RaiseTransitionDiagnostic(string phase)
        {
            if (ShouldLogVerbose() && (RevealMapEveryFloor || RevealMapEnabled) && _transitionDiagnostic != null)
            {
                _transitionDiagnostic(phase);
            }
        }

        private void LogShortcut(string message)
        {
            if (_shortcutLog != null) _shortcutLog(message);
        }

        private static bool IsSceneMatch(string left, string right)
        {
            return !string.IsNullOrEmpty(left) && string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
