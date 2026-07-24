// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.IO;
using BepInEx;
using EtgGameplayDashboard.Core;
using EtgGameplayDashboard.Core.Input;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private void BindConfiguration()
        {
            _enableEtgGameplayDashboardConfig = Config.Bind(
                "General",
                "EnableEtgGameplayDashboard",
                true,
                "Enable or disable the automatic start-of-run loadout grant.");
            _uiLanguageConfig = Config.Bind(
                "UI",
                "Language",
                "auto",
                "Command panel language. Use auto, en, or zh-CN.");
            _uiLanguageConfig.Value = GuiText.NormalizeLanguageOverride(_uiLanguageConfig.Value);
            GuiText.SetLanguageOverride(_uiLanguageConfig.Value);
            _commandPanelKeyConfig = Config.Bind(
                "UI",
                "CommandPanelKey",
                "F7",
                "Command panel keyboard toggle key. Use a Unity KeyCode name such as F7, F8, Insert, or BackQuote.");
            _commandPanelKeyConfig.Value = NormalizeCommandPanelKeyName(_commandPanelKeyConfig.Value);
            _roomEnemyRewindKeyConfig = Config.Bind(
                "UI",
                "RoomEnemyRewindKey",
                "C",
                "Room enemy rewind keyboard shortcut. Use a Unity KeyCode name such as C, Z, X, or V.");
            _roomEnemyRewindKeyConfig.Value = NormalizeRoomEnemyRewindKeyName(_roomEnemyRewindKeyConfig.Value);
            _roomEnemyRefreshRecordingEnabledConfig = Config.Bind(
                "UI",
                "RoomEnemyRefreshRecordingEnabled",
                false,
                "Enable recording of standard and Boss room enemy waves for Rewind. Disabled by default.");
            _roomEnemyRefreshMethodConfig = Config.Bind(
                "UI",
                "RoomEnemyRefreshMethod",
                "rewind",
                "Room enemy refresh mode. Use rewind or respawn.");
            _roomEnemyRefreshMethodConfig.Value = NormalizeRoomEnemyRefreshMethod(_roomEnemyRefreshMethodConfig.Value);
            _playerRewindEnabledConfig = Config.Bind(
                "UI",
                "PlayerRewindEnabled",
                false,
                "Restore the player's recorded state when rewinding a room. Disabled by default.");
            _roomRewindCleanupEnabledConfig = Config.Bind(
                "UI",
                "RoomRewindCleanupEnabled",
                true,
                "Remove rewind-room decals, scene drops, currency, and Boss reward pedestals before replay. Enabled by default.");
            _commandPanelControllerShortcutConfig = Config.Bind(
                "UI",
                "CommandPanelControllerShortcut",
                "LB+R3",
                "Controller shortcut for opening the command panel. Supported values: LB+R3, LB+X, LB+Y, or R3.");
            _commandPanelControllerShortcutConfig.Value = NormalizeCommandPanelControllerShortcut(_commandPanelControllerShortcutConfig.Value);
            _disableCommandPanelControllerShortcutConfig = Config.Bind(
                "UI",
                "DisableCommandPanelControllerShortcut",
                false,
                "Disable the controller shortcut for opening or closing the command panel. The keyboard shortcut remains available.");
            _uiScalePresetConfig = Config.Bind(
                "UI",
                "PanelScalePreset",
                UiScalePresetCatalog.DefaultPreset,
                "Command panel UI size preset. Use x-small, small, medium-small, medium, medium-large, large, x-large, or xx-large.");
            _uiScalePresetConfig.Value = NormalizeUiScalePreset(_uiScalePresetConfig.Value);
            _themePresetConfig = Config.Bind(
                "UI",
                "ThemePreset",
                DashboardThemeCatalog.DefaultThemeId,
                "Stable dashboard theme ID. Theme names and colors are defined by the plugin and are not stored in config.");
            _themePresetConfig.Value = DashboardThemeCatalog.Normalize(_themePresetConfig.Value);
            _showStartItemsPresetIconsConfig = Config.Bind(
                "UI",
                "ShowStartItemsPresetIcons",
                true,
                "Show item icons in the Start Items preset list preview.");
            _showPlayerStatsPanelConfig = Config.Bind(
                "UI",
                "ShowPlayerStatsPanel",
                false,
                "Show or hide the player stats side panel by default.");
            _showPickupInfoOverlayConfig = Config.Bind(
                "UI",
                "ShowPickupInfoOverlay",
                true,
                "Show or hide the nearby dropped-pickup detailed info overlay.");
            _showPickupInfoQualityConfig = Config.Bind(
                "UI",
                "ShowPickupInfoQuality",
                true,
                "Show or hide the Quality section in the nearby pickup info overlay.");
            _showPickupInfoTypeConfig = Config.Bind(
                "UI",
                "ShowPickupInfoType",
                true,
                "Show or hide the Type section in the nearby pickup info overlay.");
            _showPickupInfoEffectsConfig = Config.Bind(
                "UI",
                "ShowPickupInfoEffects",
                true,
                "Show or hide the Effects section in the nearby pickup info overlay.");
            _showPickupInfoSynergiesConfig = Config.Bind(
                "UI",
                "ShowPickupInfoSynergies",
                true,
                "Show or hide the Synergies section in the nearby pickup info overlay.");
            _showPickupInfoSummaryConfig = Config.Bind(
                "UI",
                "ShowPickupInfoSummary",
                true,
                "Show or hide the Summary section in the nearby pickup info overlay.");
            _showPickupInfoNotesConfig = Config.Bind(
                "UI",
                "ShowPickupInfoNotes",
                true,
                "Show or hide the Notes section in the nearby pickup info overlay.");
            _experimentalModeConfig = Config.Bind(
                "UI",
                "ExperimentalMode",
                false,
                "Enable unfinished or lower-quality control-panel features.");
            _ammonomiconFastOpenEnabledConfig = Config.Bind(
                "UI",
                "AmmonomiconFastOpen",
                false,
                "Enable or disable fast-open for the Ammonomicon. When enabled, the opening animation is skipped.");
            _mapTeleportVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableMapTeleportVerboseLogs",
                false,
                "Enable verbose Reveal Map diagnostic logs, including teleporter-promotion sampling. Keep disabled for normal play and enable only when debugging map or teleporter behavior.");
            _muncherVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableMuncherVerboseLogs",
                false,
                "Enable verbose Gunber / Evil Muncher spawn diagnostic logs. Keep disabled for normal play and enable only when debugging muncher spawn, placement, or room-registration behavior.");
            _roomEnemyReplayVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableRoomEnemyReplayVerboseLogs",
                false,
                "Enable verbose room enemy replay diagnostics, including recorded waves, replay spawn results, and Boss rewind phase timings. Keep disabled for normal play and enable only when debugging Refresh Room Enemies.");
            _bossIntroSkipVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableBossIntroSkipVerboseLogs",
                false,
                "Enable verbose Boss intro skip diagnostics, including Boss trigger-zone detection, GenericIntroDoer state, native skip requests, and startup failures. Keep disabled for normal play and enable only when debugging Skip Boss Intro.");
            _floorTeleportVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableFloorTeleportVerboseLogs",
                false,
                "Enable verbose floor teleport diagnostic logs, including foyer bootstrap and deferred readiness checks. Keep disabled for normal play and enable only when debugging floor teleport behavior.");
            _bossRushVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableBossRushVerboseLogs",
                false,
                "Enable verbose Boss Rush flow diagnostic logs, including floor readiness and room handoff tracing. Keep disabled for normal play and enable only when debugging Boss Rush runtime behavior.");
            _commandPanelHealthVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelHealthVerboseLogs",
                false,
                "Enable verbose command-panel health and armor diagnostics, including input override lifecycle, weapon-switch side effects, and tracked max-health rollback restoration. Keep disabled for normal play and enable only when debugging repeated heart or armor HUD animations.");
            _commandPanelCursorVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelCursorVerboseLogs",
                true,
                "Enable verbose command-panel cursor diagnostics, including cursor visibility changes, active input-device switches, P1/P2 input state, cursor tint, and mouse click attempts while the panel is open. Enabled temporarily while diagnosing two-player keyboard/controller handoff and unexpected cursor colors.");
            _commandPanelGameplayInputVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelGameplayInputVerboseLogs",
                false,
                "Enable sampled command-panel gameplay keyboard diagnostics. Logs WASD key state changes together with panel visibility, player input override state, and PlayerInputState. Keep disabled for normal play and enable only when debugging gameplay movement while the panel is open.");
            _commandPanelControllerGameplayInputVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelControllerGameplayInputVerboseLogs",
                false,
                "Enable sampled command-panel gameplay controller diagnostics. Logs the active controller, D-pad, left stick, right stick, player input override state, and PlayerInputState. Keep disabled for normal play and enable only when debugging controller movement while the panel is open.");
            _commandPanelShortcutVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelShortcutVerboseLogs",
                true,
                "Enable command-panel keyboard/controller shortcut diagnostics, including configured keys, shortcut detection results, panel visibility, game type, and P1/P2 readiness. Enabled temporarily while diagnosing panel opening failures in two-player mode.");
            _commandPanelCursorRenderVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelCursorRenderVerboseLogs",
                false,
                "Enable sampled command-panel cursor render-order diagnostics for ETG GameCursorController.OnGUI and EtgGameplayDashboard.OnGUI. Keep disabled for normal play and enable only when debugging cursor layering.");
            _controllerAimVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableControllerAimVerboseLogs",
                false,
                "Enable sampled controller/mouse aim diagnostics. Logs the player center, raw aim point, aim distance, input vector, and device mode. Keep disabled for normal play and enable only while reproducing controller or cursor view rotation.");
            _commandPanelCursorRenderProbeConfig = Config.Bind(
                "Debug",
                "EnableCommandPanelCursorRenderProbe",
                false,
                "Temporarily draw a white, exact-position copy of the ETG mouse cursor after the Control Panel to verify cursor layering. This does not disable the original cursor and should be disabled after testing.");
            _enableCommandPanelCursorAbovePanelConfig = Config.Bind(
                "UI",
                "EnableCommandPanelCursorAbovePanel",
                false,
                "Draw the ETG mouse cursor above the Control Panel while it is open. The original mouse cursor is suppressed only while the panel is visible; controller navigation remains panel-controlled.");
            _activeItemGrantVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableActiveItemGrantVerboseLogs",
                false,
                "Enable verbose active-item grant diagnostics, including temporary slot-capacity expansion, ETG grant-path rejection details, and rollback restoration tracing. Keep disabled for normal play and enable only when debugging active items dropping near the player instead of entering the active-item bar.");
            _nearbyPickupVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableNearbyPickupVerboseLogs",
                false,
                "Enable verbose nearby pickup overlay diagnostics, including dropped-pickup scans, shop-item scans, gameplay-catalog lookup results, and final overlay target selection. Keep disabled for normal play and enable only when debugging nearby pickup info or shop display behavior.");
            _startupWindowFocusVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableStartupWindowFocusVerboseLogs",
                false,
                "Enable verbose startup window-focus diagnostics, including startup timing, ETG window enumeration, Win32 foreground-call tracing, and foreground-monitor snapshots. Keep disabled for normal play and enable only when debugging Steam launch focus or taskbar visibility behavior.");
            _performanceVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnablePerformanceVerboseLogs",
                false,
                "Enable verbose performance diagnostics, including FPS summaries, long-frame capture, Update-step timing, deferred teleport timing, character switch timing, and automatic loadout grant timing. Keep disabled for normal play and enable only when debugging scene-entry stutter or mod-induced frame drops.");
            _characterSwitchVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableCharacterSwitchVerboseLogs",
                true,
                "Enable detailed Breach character-switch diagnostics, including P1/P2 registration before and after replacement. Enabled by default temporarily to diagnose P2 switching.");
            _damageDiagnosticsVerboseLogsConfig = Config.Bind(
                "Debug",
                "EnableDamageDiagnosticsVerboseLogs",
                false,
                "Enable per-hit damage diagnostics, including actual damage, target health, Boss state, current gun, projectile base damage, and final player Damage stat. Keep disabled for normal play and enable only while comparing damage multipliers across guns.");
            _activeStartItemsPresetConfig = Config.Bind(
                "StartItems",
                "ActivePreset",
                StartItemsPresetNames.DefaultPresetId,
                "Active start-items preset id from ETG-Gameplay-Dashboard.rules.json5.");
            _combatCursorColorEnabledConfig = Config.Bind(
                "Combat",
                "CursorColorEnabled",
                false,
                "Enable the custom combat cursor color, including the cursor above the Control Panel. Disabled by default.");
            _combatCursorColorPresetConfig = Config.Bind(
                "Combat",
                "CursorColorPreset",
                CombatCursorColorCatalog.DefaultPresetId,
                "Stable combat cursor color preset ID. Display names and HEX values are defined by the plugin and are not stored in config.");
            _combatCursorColorPresetConfig.Value = CombatCursorColorCatalog.Normalize(_combatCursorColorPresetConfig.Value);
            _enemyHealthBarsEnabledConfig = Config.Bind(
                "Combat",
                "EnemyHealthBarsEnabled",
                false,
                "Keep Enemy HP Bars enabled across game launches. Disabled by default.");
            _controllerAimLockEnabledConfig = Config.Bind(
                "Combat",
                "ControllerAimLockEnabled",
                false,
                "Keep Controller Aim Lock enabled across game launches. The setting affects controller camera aim look only and is disabled by default.");
            _keyboardAimAssistEnabledConfig = Config.Bind(
                "Combat",
                "KeyboardAimAssistEnabled",
                false,
                "Keep Keyboard Aim Assist enabled across game launches. Mouse aiming remains the base direction and vanilla controller-style target assist is applied. Disabled by default.");
            _keyboardAimAssistLevelConfig = Config.Bind(
                "Combat",
                "KeyboardAimAssistLevel",
                "Medium",
                "Keyboard Aim Assist strength: Off, Weak, Medium, or Strong.");
            _keyboardAimAssistModeConfig = Config.Bind(
                "Combat",
                "KeyboardAimAssistMode",
                "Off",
                "Keyboard Aim Assist mode: Off, AutoAim, or SuperAutoAim.");
            _keyboardAimAssistMultiplierConfig = Config.Bind(
                "Combat",
                "KeyboardAimAssistMultiplier",
                1f,
                "Keyboard Aim Assist angle multiplier. Supported values: 0.5, 1.0, 1.5, or 2.0.");
            _rapidFireEnabledConfig = Config.Bind(
                "Combat",
                "RapidFireEnabled",
                false,
                "Keep Hold Rapid enabled across game launches. Disabled by default.");
            _autoReloadModeConfig = Config.Bind(
                "Combat",
                "AutoReloadMode",
                "Off",
                "Persisted Auto Reload mode: Off, Instant, or Animated.");
            _ammoModeConfig = Config.Bind(
                "Combat",
                "AmmoMode",
                "Off",
                "Persisted Ammo Mode: Off, InfiniteReserve, or NoConsume.");
        }

        private void InitializeResolversAndProviders()
        {
            _aliasRegistry = PickupAliasRegistry.Empty;
            _configResolver = new EtgLoadoutConfigResolver(_pickupResolver);
            _pickupCatalogExporter = new EtgPickupCatalogExporter(
                Path.Combine(Paths.ConfigPath, PickupCatalogTextFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogGroupedJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupNamesJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _aliasFileProvider = new JsonPickupAliasFileProvider(DashboardFileLayout.GetAliasFilePath(Paths.ConfigPath));
            _pickupGameplayProvider = new JsonPickupGameplayProvider(
                DashboardFileLayout.GetPickupGameplayFilePath(Paths.ConfigPath),
                DashboardFileLayout.GetPickupInfoTermsFilePath(Paths.ConfigPath));
            _randomPoolSelectionStateProvider = new RandomPoolSelectionStateProvider(Path.Combine(Paths.ConfigPath, RandomPoolSelectionStateFileName));
            _ruleFileProvider = new JsonLoadoutRuleFileProvider(
                DashboardFileLayout.GetRulesFilePath(Paths.ConfigPath),
                DashboardFileLayout.GetPresetsDirectoryPath(Paths.ConfigPath));
            _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
        }

        private void InitializeServices()
        {
            if (IsNearbyPickupVerboseLoggingEnabled())
            {
                LogPickupGameplayInputFiles();
            }

            string pickupInfoTermsMessage = string.Empty;
            string pickupInfoTermsWarning = string.Empty;
            _pickupInfoTermsRegistry = _pickupGameplayProvider != null
                ? _pickupGameplayProvider.LoadTerms(out pickupInfoTermsMessage, out pickupInfoTermsWarning)
                : PickupInfoTermsRegistry.Empty;
            if (!string.IsNullOrEmpty(pickupInfoTermsMessage))
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Init(pickupInfoTermsMessage));
            }

            if (!string.IsNullOrEmpty(pickupInfoTermsWarning))
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Init(pickupInfoTermsWarning));
            }

            string pickupGameplayMessage = string.Empty;
            string pickupGameplayWarning = string.Empty;
            _pickupGameplayRegistry = _pickupGameplayProvider != null
                ? _pickupGameplayProvider.Load(out pickupGameplayMessage, out pickupGameplayWarning)
                : PickupGameplayRegistry.Empty;
            if (!string.IsNullOrEmpty(pickupGameplayMessage))
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Init(pickupGameplayMessage));
            }

            if (!string.IsNullOrEmpty(pickupGameplayWarning))
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Init(pickupGameplayWarning));
            }

            _nearbyPickupTipService = new NearbyPickupTipService(_pickupGameplayRegistry, Logger, IsNearbyPickupVerboseLoggingEnabled, IsPickupInfoOverlayEnabled);
            _rapidFireToggleService = new RapidFireToggleService(
                _rapidFireEnabledConfig.Value,
                PersistRapidFireEnabled);
            _autoReloadToggleService = new AutoReloadToggleService(
                ParseAutoReloadMode(_autoReloadModeConfig.Value),
                PersistAutoReloadMode);
            _armorNoConsumeToggleService = new ArmorNoConsumeToggleService();
            _blankNoConsumeToggleService = new BlankNoConsumeToggleService();
            _keyNoConsumeToggleService = new KeyNoConsumeToggleService();
            _currencyNoConsumeToggleService = new CurrencyNoConsumeToggleService();
            _invincibilityToggleService = new InvincibilityToggleService();
            _enemyHealthBarToggleService = new EnemyHealthBarToggleService(
                _enemyHealthBarsEnabledConfig.Value,
                PersistEnemyHealthBarsEnabled);
            _controllerAimLockService = new ControllerAimLockService(
                _controllerAimLockEnabledConfig.Value,
                PersistControllerAimLockEnabled);
            KeyboardAimAssistSettings keyboardAimAssistSettings = KeyboardAimAssistSettings.FromConfig(
                _keyboardAimAssistEnabledConfig.Value,
                _keyboardAimAssistLevelConfig.Value,
                _keyboardAimAssistModeConfig.Value,
                _keyboardAimAssistMultiplierConfig.Value);
            _keyboardAimAssistService = new KeyboardAimAssistService(
                keyboardAimAssistSettings,
                PersistKeyboardAimAssistSettings,
                new KeyboardAimAssistTargetSelector());
            PersistKeyboardAimAssistSettings(keyboardAimAssistSettings);
            _ammoModeToggleService = new AmmoModeToggleService(
                ParseAmmoMode(_ammoModeConfig.Value),
                PersistAmmoMode);
            _ammonomiconFastOpenToggleService = new AmmonomiconFastOpenToggleService();
            _ammonomiconFastOpenToggleService.SetIsFastOpenEnabled(_ammonomiconFastOpenEnabledConfig.Value);
            _playerHealthOverrideService = new PlayerHealthOverrideService(Logger, IsCommandPanelHealthVerboseLoggingEnabled);
            _playerActiveItemCapacityOverrideService = new PlayerActiveItemCapacityOverrideService(Logger, IsActiveItemGrantVerboseLoggingEnabled);
            _playerDebugCommandService = new PlayerDebugCommandService(_playerHealthOverrideService);
            _playerStatMultiplierService = new PlayerStatMultiplierService();
            _damageDiagnosticsService = new DamageDiagnosticsService(
                Logger,
                IsDamageDiagnosticsVerboseLoggingEnabled,
                delegate { return _playerStatMultiplierService != null ? _playerStatMultiplierService.DamageMultiplier : 1f; });
            _pickupGranter = new EtgPickupGranter(_playerActiveItemCapacityOverrideService, IsActiveItemGrantVerboseLoggingEnabled);
            _bossRushService = new BossRushService(Logger, IsBossRushVerboseLoggingEnabled);
            _roomEnemyReplayService = new RoomEnemyReplayService(Logger, IsRoomEnemyReplayVerboseLoggingEnabled, IsPlayerRewindEnabled, IsRoomRewindCleanupEnabled, SetRoomEnemyRefreshRecordingEnabled);
            _roomEnemyReplayService.SetRecordingEnabled(IsRoomEnemyRefreshRecordingEnabled());
            _gameWindowFocusService = new GameWindowFocusService(Logger, IsStartupWindowFocusVerboseLoggingEnabled);
            _performanceDiagnostics = new PerformanceDiagnostics(Logger, IsPerformanceVerboseLoggingEnabled);
        }

        private void LogPickupGameplayInputFiles()
        {
            if (Logger == null || _pickupGameplayProvider == null)
            {
                return;
            }

            LogPickupGameplayInputFile("Gameplay", _pickupGameplayProvider.GameplayFilePath);
            LogPickupGameplayInputFile("Terms", _pickupGameplayProvider.TermsFilePath);
        }

        private void LogPickupGameplayInputFile(string label, string path)
        {
            string resolvedPath = path ?? string.Empty;
            bool exists = !string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath);
            long size = 0L;
            if (exists)
            {
                try
                {
                    size = new FileInfo(resolvedPath).Length;
                }
                catch (IOException)
                {
                    size = -1L;
                }
                catch (UnauthorizedAccessException)
                {
                    size = -1L;
                }
            }

            Logger.LogInfo(
                EtgGameplayDashboardLog.Init(
                    "Pickup gameplay " +
                    label +
                    " file: Path='" +
                    resolvedPath +
                    "', Exists=" +
                    exists +
                    ", SizeBytes=" +
                    size +
                    "."));
        }

        private void InitializeControllers()
        {
            GrantCommandService grantCommandService = new GrantCommandService(_pickupResolver, _pickupGranter, GetAliasRegistry);
            _bossNameCatalog = BossNameCatalog.Load(DashboardFileLayout.GetBossNamesFilePath(Paths.ConfigPath));
            RoomDebugCommandService roomDebugCommandService = new RoomDebugCommandService(IsMapTeleportVerboseLoggingEnabled, IsMuncherVerboseLoggingEnabled, _roomEnemyReplayService, IsRoomEnemyReplayVerboseLoggingEnabled, IsPlayerRewindEnabled, SetPlayerRewindEnabled, IsRoomRewindCleanupEnabled, SetRoomRewindCleanupEnabled, IsBossSelectionVerboseLoggingEnabled, _bossNameCatalog, Logger);
            LoadoutRuleEditorService loadoutRuleEditorService = new LoadoutRuleEditorService(
                _ruleFileProvider,
                _pickupResolver.GetGrantablePickupCatalog,
                InvalidateResolvedLoadoutConfig,
                GetActiveStartItemsPreset,
                SetActiveStartItemsPreset,
                _ownedPickupReader);
            _loadoutPresetRandomService = new LoadoutPresetRandomService(loadoutRuleEditorService);

            DashboardTheme.Select(GetThemePreset());
            _commandController = new InGameCommandController(
                grantCommandService,
                _playerDebugCommandService,
                roomDebugCommandService,
                new FoyerCharacterSwitchService(
                    Logger,
                    IsPerformanceVerboseLoggingEnabled,
                    IsCharacterSwitchVerboseLoggingEnabled,
                    new PlayerInputOwnershipService(delegate { BraveInput.ReassignAllControllers(); })),
                _bossRushService,
                _rapidFireToggleService,
                _autoReloadToggleService,
                _armorNoConsumeToggleService,
                _blankNoConsumeToggleService,
                _keyNoConsumeToggleService,
                _currencyNoConsumeToggleService,
                _invincibilityToggleService,
                _enemyHealthBarToggleService,
                _controllerAimLockService,
                _keyboardAimAssistService,
                _playerStatMultiplierService,
                _ammoModeToggleService,
                _ammonomiconFastOpenToggleService,
                loadoutRuleEditorService,
                _loadoutPresetRandomService,
                _pickupResolver.GetGrantablePickupCatalog,
                GetPickupGameplayDisplayName,
                GetAliasRegistry,
                GetUiLanguage,
                SetUiLanguage,
                LogCommandInput,
                GetCommandPanelKey,
                GetCommandPanelKeyName,
                SetCommandPanelKey,
                GetRoomEnemyRewindKey,
                GetRoomEnemyRefreshMethod,
                SetRoomEnemyRefreshMethod,
                GetCommandPanelControllerShortcut,
                SetCommandPanelControllerShortcut,
                IsCommandPanelControllerShortcutEnabled,
                SetCommandPanelControllerShortcutEnabled,
                GetUiScalePreset,
                SetUiScalePreset,
                GetThemePreset,
                SetThemePreset,
                IsStartItemsPresetIconsEnabled,
                SetStartItemsPresetIconsEnabled,
                IsPlayerStatsPanelShown,
                SetPlayerStatsPanelShown,
                IsPickupInfoOverlayEnabled,
                SetPickupInfoOverlayEnabled,
                IsPickupInfoQualityEnabled,
                SetPickupInfoQualityEnabled,
                IsPickupInfoTypeEnabled,
                SetPickupInfoTypeEnabled,
                IsPickupInfoEffectsEnabled,
                SetPickupInfoEffectsEnabled,
                IsPickupInfoSynergiesEnabled,
                SetPickupInfoSynergiesEnabled,
                IsPickupInfoSummaryEnabled,
                SetPickupInfoSummaryEnabled,
                IsPickupInfoNotesEnabled,
                SetPickupInfoNotesEnabled,
                IsExperimentalModeEnabled,
                SetExperimentalModeEnabled,
                SetAmmonomiconFastOpenEnabled,
                IsMapTeleportVerboseLoggingEnabled,
                IsFloorTeleportVerboseLoggingEnabled,
                IsCommandPanelHealthVerboseLoggingEnabled,
                IsCommandPanelCursorVerboseLoggingEnabled,
                IsCommandPanelGameplayInputVerboseLoggingEnabled,
                IsCommandPanelControllerGameplayInputVerboseLoggingEnabled,
                IsCommandPanelShortcutVerboseLoggingEnabled,
                GetCombatCursorColor,
                SetCombatCursorColor,
                IsPerformanceVerboseLoggingEnabled,
                Logger,
                BeginDeferredTeleportFromFoyer);
        }

        private void InitializeRuntimeState()
        {
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _runState = new RunGrantState();
            _runLifecycleTracker = new RunLifecycleTracker(CharacterSelectSceneName, LoadingSceneName);
            _sceneWatcher = new RunSceneWatcher(CharacterSelectSceneName);
        }

        private string GetPickupGameplayDisplayName(int pickupId)
        {
            if (_pickupGameplayRegistry == null)
            {
                return string.Empty;
            }

            PickupGameplayEntry entry;
            if (!_pickupGameplayRegistry.TryGetEntry(pickupId, out entry) || entry == null)
            {
                return string.Empty;
            }

            if (string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                return entry.ChineseDisplayName;
            }

            return entry.EnglishDisplayName;
        }

        private void LogStartupConfiguration()
        {
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Waiting for GameManager startup."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Automatic random loadout is " + (_enableEtgGameplayDashboardConfig.Value ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command panel language preference is " + GetUiLanguage() + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command panel keyboard toggle key is " + GetCommandPanelKey() + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command panel gamepad open input is 360 controller " + GetCommandPanelControllerShortcut() + " press."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command panel gamepad shortcut is " + (IsCommandPanelControllerShortcutEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command panel UI size preset is " + GetUiScalePreset() + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Player stats side panel is " + (IsPlayerStatsPanelShown() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Pickup info overlay is " + (IsPickupInfoOverlayEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Pickup info sections: quality=" + (IsPickupInfoQualityEnabled() ? "on" : "off") + ", type=" + (IsPickupInfoTypeEnabled() ? "on" : "off") + ", effects=" + (IsPickupInfoEffectsEnabled() ? "on" : "off") + ", synergies=" + (IsPickupInfoSynergiesEnabled() ? "on" : "off") + ", summary=" + (IsPickupInfoSummaryEnabled() ? "on" : "off") + ", notes=" + (IsPickupInfoNotesEnabled() ? "on" : "off") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command panel experimental mode is " + (IsExperimentalModeEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Ammonomicon fast open is " + (IsAmmonomiconFastOpenEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Combat persisted states: rapid=" + (_rapidFireEnabledConfig.Value ? "on" : "off") + ", autoReload=" + _autoReloadModeConfig.Value + ", ammoMode=" + _ammoModeConfig.Value + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Nearby pickup info mode is gameplay."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Reveal Map verbose logs are " + (IsMapTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Muncher verbose logs are " + (IsMuncherVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Room enemy replay verbose logs are " + (IsRoomEnemyReplayVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init(
                "Room enemy replay logging configuration: " +
                "Section=" + (_roomEnemyReplayVerboseLogsConfig != null ? _roomEnemyReplayVerboseLogsConfig.Definition.Section : "<null>") +
                ", Key=" + (_roomEnemyReplayVerboseLogsConfig != null ? _roomEnemyReplayVerboseLogsConfig.Definition.Key : "<null>") +
                ", EffectiveValue=" + IsRoomEnemyReplayVerboseLoggingEnabled() +
                ", ConfigPath=" + (Config != null ? Config.ConfigFilePath : "<null>") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Floor teleport verbose logs are " + (IsFloorTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Boss Rush verbose logs are " + (IsBossRushVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command-panel health verbose logs are " + (IsCommandPanelHealthVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command-panel cursor verbose logs are " + (IsCommandPanelCursorVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Command-panel shortcut verbose logs are " + (IsCommandPanelShortcutVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Active-item grant verbose logs are " + (IsActiveItemGrantVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Nearby pickup verbose logs are " + (IsNearbyPickupVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Startup window-focus verbose logs are " + (IsStartupWindowFocusVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Performance verbose logs are " + (IsPerformanceVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Character switch verbose logs are " + (IsCharacterSwitchVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Damage diagnostics verbose logs are " + (IsDamageDiagnosticsVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Active start-items preset is " + GetActiveStartItemsPreset() + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Nearby pickup gameplay info loaded: " + (_pickupGameplayRegistry != null ? _pickupGameplayRegistry.Count.ToString() : "0") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Boss Rush service initialized. Startup self-check is running."));
        }

        private void ResetServices()
        {
            if (_playerStatMultiplierService != null)
            {
                _playerStatMultiplierService.Reset();
            }

            if (_damageDiagnosticsService != null)
            {
                _damageDiagnosticsService.Reset();
            }

            if (_roomEnemyReplayService != null)
            {
                _roomEnemyReplayService.Clear();
            }

            if (_rapidFireToggleService != null)
            {
                _rapidFireToggleService.Reset();
            }

            if (_autoReloadToggleService != null)
            {
                _autoReloadToggleService.Reset();
            }

            if (_blankNoConsumeToggleService != null)
            {
                _blankNoConsumeToggleService.Reset();
            }

            if (_armorNoConsumeToggleService != null)
            {
                _armorNoConsumeToggleService.Reset();
            }

            if (_keyNoConsumeToggleService != null)
            {
                _keyNoConsumeToggleService.Reset();
            }

            if (_currencyNoConsumeToggleService != null)
            {
                _currencyNoConsumeToggleService.Reset();
            }

            if (_invincibilityToggleService != null)
            {
                _invincibilityToggleService.Reset();
            }

            if (_enemyHealthBarToggleService != null)
            {
                _enemyHealthBarToggleService.Reset();
            }

            if (_ammoModeToggleService != null)
            {
                _ammoModeToggleService.Reset();
            }

            if (_playerHealthOverrideService != null)
            {
                _playerHealthOverrideService.Reset();
            }

            if (_playerActiveItemCapacityOverrideService != null)
            {
                _playerActiveItemCapacityOverrideService.Reset();
            }

            if (_bossRushService != null)
            {
                _bossRushService.Dispose();
            }
        }
    }
}
