// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;
using Dungeonator;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        public InGameCommandController(
            GrantCommandService commandService,
            PlayerDebugCommandService playerDebugCommandService,
            RoomDebugCommandService roomDebugCommandService,
            FoyerCharacterSwitchService foyerCharacterSwitchService,
            BossRushService bossRushService,
            RapidFireToggleService rapidFireToggleService,
            AutoReloadToggleService autoReloadToggleService,
            ArmorNoConsumeToggleService armorNoConsumeToggleService,
            BlankNoConsumeToggleService blankNoConsumeToggleService,
            KeyNoConsumeToggleService keyNoConsumeToggleService,
            CurrencyNoConsumeToggleService currencyNoConsumeToggleService,
            InvincibilityToggleService invincibilityToggleService,
            EnemyHealthBarToggleService enemyHealthBarToggleService,
            ControllerAimLockService controllerAimLockService,
            KeyboardAimAssistService keyboardAimAssistService,
            PlayerStatMultiplierService playerStatMultiplierService,
            AmmoModeToggleService ammoModeToggleService,
            AmmonomiconFastOpenToggleService ammonomiconFastOpenToggleService,
            LoadoutRuleEditorService loadoutRuleEditorService,
            LoadoutPresetRandomService loadoutPresetRandomService,
            System.Func<EtgPickupCatalogEntry[]> pickupCatalogProvider,
            System.Func<int, string> pickupGameplayNameProvider,
            System.Func<PickupAliasRegistry> aliasRegistryProvider,
            System.Func<string> languageProvider,
            System.Action<string> languageSetter,
            System.Action<string> inputLogHandler,
            System.Func<KeyCode> toggleKeyProvider,
            System.Func<string> toggleKeyNameProvider,
            System.Action<string> toggleKeySetter,
            System.Func<KeyCode> roomEnemyRewindKeyProvider,
            System.Func<string> roomEnemyRefreshMethodProvider,
            System.Action<string> roomEnemyRefreshMethodSetter,
            System.Func<string> controllerShortcutProvider,
            System.Action<string> controllerShortcutSetter,
            System.Func<bool> controllerShortcutEnabledProvider,
            System.Action<bool> controllerShortcutEnabledSetter,
            System.Func<string> uiScalePresetProvider,
            System.Action<string> uiScalePresetSetter,
            System.Func<string> themeProvider,
            System.Action<string> themeSetter,
            System.Func<bool> startItemsPresetIconsEnabledProvider,
            System.Action<bool> startItemsPresetIconsEnabledSetter,
            System.Func<bool> playerStatsPanelShownProvider,
            System.Action<bool> playerStatsPanelShownSetter,
            System.Func<bool> pickupInfoOverlayEnabledProvider,
            System.Action<bool> pickupInfoOverlayEnabledSetter,
            System.Func<bool> pickupInfoQualityEnabledProvider,
            System.Action<bool> pickupInfoQualityEnabledSetter,
            System.Func<bool> pickupInfoTypeEnabledProvider,
            System.Action<bool> pickupInfoTypeEnabledSetter,
            System.Func<bool> pickupInfoEffectsEnabledProvider,
            System.Action<bool> pickupInfoEffectsEnabledSetter,
            System.Func<bool> pickupInfoSynergiesEnabledProvider,
            System.Action<bool> pickupInfoSynergiesEnabledSetter,
            System.Func<bool> pickupInfoSummaryEnabledProvider,
            System.Action<bool> pickupInfoSummaryEnabledSetter,
            System.Func<bool> pickupInfoNotesEnabledProvider,
            System.Action<bool> pickupInfoNotesEnabledSetter,
            System.Func<bool> experimentalModeProvider,
            System.Action<bool> experimentalModeSetter,
            System.Action<bool> ammonomiconFastOpenEnabledSetter,
            System.Func<bool> mapTeleportVerboseLoggingEnabledProvider,
            System.Func<bool> floorTeleportVerboseLoggingEnabledProvider,
            System.Func<bool> commandPanelHealthVerboseLoggingEnabledProvider,
            System.Func<bool> commandPanelCursorVerboseLoggingEnabledProvider,
            System.Func<bool> commandPanelGameplayInputVerboseLoggingEnabledProvider,
            System.Func<bool> commandPanelControllerGameplayInputVerboseLoggingEnabledProvider,
            System.Func<bool> commandPanelShortcutVerboseLoggingEnabledProvider,
            System.Func<string> combatCursorColorProvider,
            System.Action<string> combatCursorColorSetter,
            System.Func<bool> performanceVerboseLoggingEnabledProvider,
            BepInEx.Logging.ManualLogSource performanceLogger,
            System.Func<EtgFloorDefinition, string, string, bool> deferredTeleportRequestHandler)
        {
            _commandService = commandService;
            _playerDebugCommandService = playerDebugCommandService;
            _roomDebugCommandService = roomDebugCommandService;
            _foyerCharacterSwitchService = foyerCharacterSwitchService;
            _bossRushService = bossRushService;
            _rapidFireToggleService = rapidFireToggleService;
            _autoReloadToggleService = autoReloadToggleService;
            _armorNoConsumeToggleService = armorNoConsumeToggleService;
            _blankNoConsumeToggleService = blankNoConsumeToggleService;
            _keyNoConsumeToggleService = keyNoConsumeToggleService;
            _currencyNoConsumeToggleService = currencyNoConsumeToggleService;
            _invincibilityToggleService = invincibilityToggleService;
            _enemyHealthBarToggleService = enemyHealthBarToggleService;
            _controllerAimLockService = controllerAimLockService;
            _keyboardAimAssistService = keyboardAimAssistService;
            _playerStatMultiplierService = playerStatMultiplierService;
            _ammoModeToggleService = ammoModeToggleService;
            _ammonomiconFastOpenToggleService = ammonomiconFastOpenToggleService;
            _loadoutRuleEditorService = loadoutRuleEditorService;
            _loadoutPresetRandomService = loadoutPresetRandomService;
            _pickupCatalogProvider = pickupCatalogProvider;
            _pickupGameplayNameProvider = pickupGameplayNameProvider;
            _aliasRegistryProvider = aliasRegistryProvider;
            _languageProvider = languageProvider;
            _languageSetter = languageSetter;
            _inputLogHandler = inputLogHandler;
            _toggleKeyProvider = toggleKeyProvider;
            _toggleKeyNameProvider = toggleKeyNameProvider;
            _toggleKeySetter = toggleKeySetter;
            _roomEnemyRewindKeyProvider = roomEnemyRewindKeyProvider;
            _roomEnemyRefreshMethodProvider = roomEnemyRefreshMethodProvider;
            _roomEnemyRefreshMethodSetter = roomEnemyRefreshMethodSetter;
            _controllerShortcutProvider = controllerShortcutProvider;
            _controllerShortcutSetter = controllerShortcutSetter;
            _controllerShortcutEnabledProvider = controllerShortcutEnabledProvider;
            _controllerShortcutEnabledSetter = controllerShortcutEnabledSetter;
            _uiScalePresetProvider = uiScalePresetProvider;
            _uiScalePresetSetter = uiScalePresetSetter;
            _themeProvider = themeProvider;
            _themeSetter = themeSetter;
            _startItemsPresetIconsEnabledProvider = startItemsPresetIconsEnabledProvider;
            _startItemsPresetIconsEnabledSetter = startItemsPresetIconsEnabledSetter;
            _playerStatsPanelShownProvider = playerStatsPanelShownProvider;
            _playerStatsPanelShownSetter = playerStatsPanelShownSetter;
            _pickupInfoOverlayEnabledProvider = pickupInfoOverlayEnabledProvider;
            _pickupInfoOverlayEnabledSetter = pickupInfoOverlayEnabledSetter;
            _pickupInfoQualityEnabledProvider = pickupInfoQualityEnabledProvider;
            _pickupInfoQualityEnabledSetter = pickupInfoQualityEnabledSetter;
            _pickupInfoTypeEnabledProvider = pickupInfoTypeEnabledProvider;
            _pickupInfoTypeEnabledSetter = pickupInfoTypeEnabledSetter;
            _pickupInfoEffectsEnabledProvider = pickupInfoEffectsEnabledProvider;
            _pickupInfoEffectsEnabledSetter = pickupInfoEffectsEnabledSetter;
            _pickupInfoSynergiesEnabledProvider = pickupInfoSynergiesEnabledProvider;
            _pickupInfoSynergiesEnabledSetter = pickupInfoSynergiesEnabledSetter;
            _pickupInfoSummaryEnabledProvider = pickupInfoSummaryEnabledProvider;
            _pickupInfoSummaryEnabledSetter = pickupInfoSummaryEnabledSetter;
            _pickupInfoNotesEnabledProvider = pickupInfoNotesEnabledProvider;
            _pickupInfoNotesEnabledSetter = pickupInfoNotesEnabledSetter;
            _experimentalModeProvider = experimentalModeProvider;
            _experimentalModeSetter = experimentalModeSetter;
            _ammonomiconFastOpenEnabledSetter = ammonomiconFastOpenEnabledSetter;
            _mapTeleportVerboseLoggingEnabledProvider = mapTeleportVerboseLoggingEnabledProvider;
            _floorTeleportVerboseLoggingEnabledProvider = floorTeleportVerboseLoggingEnabledProvider;
            _commandPanelHealthVerboseLoggingEnabledProvider = commandPanelHealthVerboseLoggingEnabledProvider;
            _commandPanelCursorVerboseLoggingEnabledProvider = commandPanelCursorVerboseLoggingEnabledProvider;
            _commandPanelGameplayInputVerboseLoggingEnabledProvider = commandPanelGameplayInputVerboseLoggingEnabledProvider;
            _commandPanelControllerGameplayInputVerboseLoggingEnabledProvider = commandPanelControllerGameplayInputVerboseLoggingEnabledProvider;
            _commandPanelShortcutVerboseLoggingEnabledProvider = commandPanelShortcutVerboseLoggingEnabledProvider;
            _combatCursorColorProvider = combatCursorColorProvider;
            _combatCursorColorSetter = combatCursorColorSetter;
            _performanceVerboseLoggingEnabledProvider = performanceVerboseLoggingEnabledProvider;
            _performanceLogger = performanceLogger;
            _deferredTeleportRequestHandler = deferredTeleportRequestHandler;
            string persistedRoomEnemyRefreshMethod = _roomEnemyRefreshMethodProvider != null ? _roomEnemyRefreshMethodProvider() : "rewind";
            _roomEnemyRefreshMethod = string.Equals(persistedRoomEnemyRefreshMethod, "respawn", System.StringComparison.OrdinalIgnoreCase)
                ? RoomEnemyRefreshMethod.RespawnEnemies
                : RoomEnemyRefreshMethod.Rewind;
            _showPlayerStatsPanel = _playerStatsPanelShownProvider != null && _playerStatsPanelShownProvider();
            _showPickupInfoOverlay = _pickupInfoOverlayEnabledProvider == null || _pickupInfoOverlayEnabledProvider();
            _showPickupInfoQuality = _pickupInfoQualityEnabledProvider == null || _pickupInfoQualityEnabledProvider();
            _showPickupInfoType = _pickupInfoTypeEnabledProvider == null || _pickupInfoTypeEnabledProvider();
            _showPickupInfoEffects = _pickupInfoEffectsEnabledProvider == null || _pickupInfoEffectsEnabledProvider();
            _showPickupInfoSynergies = _pickupInfoSynergiesEnabledProvider == null || _pickupInfoSynergiesEnabledProvider();
            _showPickupInfoSummary = _pickupInfoSummaryEnabledProvider == null || _pickupInfoSummaryEnabledProvider();
            _showPickupInfoNotes = _pickupInfoNotesEnabledProvider == null || _pickupInfoNotesEnabledProvider();
            if (_bossRushService != null)
            {
                _bossRushService.StatusRaised += OnBossRushStatusRaised;
            }
        }

        public void Update()
        {
            SyncPanelInputOverride();
            LogGameplayKeyboardInputState();
            LogControllerGameplayInputState();
            LogJoystickButtonStateChanges();
            LogControllerStickStateChanges();
            LogHealthDiagnosticStateChanges();
            LogCursorVisibilityStateChanges();
            UpdateMapFeatureActivationState();
            LogMapDirectTeleportRoomTransitionIfNeeded();
            LogMapDirectTeleportRuntimeStateIfNeeded();

            HandleControllerNavigation();

            bool keyboardTogglePressed = Input.GetKeyDown(GetToggleKey());
            bool controllerTogglePressed = IsGamepadToggleShortcutPressed();
            LogCommandPanelShortcutState(keyboardTogglePressed, controllerTogglePressed);
            if (keyboardTogglePressed || controllerTogglePressed)
            {
                LogCommandPanelShortcutDiagnostic(
                    "Command panel toggle accepted. Source=" +
                    (keyboardTogglePressed ? "Keyboard" : "Controller") +
                    ".");
                Toggle();
            }

            TryHandleRoomEnemyRewindShortcut();

            if (!_isVisible)
            {
                return;
            }

            LogMouseButtonAttempts();
            LogDisabledKeyboardNavigationKeyAttempts();
        }

        public void OnGUI(PlayerController player, BepInEx.Logging.ManualLogSource logger)
        {
            LogCommandPanelPerformanceStage("OnGUI.begin");
            long stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            EnsureStyles();
            LogCommandPanelPerformanceStage("EnsureStyles", stageStartedAtTimestamp);
            stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            ReleaseGuiFocusIfPending();
            string currentLanguageCode = GuiText.CurrentLanguageCode;
            if (!string.Equals(_lastGuiLanguageCode, currentLanguageCode, System.StringComparison.Ordinal))
            {
                _lastGuiLanguageCode = currentLanguageCode;
                HandleLanguageChanged();
            }
            LogCommandPanelPerformanceStage("LanguageAndFocus", stageStartedAtTimestamp);

            FoyerCharacterOption[] characterOptions = EmptyCharacterOptions;
            string characterAvailability = _cachedCharacterAvailability;
            float panelHeight = GetCommandPanelHeight();
            stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            if (_isVisible && _currentPage == PanelPage.Pickups)
            {
                RefreshPickupBrowserData();
                panelHeight = PickupBrowserPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.Characters)
            {
                RefreshCharacterPageData(false);
                characterOptions = _cachedCharacterOptions;
                characterAvailability = _cachedCharacterAvailability;
                panelHeight = GetPanelHeight(characterOptions, characterAvailability);
            }
            else if (_isVisible && _currentPage == PanelPage.Currency)
            {
                panelHeight = CurrencyPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.BossRush)
            {
                panelHeight = BossRushPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.LoadoutEditor)
            {
                panelHeight = LoadoutEditorPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.About)
            {
                panelHeight = AboutPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.Settings)
            {
                panelHeight = SettingsPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.PickupInfoConfig)
            {
                panelHeight = PickupInfoConfigPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.AdvancedTools)
            {
                panelHeight = AdvancedToolsPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.ControllerHelp)
            {
                panelHeight = ControllerHelpPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.KeyboardHelp)
            {
                panelHeight = KeyboardHelpPanelHeight;
            }
            else if (_isVisible && _currentPage == PanelPage.CursorColor)
            {
                panelHeight = CursorColorPanelHeight;
            }
            LogCommandPanelPerformanceStage("PageDataAndHeight", stageStartedAtTimestamp);

            Matrix4x4 previousGuiMatrix = GUI.matrix;
            GUI.matrix = GetAutoScaledGuiMatrix();
            LogCommandPanelPerformanceStage("GuiMatrix", BeginCommandPanelPerformanceStage());
            try
            {
                DrawPlayerStatsPanelIfEnabled(player);
                DrawStatusOverlay(panelHeight);
                if (!_isVisible)
                {
                    return;
                }

                Rect panelRect = GetMainPanelRect(panelHeight);
                GUI.Box(ExpandPanelBorderRect(panelRect), GUIContent.none, _panelStyle);
                DrawTeleportPanelIfEnabled(panelRect, logger);
                if (_currentPage == PanelPage.Characters)
                {
                    DrawCharacterPage(panelRect, characterOptions, characterAvailability, logger);
                    return;
                }

                if (_currentPage == PanelPage.Pickups)
                {
                    DrawPickupPage(panelRect, player, logger);
                    return;
                }

                if (_currentPage == PanelPage.Currency)
                {
                    DrawCurrencyPage(panelRect, player, logger);
                    return;
                }

                if (_currentPage == PanelPage.BossRush)
                {
                    DrawBossRushPage(panelRect, logger);
                    return;
                }

                if (_currentPage == PanelPage.LoadoutEditor)
                {
                    DrawLoadoutEditorPage(panelRect, player, logger);
                    return;
                }

                if (_currentPage == PanelPage.About)
                {
                    DrawAboutPage(panelRect);
                    return;
                }

                if (_currentPage == PanelPage.AdvancedTools)
                {
                    DrawAdvancedToolsPage(panelRect, player, logger);
                    return;
                }

                if (_currentPage == PanelPage.ControllerHelp)
                {
                    DrawControllerHelpPage(panelRect);
                    return;
                }

                if (_currentPage == PanelPage.KeyboardHelp)
                {
                    DrawKeyboardHelpPage(panelRect);
                    return;
                }

                if (_currentPage == PanelPage.CursorColor)
                {
                    DrawCursorColorPage(panelRect);
                    return;
                }

                if (_currentPage == PanelPage.Settings)
                {
                    DrawSettingsPage(panelRect, logger);
                    DrawExperimentalModeConfirmDialog(panelRect, logger);
                    return;
                }

                if (_currentPage == PanelPage.PickupInfoConfig)
                {
                    DrawPickupInfoConfigPage(panelRect);
                    return;
                }

                DrawCommandPage(panelRect, player, logger);

                DrawExperimentalModeConfirmDialog(panelRect, logger);
            }
            finally
            {
                CompleteCommandPanelPerformanceTrace("OnGUI.complete");
                GUI.matrix = previousGuiMatrix;
            }
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                BeginCommandPanelPerformanceTrace();
                SyncPanelInputOverride();
                _focusInputField = false;
                _focusPickupSearchField = false;
                RequestGuiFocusRelease();
                LogCommandPanelPerformanceStage("Toggle.open.ready");
                return;
            }

            CompleteCommandPanelPerformanceTrace("Toggle.close");
            ResetClosedPanelState();
        }

        private void HandleControllerNavigation()
        {
            if (!_isVisible)
            {
                ResetControllerNavigationAxes();
                return;
            }

            bool isControllerBackPressed = IsPanelBackPressed();
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Detected controller back press. Page=" +
                    _currentPage +
                    ", CommandFocus=" +
                    _commandPageFocusedControlId +
                    ", SettingsFocus=" +
                    _settingsPageFocusedControlId +
                    ", ExperimentalDialog=" +
                    _showExperimentalModeConfirmDialog +
                    ".");
            }

            if (_showExperimentalModeConfirmDialog && _currentPage == PanelPage.Settings)
            {
                if (isControllerBackPressed)
                {
                    LogGamepadShortcutState("Controller back press dismissed the experimental mode confirmation dialog.");
                    _showExperimentalModeConfirmDialog = false;
                }

                if (IsPanelConfirmPressed())
                {
                    SetExperimentalModeEnabled(true, null);
                }

                ResetControllerNavigationAxes();
                return;
            }

            if (_showTeleportPanel)
            {
                HandleTeleportPanelControllerNavigation(isControllerBackPressed);
                return;
            }

            switch (_currentPage)
            {
                case PanelPage.Command:
                    HandleCommandPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Settings:
                    HandleSettingsPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.PickupInfoConfig:
                    HandlePickupInfoConfigPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Characters:
                    HandleCharacterPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.AdvancedTools:
                    HandleAdvancedToolsPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.ControllerHelp:
                    HandleControllerHelpPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.KeyboardHelp:
                    HandleKeyboardHelpPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.CursorColor:
                    HandleCursorColorPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Pickups:
                    HandlePickupPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Currency:
                    HandleCurrencyPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.LoadoutEditor:
                    HandleLoadoutEditorPageControllerNavigation(isControllerBackPressed);
                    return;
                default:
                    if (isControllerBackPressed)
                    {
                        LogGamepadShortcutState(
                            "Controller back press detected on a page without controller back handling. Page=" +
                            _currentPage +
                            ".");
                    }

                    ResetControllerNavigationAxes();
                    return;
            }
        }

        private void HandleCommandPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is closing the command page.");
                Close();
                return;
            }

            if (Input.GetKeyDown(GetJoystickButtonKeyCode(4)))
            {
                CycleCommandCategory(-1);
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _commandPageFocusedControlId;
                _commandPageFocusedControlId = MoveControllerFocus(GetCommandPageFocusEntries(), _commandPageFocusedControlId, navigationDirection.Value);
                if (IsCommandPanelHealthVerboseLoggingEnabled())
                {
                    LogGamepadShortcutState(
                        "Command page controller navigation moved focus. Direction=" +
                        navigationDirection.Value +
                        ", From=" +
                        previousControlId +
                        ", To=" +
                        _commandPageFocusedControlId +
                        ", PlayerVitals=" +
                        DescribePlayerVitals(GetCurrentPlayer()) +
                        ".");
                }
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteCommandPageFocusedControl();
            }
        }

        private void HandleSettingsPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from settings to the command page.");
                _currentPage = PanelPage.Command;
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _settingsPageFocusedControlId;
                _settingsPageFocusedControlId = MoveControllerFocus(GetSettingsPageFocusEntries(), _settingsPageFocusedControlId, navigationDirection.Value);
                LogGamepadShortcutState(
                    "Settings page controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _settingsPageFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteSettingsPageFocusedControl();
            }
        }

        private void HandlePickupInfoConfigPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from pickup info config to the command page.");
                _currentPage = PanelPage.Command;
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _pickupInfoConfigFocusedControlId;
                _pickupInfoConfigFocusedControlId = MoveControllerFocus(PickupInfoConfigPageFocusEntries, _pickupInfoConfigFocusedControlId, navigationDirection.Value);
                LogGamepadShortcutState(
                    "Pickup info config controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _pickupInfoConfigFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecutePickupInfoConfigPageFocusedControl();
            }
        }

        private void HandleCharacterPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is returning from characters to the command page.");
                CloseCharacterPage();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _characterPageFocusedControlId;
                _characterPageFocusedControlId = MoveControllerFocus(
                    GetCharacterPageFocusEntries(_cachedCharacterOptions),
                    _characterPageFocusedControlId,
                    navigationDirection.Value);
                LogGamepadShortcutState(
                    "Character page controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _characterPageFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteCharacterPageFocusedControl(null);
            }
        }

        private void HandleControllerHelpPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed || IsPanelConfirmPressed())
            {
                LogGamepadShortcutState("Controller navigation is returning from controller help to settings.");
                OpenSettingsPage();
                return;
            }

            ResetControllerNavigationAxes();
        }

        private void HandleKeyboardHelpPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed || IsPanelConfirmPressed())
            {
                LogGamepadShortcutState("Keyboard navigation is returning from keyboard help to settings.");
                OpenSettingsPage();
                return;
            }

            ResetControllerNavigationAxes();
        }

        private void HandlePickupPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Controller back press detected on pickup browser. Mode=" +
                    _pickupBrowserMode +
                    ", Focus=" +
                    _pickupPageFocusedControlId +
                    ".");
                ClosePickupPage();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _pickupPageFocusedControlId;
                _pickupPageFocusedControlId = MoveControllerFocus(
                    GetPickupPageFocusEntries(),
                    _pickupPageFocusedControlId,
                    navigationDirection.Value);
                LogGamepadShortcutState(
                    "Pickup browser controller navigation moved focus. Mode=" +
                    _pickupBrowserMode +
                    ", Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _pickupPageFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecutePickupPageFocusedControl(GetSelectedCommandTargetPlayer(), null);
            }
        }

        private void HandleCurrencyPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Controller back press detected on currency page. Focus=" +
                    _currencyPageFocusedControlId +
                    ".");
                CloseCurrencyPage();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _currencyPageFocusedControlId;
                _currencyPageFocusedControlId = MoveControllerFocus(
                    GetCurrencyPageFocusEntries(),
                    _currencyPageFocusedControlId,
                    navigationDirection.Value);
                if (IsCommandPanelHealthVerboseLoggingEnabled())
                {
                    LogGamepadShortcutState(
                        "Currency page controller navigation moved focus. Direction=" +
                        navigationDirection.Value +
                        ", From=" +
                        previousControlId +
                        ", To=" +
                        _currencyPageFocusedControlId +
                        ", PlayerVitals=" +
                        DescribePlayerVitals(GetCurrentPlayer()) +
                        ".");
                }
            }

            if (IsPanelConfirmPressed())
            {
                LogGamepadShortcutState(
                    "Controller confirm is activating currency page control. Focus=" +
                    _currencyPageFocusedControlId +
                    ".");
                ExecuteCurrencyPageFocusedControl(GetCurrentPlayer(), null);
            }
        }

        private void HandleLoadoutEditorPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState(
                    "Controller back press detected on loadout editor. Mode=" +
                    _loadoutEditorMode +
                    ", Focus=" +
                    _loadoutEditorFocusedControlId +
                    ".");
                HandleLoadoutEditorBackNavigation();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                string previousControlId = _loadoutEditorFocusedControlId;
                _loadoutEditorFocusedControlId = MoveControllerFocus(
                    GetLoadoutEditorFocusEntries(),
                    _loadoutEditorFocusedControlId,
                    navigationDirection.Value);
                LogGamepadShortcutState(
                    "Loadout editor controller navigation moved focus. Mode=" +
                    _loadoutEditorMode +
                    ", Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _loadoutEditorFocusedControlId +
                    ".");
            }

            if (IsPanelConfirmPressed())
            {
                ExecuteLoadoutEditorFocusedControl(GetCurrentPlayer(), null);
            }
        }

        private void HandleTeleportPanelControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed)
            {
                LogGamepadShortcutState("Controller back press is closing the teleport panel.");
                CloseTeleportPanel();
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                int previousIndex = _teleportSelectedIndex;
                switch (navigationDirection.Value)
                {
                    case ControllerNavDirection.Up:
                        _teleportSelectedIndex = (_teleportSelectedIndex + TeleportOptions.Length - 1) % TeleportOptions.Length;
                        break;
                    case ControllerNavDirection.Down:
                        _teleportSelectedIndex = (_teleportSelectedIndex + 1) % TeleportOptions.Length;
                        break;
                    default:
                        LogGamepadShortcutState(
                            "Ignored teleport panel horizontal navigation. Direction=" +
                            navigationDirection.Value +
                            ", SelectedIndex=" +
                            _teleportSelectedIndex +
                            ".");
                        return;
                }

                LogGamepadShortcutState(
                    "Teleport panel selection changed. Direction=" +
                    navigationDirection.Value +
                    ", FromIndex=" +
                    previousIndex +
                    ", ToIndex=" +
                    _teleportSelectedIndex +
                    ", Token=" +
                    TeleportOptions[_teleportSelectedIndex].CommandToken +
                    ".");
                return;
            }

            if (IsPanelConfirmPressed())
            {
                TeleportOption selectedOption = TeleportOptions[_teleportSelectedIndex];
                LogGamepadShortcutState(
                    "Controller confirm is activating teleport option. SelectedIndex=" +
                    _teleportSelectedIndex +
                    ", Token=" +
                    selectedOption.CommandToken +
                    ".");
                ExecuteTeleport(selectedOption, null);
            }
        }

        private ControllerNavDirection? GetControllerNavigationDirection()
        {
            ControllerNavDirection? keyboardDirection = GetKeyboardNavigationDirection();
            if (keyboardDirection.HasValue)
            {
                return keyboardDirection;
            }

            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            float horizontal;
            float vertical;
            GetControllerNavigationAxes(braveInput, out horizontal, out vertical);
            LogControllerNavigationAxisState(horizontal, vertical);

            if (Mathf.Abs(horizontal) < 0.5f)
            {
                _wasControllerHorizontalNavigationActive = false;
            }
            else if (!_wasControllerHorizontalNavigationActive)
            {
                _wasControllerHorizontalNavigationActive = true;
                LogGamepadShortcutState(
                    "Controller navigation direction detected from horizontal axis. Horizontal=" +
                    horizontal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    ", Vertical=" +
                    vertical.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    ".");
                return horizontal > 0f ? ControllerNavDirection.Right : ControllerNavDirection.Left;
            }

            if (Mathf.Abs(vertical) < 0.5f)
            {
                _wasControllerVerticalNavigationActive = false;
            }
            else if (!_wasControllerVerticalNavigationActive)
            {
                _wasControllerVerticalNavigationActive = true;
                LogGamepadShortcutState(
                    "Controller navigation direction detected from vertical axis. Horizontal=" +
                    horizontal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    ", Vertical=" +
                    vertical.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    ".");
                return vertical > 0f ? ControllerNavDirection.Up : ControllerNavDirection.Down;
            }

            return null;
        }

        private static void GetControllerNavigationAxes(BraveInput braveInput, out float horizontal, out float vertical)
        {
            horizontal = 0f;
            vertical = 0f;

            if ((object)braveInput == null || braveInput.ActiveActions == null)
            {
                return;
            }

            InControl.InputDevice activeDevice = braveInput.ActiveActions.Device;
            if (activeDevice == null || activeDevice.DeviceClass != InControl.InputDeviceClass.Controller)
            {
                return;
            }

            float dpadX = activeDevice.DPadX != null ? activeDevice.DPadX.Value : 0f;
            float dpadY = activeDevice.DPadY != null ? activeDevice.DPadY.Value : 0f;
            horizontal = dpadX;
            vertical = dpadY;
        }

        private ControllerNavDirection? GetKeyboardNavigationDirection()
        {
            ControllerNavDirection? heldDirection = GetHeldKeyboardNavigationDirection();
            if (!heldDirection.HasValue)
            {
                _heldKeyboardNavigationDirection = null;
                _nextKeyboardNavigationRepeatAt = 0f;
                return null;
            }

            if (Input.GetKeyDown(GetKeyboardNavigationKeyCode(heldDirection.Value)))
            {
                _heldKeyboardNavigationDirection = heldDirection.Value;
                _nextKeyboardNavigationRepeatAt = Time.unscaledTime + KeyboardNavigationRepeatDelaySeconds;
                return heldDirection.Value;
            }

            if (_heldKeyboardNavigationDirection.HasValue && _heldKeyboardNavigationDirection.Value != heldDirection.Value)
            {
                _heldKeyboardNavigationDirection = heldDirection.Value;
                _nextKeyboardNavigationRepeatAt = Time.unscaledTime + KeyboardNavigationRepeatDelaySeconds;
                return heldDirection.Value;
            }

            if (!_heldKeyboardNavigationDirection.HasValue)
            {
                _heldKeyboardNavigationDirection = heldDirection.Value;
                _nextKeyboardNavigationRepeatAt = Time.unscaledTime + KeyboardNavigationRepeatDelaySeconds;
                return heldDirection.Value;
            }

            if (Time.unscaledTime >= _nextKeyboardNavigationRepeatAt)
            {
                _nextKeyboardNavigationRepeatAt = Time.unscaledTime + KeyboardNavigationRepeatIntervalSeconds;
                return heldDirection.Value;
            }

            return null;
        }

        private static ControllerNavDirection? GetHeldKeyboardNavigationDirection()
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                return ControllerNavDirection.Left;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                return ControllerNavDirection.Right;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                return ControllerNavDirection.Up;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                return ControllerNavDirection.Down;
            }

            return null;
        }

        private static KeyCode GetKeyboardNavigationKeyCode(ControllerNavDirection direction)
        {
            switch (direction)
            {
                case ControllerNavDirection.Left:
                    return KeyCode.LeftArrow;
                case ControllerNavDirection.Right:
                    return KeyCode.RightArrow;
                case ControllerNavDirection.Up:
                    return KeyCode.UpArrow;
                case ControllerNavDirection.Down:
                default:
                    return KeyCode.DownArrow;
            }
        }

        private void ResetControllerNavigationAxes()
        {
            if (_wasControllerHorizontalNavigationActive || _wasControllerVerticalNavigationActive)
            {
                LogGamepadShortcutState(
                    "Reset controller navigation axis latch state. HorizontalActive=" +
                    _wasControllerHorizontalNavigationActive +
                    ", VerticalActive=" +
                    _wasControllerVerticalNavigationActive +
                    ".");
            }

            _wasControllerHorizontalNavigationActive = false;
            _wasControllerVerticalNavigationActive = false;
            _heldKeyboardNavigationDirection = null;
            _nextKeyboardNavigationRepeatAt = 0f;
        }

        private static string MoveControllerFocus(ControllerFocusEntry[] entries, string currentControlId, ControllerNavDirection direction)
        {
            if (entries == null || entries.Length == 0)
            {
                return string.Empty;
            }

            int currentIndex = FindControllerFocusEntryIndex(entries, currentControlId);
            if (currentIndex < 0)
            {
                return entries[0].ControlId;
            }

            ControllerFocusEntry currentEntry = entries[currentIndex];
            int bestIndex = currentIndex;
            int bestScore = int.MaxValue;
            for (int i = 0; i < entries.Length; i++)
            {
                if (i == currentIndex)
                {
                    continue;
                }

                ControllerFocusEntry candidate = entries[i];
                int rowDelta = candidate.Row - currentEntry.Row;
                int columnDelta = candidate.Column - currentEntry.Column;
                int score = GetControllerFocusScore(direction, rowDelta, columnDelta);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestIndex = i;
            }

            return entries[bestIndex].ControlId;
        }

        private static int FindControllerFocusEntryIndex(ControllerFocusEntry[] entries, string controlId)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].ControlId, controlId, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetControllerFocusScore(ControllerNavDirection direction, int rowDelta, int columnDelta)
        {
            switch (direction)
            {
                case ControllerNavDirection.Left:
                    if (columnDelta >= 0)
                    {
                        return int.MaxValue;
                    }

                    return (Mathf.Abs(columnDelta) * 10) + Mathf.Abs(rowDelta);
                case ControllerNavDirection.Right:
                    if (columnDelta <= 0)
                    {
                        return int.MaxValue;
                    }

                    return (columnDelta * 10) + Mathf.Abs(rowDelta);
                case ControllerNavDirection.Up:
                    if (rowDelta >= 0)
                    {
                        return int.MaxValue;
                    }

                    return (Mathf.Abs(rowDelta) * 10) + Mathf.Abs(columnDelta);
                case ControllerNavDirection.Down:
                    if (rowDelta <= 0)
                    {
                        return int.MaxValue;
                    }

                    return (rowDelta * 10) + Mathf.Abs(columnDelta);
                default:
                    return int.MaxValue;
            }
        }

        private bool IsPanelConfirmPressed()
        {
            bool isControllerConfirmPressed = Input.GetKeyDown(GetJoystickButtonKeyCode(0));
            bool isKeyboardConfirmPressed = Input.GetKeyDown(KeyCode.Insert);
            if (isKeyboardConfirmPressed)
            {
                LogGamepadShortcutState("Detected keyboard confirm press. Key=Insert.");
            }

            return isControllerConfirmPressed || isKeyboardConfirmPressed;
        }

        private bool IsPanelBackPressed()
        {
            bool isControllerBackPressed = Input.GetKeyDown(GetJoystickButtonKeyCode(1));
            bool isKeyboardBackPressed = Input.GetKeyDown(KeyCode.Delete);
            if (isKeyboardBackPressed)
            {
                LogGamepadShortcutState("Detected keyboard back press. Key=Delete.");
            }

            return isControllerBackPressed || isKeyboardBackPressed;
        }

        private bool IsGamepadToggleShortcutPressed()
        {
            if (!IsControllerShortcutEnabled())
            {
                _controllerShortcutR3PressedAt = -1f;
                _controllerShortcutHoldTriggered = false;
                return false;
            }

            const int rightStickButtonIndex = 9;
            KeyCode rightStickButtonKeyCode = GetJoystickButtonKeyCode(rightStickButtonIndex);
            string shortcut = GetConfiguredControllerShortcut();
            bool isRightStickPressed = Input.GetKey(rightStickButtonKeyCode);
            bool isRightStickDown = Input.GetKeyDown(rightStickButtonKeyCode);

            if (!isRightStickPressed)
            {
                _controllerShortcutR3PressedAt = -1f;
                _controllerShortcutHoldTriggered = false;
            }

            if (shortcut == "R3")
            {
                if (isRightStickDown)
                {
                    _controllerShortcutR3PressedAt = Time.unscaledTime;
                    _controllerShortcutHoldTriggered = false;

                    if (_isVisible)
                    {
                        _controllerShortcutHoldTriggered = true;
                        LogGamepadShortcutState("Detected command panel R3 press while open. Closing command panel.");
                        return true;
                    }
                }

                if (!isRightStickPressed || _controllerShortcutHoldTriggered || _controllerShortcutR3PressedAt < 0f)
                {
                    return false;
                }

                if (Time.unscaledTime - _controllerShortcutR3PressedAt < 0.5f)
                {
                    return false;
                }

                _controllerShortcutHoldTriggered = true;
                LogGamepadShortcutState("Detected command panel R3 hold for 0.5 seconds. Opening command panel.");
                return true;
            }

            int triggerButtonIndex = shortcut == "LB+X" ? 2 : (shortcut == "LB+Y" ? 3 : 9);
            bool isTriggerDown = Input.GetKeyDown(GetJoystickButtonKeyCode(triggerButtonIndex));
            bool modifierPressed = Input.GetKey(GetJoystickButtonKeyCode(4));

            if (!isTriggerDown || !modifierPressed)
            {
                if (isTriggerDown)
                {
                    LogGamepadShortcutState(
                        "Ignored command panel controller shortcut press. Shortcut=" +
                        shortcut +
                        ", ModifierPressed=" +
                        modifierPressed +
                        ", Visible=" +
                        _isVisible +
                        ", TriggerDown=" +
                        isTriggerDown +
                        ".");
                }

                return false;
            }

            LogGamepadShortcutState("Detected command panel " + shortcut + " press. Opening command panel.");
            return true;
        }

        private bool IsControllerShortcutEnabled()
        {
            return _controllerShortcutEnabledProvider == null || _controllerShortcutEnabledProvider();
        }

        private void LogJoystickButtonStateChanges()
        {
            for (int i = 0; i < _wasJoystickButtonPressed.Length; i++)
            {
                KeyCode buttonKeyCode = GetJoystickButtonKeyCode(i);
                bool isPressed = Input.GetKey(buttonKeyCode);
                if (isPressed == _wasJoystickButtonPressed[i])
                {
                    continue;
                }

                _wasJoystickButtonPressed[i] = isPressed;
                LogGamepadShortcutState(
                    "Observed joystick button state change. Button=" +
                    i +
                    ", Pressed=" +
                    isPressed +
                    ", Down=" +
                    Input.GetKeyDown(buttonKeyCode) +
                    ", Up=" +
                    Input.GetKeyUp(buttonKeyCode) +
                    ".");
            }
        }

        private void LogControllerStickStateChanges()
        {
            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            InControl.InputDevice activeDevice =
                (object)braveInput != null && braveInput.ActiveActions != null
                    ? braveInput.ActiveActions.Device
                    : null;
            if (activeDevice == null || activeDevice.DeviceClass != InControl.InputDeviceClass.Controller)
            {
                return;
            }

            float dpadX = activeDevice.DPadX != null ? activeDevice.DPadX.Value : 0f;
            float dpadY = activeDevice.DPadY != null ? activeDevice.DPadY.Value : 0f;
            float leftStickX = activeDevice.LeftStickX != null ? activeDevice.LeftStickX.Value : 0f;
            float leftStickY = activeDevice.LeftStickY != null ? activeDevice.LeftStickY.Value : 0f;
            float rightStickX = activeDevice.RightStickX != null ? activeDevice.RightStickX.Value : 0f;
            float rightStickY = activeDevice.RightStickY != null ? activeDevice.RightStickY.Value : 0f;

            LogNamedControllerAxisStateChange(
                "DPad",
                dpadX,
                dpadY,
                ref _lastLoggedControllerDpadHorizontalAxis,
                ref _lastLoggedControllerDpadVerticalAxis);
            LogNamedControllerAxisStateChange(
                "LeftStick",
                leftStickX,
                leftStickY,
                ref _lastLoggedControllerLeftStickHorizontalAxis,
                ref _lastLoggedControllerLeftStickVerticalAxis);
            LogNamedControllerAxisStateChange(
                "RightStick",
                rightStickX,
                rightStickY,
                ref _lastLoggedControllerRightStickHorizontalAxis,
                ref _lastLoggedControllerRightStickVerticalAxis);
        }

        private void LogDisabledKeyboardNavigationKeyAttempts()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.W, "Up");
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.A, "Left");
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.S, "Down");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                LogDisabledKeyboardNavigationKeyAttempt(KeyCode.D, "Right");
            }
        }

        private void LogGameplayKeyboardInputState()
        {
            if (!IsCommandPanelGameplayInputVerboseLoggingEnabled())
            {
                _hasLoggedGameplayInputState = false;
                return;
            }

            bool isWPressed = Input.GetKey(KeyCode.W);
            bool isAPressed = Input.GetKey(KeyCode.A);
            bool isSPressed = Input.GetKey(KeyCode.S);
            bool isDPressed = Input.GetKey(KeyCode.D);
            PlayerController player = GetCurrentPlayer();
            bool isInputOverridden = (object)player != null && player.IsInputOverridden;
            string inputState = (object)player != null ? player.CurrentInputState.ToString() : "<none>";
            bool stateChanged = !_hasLoggedGameplayInputState ||
                _lastLoggedGameplayPanelVisible != _isVisible ||
                _lastLoggedGameplayW != isWPressed ||
                _lastLoggedGameplayA != isAPressed ||
                _lastLoggedGameplayS != isSPressed ||
                _lastLoggedGameplayD != isDPressed ||
                _lastLoggedGameplayInputOverridden != isInputOverridden ||
                !string.Equals(_lastLoggedGameplayInputState, inputState, System.StringComparison.Ordinal);
            if (!stateChanged)
            {
                return;
            }

            LogGamepadShortcutState(
                "Observed gameplay keyboard input state. PanelVisible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", W=" +
                isWPressed +
                ", A=" +
                isAPressed +
                ", S=" +
                isSPressed +
                ", D=" +
                isDPressed +
                ", PlayerId=" +
                ((object)player != null ? player.GetInstanceID().ToString() : "<none>") +
                ", IsInputOverridden=" +
                isInputOverridden +
                ", CurrentInputState=" +
                inputState +
                ", CurrentFocus=" +
                GUIUtility.keyboardControl +
                ".");

            _hasLoggedGameplayInputState = true;
            _lastLoggedGameplayPanelVisible = _isVisible;
            _lastLoggedGameplayW = isWPressed;
            _lastLoggedGameplayA = isAPressed;
            _lastLoggedGameplayS = isSPressed;
            _lastLoggedGameplayD = isDPressed;
            _lastLoggedGameplayInputOverridden = isInputOverridden;
            _lastLoggedGameplayInputState = inputState;
        }

        private void LogControllerGameplayInputState()
        {
            if (!IsCommandPanelControllerGameplayInputVerboseLoggingEnabled())
            {
                _hasLoggedControllerGameplayInputState = false;
                return;
            }

            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            InControl.InputDevice activeDevice =
                (object)braveInput != null && braveInput.ActiveActions != null
                    ? braveInput.ActiveActions.Device
                    : null;
            string device = activeDevice == null
                ? "<none>"
                : activeDevice.DeviceClass + "/" + activeDevice.GetType().Name;
            float dpadX = activeDevice != null && activeDevice.DPadX != null ? activeDevice.DPadX.Value : 0f;
            float dpadY = activeDevice != null && activeDevice.DPadY != null ? activeDevice.DPadY.Value : 0f;
            float leftStickX = activeDevice != null && activeDevice.LeftStickX != null ? activeDevice.LeftStickX.Value : 0f;
            float leftStickY = activeDevice != null && activeDevice.LeftStickY != null ? activeDevice.LeftStickY.Value : 0f;
            float rightStickX = activeDevice != null && activeDevice.RightStickX != null ? activeDevice.RightStickX.Value : 0f;
            float rightStickY = activeDevice != null && activeDevice.RightStickY != null ? activeDevice.RightStickY.Value : 0f;
            PlayerController player = GetCurrentPlayer();
            bool isInputOverridden = (object)player != null && player.IsInputOverridden;
            string inputState = (object)player != null ? player.CurrentInputState.ToString() : "<none>";
            bool stateChanged = !_hasLoggedControllerGameplayInputState ||
                _lastLoggedControllerGameplayPanelVisible != _isVisible ||
                !string.Equals(_lastLoggedControllerGameplayDevice, device, System.StringComparison.Ordinal) ||
                _lastLoggedControllerGameplayInputOverridden != isInputOverridden ||
                !string.Equals(_lastLoggedControllerGameplayInputState, inputState, System.StringComparison.Ordinal) ||
                Mathf.Abs(_lastLoggedControllerGameplayDpadHorizontal - dpadX) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayDpadVertical - dpadY) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayLeftStickHorizontal - leftStickX) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayLeftStickVertical - leftStickY) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayRightStickHorizontal - rightStickX) > 0.01f ||
                Mathf.Abs(_lastLoggedControllerGameplayRightStickVertical - rightStickY) > 0.01f;
            if (!stateChanged)
            {
                return;
            }

            LogGamepadShortcutState(
                "Observed gameplay controller input state. PanelVisible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", Device=" +
                device +
                ", DPad=" +
                dpadX.ToString("F2") +
                "," +
                dpadY.ToString("F2") +
                ", LeftStick=" +
                leftStickX.ToString("F2") +
                "," +
                leftStickY.ToString("F2") +
                ", RightStick=" +
                rightStickX.ToString("F2") +
                "," +
                rightStickY.ToString("F2") +
                ", PlayerId=" +
                ((object)player != null ? player.GetInstanceID().ToString() : "<none>") +
                ", IsInputOverridden=" +
                isInputOverridden +
                ", CurrentInputState=" +
                inputState +
                ", CurrentFocus=" +
                GUIUtility.keyboardControl +
                ".");

            _hasLoggedControllerGameplayInputState = true;
            _lastLoggedControllerGameplayPanelVisible = _isVisible;
            _lastLoggedControllerGameplayDevice = device;
            _lastLoggedControllerGameplayInputOverridden = isInputOverridden;
            _lastLoggedControllerGameplayInputState = inputState;
            _lastLoggedControllerGameplayDpadHorizontal = dpadX;
            _lastLoggedControllerGameplayDpadVertical = dpadY;
            _lastLoggedControllerGameplayLeftStickHorizontal = leftStickX;
            _lastLoggedControllerGameplayLeftStickVertical = leftStickY;
            _lastLoggedControllerGameplayRightStickHorizontal = rightStickX;
            _lastLoggedControllerGameplayRightStickVertical = rightStickY;
        }

        private void LogDisabledKeyboardNavigationKeyAttempt(KeyCode keyCode, string mappedDirection)
        {
            LogGamepadShortcutState(
                "Observed disabled keyboard navigation key press. Key=" +
                keyCode +
                ", MappedDirection=" +
                mappedDirection +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ".");
        }

        private void LogMouseButtonAttempts()
        {
            if (!IsCommandPanelCursorVerboseLoggingEnabled())
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                LogMouseButtonAttempt(0, "Left");
            }

            if (Input.GetMouseButtonDown(1))
            {
                LogMouseButtonAttempt(1, "Right");
            }
        }

        private void LogMouseButtonAttempt(int buttonIndex, string buttonName)
        {
            Vector3 mousePosition = Input.mousePosition;
            LogGamepadShortcutState(
                "Observed mouse button press. Button=" +
                buttonName +
                ", ButtonIndex=" +
                buttonIndex +
                ", MouseX=" +
                mousePosition.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", MouseY=" +
                mousePosition.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ".");
        }

        private static KeyCode GetJoystickButtonKeyCode(int buttonIndex)
        {
            return KeyCode.JoystickButton0 + buttonIndex;
        }

        private static string DescribePlayerVitals(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<player:null>";
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return "<health:null>";
            }

            return
                "CurrentHealth=" +
                healthHaver.GetCurrentHealth().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                ", MaxHealth=" +
                healthHaver.GetMaxHealth().ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                ", Armor=" +
                healthHaver.Armor.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                ", Blanks=" +
                player.Blanks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void LogHealthDiagnosticStateChanges()
        {
            if (!IsCommandPanelHealthVerboseLoggingEnabled())
            {
                ResetHealthDiagnosticState();
                return;
            }

            PlayerController player = GetCurrentPlayer();
            if ((object)player == null || (object)player.healthHaver == null)
            {
                ResetHealthDiagnosticState();
                return;
            }

            HealthHaver healthHaver = player.healthHaver;
            Gun currentGun = player.CurrentGun;
            int gunId = (object)currentGun != null ? currentGun.GetInstanceID() : 0;
            string gunName = (object)currentGun != null ? currentGun.name : "<none>";
            float currentHealth = healthHaver.GetCurrentHealth();
            float maxHealth = healthHaver.GetMaxHealth();
            float armor = healthHaver.Armor;
            bool playerChanged = !ReferenceEquals(_lastHealthDiagnosticPlayer, player);
            bool gunChanged = playerChanged || gunId != _lastHealthDiagnosticGunId;
            bool vitalsChanged = playerChanged ||
                float.IsNaN(_lastHealthDiagnosticCurrentHealth) ||
                Mathf.Abs(currentHealth - _lastHealthDiagnosticCurrentHealth) > 0.001f ||
                Mathf.Abs(maxHealth - _lastHealthDiagnosticMaxHealth) > 0.001f ||
                Mathf.Abs(armor - _lastHealthDiagnosticArmor) > 0.001f;

            if (gunChanged || vitalsChanged)
            {
                LogCommandPanelHealthDiagnostic(
                    "Observed player health state change. PreviousCurrentHealth=" +
                    FormatDiagnosticFloat(_lastHealthDiagnosticCurrentHealth) +
                    ", PreviousMaxHealth=" +
                    FormatDiagnosticFloat(_lastHealthDiagnosticMaxHealth) +
                    ", PreviousArmor=" +
                    FormatDiagnosticFloat(_lastHealthDiagnosticArmor) +
                    ", CurrentCurrentHealth=" +
                    FormatDiagnosticFloat(currentHealth) +
                    ", CurrentMaxHealth=" +
                    FormatDiagnosticFloat(maxHealth) +
                    ", CurrentArmor=" +
                    FormatDiagnosticFloat(armor) +
                    ", PreviousGunId=" +
                    _lastHealthDiagnosticGunId +
                    ", PreviousGunName=" +
                    (_lastHealthDiagnosticGunName ?? "<none>") +
                    ", CurrentGunId=" +
                    gunId +
                    ", CurrentGunName=" +
                    gunName +
                    ", GunChanged=" +
                    gunChanged +
                    ", VitalsChanged=" +
                    vitalsChanged +
                    ", Visible=" +
                    _isVisible +
                    ", Page=" +
                    _currentPage +
                    ", PlayerFocus=" +
                    _commandPageFocusedControlId +
                    ", SettingsFocus=" +
                    _settingsPageFocusedControlId +
                    ".");
            }

            _lastHealthDiagnosticPlayer = player;
            _lastHealthDiagnosticCurrentHealth = currentHealth;
            _lastHealthDiagnosticMaxHealth = maxHealth;
            _lastHealthDiagnosticArmor = armor;
            _lastHealthDiagnosticGunId = gunId;
            _lastHealthDiagnosticGunName = gunName;
        }

        private void ResetHealthDiagnosticState()
        {
            _lastHealthDiagnosticPlayer = null;
            _lastHealthDiagnosticCurrentHealth = float.NaN;
            _lastHealthDiagnosticMaxHealth = float.NaN;
            _lastHealthDiagnosticArmor = float.NaN;
            _lastHealthDiagnosticGunId = -1;
            _lastHealthDiagnosticGunName = string.Empty;
        }

        private static string FormatDiagnosticFloat(float value)
        {
            return float.IsNaN(value)
                ? "<unset>"
                : value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void LogNamedControllerAxisStateChange(
            string inputName,
            float horizontal,
            float vertical,
            ref float lastHorizontal,
            ref float lastVertical)
        {
            bool didHorizontalChange =
                float.IsNaN(lastHorizontal) ||
                Mathf.Abs(horizontal - lastHorizontal) > 0.01f;
            bool didVerticalChange =
                float.IsNaN(lastVertical) ||
                Mathf.Abs(vertical - lastVertical) > 0.01f;
            if (!didHorizontalChange && !didVerticalChange)
            {
                return;
            }

            lastHorizontal = horizontal;
            lastVertical = vertical;
            LogGamepadShortcutState(
                "Observed controller input axis change. Input=" +
                inputName +
                ", Horizontal=" +
                horizontal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Vertical=" +
                vertical.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ".");
        }

        private void LogControllerNavigationAxisState(float horizontal, float vertical)
        {
            bool didHorizontalChange =
                float.IsNaN(_lastLoggedControllerHorizontalAxis) ||
                Mathf.Abs(horizontal - _lastLoggedControllerHorizontalAxis) > 0.01f;
            bool didVerticalChange =
                float.IsNaN(_lastLoggedControllerVerticalAxis) ||
                Mathf.Abs(vertical - _lastLoggedControllerVerticalAxis) > 0.01f;
            if (!didHorizontalChange && !didVerticalChange)
            {
                return;
            }

            _lastLoggedControllerHorizontalAxis = horizontal;
            _lastLoggedControllerVerticalAxis = vertical;
            LogGamepadShortcutState(
                "Observed controller navigation axis change. Horizontal=" +
                horizontal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Vertical=" +
                vertical.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", HorizontalLatched=" +
                _wasControllerHorizontalNavigationActive +
                ", VerticalLatched=" +
                _wasControllerVerticalNavigationActive +
                ".");
        }

        private void LogGamepadShortcutState(string message)
        {
            if (_inputLogHandler != null)
            {
                _inputLogHandler(message);
            }
        }

        private void Close()
        {
            _isVisible = false;
            ResetClosedPanelState();
        }

        private void SyncPanelInputOverride()
        {
            PlayerController currentPlayer = GetCurrentPlayer();
            if (!_isVisible)
            {
                ClearPanelInputOverride();
                return;
            }

            if ((object)_panelInputOverridePlayer != null && !ReferenceEquals(_panelInputOverridePlayer, currentPlayer))
            {
                _panelInputOverridePlayer.ClearInputOverride(PanelInputOverrideReason);
                LogCommandPanelHealthDiagnostic(
                    "Cleared command panel input override from stale player instance. PreviousPlayerId=" +
                    _panelInputOverridePlayer.GetInstanceID() +
                    ".");
                _panelInputOverridePlayer = null;
            }

            // Panel navigation reads D-pad input directly. Do not put the player into NoInput here:
            // ETG's input override also blocks the controller left stick used for gameplay movement.
            ClearPanelInputOverride();
        }

        private void ClearPanelInputOverride()
        {
            if ((object)_panelInputOverridePlayer == null)
            {
                return;
            }

            _panelInputOverridePlayer.ClearInputOverride(PanelInputOverrideReason);
            LogCommandPanelHealthDiagnostic(
                "Cleared command panel input override. PlayerId=" +
                _panelInputOverridePlayer.GetInstanceID() +
                ", CurrentInputState=" +
                _panelInputOverridePlayer.CurrentInputState +
                ", IsInputOverridden=" +
                _panelInputOverridePlayer.IsInputOverridden +
                ".");
            _panelInputOverridePlayer = null;
        }

        private bool IsCommandPanelHealthVerboseLoggingEnabled()
        {
            return _commandPanelHealthVerboseLoggingEnabledProvider != null &&
                _commandPanelHealthVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelCursorVerboseLoggingEnabled()
        {
            return _commandPanelCursorVerboseLoggingEnabledProvider != null &&
                _commandPanelCursorVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelGameplayInputVerboseLoggingEnabled()
        {
            return _commandPanelGameplayInputVerboseLoggingEnabledProvider != null &&
                _commandPanelGameplayInputVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelControllerGameplayInputVerboseLoggingEnabled()
        {
            return _commandPanelControllerGameplayInputVerboseLoggingEnabledProvider != null &&
                _commandPanelControllerGameplayInputVerboseLoggingEnabledProvider();
        }

        private bool IsCommandPanelShortcutVerboseLoggingEnabled()
        {
            return _commandPanelShortcutVerboseLoggingEnabledProvider != null &&
                _commandPanelShortcutVerboseLoggingEnabledProvider();
        }

        private void LogCommandPanelShortcutDiagnostic(string message)
        {
            if (!IsCommandPanelShortcutVerboseLoggingEnabled())
            {
                return;
            }

            LogGamepadShortcutState("Command panel shortcut diagnostic. " + message);
        }

        private void LogCommandPanelShortcutState(bool keyboardTogglePressed, bool controllerTogglePressed)
        {
            if (!IsCommandPanelShortcutVerboseLoggingEnabled())
            {
                _hasLoggedCommandPanelShortcutState = false;
                return;
            }

            KeyCode toggleKey = GetToggleKey();
            bool keyboardToggleHeld = Input.GetKey(toggleKey);
            GameManager gameManager = GameManager.Instance;
            string gameType = (object)gameManager != null ? gameManager.CurrentGameType.ToString() : "<null>";
            string primaryPlayer = (object)gameManager != null ? DescribeCommandPanelShortcutPlayer(gameManager.PrimaryPlayer) : "<none>";
            string secondaryPlayer = (object)gameManager != null ? DescribeCommandPanelShortcutPlayer(gameManager.SecondaryPlayer) : "<none>";
            bool stateChanged = !_hasLoggedCommandPanelShortcutState ||
                _lastLoggedCommandPanelKeyboardHeld != keyboardToggleHeld ||
                _lastLoggedCommandPanelKeyboardDown != keyboardTogglePressed ||
                _lastLoggedCommandPanelControllerDetected != controllerTogglePressed ||
                _lastLoggedCommandPanelVisible != _isVisible;
            if (!stateChanged)
            {
                return;
            }

            LogGamepadShortcutState(
                "Command panel shortcut sample. " +
                "ToggleKey=" + toggleKey +
                ", KeyboardHeld=" + keyboardToggleHeld +
                ", KeyboardDown=" + keyboardTogglePressed +
                ", ControllerDetected=" + controllerTogglePressed +
                ", ControllerShortcutEnabled=" + IsControllerShortcutEnabled() +
                ", ConfiguredControllerShortcut=" + GetConfiguredControllerShortcut() +
                ", Visible=" + _isVisible +
                ", GameType=" + gameType +
                ", P1=" + primaryPlayer +
                ", P2=" + secondaryPlayer +
                ".");

            _hasLoggedCommandPanelShortcutState = true;
            _lastLoggedCommandPanelKeyboardHeld = keyboardToggleHeld;
            _lastLoggedCommandPanelKeyboardDown = keyboardTogglePressed;
            _lastLoggedCommandPanelControllerDetected = controllerTogglePressed;
            _lastLoggedCommandPanelVisible = _isVisible;
        }

        private static string DescribeCommandPanelShortcutPlayer(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<null>";
            }

            try
            {
                return "Id=" + player.GetInstanceID() +
                       ",Name=" + player.name +
                       ",Active=" + player.gameObject.activeInHierarchy +
                       ",InputOverridden=" + player.IsInputOverridden;
            }
            catch (System.Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

        private void LogCommandPanelHealthDiagnostic(string message)
        {
            if (!IsCommandPanelHealthVerboseLoggingEnabled())
            {
                return;
            }

            LogGamepadShortcutState(message);
        }

        private void HandleLanguageChanged()
        {
            ResetPickupBrowserState();
            ResetCharacterPageCache();
            RefreshLocalizedLoadoutEditorState();
        }

        private void RefreshLocalizedLoadoutEditorState()
        {
            RefreshLoadoutPresetEntries();
            RefreshLoadoutEditorEntries();
            RefreshLoadoutRandomPoolEntries();
            RefreshLoadoutPickupEntries();

            if (_loadoutEditorMode != LoadoutEditorMode.RandomPoolDetail)
            {
                _loadoutPresetRenameText = GetLoadoutEditorActivePresetDisplayName();
            }

            if (_loadoutEditorMode == LoadoutEditorMode.RandomPoolDetail)
            {
                _loadoutRandomPoolRenameText = GetLoadoutEditorActiveRandomPoolDisplayName();
            }
        }

        private void ResetClosedPanelState()
        {
            ClearPanelInputOverride();
            _focusInputField = false;
            _focusPickupSearchField = false;
            _currentPage = PanelPage.Command;
            _commandPageFocusedControlId = "cmd.settings";
            _settingsPageFocusedControlId = "settings.toggle_key";
            _pickupInfoConfigFocusedControlId = "pickup_info_config.quality";
            _characterPageFocusedControlId = "characters.mode";
            _loadoutEditorFocusedControlId = "loadout.back";
            _inputText = string.Empty;
            CloseTeleportPanel();
            ResetPickupBrowserState();
            ResetCharacterPageCache();
            ResetControllerNavigationAxes();
            RequestGuiFocusRelease();
        }

        private void RequestGuiFocusRelease()
        {
            LogGamepadShortcutState(
                "Queued GUI focus release. Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", KeyboardControl=" +
                GUIUtility.keyboardControl +
                ", HotControl=" +
                GUIUtility.hotControl +
                ".");
            _releaseGuiFocusPending = true;
        }

        private void UpdateMapFeatureActivationState()
        {
            if (string.IsNullOrEmpty(_revealMapActivatedSceneName) && string.IsNullOrEmpty(_mapDirectTeleportActivatedSceneName))
            {
                return;
            }

            string currentSceneName = GetCurrentMapFeatureActivationKey();
            bool isRevealMapStillActive =
                !string.IsNullOrEmpty(_revealMapActivatedSceneName) &&
                string.Equals(_revealMapActivatedSceneName, currentSceneName, System.StringComparison.Ordinal);
            bool isMapDirectTeleportStillActive =
                !string.IsNullOrEmpty(_mapDirectTeleportActivatedSceneName) &&
                string.Equals(_mapDirectTeleportActivatedSceneName, currentSceneName, System.StringComparison.Ordinal);
            if (isRevealMapStillActive || isMapDirectTeleportStillActive)
            {
                return;
            }

            if (ShouldLogMapTeleportVerbose())
            {
                LogGamepadShortcutState(
                    "Map feature activation reset. " +
                    "PreviousRevealMapScene=" +
                    _revealMapActivatedSceneName +
                    ", PreviousMapDirectTeleportScene=" +
                    _mapDirectTeleportActivatedSceneName +
                    ", CurrentScene=" +
                    currentSceneName +
                    ".");
            }
            _revealMapActivatedSceneName = string.Empty;
            _mapDirectTeleportActivatedSceneName = string.Empty;
            _lastMapDirectTeleportRoomKey = string.Empty;
        }

        private bool IsRevealMapActive()
        {
            return
                !string.IsNullOrEmpty(_revealMapActivatedSceneName) &&
                string.Equals(_revealMapActivatedSceneName, GetCurrentMapFeatureActivationKey(), System.StringComparison.Ordinal);
        }

        private void MarkRevealMapActivatedForCurrentScene()
        {
            _revealMapActivatedSceneName = GetCurrentMapFeatureActivationKey();
        }

        private bool IsMapDirectTeleportActive()
        {
            return
                !string.IsNullOrEmpty(_mapDirectTeleportActivatedSceneName) &&
                string.Equals(_mapDirectTeleportActivatedSceneName, GetCurrentMapFeatureActivationKey(), System.StringComparison.Ordinal);
        }

        private void MarkMapDirectTeleportActivatedForCurrentScene()
        {
            _mapDirectTeleportActivatedSceneName = GetCurrentMapFeatureActivationKey();
            _nextMapDirectTeleportDebugLogAt = 0f;
            _lastMapDirectTeleportRoomKey = string.Empty;
        }

        private static string GetCurrentMapFeatureActivationKey()
        {
            GameManager gameManager = GameManager.Instance;
            string dungeonSceneName = GetLastLoadedDungeonSceneName(gameManager);
            if (!string.IsNullOrEmpty(dungeonSceneName) &&
                !string.Equals(dungeonSceneName, "<unknown>", System.StringComparison.Ordinal) &&
                !string.Equals(dungeonSceneName, "<no_game_manager>", System.StringComparison.Ordinal) &&
                !dungeonSceneName.StartsWith("<exception:", System.StringComparison.Ordinal))
            {
                return dungeonSceneName;
            }

            return GetLoadedUnitySceneName();
        }

        private void LogMapDirectTeleportRoomTransitionIfNeeded()
        {
            if (!IsMapDirectTeleportActive() || !ShouldLogMapTeleportVerbose())
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            RoomHandler currentRoom = player != null ? player.CurrentRoom : null;
            string currentRoomKey = GetMapDirectTeleportRoomKey(currentRoom);
            if (string.Equals(currentRoomKey, _lastMapDirectTeleportRoomKey, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastMapDirectTeleportRoomKey = currentRoomKey;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            int minimapTeleportEntryCount = minimap != null && minimap.RoomToTeleportMap != null ? minimap.RoomToTeleportMap.Count : -1;
            LogGamepadShortcutState(
                "Map direct teleport room transition. " +
                "UnityScene=" +
                GetLoadedUnitySceneName() +
                ", LastLoadedDungeonScene=" +
                GetLastLoadedDungeonSceneName(gameManager) +
                ", CurrentRoom=" +
                DescribeMapDirectTeleportRoom(currentRoom) +
                ", CurrentRoomCanTeleportFrom=" +
                (currentRoom != null ? currentRoom.CanTeleportFromRoom().ToString() : "<unknown>") +
                ", CurrentRoomCanTeleportTo=" +
                (currentRoom != null ? currentRoom.CanTeleportToRoom().ToString() : "<unknown>") +
                ", CurrentRoomTeleportersActive=" +
                (currentRoom != null ? currentRoom.TeleportersActive.ToString() : "<unknown>") +
                ", CurrentRoomRevealedOnMap=" +
                (currentRoom != null ? currentRoom.RevealedOnMap.ToString() : "<unknown>") +
                ", CurrentRoomMinimapTeleportRegistered=" +
                IsMapDirectTeleportRoomRegistered(minimap, currentRoom) +
                ", MinimapTeleportEntries=" +
                minimapTeleportEntryCount +
                ", ConnectedRooms=[" +
                DescribeConnectedMapDirectTeleportRooms(currentRoom, minimap) +
                "].");
        }

        private void LogMapDirectTeleportRuntimeStateIfNeeded()
        {
            if (!IsMapDirectTeleportActive() || !ShouldLogMapTeleportVerbose() || Time.unscaledTime < _nextMapDirectTeleportDebugLogAt)
            {
                return;
            }

            _nextMapDirectTeleportDebugLogAt = Time.unscaledTime + 1f;
            GameManager gameManager = GameManager.Instance;
            PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
            RoomHandler currentRoom = player != null ? player.CurrentRoom : null;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            string currentRoomLabel = currentRoom != null ? DescribeMapDirectTeleportRoom(currentRoom) : "<none>";
            string currentRoomCanTeleportFrom = currentRoom != null ? currentRoom.CanTeleportFromRoom().ToString() : "<unknown>";
            string currentRoomCanTeleportTo = currentRoom != null ? currentRoom.CanTeleportToRoom().ToString() : "<unknown>";
            string currentRoomTeleportersActive = currentRoom != null ? currentRoom.TeleportersActive.ToString() : "<unknown>";
            int minimapTeleportEntryCount = minimap != null && minimap.RoomToTeleportMap != null ? minimap.RoomToTeleportMap.Count : -1;
            string lastLoadedDungeonScene = GetLastLoadedDungeonSceneName(gameManager);
            LogGamepadShortcutState(
                "Map direct teleport runtime sample. " +
                "UnityScene=" +
                GetLoadedUnitySceneName() +
                ", LastLoadedDungeonScene=" +
                lastLoadedDungeonScene +
                ", ActiveSceneBinding=" +
                _mapDirectTeleportActivatedSceneName +
                ", MinimapHasInstance=" +
                Minimap.HasInstance +
                ", MinimapTeleportEntries=" +
                minimapTeleportEntryCount +
                ", CurrentRoom=" +
                currentRoomLabel +
                ", CurrentRoomCanTeleportFrom=" +
                currentRoomCanTeleportFrom +
                ", CurrentRoomCanTeleportTo=" +
                currentRoomCanTeleportTo +
                ", CurrentRoomTeleportersActive=" +
                currentRoomTeleportersActive +
                ", PlayerReady=" +
                ((object)player != null) +
                ", DungeonReady=" +
                ((object)gameManager != null && (object)gameManager.Dungeon != null && gameManager.Dungeon.data != null) +
                ".");
        }

        private static string DescribeMapDirectTeleportRoom(RoomHandler room)
        {
            if ((object)room == null)
            {
                return "<null>";
            }

            string roomName = room.GetRoomName();
            IntVector2 basePosition = room.area != null ? room.area.basePosition : IntVector2.Zero;
            string category = room.area != null ? room.area.PrototypeRoomCategory.ToString() : "<unknown>";
            return
                (string.IsNullOrEmpty(roomName) ? "<unnamed>" : roomName) +
                "@" +
                basePosition.x +
                "," +
                basePosition.y +
                "#" +
                category;
        }

        private static bool IsMapDirectTeleportRoomRegistered(Minimap minimap, RoomHandler room)
        {
            return minimap != null &&
                minimap.RoomToTeleportMap != null &&
                room != null &&
                minimap.RoomToTeleportMap.ContainsKey(room);
        }

        private static string GetMapDirectTeleportRoomKey(RoomHandler room)
        {
            return room != null ? DescribeMapDirectTeleportRoom(room) : "<none>";
        }

        private static string DescribeConnectedMapDirectTeleportRooms(RoomHandler room, Minimap minimap)
        {
            if (room == null || room.connectedRooms == null || room.connectedRooms.Count == 0)
            {
                return string.Empty;
            }

            System.Collections.Generic.List<string> roomLabels = new System.Collections.Generic.List<string>();
            for (int index = 0; index < room.connectedRooms.Count; index++)
            {
                RoomHandler connectedRoom = room.connectedRooms[index];
                roomLabels.Add(
                    DescribeMapDirectTeleportRoom(connectedRoom) +
                    "{CanTo=" +
                    (connectedRoom != null ? connectedRoom.CanTeleportToRoom().ToString() : "<unknown>") +
                    ", TeleActive=" +
                    (connectedRoom != null ? connectedRoom.TeleportersActive.ToString() : "<unknown>") +
                    ", Revealed=" +
                    (connectedRoom != null ? connectedRoom.RevealedOnMap.ToString() : "<unknown>") +
                    ", Registered=" +
                    IsMapDirectTeleportRoomRegistered(minimap, connectedRoom) +
                    "}");
            }

            return string.Join("; ", roomLabels.ToArray());
        }

        private static string GetLastLoadedDungeonSceneName(GameManager gameManager)
        {
            if ((object)gameManager == null)
            {
                return "<no_game_manager>";
            }

            try
            {
                GameLevelDefinition levelDefinition = gameManager.GetLastLoadedLevelDefinition();
                if (levelDefinition == null || string.IsNullOrEmpty(levelDefinition.dungeonSceneName))
                {
                    return "<unknown>";
                }

                return levelDefinition.dungeonSceneName;
            }
            catch (System.Exception exception)
            {
                return "<exception:" + exception.GetType().Name + ">";
            }
        }

        private bool ShouldLogMapTeleportVerbose()
        {
            return _mapTeleportVerboseLoggingEnabledProvider != null && _mapTeleportVerboseLoggingEnabledProvider();
        }

        private bool ShouldLogFloorTeleportVerbose()
        {
            return _floorTeleportVerboseLoggingEnabledProvider != null && _floorTeleportVerboseLoggingEnabledProvider();
        }

        private void ReleaseGuiFocusIfPending()
        {
            if (!_releaseGuiFocusPending)
            {
                return;
            }

            _releaseGuiFocusPending = false;
            LogGamepadShortcutState(
                "Releasing GUI focus. Visible=" +
                _isVisible +
                ", Page=" +
                _currentPage +
                ", KeyboardControlBefore=" +
                GUIUtility.keyboardControl +
                ", HotControlBefore=" +
                GUIUtility.hotControl +
                ".");
            ReleaseGuiFocus();
        }

        private static void ReleaseGuiFocus()
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
        }

        private void LogCursorVisibilityStateChanges()
        {
            if (!IsCommandPanelCursorVerboseLoggingEnabled())
            {
                return;
            }

            bool cursorVisible = Cursor.visible;
            CursorLockMode cursorLockMode = Cursor.lockState;
            if (!_hasLoggedCursorVisibilityState ||
                _lastLoggedCursorVisible != cursorVisible ||
                _lastLoggedCursorLockMode != cursorLockMode)
            {
                _hasLoggedCursorVisibilityState = true;
                _lastLoggedCursorVisible = cursorVisible;
                _lastLoggedCursorLockMode = cursorLockMode;
                LogGamepadShortcutState(
                    "Observed cursor visibility state change. CursorVisible=" +
                    cursorVisible +
                    ", CursorLockMode=" +
                    cursorLockMode +
                    ", Visible=" +
                    _isVisible +
                    ", Page=" +
                    _currentPage +
                    ", KeyboardControl=" +
                    GUIUtility.keyboardControl +
                    ", HotControl=" +
                    GUIUtility.hotControl +
                    ".");
            }

            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            InControl.InputDevice activeDevice =
                (object)braveInput != null && braveInput.ActiveActions != null
                    ? braveInput.ActiveActions.Device
                    : null;
            string deviceName = activeDevice != null ? activeDevice.Name ?? "<unnamed>" : "<none>";
            string deviceClass = activeDevice != null ? activeDevice.DeviceClass.ToString() : "<none>";
            if (!string.Equals(_lastLoggedActiveInputDeviceName, deviceName, System.StringComparison.Ordinal) ||
                !string.Equals(_lastLoggedActiveInputDeviceClass, deviceClass, System.StringComparison.Ordinal))
            {
                _lastLoggedActiveInputDeviceName = deviceName;
                _lastLoggedActiveInputDeviceClass = deviceClass;
                LogGamepadShortcutState(
                    "Observed active input device change. DeviceName=" +
                    deviceName +
                    ", DeviceClass=" +
                    deviceClass +
                    ", Visible=" +
                    _isVisible +
                    ", Page=" +
                    _currentPage +
                    ".");
            }
        }

        private KeyCode GetToggleKey()
        {
            return _toggleKeyProvider != null ? _toggleKeyProvider() : KeyCode.F7;
        }

        private void TryHandleRoomEnemyRewindShortcut()
        {
            if (_isVisible || !Input.GetKeyDown(GetRoomEnemyRewindKey()))
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null || gameManager.IsFoyer)
            {
                return;
            }

            PlayerController player = GetCurrentPlayer();
            if ((object)player != null)
            {
                ExecuteSelectedRoomEnemyRefresh(player, null);
            }
        }

        private KeyCode GetRoomEnemyRewindKey()
        {
            return _roomEnemyRewindKeyProvider != null ? _roomEnemyRewindKeyProvider() : RoomEnemyRewindShortcutKey;
        }

        private float GetAutoUiScale()
        {
            float widthScale = Screen.width / ReferenceScreenWidth;
            float heightScale = Screen.height / ReferenceScreenHeight;
            float rawScale = Mathf.Min(widthScale, heightScale) * GetUiScaleMultiplier();
            return Mathf.Clamp(rawScale, MinimumUiScale, MaximumUiScale);
        }

        private float GetScaledScreenWidth()
        {
            return Screen.width / GetAutoUiScale();
        }

        private float GetScaledScreenHeight()
        {
            return Screen.height / GetAutoUiScale();
        }

        private Matrix4x4 GetAutoScaledGuiMatrix()
        {
            float scale = GetAutoUiScale();
            return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        private Rect GetMainPanelRect(float panelHeight)
        {
            float desiredPanelWidth = _currentPage == PanelPage.LoadoutEditor
                ? LoadoutEditorPanelWidth
                : _currentPage == PanelPage.Settings
                    ? SettingsPanelWidth
                    : PanelWidth;
            float panelWidth = Mathf.Min(desiredPanelWidth, Mathf.Max(1f, GetScaledScreenWidth() - 24f));
            return new Rect(
                (GetScaledScreenWidth() - panelWidth) * 0.5f,
                GetScaledScreenHeight() - PanelBottomMargin - panelHeight,
                panelWidth,
                panelHeight);
        }

        private static Rect ExpandPanelBorderRect(Rect rect)
        {
            float border = DashboardTheme.PanelBorderThickness;
            return new Rect(rect.x - border, rect.y - border, rect.width + (border * 2f), rect.height + (border * 2f));
        }

        private float GetUiScaleMultiplier()
        {
            return UiScalePresetCatalog.GetScaleMultiplier(GetConfiguredUiScalePreset());
        }
    }
}
