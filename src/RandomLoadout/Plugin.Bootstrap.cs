using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;
using RandomLoadout.Core;

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
            _activeStartItemsPresetConfig = Config.Bind(
                "StartItems",
                "ActivePreset",
                "default",
                "Active start-items preset name from RandomLoadout.rules.json5.");
            _aliasRegistry = PickupAliasRegistry.Empty;
            _configResolver = new EtgLoadoutConfigResolver(_pickupResolver);
            _pickupCatalogExporter = new EtgPickupCatalogExporter(
                Path.Combine(Paths.ConfigPath, PickupCatalogTextFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogGroupedJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _aliasFileProvider = new JsonPickupAliasFileProvider(Path.Combine(Paths.ConfigPath, NAME + ".aliases.json5"));
            _rapidFireToggleService = new RapidFireToggleService();
            _autoReloadToggleService = new AutoReloadToggleService();
            _invincibilityToggleService = new InvincibilityToggleService();
            _noAmmoConsumptionToggleService = new NoAmmoConsumptionToggleService();
            _bossRushService = new BossRushService(Logger);
            _ruleFileProvider = new JsonLoadoutRuleFileProvider(
                Path.Combine(Paths.ConfigPath, NAME + ".rules.json5"),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
            _commandController = new InGameCommandController(
                new GrantCommandService(_pickupResolver, _pickupGranter, GetAliasRegistry),
                new PlayerDebugCommandService(),
                new FoyerCharacterSwitchService(),
                _bossRushService,
                _rapidFireToggleService,
                _autoReloadToggleService,
                _invincibilityToggleService,
                _noAmmoConsumptionToggleService,
                new LoadoutRuleEditorService(_ruleFileProvider, _pickupResolver.GetGrantablePickupCatalog, InvalidateResolvedLoadoutConfig, GetActiveStartItemsPreset, SetActiveStartItemsPreset, _ownedPickupReader),
                _pickupResolver.GetGrantablePickupCatalog,
                GetAliasRegistry,
                GetUiLanguage,
                SetUiLanguage);
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _runState = new RunGrantState();
            _runLifecycleTracker = new RunLifecycleTracker(CharacterSelectSceneName, LegacyCharacterSelectSceneName, LoadingSceneName);
            _sceneWatcher = new RunSceneWatcher(CharacterSelectSceneName);
            _bossRushHarmony = new Harmony(GUID + ".bossrush");
            _ammonomiconAnimationHarmony = new Harmony(GUID + ".ammonomicon_animation");

            Logger.LogInfo(RandomLoadoutLog.Init("Waiting for GameManager startup."));
            Logger.LogInfo(RandomLoadoutLog.Init("Automatic random loadout is " + (_enableRandomLoadoutConfig.Value ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Command panel language preference is " + GetUiLanguage() + "."));
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

            if (_noAmmoConsumptionToggleService != null)
            {
                _noAmmoConsumptionToggleService.Reset();
            }

            AmmonomiconAnimationToggleService.Reset();

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
            Logger.LogInfo(
                RandomLoadoutLog.Init(
                    "Loaded start-loadout rules. File=" +
                    _ruleFileProvider.FilePath +
                    ", DefinitionCount=" +
                    (_ruleDefinitions != null ? _ruleDefinitions.Length : 0) +
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

        private string GetActiveStartItemsPreset()
        {
            return _activeStartItemsPresetConfig != null ? _activeStartItemsPresetConfig.Value : "default";
        }

        private void SetActiveStartItemsPreset(string presetName)
        {
            string normalized = string.IsNullOrEmpty(presetName) ? "default" : presetName.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                normalized = "default";
            }

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
