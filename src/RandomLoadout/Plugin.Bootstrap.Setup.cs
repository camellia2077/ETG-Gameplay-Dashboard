// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.IO;
using BepInEx;
using RandomLoadout.Core;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private void BindConfiguration()
        {
            _enableRandomLoadoutConfig = Config.Bind(
                "General",
                "EnableRandomLoadout",
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
            _uiScalePresetConfig = Config.Bind(
                "UI",
                "PanelScalePreset",
                UiScalePresetCatalog.DefaultPreset,
                "Command panel UI size preset. Use x-small, small, medium-small, medium, medium-large, large, x-large, or xx-large.");
            _uiScalePresetConfig.Value = NormalizeUiScalePreset(_uiScalePresetConfig.Value);
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
                false,
                "Enable verbose command-panel cursor diagnostics, including cursor visibility changes, active input-device switches, and mouse click attempts while the panel is open. Keep disabled for normal play and enable only when debugging controller-to-mouse handoff issues.");
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
            _activeStartItemsPresetConfig = Config.Bind(
                "StartItems",
                "ActivePreset",
                StartItemsPresetNames.DefaultPresetId,
                "Active start-items preset id from ETG-Gameplay-Dashboard.rules.json5.");
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
                DashboardFileLayout.GetPickupGameplaySimplifiedChineseWorkFilePath(Paths.ConfigPath));
            _randomPoolSelectionStateProvider = new RandomPoolSelectionStateProvider(Path.Combine(Paths.ConfigPath, RandomPoolSelectionStateFileName));
            _ruleFileProvider = new JsonLoadoutRuleFileProvider(
                DashboardFileLayout.GetRulesFilePath(Paths.ConfigPath),
                DashboardFileLayout.GetPresetsDirectoryPath(Paths.ConfigPath));
            _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
        }

        private void InitializeServices()
        {
            string pickupInfoTermsMessage = string.Empty;
            string pickupInfoTermsWarning = string.Empty;
            _pickupInfoTermsRegistry = _pickupGameplayProvider != null
                ? _pickupGameplayProvider.LoadTerms(out pickupInfoTermsMessage, out pickupInfoTermsWarning)
                : PickupInfoTermsRegistry.Empty;
            if (!string.IsNullOrEmpty(pickupInfoTermsMessage))
            {
                Logger.LogInfo(RandomLoadoutLog.Init(pickupInfoTermsMessage));
            }

            if (!string.IsNullOrEmpty(pickupInfoTermsWarning))
            {
                Logger.LogWarning(RandomLoadoutLog.Init(pickupInfoTermsWarning));
            }

            string pickupGameplayMessage = string.Empty;
            string pickupGameplayWarning = string.Empty;
            _pickupGameplayRegistry = _pickupGameplayProvider != null
                ? _pickupGameplayProvider.Load(out pickupGameplayMessage, out pickupGameplayWarning)
                : PickupGameplayRegistry.Empty;
            if (!string.IsNullOrEmpty(pickupGameplayMessage))
            {
                Logger.LogInfo(RandomLoadoutLog.Init(pickupGameplayMessage));
            }

            if (!string.IsNullOrEmpty(pickupGameplayWarning))
            {
                Logger.LogWarning(RandomLoadoutLog.Init(pickupGameplayWarning));
            }

            _nearbyPickupTipService = new NearbyPickupTipService(_pickupGameplayRegistry, Logger, IsNearbyPickupVerboseLoggingEnabled, IsPickupInfoOverlayEnabled);
            _rapidFireToggleService = new RapidFireToggleService();
            _autoReloadToggleService = new AutoReloadToggleService();
            _armorNoConsumeToggleService = new ArmorNoConsumeToggleService();
            _blankNoConsumeToggleService = new BlankNoConsumeToggleService();
            _keyNoConsumeToggleService = new KeyNoConsumeToggleService();
            _currencyNoConsumeToggleService = new CurrencyNoConsumeToggleService();
            _invincibilityToggleService = new InvincibilityToggleService();
            _ammoModeToggleService = new AmmoModeToggleService();
            _ammonomiconFastOpenToggleService = new AmmonomiconFastOpenToggleService();
            _ammonomiconFastOpenToggleService.SetIsFastOpenEnabled(_ammonomiconFastOpenEnabledConfig.Value);
            _playerHealthOverrideService = new PlayerHealthOverrideService(Logger, IsCommandPanelHealthVerboseLoggingEnabled);
            _playerActiveItemCapacityOverrideService = new PlayerActiveItemCapacityOverrideService(Logger, IsActiveItemGrantVerboseLoggingEnabled);
            _playerDebugCommandService = new PlayerDebugCommandService(_playerHealthOverrideService);
            _pickupGranter = new EtgPickupGranter(_playerActiveItemCapacityOverrideService, IsActiveItemGrantVerboseLoggingEnabled);
            _bossRushService = new BossRushService(Logger, IsBossRushVerboseLoggingEnabled);
            _gameWindowFocusService = new GameWindowFocusService(Logger, IsStartupWindowFocusVerboseLoggingEnabled);
            _performanceDiagnostics = new PerformanceDiagnostics(Logger, IsPerformanceVerboseLoggingEnabled);
        }

        private void InitializeControllers()
        {
            GrantCommandService grantCommandService = new GrantCommandService(_pickupResolver, _pickupGranter, GetAliasRegistry);
            RoomDebugCommandService roomDebugCommandService = new RoomDebugCommandService(IsMapTeleportVerboseLoggingEnabled, IsMuncherVerboseLoggingEnabled);
            LoadoutRuleEditorService loadoutRuleEditorService = new LoadoutRuleEditorService(
                _ruleFileProvider,
                _pickupResolver.GetGrantablePickupCatalog,
                InvalidateResolvedLoadoutConfig,
                GetActiveStartItemsPreset,
                SetActiveStartItemsPreset,
                _ownedPickupReader);

            _commandController = new InGameCommandController(
                grantCommandService,
                _playerDebugCommandService,
                roomDebugCommandService,
                new FoyerCharacterSwitchService(Logger, IsPerformanceVerboseLoggingEnabled),
                _bossRushService,
                _rapidFireToggleService,
                _autoReloadToggleService,
                _armorNoConsumeToggleService,
                _blankNoConsumeToggleService,
                _keyNoConsumeToggleService,
                _currencyNoConsumeToggleService,
                _invincibilityToggleService,
                _ammoModeToggleService,
                _ammonomiconFastOpenToggleService,
                loadoutRuleEditorService,
                _pickupResolver.GetGrantablePickupCatalog,
                GetAliasRegistry,
                GetUiLanguage,
                SetUiLanguage,
                LogCommandInput,
                GetCommandPanelKey,
                GetCommandPanelKeyName,
                SetCommandPanelKey,
                GetUiScalePreset,
                SetUiScalePreset,
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
                BeginDeferredTeleportFromFoyer);
        }

        private void InitializeRuntimeState()
        {
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _runState = new RunGrantState();
            _runLifecycleTracker = new RunLifecycleTracker(CharacterSelectSceneName, LoadingSceneName);
            _sceneWatcher = new RunSceneWatcher(CharacterSelectSceneName);
        }

        private void LogStartupConfiguration()
        {
            Logger.LogInfo(RandomLoadoutLog.Init("Waiting for GameManager startup."));
            Logger.LogInfo(RandomLoadoutLog.Init("Automatic random loadout is " + (_enableRandomLoadoutConfig.Value ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel language preference is " + GetUiLanguage() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel keyboard toggle key is " + GetCommandPanelKey() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel gamepad open input is 360 controller R3 short press."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel UI size preset is " + GetUiScalePreset() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Player stats side panel is " + (IsPlayerStatsPanelShown() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Pickup info overlay is " + (IsPickupInfoOverlayEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Pickup info sections: quality=" + (IsPickupInfoQualityEnabled() ? "on" : "off") + ", type=" + (IsPickupInfoTypeEnabled() ? "on" : "off") + ", effects=" + (IsPickupInfoEffectsEnabled() ? "on" : "off") + ", synergies=" + (IsPickupInfoSynergiesEnabled() ? "on" : "off") + ", summary=" + (IsPickupInfoSummaryEnabled() ? "on" : "off") + ", notes=" + (IsPickupInfoNotesEnabled() ? "on" : "off") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel experimental mode is " + (IsExperimentalModeEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Ammonomicon fast open is " + (IsAmmonomiconFastOpenEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Nearby pickup info mode is gameplay."));
            Logger.LogInfo(RandomLoadoutLog.Init("Reveal Map verbose logs are " + (IsMapTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Muncher verbose logs are " + (IsMuncherVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Floor teleport verbose logs are " + (IsFloorTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush verbose logs are " + (IsBossRushVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command-panel health verbose logs are " + (IsCommandPanelHealthVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command-panel cursor verbose logs are " + (IsCommandPanelCursorVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Active-item grant verbose logs are " + (IsActiveItemGrantVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Nearby pickup verbose logs are " + (IsNearbyPickupVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Startup window-focus verbose logs are " + (IsStartupWindowFocusVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Performance verbose logs are " + (IsPerformanceVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Active start-items preset is " + GetActiveStartItemsPreset() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Nearby pickup gameplay info loaded: " + (_pickupGameplayRegistry != null ? _pickupGameplayRegistry.Count.ToString() : "0") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush service initialized. Startup self-check is running."));
        }

        private void ResetServices()
        {
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
