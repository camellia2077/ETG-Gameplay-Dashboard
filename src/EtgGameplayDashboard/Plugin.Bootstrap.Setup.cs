// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.IO;
using BepInEx;
using EtgGameplayDashboard.Core.Input;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private void InitializeConfiguration()
        {
            _configuration = new PluginConfigurationFacade(
                Config,
                Logger,
                normalized =>
                {
                    if (_ruleFileProvider != null)
                    {
                        _ruleFileProvider.ActivePresetName = normalized;
                    }

                    InvalidateResolvedLoadoutConfig();
                },
                normalized =>
                {
                    if (_commandController != null)
                    {
                        _commandController.RefreshTheme();
                    }

                    RefreshPickupWikiTipTheme();
                },
                isEnabled =>
                {
                    if (_ammonomiconFastOpenToggleService != null &&
                        AmmonomiconFastOpenToggleService.IsFastOpenEnabled != isEnabled)
                    {
                        AmmonomiconFastOpenToggleService.SetIsFastOpenEnabled(isEnabled);
                    }
                });
            _configuration.BindConfiguration();
        }
        private void InitializeResolversAndProviders()
        {
            _aliasRegistry = PickupAliasRegistry.Empty;
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
                _configuration.RapidFireEnabledConfig.Value,
                PersistRapidFireEnabled);
            _skipChargeToggleService = new SkipChargeToggleService();
            _autoReloadToggleService = new AutoReloadToggleService(
                ParseAutoReloadMode(_configuration.AutoReloadModeConfig.Value),
                PersistAutoReloadMode);
            _armorNoConsumeToggleService = new ArmorNoConsumeToggleService();
            _blankNoConsumeToggleService = new BlankNoConsumeToggleService();
            _keyNoConsumeToggleService = new KeyNoConsumeToggleService();
            _currencyNoConsumeToggleService = new CurrencyNoConsumeToggleService();
            _invincibilityToggleService = new InvincibilityToggleService();
            _playerFlightToggleService = new PlayerFlightToggleService();
            _enemyHealthBarToggleService = new EnemyHealthBarToggleService(
                _configuration.EnemyHealthBarsEnabledConfig.Value,
                PersistEnemyHealthBarsEnabled);
            _controllerAimLockService = new ControllerAimLockService(
                _configuration.ControllerAimLockEnabledConfig.Value,
                PersistControllerAimLockEnabled);
            KeyboardAimAssistSettings keyboardAimAssistSettings = KeyboardAimAssistSettings.FromConfig(
                _configuration.KeyboardAimAssistEnabledConfig.Value,
                _configuration.KeyboardAimAssistLevelConfig.Value,
                _configuration.KeyboardAimAssistModeConfig.Value,
                _configuration.KeyboardAimAssistMultiplierConfig.Value);
            _keyboardAimAssistService = new KeyboardAimAssistService(
                keyboardAimAssistSettings,
                PersistKeyboardAimAssistSettings,
                new KeyboardAimAssistTargetSelector());
            PersistKeyboardAimAssistSettings(keyboardAimAssistSettings);
            _ammoModeToggleService = new AmmoModeToggleService(
                ParseAmmoMode(_configuration.AmmoModeConfig.Value),
                PersistAmmoMode);
            _activeItemNoCooldownToggleService = new ActiveItemNoCooldownToggleService(
                _configuration.ActiveItemNoCooldownEnabledConfig.Value,
                PersistActiveItemNoCooldownEnabled);
            _ammonomiconFastOpenToggleService = new AmmonomiconFastOpenToggleService();
            AmmonomiconFastOpenToggleService.SetIsFastOpenEnabled(_configuration.AmmonomiconFastOpenEnabledConfig.Value);
            _playerHealthOverrideService = new PlayerHealthOverrideService(Logger, IsCommandPanelHealthVerboseLoggingEnabled);
            _playerActiveItemCapacityOverrideService = new PlayerActiveItemCapacityOverrideService(Logger, IsActiveItemGrantVerboseLoggingEnabled);
            _playerDebugCommandService = new PlayerDebugCommandService(_playerHealthOverrideService);
            _playerRuntimeStatOverrideService = new PlayerRuntimeStatOverrideService();
            _projectileModifierService = new ProjectileModifierService();
            _damageDiagnosticsService = new DamageDiagnosticsService(
                Logger,
                IsDamageDiagnosticsVerboseLoggingEnabled,
                delegate { return _playerRuntimeStatOverrideService != null ? _playerRuntimeStatOverrideService.DamageMultiplier : 1f; });
            _pickupGranter = new EtgPickupGranter(_playerActiveItemCapacityOverrideService, IsActiveItemGrantVerboseLoggingEnabled);
            _bossRushCoroutineHost = gameObject.AddComponent<BossRushCoroutineHost>();
            _bossRushService = new BossRushService(Logger, IsBossRushVerboseLoggingEnabled, _bossRushCoroutineHost);
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
                _skipChargeToggleService,
                _autoReloadToggleService,
                _armorNoConsumeToggleService,
                _blankNoConsumeToggleService,
                _keyNoConsumeToggleService,
                _currencyNoConsumeToggleService,
                _invincibilityToggleService,
                _playerFlightToggleService,
                _enemyHealthBarToggleService,
                _controllerAimLockService,
                _keyboardAimAssistService,
                _playerRuntimeStatOverrideService,
                _projectileModifierService,
                _ammoModeToggleService,
                _activeItemNoCooldownToggleService,
                _ammonomiconFastOpenToggleService,
                loadoutRuleEditorService,
                _loadoutPresetRandomService,
                _pickupResolver.GetGrantablePickupCatalog,
                GetPickupGameplayDisplayName,
                GetAliasRegistry,
                GetKeyboardShortcutRegistry,
                SetKeyboardShortcuts,
                GetUiLanguage,
                SetUiLanguage,
                LogCommandInput,
                GetCommandPanelKey,
                GetCommandPanelKeyName,
                SetCommandPanelKey,
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
                IsCommandPanelCloseButtonShown,
                SetCommandPanelCloseButtonShown,
                IsRevealMapEveryFloor,
                SetRevealMapEveryFloor,
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
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Automatic random loadout is " + (_configuration.EnableEtgGameplayDashboardConfig.Value ? "enabled" : "disabled") + "."));
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
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Combat persisted states: rapid=" + (_configuration.RapidFireEnabledConfig.Value ? "on" : "off") + ", autoReload=" + _configuration.AutoReloadModeConfig.Value + ", ammoMode=" + _configuration.AmmoModeConfig.Value + ", activeItemNoCooldown=" + (_configuration.ActiveItemNoCooldownEnabledConfig.Value ? "on" : "off") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Nearby pickup info mode is gameplay."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Reveal Map verbose logs are " + (IsMapTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Muncher verbose logs are " + (IsMuncherVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Room enemy replay verbose logs are " + (IsRoomEnemyReplayVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init(
                "Room enemy replay logging configuration: " +
                "Section=" + (_configuration.RoomEnemyReplayVerboseLogsConfig != null ? _configuration.RoomEnemyReplayVerboseLogsConfig.Definition.Section : "<null>") +
                ", Key=" + (_configuration.RoomEnemyReplayVerboseLogsConfig != null ? _configuration.RoomEnemyReplayVerboseLogsConfig.Definition.Key : "<null>") +
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
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Nearby pickup gameplay info loaded: " + (_pickupGameplayRegistry != null ? _pickupGameplayRegistry.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + "."));
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Boss Rush service initialized. Startup self-check is running."));
        }

        private void ResetServices(bool pluginDestroying = false)
        {
            if (_playerRuntimeStatOverrideService != null)
            {
                _playerRuntimeStatOverrideService.Reset(!pluginDestroying);
            }

            if (_projectileModifierService != null)
            {
                _projectileModifierService.Reset(!pluginDestroying);
            }

            if (_damageDiagnosticsService != null)
            {
                _damageDiagnosticsService.Reset();
            }

            if (_roomEnemyReplayService != null)
            {
                _roomEnemyReplayService.Clear(pluginDestroying);
            }

            if (_rapidFireToggleService != null)
            {
                _rapidFireToggleService.Reset();
            }

            if (_skipChargeToggleService != null)
            {
                _skipChargeToggleService.Reset();
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

            if (_playerFlightToggleService != null)
            {
                _playerFlightToggleService.Reset();
            }

            if (_enemyHealthBarToggleService != null)
            {
                _enemyHealthBarToggleService.Reset();
            }

            if (_ammoModeToggleService != null)
            {
                _ammoModeToggleService.Reset();
            }

            if (_activeItemNoCooldownToggleService != null)
            {
                _activeItemNoCooldownToggleService.Reset();
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
