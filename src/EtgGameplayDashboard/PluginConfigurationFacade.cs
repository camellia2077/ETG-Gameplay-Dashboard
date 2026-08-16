// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using EtgGameplayDashboard.Core.Input;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class PluginConfigurationFacade
    {
        private readonly ConfigFile _configFile;
        private readonly ManualLogSource _logger;
        private readonly Action<string> _activeStartItemsPresetChanged;
        private readonly Action<string> _themeChanged;
        private readonly Action<bool> _ammonomiconFastOpenChanged;

        private ConfigEntry<bool> _enableEtgGameplayDashboardConfig;
        private ConfigEntry<string> _uiLanguageConfig;
        private ConfigEntry<string> _commandPanelKeyConfig;
        private ConfigEntry<string> _keyboardShortcutsConfig;
        private ConfigEntry<bool> _roomEnemyRefreshRecordingEnabledConfig;
        private ConfigEntry<string> _roomEnemyRefreshMethodConfig;
        private ConfigEntry<bool> _playerRewindEnabledConfig;
        private ConfigEntry<bool> _roomRewindCleanupEnabledConfig;
        private ConfigEntry<string> _commandPanelControllerShortcutConfig;
        private ConfigEntry<bool> _disableCommandPanelControllerShortcutConfig;
        private ConfigEntry<string> _uiScalePresetConfig;
        private ConfigEntry<string> _themePresetConfig;
        private ConfigEntry<bool> _showStartItemsPresetIconsConfig;
        private ConfigEntry<bool> _showPlayerStatsPanelConfig;
        private ConfigEntry<bool> _showCommandPanelCloseButtonConfig;
        private ConfigEntry<bool> _revealMapEveryFloorConfig;
        private ConfigEntry<bool> _showPickupInfoOverlayConfig;
        private ConfigEntry<bool> _showPickupInfoQualityConfig;
        private ConfigEntry<bool> _showPickupInfoTypeConfig;
        private ConfigEntry<bool> _showPickupInfoEffectsConfig;
        private ConfigEntry<bool> _showPickupInfoSynergiesConfig;
        private ConfigEntry<bool> _showPickupInfoSummaryConfig;
        private ConfigEntry<bool> _showPickupInfoNotesConfig;
        private ConfigEntry<bool> _experimentalModeConfig;
        private ConfigEntry<bool> _ammonomiconFastOpenEnabledConfig;
        private ConfigEntry<bool> _mapTeleportVerboseLogsConfig;
        private ConfigEntry<bool> _muncherVerboseLogsConfig;
        private ConfigEntry<bool> _roomEnemyReplayVerboseLogsConfig;
        private ConfigEntry<bool> _bossIntroSkipVerboseLogsConfig;
        private ConfigEntry<bool> _floorTeleportVerboseLogsConfig;
        private ConfigEntry<bool> _bossRushVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelHealthVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelCursorVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelGameplayInputVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelControllerGameplayInputVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelShortcutVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelCursorRenderVerboseLogsConfig;
        private ConfigEntry<bool> _controllerAimVerboseLogsConfig;
        private ConfigEntry<bool> _commandPanelCursorRenderProbeConfig;
        private ConfigEntry<bool> _enableCommandPanelCursorAbovePanelConfig;
        private ConfigEntry<bool> _activeItemGrantVerboseLogsConfig;
        private ConfigEntry<bool> _nearbyPickupVerboseLogsConfig;
        private ConfigEntry<bool> _startupWindowFocusVerboseLogsConfig;
        private ConfigEntry<bool> _performanceVerboseLogsConfig;
        private ConfigEntry<bool> _characterSwitchVerboseLogsConfig;
        private ConfigEntry<bool> _damageDiagnosticsVerboseLogsConfig;
        private ConfigEntry<string> _activeStartItemsPresetConfig;
        private ConfigEntry<bool> _combatCursorColorEnabledConfig;
        private ConfigEntry<string> _combatCursorColorPresetConfig;
        private ConfigEntry<bool> _enemyHealthBarsEnabledConfig;
        private ConfigEntry<bool> _bossIntroSkipEnabledConfig;
        private ConfigEntry<bool> _skipChargeEnabledConfig;
        private ConfigEntry<bool> _invincibilityEnabledConfig;
        private ConfigEntry<bool> _flightEnabledConfig;
        private ConfigEntry<bool> _controllerAimLockEnabledConfig;
        private ConfigEntry<bool> _keyboardAimAssistEnabledConfig;
        private ConfigEntry<string> _keyboardAimAssistLevelConfig;
        private ConfigEntry<string> _keyboardAimAssistModeConfig;
        private ConfigEntry<float> _keyboardAimAssistMultiplierConfig;
        private ConfigEntry<bool> _rapidFireEnabledConfig;
        private ConfigEntry<string> _autoReloadModeConfig;
        private ConfigEntry<string> _ammoModeConfig;
        private ConfigEntry<bool> _activeItemNoCooldownEnabledConfig;
        private KeyboardShortcutRegistry _keyboardShortcutRegistry;
        internal ConfigEntry<bool> EnableEtgGameplayDashboardConfig { get { return _enableEtgGameplayDashboardConfig; } }
        internal ConfigEntry<string> UiLanguageConfig { get { return _uiLanguageConfig; } }
        internal ConfigEntry<string> CommandPanelKeyConfig { get { return _commandPanelKeyConfig; } }
        internal ConfigEntry<string> KeyboardShortcutsConfig { get { return _keyboardShortcutsConfig; } }
        internal ConfigEntry<bool> RoomEnemyRefreshRecordingEnabledConfig { get { return _roomEnemyRefreshRecordingEnabledConfig; } }
        internal ConfigEntry<string> RoomEnemyRefreshMethodConfig { get { return _roomEnemyRefreshMethodConfig; } }
        internal ConfigEntry<bool> PlayerRewindEnabledConfig { get { return _playerRewindEnabledConfig; } }
        internal ConfigEntry<bool> RoomRewindCleanupEnabledConfig { get { return _roomRewindCleanupEnabledConfig; } }
        internal ConfigEntry<string> CommandPanelControllerShortcutConfig { get { return _commandPanelControllerShortcutConfig; } }
        internal ConfigEntry<bool> DisableCommandPanelControllerShortcutConfig { get { return _disableCommandPanelControllerShortcutConfig; } }
        internal ConfigEntry<string> UiScalePresetConfig { get { return _uiScalePresetConfig; } }
        internal ConfigEntry<string> ThemePresetConfig { get { return _themePresetConfig; } }
        internal ConfigEntry<bool> ShowStartItemsPresetIconsConfig { get { return _showStartItemsPresetIconsConfig; } }
        internal ConfigEntry<bool> ShowPlayerStatsPanelConfig { get { return _showPlayerStatsPanelConfig; } }
        internal ConfigEntry<bool> ShowCommandPanelCloseButtonConfig { get { return _showCommandPanelCloseButtonConfig; } }
        internal ConfigEntry<bool> RevealMapEveryFloorConfig { get { return _revealMapEveryFloorConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoOverlayConfig { get { return _showPickupInfoOverlayConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoQualityConfig { get { return _showPickupInfoQualityConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoTypeConfig { get { return _showPickupInfoTypeConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoEffectsConfig { get { return _showPickupInfoEffectsConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoSynergiesConfig { get { return _showPickupInfoSynergiesConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoSummaryConfig { get { return _showPickupInfoSummaryConfig; } }
        internal ConfigEntry<bool> ShowPickupInfoNotesConfig { get { return _showPickupInfoNotesConfig; } }
        internal ConfigEntry<bool> ExperimentalModeConfig { get { return _experimentalModeConfig; } }
        internal ConfigEntry<bool> AmmonomiconFastOpenEnabledConfig { get { return _ammonomiconFastOpenEnabledConfig; } }
        internal ConfigEntry<bool> MapTeleportVerboseLogsConfig { get { return _mapTeleportVerboseLogsConfig; } }
        internal ConfigEntry<bool> MuncherVerboseLogsConfig { get { return _muncherVerboseLogsConfig; } }
        internal ConfigEntry<bool> RoomEnemyReplayVerboseLogsConfig { get { return _roomEnemyReplayVerboseLogsConfig; } }
        internal ConfigEntry<bool> BossIntroSkipVerboseLogsConfig { get { return _bossIntroSkipVerboseLogsConfig; } }
        internal ConfigEntry<bool> FloorTeleportVerboseLogsConfig { get { return _floorTeleportVerboseLogsConfig; } }
        internal ConfigEntry<bool> BossRushVerboseLogsConfig { get { return _bossRushVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelHealthVerboseLogsConfig { get { return _commandPanelHealthVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelCursorVerboseLogsConfig { get { return _commandPanelCursorVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelGameplayInputVerboseLogsConfig { get { return _commandPanelGameplayInputVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelControllerGameplayInputVerboseLogsConfig { get { return _commandPanelControllerGameplayInputVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelShortcutVerboseLogsConfig { get { return _commandPanelShortcutVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelCursorRenderVerboseLogsConfig { get { return _commandPanelCursorRenderVerboseLogsConfig; } }
        internal ConfigEntry<bool> ControllerAimVerboseLogsConfig { get { return _controllerAimVerboseLogsConfig; } }
        internal ConfigEntry<bool> CommandPanelCursorRenderProbeConfig { get { return _commandPanelCursorRenderProbeConfig; } }
        internal ConfigEntry<bool> EnableCommandPanelCursorAbovePanelConfig { get { return _enableCommandPanelCursorAbovePanelConfig; } }
        internal ConfigEntry<bool> ActiveItemGrantVerboseLogsConfig { get { return _activeItemGrantVerboseLogsConfig; } }
        internal ConfigEntry<bool> NearbyPickupVerboseLogsConfig { get { return _nearbyPickupVerboseLogsConfig; } }
        internal ConfigEntry<bool> StartupWindowFocusVerboseLogsConfig { get { return _startupWindowFocusVerboseLogsConfig; } }
        internal ConfigEntry<bool> PerformanceVerboseLogsConfig { get { return _performanceVerboseLogsConfig; } }
        internal ConfigEntry<bool> CharacterSwitchVerboseLogsConfig { get { return _characterSwitchVerboseLogsConfig; } }
        internal ConfigEntry<bool> DamageDiagnosticsVerboseLogsConfig { get { return _damageDiagnosticsVerboseLogsConfig; } }
        internal ConfigEntry<string> ActiveStartItemsPresetConfig { get { return _activeStartItemsPresetConfig; } }
        internal ConfigEntry<bool> CombatCursorColorEnabledConfig { get { return _combatCursorColorEnabledConfig; } }
        internal ConfigEntry<string> CombatCursorColorPresetConfig { get { return _combatCursorColorPresetConfig; } }
        internal ConfigEntry<bool> EnemyHealthBarsEnabledConfig { get { return _enemyHealthBarsEnabledConfig; } }
        internal ConfigEntry<bool> BossIntroSkipEnabledConfig { get { return _bossIntroSkipEnabledConfig; } }
        internal ConfigEntry<bool> SkipChargeEnabledConfig { get { return _skipChargeEnabledConfig; } }
        internal ConfigEntry<bool> InvincibilityEnabledConfig { get { return _invincibilityEnabledConfig; } }
        internal ConfigEntry<bool> FlightEnabledConfig { get { return _flightEnabledConfig; } }
        internal ConfigEntry<bool> ControllerAimLockEnabledConfig { get { return _controllerAimLockEnabledConfig; } }
        internal ConfigEntry<bool> KeyboardAimAssistEnabledConfig { get { return _keyboardAimAssistEnabledConfig; } }
        internal ConfigEntry<string> KeyboardAimAssistLevelConfig { get { return _keyboardAimAssistLevelConfig; } }
        internal ConfigEntry<string> KeyboardAimAssistModeConfig { get { return _keyboardAimAssistModeConfig; } }
        internal ConfigEntry<float> KeyboardAimAssistMultiplierConfig { get { return _keyboardAimAssistMultiplierConfig; } }
        internal ConfigEntry<bool> RapidFireEnabledConfig { get { return _rapidFireEnabledConfig; } }
        internal ConfigEntry<string> AutoReloadModeConfig { get { return _autoReloadModeConfig; } }
        internal ConfigEntry<string> AmmoModeConfig { get { return _ammoModeConfig; } }
        internal ConfigEntry<bool> ActiveItemNoCooldownEnabledConfig { get { return _activeItemNoCooldownEnabledConfig; } }
        internal KeyboardShortcutRegistry KeyboardShortcutRegistry { get { return _keyboardShortcutRegistry; } }

        internal PluginConfigurationFacade(
            ConfigFile configFile,
            ManualLogSource logger,
            Action<string> activeStartItemsPresetChanged,
            Action<string> themeChanged,
            Action<bool> ammonomiconFastOpenChanged)
        {
            _configFile = configFile;
            _logger = logger;
            _activeStartItemsPresetChanged = activeStartItemsPresetChanged;
            _themeChanged = themeChanged;
            _ammonomiconFastOpenChanged = ammonomiconFastOpenChanged;
        }

        private void SaveConfig()
        {
            if (_configFile != null)
            {
                _configFile.Save();
            }
        }

        private bool SetAndSave<T>(ConfigEntry<T> config, T value)
        {
            if (config == null)
            {
                return false;
            }

            config.Value = value;
            SaveConfig();
            return true;
        }

        private static bool ReadBool(ConfigEntry<bool> config, bool defaultValue)
        {
            return config == null ? defaultValue : config.Value;
        }

        internal void BindConfiguration()
        {
            BindGeneralConfiguration();
            BindUiConfiguration();
            BindDebugConfiguration();
            BindStartItemsConfiguration();
            BindCombatConfiguration();
        }

        private void BindGeneralConfiguration()
        {
            _enableEtgGameplayDashboardConfig = _configFile.Bind(
                "General",
                "EnableEtgGameplayDashboard",
                true,
                "Enable or disable the automatic start-of-run loadout grant.");
        }

        private void BindUiConfiguration()
        {
            _uiLanguageConfig = _configFile.Bind(
                "UI",
                "Language",
                "auto",
                "Command panel language. Use auto, en, or zh-CN.");
            _uiLanguageConfig.Value = GuiText.NormalizeLanguageOverride(_uiLanguageConfig.Value);
            GuiText.SetLanguageOverride(_uiLanguageConfig.Value);
            _commandPanelKeyConfig = _configFile.Bind(
                "UI",
                "CommandPanelKey",
                "F7",
                "Command panel keyboard toggle key. Use a Unity KeyCode name such as F7, F8, Insert, or BackQuote.");
            _commandPanelKeyConfig.Value = NormalizeCommandPanelKeyName(_commandPanelKeyConfig.Value);
            _keyboardShortcutsConfig = _configFile.Bind(
                "UI",
                "KeyboardShortcuts",
                "room.rewind=C",
                "Keyboard shortcuts as targetId=KeyCode pairs separated by commas. Catalog pickups use numeric IDs; currency actions use names such as currency.max_health; room.rewind controls Room rewind. Control panel keys are not allowed.");
            _keyboardShortcutRegistry = KeyboardShortcutRegistry.Parse(_keyboardShortcutsConfig.Value);
            _roomEnemyRefreshRecordingEnabledConfig = _configFile.Bind(
                "UI",
                "RoomEnemyRefreshRecordingEnabled",
                false,
                "Enable recording of standard and Boss room enemy waves for Rewind. Disabled by default.");
            _roomEnemyRefreshMethodConfig = _configFile.Bind(
                "UI",
                "RoomEnemyRefreshMethod",
                "rewind",
                "Room enemy refresh mode. Use rewind or respawn.");
            _roomEnemyRefreshMethodConfig.Value = NormalizeRoomEnemyRefreshMethod(_roomEnemyRefreshMethodConfig.Value);
            _playerRewindEnabledConfig = _configFile.Bind(
                "UI",
                "PlayerRewindEnabled",
                true,
                "Restore the player's recorded state when rewinding a room. Enabled by default.");
            _roomRewindCleanupEnabledConfig = _configFile.Bind(
                "UI",
                "RoomRewindCleanupEnabled",
                true,
                "Remove rewind-room decals, scene drops, currency, and Boss reward pedestals before replay. Enabled by default.");
            _commandPanelControllerShortcutConfig = _configFile.Bind(
                "UI",
                "CommandPanelControllerShortcut",
                "LB+R3",
                "Controller shortcut for opening the command panel. Supported values: LB+R3, LB+X, LB+Y, or R3.");
            _commandPanelControllerShortcutConfig.Value = NormalizeCommandPanelControllerShortcut(_commandPanelControllerShortcutConfig.Value);
            _disableCommandPanelControllerShortcutConfig = _configFile.Bind(
                "UI",
                "DisableCommandPanelControllerShortcut",
                false,
                "Disable the controller shortcut for opening or closing the command panel. The keyboard shortcut remains available.");
            _uiScalePresetConfig = _configFile.Bind(
                "UI",
                "PanelScalePreset",
                UiScalePresetCatalog.DefaultPreset,
                "Command panel UI size preset. Use x-small, small, medium-small, medium, medium-large, large, x-large, or xx-large.");
            _uiScalePresetConfig.Value = NormalizeUiScalePreset(_uiScalePresetConfig.Value);
            _themePresetConfig = _configFile.Bind(
                "UI",
                "ThemePreset",
                DashboardThemeCatalog.DefaultThemeId,
                "Stable dashboard theme ID. Theme names and colors are defined by the plugin and are not stored in config.");
            _themePresetConfig.Value = DashboardThemeCatalog.Normalize(_themePresetConfig.Value);
            _showStartItemsPresetIconsConfig = _configFile.Bind(
                "UI",
                "ShowStartItemsPresetIcons",
                true,
                "Show item icons in the Start Items preset list preview.");
            _showPlayerStatsPanelConfig = _configFile.Bind(
                "UI",
                "ShowPlayerStatsPanel",
                false,
                "Show or hide the player stats side panel by default.");
            _showCommandPanelCloseButtonConfig = _configFile.Bind(
                "UI",
                "ShowCommandPanelCloseButton",
                true,
                "Show or hide the X close button on the command panel.");
            _revealMapEveryFloorConfig = _configFile.Bind(
                "UI",
                "RevealMapEveryFloor",
                false,
                "Automatically reveal each new floor after entering it. Disabled keeps Reveal Map as a current-floor-only action.");
            _showPickupInfoOverlayConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoOverlay",
                true,
                "Show or hide the nearby dropped-pickup detailed info overlay.");
            _showPickupInfoQualityConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoQuality",
                true,
                "Show or hide the Quality section in the nearby pickup info overlay.");
            _showPickupInfoTypeConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoType",
                true,
                "Show or hide the Type section in the nearby pickup info overlay.");
            _showPickupInfoEffectsConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoEffects",
                true,
                "Show or hide the Effects section in the nearby pickup info overlay.");
            _showPickupInfoSynergiesConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoSynergies",
                true,
                "Show or hide the Synergies section in the nearby pickup info overlay.");
            _showPickupInfoSummaryConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoSummary",
                true,
                "Show or hide the Summary section in the nearby pickup info overlay.");
            _showPickupInfoNotesConfig = _configFile.Bind(
                "UI",
                "ShowPickupInfoNotes",
                true,
                "Show or hide the Notes section in the nearby pickup info overlay.");
            _experimentalModeConfig = _configFile.Bind(
                "UI",
                "ExperimentalMode",
                false,
                "Enable unfinished or lower-quality control-panel features.");
            _ammonomiconFastOpenEnabledConfig = _configFile.Bind(
                "UI",
                "AmmonomiconFastOpen",
                false,
                "Enable or disable fast-open for the Ammonomicon. When enabled, the opening animation is skipped.");
        }

        private void BindDebugConfiguration()
        {
            _mapTeleportVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableMapTeleportVerboseLogs",
                false,
                "Enable verbose Reveal Map and floor-transition diagnostic logs, including teleporter-promotion sampling. Keep disabled for normal play and enable only when debugging map, stairs/elevator, or teleporter behavior.");
            _muncherVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableMuncherVerboseLogs",
                false,
                "Enable verbose Gunber / Evil Muncher spawn diagnostic logs. Keep disabled for normal play and enable only when debugging muncher spawn, placement, or room-registration behavior.");
            _roomEnemyReplayVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableRoomEnemyReplayVerboseLogs",
                false,
                "Enable verbose room enemy replay diagnostics, including recorded waves, replay spawn results, and Boss rewind phase timings. Keep disabled for normal play and enable only when debugging Refresh Room Enemies.");
            _bossIntroSkipVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableBossIntroSkipVerboseLogs",
                false,
                "Enable verbose Boss intro skip diagnostics, including Boss trigger-zone detection, GenericIntroDoer state, native skip requests, and startup failures. Keep disabled for normal play and enable only when debugging Skip Boss Intro.");
            _floorTeleportVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableFloorTeleportVerboseLogs",
                false,
                "Enable verbose floor teleport diagnostic logs, including foyer bootstrap and deferred readiness checks. Keep disabled for normal play and enable only when debugging floor teleport behavior.");
            _bossRushVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableBossRushVerboseLogs",
                false,
                "Enable verbose Boss Rush flow diagnostic logs, including floor readiness and room handoff tracing. Keep disabled for normal play and enable only when debugging Boss Rush runtime behavior.");
            _commandPanelHealthVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelHealthVerboseLogs",
                false,
                "Enable verbose command-panel health and armor diagnostics, including input override lifecycle, weapon-switch side effects, and tracked max-health rollback restoration. Keep disabled for normal play and enable only when debugging repeated heart or armor HUD animations.");
            _commandPanelCursorVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelCursorVerboseLogs",
                true,
                "Enable verbose command-panel cursor diagnostics, including cursor visibility changes, active input-device switches, P1/P2 input state, cursor tint, and mouse click attempts while the panel is open. Enabled temporarily while diagnosing two-player keyboard/controller handoff and unexpected cursor colors.");
            _commandPanelGameplayInputVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelGameplayInputVerboseLogs",
                false,
                "Enable sampled command-panel gameplay keyboard diagnostics. Logs WASD key state changes together with panel visibility, player input override state, and PlayerInputState. Keep disabled for normal play and enable only when debugging gameplay movement while the panel is open.");
            _commandPanelControllerGameplayInputVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelControllerGameplayInputVerboseLogs",
                false,
                "Enable sampled command-panel gameplay controller diagnostics. Logs the active controller, D-pad, left stick, right stick, player input override state, and PlayerInputState. Keep disabled for normal play and enable only when debugging controller movement while the panel is open.");
            _commandPanelShortcutVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelShortcutVerboseLogs",
                true,
                "Enable command-panel keyboard/controller shortcut diagnostics, including configured keys, shortcut detection results, panel visibility, game type, and P1/P2 readiness. Enabled temporarily while diagnosing panel opening failures in two-player mode.");
            _commandPanelCursorRenderVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelCursorRenderVerboseLogs",
                false,
                "Enable sampled command-panel cursor render-order diagnostics for ETG GameCursorController.OnGUI and EtgGameplayDashboard.OnGUI. Keep disabled for normal play and enable only when debugging cursor layering.");
            _controllerAimVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableControllerAimVerboseLogs",
                false,
                "Enable sampled controller/mouse aim diagnostics. Logs the player center, raw aim point, aim distance, input vector, and device mode. Keep disabled for normal play and enable only while reproducing controller or cursor view rotation.");
            _commandPanelCursorRenderProbeConfig = _configFile.Bind(
                "Debug",
                "EnableCommandPanelCursorRenderProbe",
                false,
                "Temporarily draw a white, exact-position copy of the ETG mouse cursor after the Control Panel to verify cursor layering. This does not disable the original cursor and should be disabled after testing.");
            _enableCommandPanelCursorAbovePanelConfig = _configFile.Bind(
                "UI",
                "EnableCommandPanelCursorAbovePanel",
                false,
                "Draw the ETG mouse cursor above the Control Panel while it is open. The original mouse cursor is suppressed only while the panel is visible; controller navigation remains panel-controlled.");
            _activeItemGrantVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableActiveItemGrantVerboseLogs",
                false,
                "Enable verbose active-item grant diagnostics, including temporary slot-capacity expansion, ETG grant-path rejection details, and rollback restoration tracing. Keep disabled for normal play and enable only when debugging active items dropping near the player instead of entering the active-item bar.");
            _nearbyPickupVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableNearbyPickupVerboseLogs",
                false,
                "Enable verbose nearby pickup overlay diagnostics, including dropped-pickup scans, shop-item scans, gameplay-catalog lookup results, and final overlay target selection. Keep disabled for normal play and enable only when debugging nearby pickup info or shop display behavior.");
            _startupWindowFocusVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableStartupWindowFocusVerboseLogs",
                false,
                "Enable verbose startup window-focus diagnostics, including startup timing, ETG window enumeration, Win32 foreground-call tracing, and foreground-monitor snapshots. Keep disabled for normal play and enable only when debugging Steam launch focus or taskbar visibility behavior.");
            _performanceVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnablePerformanceVerboseLogs",
                false,
                "Enable verbose performance diagnostics, including FPS summaries, long-frame capture, Update-step timing, deferred teleport timing, character switch timing, and automatic loadout grant timing. Keep disabled for normal play and enable only when debugging scene-entry stutter or mod-induced frame drops.");
            _characterSwitchVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableCharacterSwitchVerboseLogs",
                true,
                "Enable detailed Breach character-switch diagnostics, including P1/P2 registration before and after replacement. Enabled by default temporarily to diagnose P2 switching.");
            _damageDiagnosticsVerboseLogsConfig = _configFile.Bind(
                "Debug",
                "EnableDamageDiagnosticsVerboseLogs",
                false,
                "Enable per-hit damage diagnostics, including actual damage, target health, Boss state, current gun, projectile base damage, and final player Damage stat. Keep disabled for normal play and enable only while comparing damage multipliers across guns.");
        }

        private void BindStartItemsConfiguration()
        {
            _activeStartItemsPresetConfig = _configFile.Bind(
                "StartItems",
                "ActivePreset",
                StartItemsPresetNames.DefaultPresetId,
                "Active start-items preset id from EtgGameplayDashboard.rules.json5.");
        }

        private void BindCombatConfiguration()
        {
            _combatCursorColorEnabledConfig = _configFile.Bind(
                "Combat",
                "CursorColorEnabled",
                false,
                "Enable the custom combat cursor color, including the cursor above the Control Panel. Disabled by default.");
            _combatCursorColorPresetConfig = _configFile.Bind(
                "Combat",
                "CursorColorPreset",
                CombatCursorColorCatalog.DefaultPresetId,
                "Stable combat cursor color preset ID. Display names and HEX values are defined by the plugin and are not stored in config.");
            _combatCursorColorPresetConfig.Value = CombatCursorColorCatalog.Normalize(_combatCursorColorPresetConfig.Value);
            _enemyHealthBarsEnabledConfig = _configFile.Bind(
                "Combat",
                "EnemyHealthBarsEnabled",
                false,
                "Keep Enemy HP Bars enabled across game launches. Disabled by default.");
            _bossIntroSkipEnabledConfig = _configFile.Bind(
                "Combat",
                "BossIntroSkipEnabled",
                false,
                "Keep Skip Boss Intro enabled when returning to the Breach or across game launches. Disabled by default.");
            _skipChargeEnabledConfig = _configFile.Bind(
                "Combat",
                "SkipChargeEnabled",
                false,
                "Keep Skip Charge enabled when returning to the Breach or across game launches. Disabled by default.");
            _invincibilityEnabledConfig = _configFile.Bind(
                "Combat",
                "InvincibilityEnabled",
                false,
                "Keep Invincibility enabled when returning to the Breach or across game launches. Disabled by default.");
            _flightEnabledConfig = _configFile.Bind(
                "Combat",
                "FlightEnabled",
                false,
                "Keep Flight enabled when returning to the Breach or across game launches. Disabled by default.");
            _controllerAimLockEnabledConfig = _configFile.Bind(
                "Combat",
                "ControllerAimLockEnabled",
                false,
                "Keep Controller Aim Lock enabled across game launches. The setting affects controller camera aim look only and is disabled by default.");
            _keyboardAimAssistEnabledConfig = _configFile.Bind(
                "Combat",
                "KeyboardAimAssistEnabled",
                false,
                "Keep Keyboard Aim Assist enabled across game launches. Mouse aiming remains the base direction and vanilla controller-style target assist is applied. Disabled by default.");
            _keyboardAimAssistLevelConfig = _configFile.Bind(
                "Combat",
                "KeyboardAimAssistLevel",
                "Medium",
                "Keyboard Aim Assist strength: Off, Weak, Medium, or Strong.");
            _keyboardAimAssistModeConfig = _configFile.Bind(
                "Combat",
                "KeyboardAimAssistMode",
                "Off",
                "Keyboard Aim Assist mode: Off, AutoAim, or SuperAutoAim.");
            _keyboardAimAssistMultiplierConfig = _configFile.Bind(
                "Combat",
                "KeyboardAimAssistMultiplier",
                1f,
                "Keyboard Aim Assist angle multiplier. Supported values: 0.5, 1.0, 1.5, or 2.0.");
            _rapidFireEnabledConfig = _configFile.Bind(
                "Combat",
                "RapidFireEnabled",
                false,
                "Keep Hold Rapid enabled across game launches. Disabled by default.");
            _autoReloadModeConfig = _configFile.Bind(
                "Combat",
                "AutoReloadMode",
                "Off",
                "Persisted Auto Reload mode: Off, Instant, or Animated.");
            _ammoModeConfig = _configFile.Bind(
                "Combat",
                "AmmoMode",
                "Off",
                "Persisted Ammo Mode: Off, InfiniteReserve, or NoConsume.");
            _activeItemNoCooldownEnabledConfig = _configFile.Bind(
                "Combat",
                "ActiveItemNoCooldownEnabled",
                false,
                "Keep Active Item No Cooldown enabled across game launches. Disabled by default.");
        }

        internal string GetUiLanguage()
        {
            return _uiLanguageConfig != null ? GuiText.NormalizeLanguageOverride(_uiLanguageConfig.Value) : "auto";
        }

        internal void SetUiLanguage(string languageCode)
        {
            string normalized = GuiText.NormalizeLanguageOverride(languageCode);
            SetAndSave(_uiLanguageConfig, normalized);

            GuiText.SetLanguageOverride(normalized);
            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel language preference changed to " + normalized + "."));
        }

        internal void LogCommandInput(string message)
        {
            if (_logger != null && !string.IsNullOrEmpty(message))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command("[Input] " + message));
            }
        }

        internal KeyCode GetCommandPanelKey()
        {
            KeyCode keyCode = ParseCommandPanelKey(GetCommandPanelKeyName());
            return keyCode != KeyCode.None ? keyCode : KeyCode.F7;
        }

        internal string GetCommandPanelKeyName()
        {
            string keyName = _commandPanelKeyConfig != null ? _commandPanelKeyConfig.Value : "F7";
            return ParseCommandPanelKey(keyName) != KeyCode.None ? keyName.Trim() : "F7";
        }

        internal void SetCommandPanelKey(string keyName)
        {
            string normalized = NormalizeCommandPanelKeyName(keyName);
            SetAndSave(_commandPanelKeyConfig, normalized);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel keyboard toggle key changed to " + normalized + "."));
        }

        internal string GetCommandPanelControllerShortcut()
        {
            return NormalizeCommandPanelControllerShortcut(_commandPanelControllerShortcutConfig != null ? _commandPanelControllerShortcutConfig.Value : "LB+R3");
        }

        internal void SetCommandPanelControllerShortcut(string shortcut)
        {
            string normalized = NormalizeCommandPanelControllerShortcut(shortcut);
            SetAndSave(_commandPanelControllerShortcutConfig, normalized);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel controller shortcut changed to " + normalized + "."));
        }

        internal bool IsCommandPanelControllerShortcutEnabled()
        {
            return _disableCommandPanelControllerShortcutConfig == null ||
                !_disableCommandPanelControllerShortcutConfig.Value;
        }

        internal void SetCommandPanelControllerShortcutEnabled(bool isEnabled)
        {
            SetAndSave(_disableCommandPanelControllerShortcutConfig, !isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel controller shortcut is " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal static string NormalizeCommandPanelControllerShortcut(string shortcut)
        {
            if (string.Equals(shortcut, "LB+R3", System.StringComparison.OrdinalIgnoreCase)) return "LB+R3";
            if (string.Equals(shortcut, "LB+X", System.StringComparison.OrdinalIgnoreCase)) return "LB+X";
            if (string.Equals(shortcut, "LB+Y", System.StringComparison.OrdinalIgnoreCase)) return "LB+Y";
            if (string.Equals(shortcut, "R3", System.StringComparison.OrdinalIgnoreCase)) return "R3";
            return "LB+R3";
        }

        internal string GetUiScalePreset()
        {
            return NormalizeUiScalePreset(_uiScalePresetConfig != null ? _uiScalePresetConfig.Value : UiScalePresetCatalog.DefaultPreset);
        }

        internal void SetUiScalePreset(string presetName)
        {
            string normalized = NormalizeUiScalePreset(presetName);
            SetAndSave(_uiScalePresetConfig, normalized);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel UI size preset changed to " + normalized + "."));
        }

        internal string GetThemePreset()
        {
            return DashboardThemeCatalog.Normalize(_themePresetConfig != null ? _themePresetConfig.Value : DashboardThemeCatalog.DefaultThemeId);
        }

        internal void SetThemePreset(string themeId)
        {
            string normalized = DashboardThemeCatalog.Normalize(themeId);
            SetAndSave(_themePresetConfig, normalized);

            DashboardTheme.Select(normalized);
            if (_themeChanged != null)
            {
                _themeChanged(normalized);
            }
        }

        internal bool IsExperimentalModeEnabled()
        {
            return ReadBool(_experimentalModeConfig, false);
        }

        internal bool IsPlayerStatsPanelShown()
        {
            return ReadBool(_showPlayerStatsPanelConfig, false);
        }

        internal KeyboardShortcutRegistry GetKeyboardShortcutRegistry()
        {
            if (_keyboardShortcutRegistry == null)
            {
                _keyboardShortcutRegistry = KeyboardShortcutRegistry.Parse(
                    _keyboardShortcutsConfig != null ? _keyboardShortcutsConfig.Value : string.Empty);
            }

            return _keyboardShortcutRegistry;
        }

        internal void SetKeyboardShortcuts(string serialized)
        {
            KeyboardShortcutRegistry normalizedRegistry = KeyboardShortcutRegistry.Parse(serialized);
            _keyboardShortcutRegistry = normalizedRegistry;
            SetAndSave(_keyboardShortcutsConfig, normalizedRegistry.Serialize());

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Keyboard shortcuts updated."));
        }

        internal bool IsCommandPanelCloseButtonShown()
        {
            return _showCommandPanelCloseButtonConfig == null || _showCommandPanelCloseButtonConfig.Value;
        }

        internal bool IsPlayerRewindEnabled()
        {
            return ReadBool(_playerRewindEnabledConfig, false);
        }

        internal bool IsRoomEnemyRefreshRecordingEnabled()
        {
            return ReadBool(_roomEnemyRefreshRecordingEnabledConfig, false);
        }

        internal void SetRoomEnemyRefreshRecordingEnabled(bool isEnabled)
        {
            if (SetAndSave(_roomEnemyRefreshRecordingEnabledConfig, isEnabled))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command("Room enemy refresh recording is " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        internal string GetRoomEnemyRefreshMethod()
        {
            return NormalizeRoomEnemyRefreshMethod(_roomEnemyRefreshMethodConfig != null ? _roomEnemyRefreshMethodConfig.Value : "rewind");
        }

        internal void SetRoomEnemyRefreshMethod(string method)
        {
            string normalized = NormalizeRoomEnemyRefreshMethod(method);
            if (SetAndSave(_roomEnemyRefreshMethodConfig, normalized))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command("Room enemy refresh method changed to " + normalized + "."));
            }
        }

        internal static string NormalizeRoomEnemyRefreshMethod(string method)
        {
            return string.Equals(method, "respawn", StringComparison.OrdinalIgnoreCase) ? "respawn" : "rewind";
        }

        internal void SetPlayerRewindEnabled(bool isEnabled)
        {
            if (SetAndSave(_playerRewindEnabledConfig, isEnabled))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command("Player rewind is " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        internal bool IsRoomRewindCleanupEnabled()
        {
            return ReadBool(_roomRewindCleanupEnabledConfig, false);
        }

        internal void SetRoomRewindCleanupEnabled(bool isEnabled)
        {
            if (SetAndSave(_roomRewindCleanupEnabledConfig, isEnabled))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command("Room rewind cleanup is " + (isEnabled ? "enabled" : "disabled") + "."));
            }
        }

        internal bool IsStartItemsPresetIconsEnabled()
        {
            return ReadBool(_showStartItemsPresetIconsConfig, false);
        }

        internal bool IsPickupInfoOverlayEnabled()
        {
            return ReadBool(_showPickupInfoOverlayConfig, true);
        }

        internal bool IsPickupInfoQualityEnabled()
        {
            return ReadBool(_showPickupInfoQualityConfig, true);
        }

        internal bool IsPickupInfoTypeEnabled()
        {
            return ReadBool(_showPickupInfoTypeConfig, true);
        }

        internal bool IsPickupInfoEffectsEnabled()
        {
            return ReadBool(_showPickupInfoEffectsConfig, true);
        }

        internal bool IsPickupInfoSynergiesEnabled()
        {
            return ReadBool(_showPickupInfoSynergiesConfig, true);
        }

        internal bool IsPickupInfoSummaryEnabled()
        {
            return ReadBool(_showPickupInfoSummaryConfig, true);
        }

        internal bool IsPickupInfoNotesEnabled()
        {
            return ReadBool(_showPickupInfoNotesConfig, true);
        }

        internal void SetPlayerStatsPanelShown(bool isEnabled)
        {
            SetAndSave(_showPlayerStatsPanelConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Player stats side panel " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetCommandPanelCloseButtonShown(bool isEnabled)
        {
            SetAndSave(_showCommandPanelCloseButtonConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel close button " + (isEnabled ? "shown" : "hidden") + "."));
        }

        internal bool IsRevealMapEveryFloor()
        {
            return ReadBool(_revealMapEveryFloorConfig, false);
        }

        internal void SetRevealMapEveryFloor(bool isEnabled)
        {
            SetAndSave(_revealMapEveryFloorConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Reveal Map every-floor mode " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetStartItemsPresetIconsEnabled(bool isEnabled)
        {
            SetAndSave(_showStartItemsPresetIconsConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Start Items preset icons " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoOverlayEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoOverlayConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info overlay " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoQualityEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoQualityConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info quality section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoTypeEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoTypeConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info type section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoEffectsEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoEffectsConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info effects section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoSynergiesEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoSynergiesConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info synergies section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoSummaryEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoSummaryConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info summary section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetPickupInfoNotesEnabled(bool isEnabled)
        {
            SetAndSave(_showPickupInfoNotesConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Pickup info notes section " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal void SetExperimentalModeEnabled(bool isEnabled)
        {
            SetAndSave(_experimentalModeConfig, isEnabled);

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Command panel experimental mode " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal bool IsAmmonomiconFastOpenEnabled()
        {
            return ReadBool(_ammonomiconFastOpenEnabledConfig, false);
        }

        internal void SetAmmonomiconFastOpenEnabled(bool isEnabled)
        {
            SetAndSave(_ammonomiconFastOpenEnabledConfig, isEnabled);

            if (_ammonomiconFastOpenChanged != null)
            {
                _ammonomiconFastOpenChanged(isEnabled);
            }

            _logger.LogInfo(EtgGameplayDashboardLog.Command("Ammonomicon fast open " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        internal bool IsMapTeleportVerboseLoggingEnabled()
        {
            return ReadBool(_mapTeleportVerboseLogsConfig, false);
        }

        internal bool IsMuncherVerboseLoggingEnabled()
        {
            return ReadBool(_muncherVerboseLogsConfig, false);
        }

        internal bool IsRoomEnemyReplayVerboseLoggingEnabled()
        {
            return ReadBool(_roomEnemyReplayVerboseLogsConfig, false);
        }

        internal bool IsBossIntroSkipVerboseLoggingEnabled()
        {
            return ReadBool(_bossIntroSkipVerboseLogsConfig, false);
        }

        internal bool IsFloorTeleportVerboseLoggingEnabled()
        {
            return ReadBool(_floorTeleportVerboseLogsConfig, false);
        }

        internal bool IsBossRushVerboseLoggingEnabled()
        {
            return ReadBool(_bossRushVerboseLogsConfig, false);
        }

        internal static bool IsBossSelectionVerboseLoggingEnabled()
        {
            // Keep diagnostics code-only so BepInEx does not recreate the old
            // [Debug] config entry and its comments in generated config files.
            return false;
        }

        internal bool IsCommandPanelHealthVerboseLoggingEnabled()
        {
            return ReadBool(_commandPanelHealthVerboseLogsConfig, false);
        }

        internal bool IsCommandPanelCursorVerboseLoggingEnabled()
        {
            return ReadBool(_commandPanelCursorVerboseLogsConfig, false);
        }

        internal bool IsCommandPanelGameplayInputVerboseLoggingEnabled()
        {
            return ReadBool(_commandPanelGameplayInputVerboseLogsConfig, false);
        }

        internal bool IsCommandPanelControllerGameplayInputVerboseLoggingEnabled()
        {
            return ReadBool(_commandPanelControllerGameplayInputVerboseLogsConfig, false);
        }

        internal bool IsCommandPanelShortcutVerboseLoggingEnabled()
        {
            return ReadBool(_commandPanelShortcutVerboseLogsConfig, false);
        }

        internal bool IsCommandPanelCursorRenderVerboseLoggingEnabled()
        {
            return ReadBool(_commandPanelCursorRenderVerboseLogsConfig, false);
        }

        internal bool IsControllerAimVerboseLoggingEnabled()
        {
            return ReadBool(_controllerAimVerboseLogsConfig, false);
        }

        internal bool IsCommandPanelCursorRenderProbeEnabled()
        {
            return ReadBool(_commandPanelCursorRenderProbeConfig, false);
        }

        internal bool IsCommandPanelCursorAbovePanelEnabled()
        {
            return ReadBool(_enableCommandPanelCursorAbovePanelConfig, false);
        }

        internal bool IsActiveItemGrantVerboseLoggingEnabled()
        {
            return ReadBool(_activeItemGrantVerboseLogsConfig, false);
        }

        internal bool IsNearbyPickupVerboseLoggingEnabled()
        {
            return ReadBool(_nearbyPickupVerboseLogsConfig, false);
        }

        internal bool IsStartupWindowFocusVerboseLoggingEnabled()
        {
            return ReadBool(_startupWindowFocusVerboseLogsConfig, false);
        }

        internal bool IsPerformanceVerboseLoggingEnabled()
        {
            return ReadBool(_performanceVerboseLogsConfig, false);
        }

        internal bool IsCharacterSwitchVerboseLoggingEnabled()
        {
            return ReadBool(_characterSwitchVerboseLogsConfig, false);
        }

        internal bool IsDamageDiagnosticsVerboseLoggingEnabled()
        {
            return ReadBool(_damageDiagnosticsVerboseLogsConfig, false);
        }

        internal string NormalizeUiScalePreset(string presetName)
        {
            string normalized = UiScalePresetCatalog.Normalize(presetName);
            if (string.Equals(normalized, presetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            _logger.LogWarning(EtgGameplayDashboardLog.Init("Invalid command panel UI size preset '" + presetName + "'. Falling back to " + UiScalePresetCatalog.DefaultPreset + "."));
            return normalized;
        }

        internal string NormalizeCommandPanelKeyName(string keyName)
        {
            string normalized = string.IsNullOrEmpty(keyName) ? "F7" : keyName.Trim();
            if (ParseCommandPanelKey(normalized) != KeyCode.None)
            {
                return normalized;
            }

            _logger.LogWarning(EtgGameplayDashboardLog.Init("Invalid command panel keyboard key '" + normalized + "'. Falling back to F7."));
            return "F7";
        }


        internal static KeyCode ParseCommandPanelKey(string keyName)
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

        internal string GetActiveStartItemsPreset()
        {
            return _activeStartItemsPresetConfig != null
                ? StartItemsPresetNames.NormalizePresetId(_activeStartItemsPresetConfig.Value)
                : StartItemsPresetNames.DefaultPresetId;
        }

        internal void SetActiveStartItemsPreset(string presetName)
        {
            string normalized = StartItemsPresetNames.NormalizePresetId(presetName);

            SetAndSave(_activeStartItemsPresetConfig, normalized);

            if (_activeStartItemsPresetChanged != null)
            {
                _activeStartItemsPresetChanged(normalized);
            }
            _logger.LogInfo(EtgGameplayDashboardLog.Command("Active start-items preset changed to " + normalized + "."));
        }

        internal string GetCombatCursorColor()
        {
            if (_combatCursorColorEnabledConfig == null || !_combatCursorColorEnabledConfig.Value)
            {
                return CombatCursorColorCatalog.DisabledId;
            }

            return _combatCursorColorPresetConfig != null
                ? CombatCursorColorCatalog.Normalize(_combatCursorColorPresetConfig.Value)
                : CombatCursorColorCatalog.DefaultPresetId;
        }

        internal void PersistEnemyHealthBarsEnabled(bool enabled)
        {
            if (!SetAndSave(_enemyHealthBarsEnabledConfig, enabled))
            {
                return;
            }
        }

        internal void PersistBossIntroSkipEnabled(bool enabled)
        {
            if (!SetAndSave(_bossIntroSkipEnabledConfig, enabled))
            {
                return;
            }
        }

        internal void PersistSkipChargeEnabled(bool enabled)
        {
            SetAndSave(_skipChargeEnabledConfig, enabled);
        }

        internal void PersistInvincibilityEnabled(bool enabled)
        {
            SetAndSave(_invincibilityEnabledConfig, enabled);
        }

        internal void PersistFlightEnabled(bool enabled)
        {
            SetAndSave(_flightEnabledConfig, enabled);
        }

        internal void PersistControllerAimLockEnabled(bool enabled)
        {
            if (!SetAndSave(_controllerAimLockEnabledConfig, enabled))
            {
                return;
            }
        }

        internal void PersistKeyboardAimAssistSettings(KeyboardAimAssistSettings settings)
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
                SaveConfig();
            }
        }

        internal void PersistRapidFireEnabled(bool enabled)
        {
            if (!SetAndSave(_rapidFireEnabledConfig, enabled))
            {
                return;
            }
        }

        internal void PersistAutoReloadMode(AutoReloadMode mode)
        {
            if (!SetAndSave(_autoReloadModeConfig, mode.ToString()))
            {
                return;
            }
        }

        internal void PersistAmmoMode(AmmoMode mode)
        {
            if (!SetAndSave(_ammoModeConfig, mode.ToString()))
            {
                return;
            }
        }

        internal void PersistActiveItemNoCooldownEnabled(bool enabled)
        {
            if (!SetAndSave(_activeItemNoCooldownEnabledConfig, enabled))
            {
                return;
            }
        }

        internal static AutoReloadMode ParseAutoReloadMode(string value)
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

        internal static AmmoMode ParseAmmoMode(string value)
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

        internal Color GetCombatCursorColorValue()
        {
            return CombatCursorColorCatalog.IsEnabled(GetCombatCursorColor())
                ? CombatCursorColorCatalog.Get(GetCombatCursorColor()).Color
                : Color.white;
        }

        internal void SetCombatCursorColor(string colorId)
        {
            string normalized = CombatCursorColorCatalog.Normalize(colorId);
            if (string.Equals(normalized, CombatCursorColorCatalog.DisabledId, System.StringComparison.OrdinalIgnoreCase))
            {
                if (_combatCursorColorEnabledConfig != null)
                {
                    _combatCursorColorEnabledConfig.Value = false;
                }

                _configFile.Save();
                _logger.LogInfo(EtgGameplayDashboardLog.Command("Combat cursor color disabled."));
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

            _configFile.Save();

            _logger.LogInfo(EtgGameplayDashboardLog.Command(
                "Combat cursor color changed to " + normalized + " " + selectedOption.Hex +
                " RGB(" + selectedOption.Color.r.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "," +
                selectedOption.Color.g.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "," +
                selectedOption.Color.b.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + ")."));
        }

    }
}
