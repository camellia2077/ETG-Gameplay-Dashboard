// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private const string CharacterSelectSceneName = "foyer";
        private const string LoadingSceneName = "LoadingDungeon";
        private const float GrantDelaySeconds = 1.5f;
        private const string PickupCatalogTextFileName = NAME + ".pickups.txt";
        private const string PickupCatalogJsonFileName = NAME + ".pickups.json";
        private const string PickupCatalogGroupedJsonFileName = NAME + ".pickups.by-category.json";
        private const string PickupNamesJsonFileName = NAME + ".pickup-names.game-language.json";
        private const string PickupCatalogRulePoolFileName = NAME + ".rules.full-pool.json5";
        private const string RandomPoolSelectionStateFileName = "ETG-Gameplay-Dashboard.selection-state.json5";

        private readonly EtgPickupResolver _pickupResolver = new EtgPickupResolver();
        private readonly EtgOwnedPickupReader _ownedPickupReader = new EtgOwnedPickupReader();
        private readonly LoadoutSelectionService _selectionService = new LoadoutSelectionService();
        private readonly UtcTickSeedProvider _seedProvider = new UtcTickSeedProvider();
        private PluginConfigurationFacade _configuration;

        private bool _roomReplayFloorTransitionObserved;
        private LoadoutRuleDefinition[] _ruleDefinitions;
        private LoadoutConfig _resolvedLoadoutConfig;
        private LoadoutRuleFilePickupModel[] _activePresetPickups;
        private PickupAliasRegistry _aliasRegistry;
        private bool _hasLoadedAliasRegistry;
        private bool _hasResolvedLoadoutConfig;
        private JsonPickupAliasFileProvider _aliasFileProvider;
        private JsonPickupGameplayProvider _pickupGameplayProvider;
        private RandomPoolSelectionStateProvider _randomPoolSelectionStateProvider;
        private EtgPickupCatalogExporter _pickupCatalogExporter;
        private JsonLoadoutRuleFileProvider _ruleFileProvider;
        private LoadoutPresetRandomService _loadoutPresetRandomService;
        private InGameCommandController _commandController;
        private PickupGameplayRegistry _pickupGameplayRegistry;
        private PickupInfoTermsRegistry _pickupInfoTermsRegistry;
        private BossNameCatalog _bossNameCatalog;
        private NearbyPickupTipService _nearbyPickupTipService;
        private RapidFireToggleService _rapidFireToggleService;
        private SkipChargeToggleService _skipChargeToggleService;
        private AutoReloadToggleService _autoReloadToggleService;
        private ArmorNoConsumeToggleService _armorNoConsumeToggleService;
        private BlankNoConsumeToggleService _blankNoConsumeToggleService;
        private KeyNoConsumeToggleService _keyNoConsumeToggleService;
        private CurrencyNoConsumeToggleService _currencyNoConsumeToggleService;
        private ActiveItemNoCooldownToggleService _activeItemNoCooldownToggleService;
        private InvincibilityToggleService _invincibilityToggleService;
        private PlayerFlightToggleService _playerFlightToggleService;
        private EnemyHealthBarToggleService _enemyHealthBarToggleService;
        private ControllerAimLockService _controllerAimLockService;
        private KeyboardAimAssistService _keyboardAimAssistService;
        private AmmoModeToggleService _ammoModeToggleService;
        private AmmonomiconFastOpenToggleService _ammonomiconFastOpenToggleService;
        private PlayerHealthOverrideService _playerHealthOverrideService;
        private PlayerActiveItemCapacityOverrideService _playerActiveItemCapacityOverrideService;
        private PlayerDebugCommandService _playerDebugCommandService;
        private PlayerRuntimeStatOverrideService _playerRuntimeStatOverrideService;
        private ProjectileModifierService _projectileModifierService;
        private DamageDiagnosticsService _damageDiagnosticsService;
        private EtgPickupGranter _pickupGranter;
        private BossRushService _bossRushService;
        private BossRushCoroutineHost _bossRushCoroutineHost;
        private RoomEnemyReplayService _roomEnemyReplayService;
        private RuntimeHookRegistry _runtimeHookRegistry;
        private GameWindowFocusService _gameWindowFocusService;
        private PerformanceDiagnostics _performanceDiagnostics;
        private bool _hasScheduledSceneReadyWindowFocusRetry;
        private bool _hasStartedWindowForegroundMonitor;
        private bool _hasExportedPickupCatalog;
        private string _lastPickupCatalogExportFailure;
        private float _gameplayPerformanceWindowStartedAt = -1f;
        private string _gameplayPerformanceSceneName = string.Empty;
        private RunGrantState _runState;
        private RunLifecycleTracker _runLifecycleTracker;
        private RunSceneWatcher _sceneWatcher;
        private EtgFloorDefinition _pendingTeleportFloor;
        private string _pendingTeleportLabelKey;
        private string _pendingTeleportCommandText;
        private string _pendingTeleportReadySceneName;
        private int _pendingTeleportReadyFrames;
        private UnityEngine.GUIStyle _pickupWikiTipPanelStyle;
        private UnityEngine.GUIStyle _pickupWikiTipTitleStyle;
        private UnityEngine.GUIStyle _pickupWikiTipBodyStyle;
        private UnityEngine.Vector2 _pickupWikiTipScrollPosition = UnityEngine.Vector2.zero;
        private int _pickupWikiTipScrollPickupId;
    }
}
