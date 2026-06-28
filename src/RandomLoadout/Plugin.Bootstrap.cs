using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private void Awake()
        {
            GuiText.Initialize(Paths.ConfigPath);
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
            _activeStartItemsPresetConfig = Config.Bind(
                "StartItems",
                "ActivePreset",
                StartItemsPresetNames.DefaultPresetId,
                "Active start-items preset id from ETG-Gameplay-Dashboard.rules.json5.");
            _aliasRegistry = PickupAliasRegistry.Empty;
            _configResolver = new EtgLoadoutConfigResolver(_pickupResolver);
            _pickupCatalogExporter = new EtgPickupCatalogExporter(
                Path.Combine(Paths.ConfigPath, PickupCatalogTextFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogGroupedJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _aliasFileProvider = new JsonPickupAliasFileProvider(DashboardFileLayout.GetAliasFilePath(Paths.ConfigPath));
            _randomPoolSelectionStateProvider = new RandomPoolSelectionStateProvider(Path.Combine(Paths.ConfigPath, RandomPoolSelectionStateFileName));
            _rapidFireToggleService = new RapidFireToggleService();
            _autoReloadToggleService = new AutoReloadToggleService();
            _invincibilityToggleService = new InvincibilityToggleService();
            _ammoModeToggleService = new AmmoModeToggleService();
            _ammonomiconFastOpenToggleService = new AmmonomiconFastOpenToggleService();
            _ammonomiconFastOpenToggleService.SetIsFastOpenEnabled(_ammonomiconFastOpenEnabledConfig.Value);
            _playerHealthOverrideService = new PlayerHealthOverrideService(Logger);
            _playerActiveItemCapacityOverrideService = new PlayerActiveItemCapacityOverrideService(Logger);
            _playerDebugCommandService = new PlayerDebugCommandService(_playerHealthOverrideService);
            _pickupGranter = new EtgPickupGranter();
            _bossRushService = new BossRushService(Logger, IsBossRushVerboseLoggingEnabled);
            _ruleFileProvider = new JsonLoadoutRuleFileProvider(
                DashboardFileLayout.GetRulesFilePath(Paths.ConfigPath),
                DashboardFileLayout.GetPresetsDirectoryPath(Paths.ConfigPath));
            _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
            _commandController = new InGameCommandController(
                new GrantCommandService(_pickupResolver, _pickupGranter, GetAliasRegistry),
                _playerDebugCommandService,
                new RoomDebugCommandService(IsMapTeleportVerboseLoggingEnabled, IsMuncherVerboseLoggingEnabled),
                new FoyerCharacterSwitchService(),
                _bossRushService,
                _rapidFireToggleService,
                _autoReloadToggleService,
                _invincibilityToggleService,
                _ammoModeToggleService,
                _ammonomiconFastOpenToggleService,
                new LoadoutRuleEditorService(_ruleFileProvider, _pickupResolver.GetGrantablePickupCatalog, InvalidateResolvedLoadoutConfig, GetActiveStartItemsPreset, SetActiveStartItemsPreset, _ownedPickupReader),
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
                IsExperimentalModeEnabled,
                SetExperimentalModeEnabled,
                SetAmmonomiconFastOpenEnabled,
                IsMapTeleportVerboseLoggingEnabled,
                IsFloorTeleportVerboseLoggingEnabled,
                BeginDeferredTeleportFromFoyer);
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _runState = new RunGrantState();
            _runLifecycleTracker = new RunLifecycleTracker(CharacterSelectSceneName, LoadingSceneName);
            _sceneWatcher = new RunSceneWatcher(CharacterSelectSceneName);
            _bossRushHarmony = new Harmony(GUID + ".bossrush");
            _ammonomiconAnimationHarmony = new Harmony(GUID + ".ammonomicon_animation");
            Logger.LogInfo(RandomLoadoutLog.Init("Waiting for GameManager startup."));
            Logger.LogInfo(RandomLoadoutLog.Init("Automatic random loadout is " + (_enableRandomLoadoutConfig.Value ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel language preference is " + GetUiLanguage() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel keyboard toggle key is " + GetCommandPanelKey() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel gamepad open input is 360 controller R3 short press."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel UI size preset is " + GetUiScalePreset() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Player stats side panel is " + (IsPlayerStatsPanelShown() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel experimental mode is " + (IsExperimentalModeEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Ammonomicon fast open is " + (IsAmmonomiconFastOpenEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Reveal Map verbose logs are " + (IsMapTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Muncher verbose logs are " + (IsMuncherVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Floor teleport verbose logs are " + (IsFloorTeleportVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush verbose logs are " + (IsBossRushVerboseLoggingEnabled() ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Active start-items preset is " + GetActiveStartItemsPreset() + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush service initialized. Startup self-check is running."));
            LogBossRushHookSelfCheck(BossRushHooks.Install(_bossRushHarmony, Logger));
            AmmonomiconAnimationHooks.Install(_ammonomiconAnimationHarmony, Logger);
            StartCoroutine(WaitForGameManagerAndSubscribe());
        }

        private void OnDestroy()
        {
            if (_rapidFireToggleService != null)
            {
                _rapidFireToggleService.Reset();
            }

            if (_autoReloadToggleService != null)
            {
                _autoReloadToggleService.Reset();
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

            if (_bossRushHarmony != null)
            {
                _bossRushHarmony.UnpatchSelf();
            }

            if (_ammonomiconAnimationHarmony != null)
            {
                _ammonomiconAnimationHarmony.UnpatchSelf();
            }

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
            Logger.LogInfo(RandomLoadoutLog.Init("GameManager startup detected. Scene watcher subscribed and GUI controller is ready."));
            Logger.LogInfo(RandomLoadoutLog.Init(NAME + " v" + VERSION + " started successfully."));
        }

        private void LogBossRushHookSelfCheck(BossRushHookInstallReport report)
        {
            if (report == null)
            {
                Logger.LogWarning(RandomLoadoutLog.Init("Boss Rush startup self-check did not produce a hook report."));
                return;
            }

            Logger.LogInfo(
                RandomLoadoutLog.Init(
                    "Boss Rush startup self-check complete. Applied hooks=" +
                    report.AppliedCount +
                    ", Skipped hooks=" +
                    report.SkippedCount +
                    "."));

            if (!report.HasSkippedHooks)
            {
                Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush startup self-check passed."));
                return;
            }

            string[] skippedHooks = report.SkippedHooks;
            for (int i = 0; i < skippedHooks.Length; i++)
            {
                Logger.LogWarning(RandomLoadoutLog.Init("Boss Rush startup self-check warning: " + skippedHooks[i]));
            }
        }

        private void OnGUI()
        {
            if (_commandController != null)
            {
                PlayerController player = null;
                GameManager gameManager = GameManager.Instance;
                if ((object)gameManager != null)
                {
                    player = gameManager.PrimaryPlayer;
                }

                _commandController.OnGUI(player, Logger);
            }
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
                RandomLoadoutLog.Init(
                    "Loaded start-loadout rules. File=" +
                    _ruleFileProvider.FilePath +
                    ", DefinitionCount=" +
                    (_ruleDefinitions != null ? _ruleDefinitions.Length : 0) +
                    ", PresetPickupCount=" +
                    _activePresetPickups.Length +
                    "."));

            for (int i = 0; i < ruleFileLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(RandomLoadoutLog.Init(ruleFileLoadResult.Messages[i]));
            }

            for (int i = 0; i < ruleFileLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(RandomLoadoutLog.Init(ruleFileLoadResult.Warnings[i]));
            }

            LoadoutConfigResolutionResult resolutionResult = _configResolver.Resolve(_ruleDefinitions, _aliasRegistry);
            _resolvedLoadoutConfig = resolutionResult.Config;
            _hasResolvedLoadoutConfig = true;
            int resolvedRuleCount = _resolvedLoadoutConfig != null && _resolvedLoadoutConfig.Rules != null
                ? _resolvedLoadoutConfig.Rules.Length
                : 0;
            Logger.LogInfo(RandomLoadoutLog.Init("Resolved start-loadout config. ResolvedRuleCount=" + resolvedRuleCount + "."));

            LogSelectionWarnings(resolutionResult.Warnings);
        }

        private void InvalidateResolvedLoadoutConfig()
        {
            _hasResolvedLoadoutConfig = false;
            _resolvedLoadoutConfig = null;
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _activePresetPickups = new LoadoutRuleFilePickupModel[0];
            Logger.LogInfo(RandomLoadoutLog.Init("Invalidated cached start-loadout config. The next automatic grant will reload rules from disk."));
        }

        private void LogSelectionWarnings(SelectionWarning[] warnings)
        {
            for (int i = 0; i < warnings.Length; i++)
            {
                SelectionWarning warning = warnings[i];
                string categoryPrefix = warning.Category.HasValue ? warning.Category.Value + ": " : string.Empty;
                Logger.LogWarning(RandomLoadoutLog.Grant(categoryPrefix + warning.Message + " [Code=" + warning.Code + "]"));
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
            Logger.LogInfo(RandomLoadoutLog.Command("Command panel language preference changed to " + normalized + "."));
        }

        private void LogCommandInput(string message)
        {
            if (Logger != null && !string.IsNullOrEmpty(message))
            {
                Logger.LogInfo(RandomLoadoutLog.Command("[Input] " + message));
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

            Logger.LogInfo(RandomLoadoutLog.Command("Command panel keyboard toggle key changed to " + normalized + "."));
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

            Logger.LogInfo(RandomLoadoutLog.Command("Command panel UI size preset changed to " + normalized + "."));
        }

        private bool IsExperimentalModeEnabled()
        {
            return _experimentalModeConfig != null && _experimentalModeConfig.Value;
        }

        private bool IsPlayerStatsPanelShown()
        {
            return _showPlayerStatsPanelConfig != null && _showPlayerStatsPanelConfig.Value;
        }

        private void SetPlayerStatsPanelShown(bool isEnabled)
        {
            if (_showPlayerStatsPanelConfig != null)
            {
                _showPlayerStatsPanelConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(RandomLoadoutLog.Command("Player stats side panel " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private void SetExperimentalModeEnabled(bool isEnabled)
        {
            if (_experimentalModeConfig != null)
            {
                _experimentalModeConfig.Value = isEnabled;
                Config.Save();
            }

            Logger.LogInfo(RandomLoadoutLog.Command("Command panel experimental mode " + (isEnabled ? "enabled" : "disabled") + "."));
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

            Logger.LogInfo(RandomLoadoutLog.Command("Ammonomicon fast open " + (isEnabled ? "enabled" : "disabled") + "."));
        }

        private bool IsMapTeleportVerboseLoggingEnabled()
        {
            return _mapTeleportVerboseLogsConfig != null && _mapTeleportVerboseLogsConfig.Value;
        }

        private bool IsMuncherVerboseLoggingEnabled()
        {
            return _muncherVerboseLogsConfig != null && _muncherVerboseLogsConfig.Value;
        }

        private bool IsFloorTeleportVerboseLoggingEnabled()
        {
            return _floorTeleportVerboseLogsConfig != null && _floorTeleportVerboseLogsConfig.Value;
        }

        private bool IsBossRushVerboseLoggingEnabled()
        {
            return _bossRushVerboseLogsConfig != null && _bossRushVerboseLogsConfig.Value;
        }

        private string NormalizeUiScalePreset(string presetName)
        {
            string normalized = UiScalePresetCatalog.Normalize(presetName);
            if (string.Equals(normalized, presetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            Logger.LogWarning(RandomLoadoutLog.Init("Invalid command panel UI size preset '" + presetName + "'. Falling back to " + UiScalePresetCatalog.DefaultPreset + "."));
            return normalized;
        }

        private string NormalizeCommandPanelKeyName(string keyName)
        {
            string normalized = string.IsNullOrEmpty(keyName) ? "F7" : keyName.Trim();
            if (ParseCommandPanelKey(normalized) != KeyCode.None)
            {
                return normalized;
            }

            Logger.LogWarning(RandomLoadoutLog.Init("Invalid command panel keyboard key '" + normalized + "'. Falling back to F7."));
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
            Logger.LogInfo(RandomLoadoutLog.Command("Active start-items preset changed to " + normalized + "."));
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
                Logger.LogInfo(RandomLoadoutLog.Alias(aliasLoadResult.Messages[i]));
            }

            for (int i = 0; i < aliasLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(RandomLoadoutLog.Alias(aliasLoadResult.Warnings[i]));
            }
        }

        private bool IsSupportedGrantablePickupId(int pickupId)
        {
            return _pickupResolver.ResolveAny(pickupId).Succeeded;
        }
    }
}
