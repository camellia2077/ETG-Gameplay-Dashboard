using BepInEx.Configuration;
using HarmonyLib;
using RandomLoadout.Core;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private const string CharacterSelectSceneName = "foyer";
        private const string LoadingSceneName = "LoadingDungeon";
        private const float GrantDelaySeconds = 1.5f;
        private const string PickupCatalogTextFileName = NAME + ".pickups.txt";
        private const string PickupCatalogJsonFileName = NAME + ".pickups.json";
        private const string PickupCatalogGroupedJsonFileName = NAME + ".pickups.by-category.json";
        private const string PickupCatalogRulePoolFileName = NAME + ".rules.full-pool.json5";
        private const string RandomPoolSelectionStateFileName = "ETG-Gameplay-Dashboard.selection-state.json5";

        private readonly EtgPickupResolver _pickupResolver = new EtgPickupResolver();
        private readonly EtgOwnedPickupReader _ownedPickupReader = new EtgOwnedPickupReader();
        private readonly LoadoutSelectionService _selectionService = new LoadoutSelectionService();
        private readonly ISeedProvider _seedProvider = new UtcTickSeedProvider();

        private ConfigEntry<bool> _enableRandomLoadoutConfig;
        private ConfigEntry<string> _uiLanguageConfig;
        private ConfigEntry<string> _commandPanelKeyConfig;
        private ConfigEntry<string> _uiScalePresetConfig;
        private ConfigEntry<bool> _showPlayerStatsPanelConfig;
        private ConfigEntry<bool> _experimentalModeConfig;
        private ConfigEntry<bool> _ammonomiconFastOpenEnabledConfig;
        private ConfigEntry<bool> _mapTeleportVerboseLogsConfig;
        private ConfigEntry<bool> _muncherVerboseLogsConfig;
        private ConfigEntry<bool> _floorTeleportVerboseLogsConfig;
        private ConfigEntry<bool> _bossRushVerboseLogsConfig;
        private ConfigEntry<string> _activeStartItemsPresetConfig;
        private LoadoutRuleDefinition[] _ruleDefinitions;
        private LoadoutConfig _resolvedLoadoutConfig;
        private LoadoutRuleFilePickupModel[] _activePresetPickups;
        private PickupAliasRegistry _aliasRegistry;
        private bool _hasLoadedAliasRegistry;
        private bool _hasResolvedLoadoutConfig;
        private JsonPickupAliasFileProvider _aliasFileProvider;
        private RandomPoolSelectionStateProvider _randomPoolSelectionStateProvider;
        private EtgLoadoutConfigResolver _configResolver;
        private EtgPickupCatalogExporter _pickupCatalogExporter;
        private JsonLoadoutRuleFileProvider _ruleFileProvider;
        private InGameCommandController _commandController;
        private RapidFireToggleService _rapidFireToggleService;
        private AutoReloadToggleService _autoReloadToggleService;
        private InvincibilityToggleService _invincibilityToggleService;
        private AmmoModeToggleService _ammoModeToggleService;
        private AmmonomiconFastOpenToggleService _ammonomiconFastOpenToggleService;
        private PlayerHealthOverrideService _playerHealthOverrideService;
        private PlayerActiveItemCapacityOverrideService _playerActiveItemCapacityOverrideService;
        private PlayerDebugCommandService _playerDebugCommandService;
        private EtgPickupGranter _pickupGranter;
        private BossRushService _bossRushService;
        private Harmony _bossRushHarmony;
        private Harmony _ammonomiconAnimationHarmony;
        private bool _hasExportedPickupCatalog;
        private string _lastPickupCatalogExportFailure;
        private RunGrantState _runState;
        private RunLifecycleTracker _runLifecycleTracker;
        private RunSceneWatcher _sceneWatcher;
        private EtgFloorDefinition _pendingTeleportFloor;
        private string _pendingTeleportLabelKey;
        private string _pendingTeleportCommandText;
        private string _pendingTeleportReadySceneName;
        private int _pendingTeleportReadyFrames;
    }
}
