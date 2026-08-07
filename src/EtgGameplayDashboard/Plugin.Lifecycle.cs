// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections;
using BepInEx;
using UnityEngine;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private void Awake()
        {
            GuiText.Initialize(Paths.ConfigPath);
            InitializeConfiguration();
            InitializeResolversAndProviders();
            InitializeServices();
            InitializeControllers();
            InitializeRuntimeState();
            CreateRuntimeHookRegistry();
            LogStartupConfiguration();
            InstallRuntimeHooks();
            StartCoroutine(WaitForGameManagerAndSubscribe());
        }

        private void OnDestroy()
        {
            CommandPanelCursorRenderHooks.ClearCursorOverride();
            ResetServices(true);
            UninstallRuntimeHooks();

            if (_sceneWatcher != null)
            {
                _sceneWatcher.Unsubscribe(OnNewLevelFullyLoaded);
            }
        }

        private IEnumerator WaitForGameManagerAndSubscribe()
        {
            while ((object)GameManager.Instance == null)
            {
                yield return null;
            }

            EnsureAliasRegistryLoaded();
            _sceneWatcher.Subscribe(GameManager.Instance, OnNewLevelFullyLoaded);
            TryExportPickupCatalogOnce();
            Logger.LogInfo(EtgGameplayDashboardLog.Init("GameManager startup detected. Scene watcher subscribed and GUI controller is ready."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init(NAME + " v" + VERSION + " started successfully."));
            StartWindowForegroundMonitor();
        }

        private void ScheduleGameWindowFocusRetryAfterSceneReady()
        {
            if (_gameWindowFocusService == null || _hasScheduledSceneReadyWindowFocusRetry)
            {
                return;
            }

            // Real-world ETG startup logs showed that focusing during plugin Awake/GameManager startup
            // was too early: Steam audio had started, but the foreground-capable ETG windows were not
            // yet stable. We therefore schedule exactly one retry after the first playable foyer load.
            _hasScheduledSceneReadyWindowFocusRetry = true;
            StartCoroutine(FocusGameWindowAfterDelay(4.0f, "first_level_loaded"));
        }

        private IEnumerator FocusGameWindowAfterDelay(float delaySeconds, string reason)
        {
            if (delaySeconds > 0f)
            {
                if (IsStartupWindowFocusVerboseLoggingEnabled())
                {
                    Logger.LogInfo(
                        EtgGameplayDashboardLog.Init(
                            "Scheduling startup window focus attempt after " +
                            delaySeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                            " seconds. Reason=" +
                            reason +
                            "."));
                }

                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            if (_gameWindowFocusService == null)
            {
                yield break;
            }

            // Keep the 1s settle delay aligned with the proven external helper timing. The successful
            // real-machine repro path was: wait for foyer readiness, allow ETG/BepInEx windows to settle,
            // then attempt foreground handoff.
            yield return StartCoroutine(_gameWindowFocusService.FocusWhenReady(10f, 0.25f, 1.0f));
        }

        private void StartWindowForegroundMonitor()
        {
            if (_gameWindowFocusService == null || _hasStartedWindowForegroundMonitor || !IsStartupWindowFocusVerboseLoggingEnabled())
            {
                return;
            }

            _hasStartedWindowForegroundMonitor = true;
            StartCoroutine(_gameWindowFocusService.LogForegroundWindowChanges(20f, 0.25f, "startup_monitor"));
        }

        private void LogBossRushHookSelfCheck(BossRushHookInstallReport report)
        {
            if (report == null)
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Init("Boss Rush startup self-check did not produce a hook report."));
                return;
            }

            Logger.LogInfo(
                EtgGameplayDashboardLog.Init(
                    "Boss Rush startup self-check complete. Applied hooks=" +
                    report.AppliedCount +
                    ", Skipped hooks=" +
                    report.SkippedCount +
                    "."));

            if (!report.HasSkippedHooks)
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Init("Boss Rush startup self-check passed."));
                return;
            }

            string[] skippedHooks = report.SkippedHooks;
            for (int i = 0; i < skippedHooks.Length; i++)
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Init("Boss Rush startup self-check warning: " + skippedHooks[i]));
            }
        }

        private void OnGUI()
        {
            CommandPanelCursorRenderHooks.LogPluginStage(
                "Plugin.OnGUI.begin",
                _commandController != null && _commandController.IsVisibleForDiagnostics);
            if (_commandController != null)
            {
                string panelEventType = Event.current != null ? Event.current.type.ToString() : "<null>";
                _commandController.LogPanelEndToEndHostStage("Plugin.OnGUI.begin", panelEventType);
                PlayerController player = null;
                GameManager gameManager = GameManager.Instance;
                if ((object)gameManager != null)
                {
                    player = gameManager.PrimaryPlayer;
                }

                _commandController.OnGUI(player, Logger);
                _commandController.LogPanelEndToEndHostStage("Plugin.OnGUI.after_command_panel", panelEventType);
                CommandPanelCursorRenderHooks.LogPluginStage(
                    "Plugin.OnGUI.after_command_panel",
                    _commandController.IsVisibleForDiagnostics);
                CommandPanelCursorRenderHooks.DrawCursorAfterPanel(_commandController.IsVisibleForDiagnostics);
                _commandController.LogPanelEndToEndHostStage("CursorAfterPanel", panelEventType);
                _commandController.CompletePanelEndToEndTraceOnRepaint(panelEventType);
            }

            DrawNearbyPickupTipOverlay();
            CommandPanelCursorRenderHooks.LogPluginStage(
                "Plugin.OnGUI.end",
                _commandController != null && _commandController.IsVisibleForDiagnostics);
        }
    }
}
