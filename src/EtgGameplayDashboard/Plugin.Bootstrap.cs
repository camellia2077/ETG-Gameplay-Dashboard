// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using BepInEx;
using EtgGameplayDashboard.Core;
using EtgGameplayDashboard.Core.Input;
using UnityEngine;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private void Awake()
        {
            GuiText.Initialize(Paths.ConfigPath);
            BindConfiguration();
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
            ResetServices();
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
                            delaySeconds.ToString("0.00") +
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
                PlayerController player = null;
                GameManager gameManager = GameManager.Instance;
                if ((object)gameManager != null)
                {
                    player = gameManager.PrimaryPlayer;
                }

                _commandController.OnGUI(player, Logger);
                CommandPanelCursorRenderHooks.LogPluginStage(
                    "Plugin.OnGUI.after_command_panel",
                    _commandController.IsVisibleForDiagnostics);
                CommandPanelCursorRenderHooks.DrawCursorAfterPanel(_commandController.IsVisibleForDiagnostics);
            }

            DrawNearbyPickupTipOverlay();
            CommandPanelCursorRenderHooks.LogPluginStage(
                "Plugin.OnGUI.end",
                _commandController != null && _commandController.IsVisibleForDiagnostics);
        }

        private void EnsureResolvedLoadoutConfig()
        {
            if (_hasResolvedLoadoutConfig)
            {
                return;
            }

            EnsureAliasRegistryLoaded();
            if (_ruleFileProvider != null)
            {
                _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
            }

            LoadoutRuleFileLoadResult ruleFileLoadResult = _ruleFileProvider.Load();
            _ruleDefinitions = ruleFileLoadResult.Definitions;
            _activePresetPickups = ruleFileLoadResult.ActivePresetPickups ?? new LoadoutRuleFilePickupModel[0];
            Logger.LogInfo(
                EtgGameplayDashboardLog.Init(
                    "Loaded start-loadout rules. File=" +
                    _ruleFileProvider.FilePath +
                    ", DefinitionCount=" +
                    (_ruleDefinitions != null ? _ruleDefinitions.Length : 0) +
                    ", PresetPickupCount=" +
                    _activePresetPickups.Length +
                    "."));

            for (int i = 0; i < ruleFileLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Init(ruleFileLoadResult.Messages[i]));
            }

            for (int i = 0; i < ruleFileLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Init(ruleFileLoadResult.Warnings[i]));
            }

            LoadoutConfigResolutionResult resolutionResult = _configResolver.Resolve(_ruleDefinitions, _aliasRegistry);
            _resolvedLoadoutConfig = resolutionResult.Config;
            _hasResolvedLoadoutConfig = true;
            int resolvedRuleCount = _resolvedLoadoutConfig != null && _resolvedLoadoutConfig.Rules != null
                ? _resolvedLoadoutConfig.Rules.Length
                : 0;
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Resolved start-loadout config. ResolvedRuleCount=" + resolvedRuleCount + "."));

            LogSelectionWarnings(resolutionResult.Warnings);
        }

        private void InvalidateResolvedLoadoutConfig()
        {
            _hasResolvedLoadoutConfig = false;
            _resolvedLoadoutConfig = null;
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _activePresetPickups = new LoadoutRuleFilePickupModel[0];
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Invalidated cached start-loadout config. The next automatic grant will reload rules from disk."));
        }

        private void LogSelectionWarnings(SelectionWarning[] warnings)
        {
            for (int i = 0; i < warnings.Length; i++)
            {
                SelectionWarning warning = warnings[i];
                string categoryPrefix = warning.Category.HasValue ? warning.Category.Value + ": " : string.Empty;
                Logger.LogWarning(EtgGameplayDashboardLog.Grant(categoryPrefix + warning.Message + " [Code=" + warning.Code + "]"));
            }
        }

        private PickupAliasRegistry GetAliasRegistry()
        {
            if (!_hasLoadedAliasRegistry)
            {
                EnsureAliasRegistryLoaded();
            }

            return _aliasRegistry ?? PickupAliasRegistry.Empty;
        }

        private string GetUiLanguage()
        {
            return _uiLanguageConfig != null ? GuiText.NormalizeLanguageOverride(_uiLanguageConfig.Value) : "auto";
        }

        private void SetUiLanguage(string languageCode)
        {
            string normalized = GuiText.NormalizeLanguageOverride(languageCode);
            if (_uiLanguageConfig != null)
            {
                _uiLanguageConfig.Value = normalized;
                Config.Save();
            }

            GuiText.SetLanguageOverride(normalized);
            Logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel language preference changed to " + normalized + "."));
        }

        private void LogCommandInput(string message)
        {
            if (Logger != null && !string.IsNullOrEmpty(message))
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Command("[Input] " + message));
            }
        }

        private KeyCode GetCommandPanelKey()
        {
            KeyCode keyCode = ParseCommandPanelKey(GetCommandPanelKeyName());
            return keyCode != KeyCode.None ? keyCode : KeyCode.F7;
        }

        private string GetCommandPanelKeyName()
        {
            string keyName = _commandPanelKeyConfig != null ? _commandPanelKeyConfig.Value : "F7";
            return ParseCommandPanelKey(keyName) != KeyCode.None ? keyName.Trim() : "F7";
        }

        private void SetCommandPanelKey(string keyName)
        {
            string normalized = NormalizeCommandPanelKeyName(keyName);
            if (_commandPanelKeyConfig != null)
            {
                _commandPanelKeyConfig.Value = normalized;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel keyboard toggle key changed to " + normalized + "."));
        }

        private KeyCode GetRoomEnemyRewindKey()
        {
            string keyName = _roomEnemyRewindKeyConfig != null ? _roomEnemyRewindKeyConfig.Value : "C";
            KeyCode keyCode = ParseCommandPanelKey(keyName);
            return keyCode != KeyCode.None ? keyCode : KeyCode.C;
        }

        private string GetCommandPanelControllerShortcut()
        {
            return NormalizeCommandPanelControllerShortcut(_commandPanelControllerShortcutConfig != null ? _commandPanelControllerShortcutConfig.Value : "LB+R3");
        }

        private void SetCommandPanelControllerShortcut(string shortcut)
        {
            string normalized = NormalizeCommandPanelControllerShortcut(shortcut);
            if (_commandPanelControllerShortcutConfig != null)
            {
                _commandPanelControllerShortcutConfig.Value = normalized;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel controller shortcut changed to " + normalized + "."));
        }

        private bool IsCommandPanelControllerShortcutEnabled()
        {
            return _disableCommandPanelControllerShortcutConfig == null ||
                !_disableCommandPanelControllerShortcutConfig.Value;
        }

        private void SetCommandPanelControllerShortcutEnabled(bool isEnabled)
        {
            if (_disableCommandPanelControllerShortcutConfig != null)
            {
                _disableCommandPanelControllerShortcutConfig.Value = !isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel controller shortcut is " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private static string NormalizeCommandPanelControllerShortcut(string shortcut)
        {
            if (string.Equals(shortcut, "LB+R3", System.StringComparison.OrdinalIgnoreCase)) return "LB+R3";
            if (string.Equals(shortcut, "LB+X", System.StringComparison.OrdinalIgnoreCase)) return "LB+X";
            if (string.Equals(shortcut, "LB+Y", System.StringComparison.OrdinalIgnoreCase)) return "LB+Y";
            if (string.Equals(shortcut, "R3", System.StringComparison.OrdinalIgnoreCase)) return "R3";
            return "LB+R3";
        }

        private string GetUiScalePreset()
        {
            return NormalizeUiScalePreset(_uiScalePresetConfig != null ? _uiScalePresetConfig.Value : UiScalePresetCatalog.DefaultPreset);
        }

        private void SetUiScalePreset(string presetName)
        {
            string normalized = NormalizeUiScalePreset(presetName);
            if (_uiScalePresetConfig != null)
            {
                _uiScalePresetConfig.Value = normalized;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel UI size preset changed to " + normalized + "."));
        }

        private string GetThemePreset()
        {
            return DashboardThemeCatalog.Normalize(_themePresetConfig != null ? _themePresetConfig.Value : DashboardThemeCatalog.DefaultThemeId);
        }

        private void SetThemePreset(string themeId)
        {
            string normalized = DashboardThemeCatalog.Normalize(themeId);
            if (_themePresetConfig != null)
            {
                _themePresetConfig.Value = normalized;
                Config.Save();
            }

            DashboardTheme.Select(normalized);
            if (_commandController != null)
            {
                _commandController.RefreshTheme();
            }

            RefreshPickupWikiTipTheme();
        }

        private bool IsExperimentalModeEnabled()
        {
            return _experimentalModeConfig != null && _experimentalModeConfig.Value;
        }

        private bool IsPlayerStatsPanelShown()
        {
            return _showPlayerStatsPanelConfig != null && _showPlayerStatsPanelConfig.Value;
        }

        private bool IsPlayerRewindEnabled()
        {
            return _playerRewindEnabledConfig != null && _playerRewindEnabledConfig.Value;
        }

        private bool IsRoomEnemyRefreshRecordingEnabled()
        {
            return _roomEnemyRefreshRecordingEnabledConfig != null && _roomEnemyRefreshRecordingEnabledConfig.Value;
        }

        private void SetRoomEnemyRefreshRecordingEnabled(bool isEnabled)
        {
            if (_roomEnemyRefreshRecordingEnabledConfig != null)
            {
                _roomEnemyRefreshRecordingEnabledConfig.Value = isEnabled;
                Config.Save();
                Logger.LogInfo(EtgGameplayDashboardLog.Command("Room enemy refresh recording is " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        private string GetRoomEnemyRefreshMethod()
        {
            return NormalizeRoomEnemyRefreshMethod(_roomEnemyRefreshMethodConfig != null ? _roomEnemyRefreshMethodConfig.Value : "rewind");
        }

        private void SetRoomEnemyRefreshMethod(string method)
        {
            string normalized = NormalizeRoomEnemyRefreshMethod(method);
            if (_roomEnemyRefreshMethodConfig != null)
            {
                _roomEnemyRefreshMethodConfig.Value = normalized;
                Config.Save();
                Logger.LogInfo(EtgGameplayDashboardLog.Command("Room enemy refresh method changed to " + normalized + "."));
            }
        }

        private static string NormalizeRoomEnemyRefreshMethod(string method)
        {
            return string.Equals(method, "respawn", StringComparison.OrdinalIgnoreCase) ? "respawn" : "rewind";
        }

        private void SetPlayerRewindEnabled(bool isEnabled)
        {
            if (_playerRewindEnabledConfig != null)
            {
                _playerRewindEnabledConfig.Value = isEnabled;
                Config.Save();
                Logger.LogInfo(EtgGameplayDashboardLog.Command("Player rewind is " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        private bool IsRoomRewindCleanupEnabled()
        {
            return _roomRewindCleanupEnabledConfig != null && _roomRewindCleanupEnabledConfig.Value;
        }

        private void SetRoomRewindCleanupEnabled(bool isEnabled)
        {
            if (_roomRewindCleanupEnabledConfig != null)
            {
                _roomRewindCleanupEnabledConfig.Value = isEnabled;
                Config.Save();
                Logger.LogInfo(EtgGameplayDashboardLog.Command("Room rewind cleanup is " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        private bool IsStartItemsPresetIconsEnabled()
        {
            return _showStartItemsPresetIconsConfig != null && _showStartItemsPresetIconsConfig.Value;
        }

        private bool IsPickupInfoOverlayEnabled()
        {
            return _showPickupInfoOverlayConfig == null || _showPickupInfoOverlayConfig.Value;
        }

        private bool IsPickupInfoQualityEnabled()
        {
            return _showPickupInfoQualityConfig == null || _showPickupInfoQualityConfig.Value;
        }

        private bool IsPickupInfoTypeEnabled()
        {
            return _showPickupInfoTypeConfig == null || _showPickupInfoTypeConfig.Value;
        }

        private bool IsPickupInfoEffectsEnabled()
        {
            return _showPickupInfoEffectsConfig == null || _showPickupInfoEffectsConfig.Value;
        }

        private bool IsPickupInfoSynergiesEnabled()
        {
            return _showPickupInfoSynergiesConfig == null || _showPickupInfoSynergiesConfig.Value;
        }

        private bool IsPickupInfoSummaryEnabled()
        {
            return _showPickupInfoSummaryConfig == null || _showPickupInfoSummaryConfig.Value;
        }

        private bool IsPickupInfoNotesEnabled()
        {
            return _showPickupInfoNotesConfig == null || _showPickupInfoNotesConfig.Value;
        }

        private void SetPlayerStatsPanelShown(bool isEnabled)
        {
            if (_showPlayerStatsPanelConfig != null)
            {
                _showPlayerStatsPanelConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Player stats side panel " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetStartItemsPresetIconsEnabled(bool isEnabled)
        {
            if (_showStartItemsPresetIconsConfig != null)
            {
                _showStartItemsPresetIconsConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Start Items preset icons " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoOverlayEnabled(bool isEnabled)
        {
            if (_showPickupInfoOverlayConfig != null)
            {
                _showPickupInfoOverlayConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info overlay " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoQualityEnabled(bool isEnabled)
        {
            if (_showPickupInfoQualityConfig != null)
            {
                _showPickupInfoQualityConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info quality section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoTypeEnabled(bool isEnabled)
        {
            if (_showPickupInfoTypeConfig != null)
            {
                _showPickupInfoTypeConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info type section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoEffectsEnabled(bool isEnabled)
        {
            if (_showPickupInfoEffectsConfig != null)
            {
                _showPickupInfoEffectsConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info effects section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoSynergiesEnabled(bool isEnabled)
        {
            if (_showPickupInfoSynergiesConfig != null)
            {
                _showPickupInfoSynergiesConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info synergies section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoSummaryEnabled(bool isEnabled)
        {
            if (_showPickupInfoSummaryConfig != null)
            {
                _showPickupInfoSummaryConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info summary section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetPickupInfoNotesEnabled(bool isEnabled)
        {
            if (_showPickupInfoNotesConfig != null)
            {
                _showPickupInfoNotesConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info notes section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetExperimentalModeEnabled(bool isEnabled)
        {
            if (_experimentalModeConfig != null)
            {
                _experimentalModeConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel experimental mode " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private bool IsAmmonomiconFastOpenEnabled()
        {
            if (_ammonomiconFastOpenToggleService != null)
            {
                return AmmonomiconFastOpenToggleService.IsFastOpenEnabled;
            }

            return _ammonomiconFastOpenEnabledConfig != null && _ammonomiconFastOpenEnabledConfig.Value;
        }

        private void SetAmmonomiconFastOpenEnabled(bool isEnabled)
        {
            if (_ammonomiconFastOpenEnabledConfig != null)
            {
                _ammonomiconFastOpenEnabledConfig.Value = isEnabled;
                Config.Save();
            }

            if (_ammonomiconFastOpenToggleService != null &&
                AmmonomiconFastOpenToggleService.IsFastOpenEnabled != isEnabled)
            {
                _ammonomiconFastOpenToggleService.SetIsFastOpenEnabled(isEnabled);
            }

            Logger.LogInfo(EtgGameplayDashboardLog.Command("Ammonomicon fast open " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private bool IsMapTeleportVerboseLoggingEnabled()
        {
            return _mapTeleportVerboseLogsConfig != null && _mapTeleportVerboseLogsConfig.Value;
        }

        private bool IsMuncherVerboseLoggingEnabled()
        {
            return _muncherVerboseLogsConfig != null && _muncherVerboseLogsConfig.Value;
        }

        private bool IsRoomEnemyReplayVerboseLoggingEnabled()
        {
            return _roomEnemyReplayVerboseLogsConfig != null && _roomEnemyReplayVerboseLogsConfig.Value;
        }

        private bool IsBossIntroSkipVerboseLoggingEnabled()
        {
            return _bossIntroSkipVerboseLogsConfig != null && _bossIntroSkipVerboseLogsConfig.Value;
        }

        private bool IsFloorTeleportVerboseLoggingEnabled()
        {
            return _floorTeleportVerboseLogsConfig != null && _floorTeleportVerboseLogsConfig.Value;
        }

        private bool IsBossRushVerboseLoggingEnabled()
        {
            return _bossRushVerboseLogsConfig != null && _bossRushVerboseLogsConfig.Value;
        }

        private bool IsBossSelectionVerboseLoggingEnabled()
        {
            // Keep diagnostics code-only so BepInEx does not recreate the old
            // [Debug] config entry and its comments in generated config files.
            return false;
        }

        private bool IsCommandPanelHealthVerboseLoggingEnabled()
        {
            return _commandPanelHealthVerboseLogsConfig != null && _commandPanelHealthVerboseLogsConfig.Value;
        }

        private bool IsCommandPanelCursorVerboseLoggingEnabled()
        {
            return _commandPanelCursorVerboseLogsConfig != null && _commandPanelCursorVerboseLogsConfig.Value;
        }

        private bool IsCommandPanelGameplayInputVerboseLoggingEnabled()
        {
            return _commandPanelGameplayInputVerboseLogsConfig != null && _commandPanelGameplayInputVerboseLogsConfig.Value;
        }

        private bool IsCommandPanelControllerGameplayInputVerboseLoggingEnabled()
        {
            return _commandPanelControllerGameplayInputVerboseLogsConfig != null && _commandPanelControllerGameplayInputVerboseLogsConfig.Value;
        }

        private bool IsCommandPanelShortcutVerboseLoggingEnabled()
        {
            return _commandPanelShortcutVerboseLogsConfig != null && _commandPanelShortcutVerboseLogsConfig.Value;
        }

        private bool IsCommandPanelCursorRenderVerboseLoggingEnabled()
        {
            return _commandPanelCursorRenderVerboseLogsConfig != null && _commandPanelCursorRenderVerboseLogsConfig.Value;
        }

        private bool IsControllerAimVerboseLoggingEnabled()
        {
            return _controllerAimVerboseLogsConfig != null && _controllerAimVerboseLogsConfig.Value;
        }

        private bool IsCommandPanelCursorRenderProbeEnabled()
        {
            return _commandPanelCursorRenderProbeConfig != null && _commandPanelCursorRenderProbeConfig.Value;
        }

        private bool IsCommandPanelCursorAbovePanelEnabled()
        {
            return _enableCommandPanelCursorAbovePanelConfig != null && _enableCommandPanelCursorAbovePanelConfig.Value;
        }

        private bool IsActiveItemGrantVerboseLoggingEnabled()
        {
            return _activeItemGrantVerboseLogsConfig != null && _activeItemGrantVerboseLogsConfig.Value;
        }

        private bool IsNearbyPickupVerboseLoggingEnabled()
        {
            return _nearbyPickupVerboseLogsConfig != null && _nearbyPickupVerboseLogsConfig.Value;
        }

        private bool IsStartupWindowFocusVerboseLoggingEnabled()
        {
            return _startupWindowFocusVerboseLogsConfig != null && _startupWindowFocusVerboseLogsConfig.Value;
        }

        private bool IsPerformanceVerboseLoggingEnabled()
        {
            return _performanceVerboseLogsConfig != null && _performanceVerboseLogsConfig.Value;
        }

        private bool IsCharacterSwitchVerboseLoggingEnabled()
        {
            return _characterSwitchVerboseLogsConfig != null && _characterSwitchVerboseLogsConfig.Value;
        }

        private bool IsDamageDiagnosticsVerboseLoggingEnabled()
        {
            return _damageDiagnosticsVerboseLogsConfig != null && _damageDiagnosticsVerboseLogsConfig.Value;
        }

        private string NormalizeUiScalePreset(string presetName)
        {
            string normalized = UiScalePresetCatalog.Normalize(presetName);
            if (string.Equals(normalized, presetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            Logger.LogWarning(EtgGameplayDashboardLog.Init("Invalid command panel UI size preset '" + presetName + "'. Falling back to " + UiScalePresetCatalog.DefaultPreset + "."));
            return normalized;
        }

        private string NormalizeCommandPanelKeyName(string keyName)
        {
            string normalized = string.IsNullOrEmpty(keyName) ? "F7" : keyName.Trim();
            if (ParseCommandPanelKey(normalized) != KeyCode.None)
            {
                return normalized;
            }

            Logger.LogWarning(EtgGameplayDashboardLog.Init("Invalid command panel keyboard key '" + normalized + "'. Falling back to F7."));
            return "F7";
        }


        private static KeyCode ParseCommandPanelKey(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
            {
                return KeyCode.F7;
            }

            try
            {
                object parsed = System.Enum.Parse(typeof(KeyCode), keyName.Trim(), true);
                if (!(parsed is KeyCode) || !System.Enum.IsDefined(typeof(KeyCode), parsed))
                {
                    return KeyCode.None;
                }

                return (KeyCode)parsed;
            }
            catch (System.ArgumentException)
            {
                return KeyCode.None;
            }
        }

        private string GetActiveStartItemsPreset()
        {
            return _activeStartItemsPresetConfig != null
                ? StartItemsPresetNames.NormalizePresetId(_activeStartItemsPresetConfig.Value)
                : StartItemsPresetNames.DefaultPresetId;
        }

        private void SetActiveStartItemsPreset(string presetName)
        {
            string normalized = StartItemsPresetNames.NormalizePresetId(presetName);

            if (_activeStartItemsPresetConfig != null)
            {
                _activeStartItemsPresetConfig.Value = normalized;
                Config.Save();
            }

            if (_ruleFileProvider != null)
            {
                _ruleFileProvider.ActivePresetName = normalized;
            }

            InvalidateResolvedLoadoutConfig();
            Logger.LogInfo(EtgGameplayDashboardLog.Command("Active start-items preset changed to " + normalized + "."));
        }

        private string NormalizeRoomEnemyRewindKeyName(string keyName)
        {
            string normalized = string.IsNullOrEmpty(keyName) ? "C" : keyName.Trim();
            if (ParseCommandPanelKey(normalized) != KeyCode.None)
            {
                return normalized;
            }

            Logger.LogWarning(EtgGameplayDashboardLog.Init("Invalid room enemy rewind keyboard key '" + normalized + "'. Falling back to C."));
            return "C";
        }

        private string GetCombatCursorColor()
        {
            if (_combatCursorColorEnabledConfig == null || !_combatCursorColorEnabledConfig.Value)
            {
                return CombatCursorColorCatalog.DisabledId;
            }

            return _combatCursorColorPresetConfig != null
                ? CombatCursorColorCatalog.Normalize(_combatCursorColorPresetConfig.Value)
                : CombatCursorColorCatalog.DefaultPresetId;
        }

        private void PersistEnemyHealthBarsEnabled(bool enabled)
        {
            if (_enemyHealthBarsEnabledConfig == null)
            {
                return;
            }

            _enemyHealthBarsEnabledConfig.Value = enabled;
            Config.Save();
        }

        private void PersistControllerAimLockEnabled(bool enabled)
        {
            if (_controllerAimLockEnabledConfig == null)
            {
                return;
            }

            _controllerAimLockEnabledConfig.Value = enabled;
            Config.Save();
        }

        private void PersistKeyboardAimAssistSettings(KeyboardAimAssistSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            bool changed = false;
            string mode = KeyboardAimAssistSettings.GetModeConfigValue(settings.Mode);
            if (_keyboardAimAssistModeConfig != null && _keyboardAimAssistModeConfig.Value != mode)
            {
                _keyboardAimAssistModeConfig.Value = mode;
                changed = true;
            }
            if (_keyboardAimAssistMultiplierConfig != null &&
                !Mathf.Approximately(_keyboardAimAssistMultiplierConfig.Value, settings.Multiplier))
            {
                _keyboardAimAssistMultiplierConfig.Value = settings.Multiplier;
                changed = true;
            }
            if (_keyboardAimAssistEnabledConfig != null &&
                _keyboardAimAssistEnabledConfig.Value != settings.IsEnabled)
            {
                _keyboardAimAssistEnabledConfig.Value = settings.IsEnabled;
                changed = true;
            }

            if (changed)
            {
                Config.Save();
            }
        }

        private void PersistRapidFireEnabled(bool enabled)
        {
            if (_rapidFireEnabledConfig == null)
            {
                return;
            }

            _rapidFireEnabledConfig.Value = enabled;
            Config.Save();
        }

        private void PersistAutoReloadMode(AutoReloadMode mode)
        {
            if (_autoReloadModeConfig == null)
            {
                return;
            }

            _autoReloadModeConfig.Value = mode.ToString();
            Config.Save();
        }

        private void PersistAmmoMode(AmmoMode mode)
        {
            if (_ammoModeConfig == null)
            {
                return;
            }

            _ammoModeConfig.Value = mode.ToString();
            Config.Save();
        }

        private static AutoReloadMode ParseAutoReloadMode(string value)
        {
            if (string.Equals(value, "Instant", StringComparison.OrdinalIgnoreCase))
            {
                return AutoReloadMode.Instant;
            }

            if (string.Equals(value, "Animated", StringComparison.OrdinalIgnoreCase))
            {
                return AutoReloadMode.Animated;
            }

            return AutoReloadMode.Off;
        }

        private static AmmoMode ParseAmmoMode(string value)
        {
            if (string.Equals(value, "InfiniteReserve", StringComparison.OrdinalIgnoreCase))
            {
                return AmmoMode.InfiniteReserve;
            }

            if (string.Equals(value, "NoConsume", StringComparison.OrdinalIgnoreCase))
            {
                return AmmoMode.NoConsume;
            }

            return AmmoMode.Off;
        }

        private Color GetCombatCursorColorValue()
        {
            return CombatCursorColorCatalog.IsEnabled(GetCombatCursorColor())
                ? CombatCursorColorCatalog.Get(GetCombatCursorColor()).Color
                : Color.white;
        }

        private void SetCombatCursorColor(string colorId)
        {
            string normalized = CombatCursorColorCatalog.Normalize(colorId);
            if (string.Equals(normalized, CombatCursorColorCatalog.DisabledId, System.StringComparison.OrdinalIgnoreCase))
            {
                if (_combatCursorColorEnabledConfig != null)
                {
                    _combatCursorColorEnabledConfig.Value = false;
                }

                Config.Save();
                Logger.LogInfo(EtgGameplayDashboardLog.Command("Combat cursor color disabled."));
                return;
            }

            CombatCursorColorOption selectedOption = CombatCursorColorCatalog.Get(normalized);
            if (_combatCursorColorPresetConfig != null)
            {
                _combatCursorColorPresetConfig.Value = normalized;
            }

            if (_combatCursorColorEnabledConfig != null)
            {
                _combatCursorColorEnabledConfig.Value = true;
            }

            Config.Save();

            Logger.LogInfo(EtgGameplayDashboardLog.Command(
                "Combat cursor color changed to " + normalized + " " + selectedOption.Hex +
                " RGB(" + selectedOption.Color.r.ToString("F3") + "," +
                selectedOption.Color.g.ToString("F3") + "," +
                selectedOption.Color.b.ToString("F3") + ")."));
        }


        private void EnsureAliasRegistryLoaded()
        {
            if (_hasLoadedAliasRegistry || _aliasFileProvider == null)
            {
                return;
            }

            if ((object)GameManager.Instance == null)
            {
                return;
            }

            AliasLoadResult aliasLoadResult = _aliasFileProvider.Load(IsSupportedGrantablePickupId);
            _aliasRegistry = aliasLoadResult.Registry ?? PickupAliasRegistry.Empty;
            _hasLoadedAliasRegistry = true;

            for (int i = 0; i < aliasLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Alias(aliasLoadResult.Messages[i]));
            }

            for (int i = 0; i < aliasLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Alias(aliasLoadResult.Warnings[i]));
            }
        }

        private bool IsSupportedGrantablePickupId(int pickupId)
        {
            return _pickupResolver.ResolveAny(pickupId).Succeeded;
        }
    }
}
