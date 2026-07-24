// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private const float GameplayPerformanceFocusWindowSeconds = 10f;

        private long BeginPerformanceSample()
        {
            return _performanceDiagnostics != null && ShouldCaptureGameplayPerformance()
                ? _performanceDiagnostics.BeginSample()
                : 0L;
        }

        private void LogPerformanceStep(string stepName, long startedAtTimestamp)
        {
            if (_performanceDiagnostics == null)
            {
                return;
            }

            if (!ShouldCaptureGameplayPerformance())
            {
                return;
            }

            _performanceDiagnostics.RecordStep(stepName, startedAtTimestamp, GetPerformanceFrameDetail());
        }

        private void LogPerformanceOperation(string operationName, long startedAtTimestamp, string detail)
        {
            if (_performanceDiagnostics == null)
            {
                return;
            }

            if (!ShouldCaptureGameplayPerformance())
            {
                return;
            }

            _performanceDiagnostics.RecordOperation(operationName, startedAtTimestamp, detail);
        }

        private void MarkPerformanceEvent(string eventSummary)
        {
            if (_performanceDiagnostics == null)
            {
                return;
            }

            _performanceDiagnostics.MarkEvent(eventSummary);
        }

        private void RecordPerformanceFrame()
        {
            if (_performanceDiagnostics == null)
            {
                return;
            }

            if (!ShouldCaptureGameplayPerformance())
            {
                return;
            }

            _performanceDiagnostics.RecordFrame(Time.unscaledDeltaTime, Time.realtimeSinceStartup, GetPerformanceFrameDetail());
        }

        private void UpdateGameplayPerformanceWindow(RunLifecycleObservation lifecycle)
        {
            if (lifecycle == null)
            {
                return;
            }

            if (!lifecycle.IsGrantableDungeonScene)
            {
                _gameplayPerformanceWindowStartedAt = -1f;
                _gameplayPerformanceSceneName = string.Empty;
                return;
            }

            if (!lifecycle.SceneChanged)
            {
                return;
            }

            _gameplayPerformanceWindowStartedAt = Time.realtimeSinceStartup;
            _gameplayPerformanceSceneName = lifecycle.SceneName ?? string.Empty;
            MarkPerformanceEvent("Gameplay performance window started in " + _gameplayPerformanceSceneName);
        }

        private bool ShouldCaptureGameplayPerformance()
        {
            if (_performanceDiagnostics == null)
            {
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null || gameManager.IsFoyer)
            {
                return false;
            }

            string sceneName = string.Empty;
            if (_sceneWatcher == null || !_sceneWatcher.TryGetCurrentSceneName(gameManager, out sceneName))
            {
                return false;
            }

            if (string.IsNullOrEmpty(sceneName) ||
                string.Equals(sceneName, CharacterSelectSceneName, System.StringComparison.Ordinal) ||
                string.Equals(sceneName, LoadingSceneName, System.StringComparison.Ordinal))
            {
                return false;
            }

            return (object)gameManager.PrimaryPlayer != null;
        }

        private bool IsGameplayPerformanceFocusWindowActive()
        {
            return _gameplayPerformanceWindowStartedAt >= 0f &&
                   Time.realtimeSinceStartup - _gameplayPerformanceWindowStartedAt <= GameplayPerformanceFocusWindowSeconds;
        }

        private string GetPerformanceFrameDetail()
        {
            GameManager gameManager = GameManager.Instance;
            string sceneName = string.Empty;
            if (_sceneWatcher != null && (object)gameManager != null)
            {
                _sceneWatcher.TryGetCurrentSceneName(gameManager, out sceneName);
            }

            PlayerController player = (object)gameManager != null ? gameManager.PrimaryPlayer : null;
            return "Scene=" +
                   (string.IsNullOrEmpty(sceneName) ? "<unknown>" : sceneName) +
                   ", HasPlayer=" +
                   ((object)player != null) +
                   ", FocusWindow=" +
                   IsGameplayPerformanceFocusWindowActive() +
                   ", GameplaySeconds=" +
                   GetGameplayPerformanceElapsedSeconds().ToString("0.00") +
                   ", PendingTeleport=" +
                   (_pendingTeleportFloor != null) +
                   ", HasGrantedThisRun=" +
                   (_runState != null && _runState.HasGrantedThisRun);
        }

        private float GetGameplayPerformanceElapsedSeconds()
        {
            if (_gameplayPerformanceWindowStartedAt < 0f)
            {
                return -1f;
            }

            return Time.realtimeSinceStartup - _gameplayPerformanceWindowStartedAt;
        }
    }
}
