// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

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
            ActiveItemNoCooldownToggleService activeItemNoCooldownToggleService,
            AmmonomiconFastOpenToggleService ammonomiconFastOpenToggleService,
            LoadoutRuleEditorService loadoutRuleEditorService,
            LoadoutPresetRandomService loadoutPresetRandomService,
            System.Func<EtgPickupCatalogEntry[]> pickupCatalogProvider,
            System.Func<int, string> pickupGameplayNameProvider,
            System.Func<PickupAliasRegistry> aliasRegistryProvider,
            System.Func<PickupShortcutRegistry> pickupShortcutRegistryProvider,
            System.Action<string> pickupShortcutConfigSetter,
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
            System.Func<bool> commandPanelCloseButtonShownProvider,
            System.Action<bool> commandPanelCloseButtonShownSetter,
            System.Func<bool> revealMapEveryFloorProvider,
            System.Action<bool> revealMapEveryFloorSetter,
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
            _activeItemNoCooldownToggleService = activeItemNoCooldownToggleService;
            _ammonomiconFastOpenToggleService = ammonomiconFastOpenToggleService;
            _loadoutRuleEditorService = loadoutRuleEditorService;
            _loadoutEditorDataCoordinator = new LoadoutEditorDataCoordinator(_loadoutRuleEditorService);
            _loadoutPresetRandomService = loadoutPresetRandomService;
            _pickupCatalogProvider = pickupCatalogProvider;
            _pickupGameplayNameProvider = pickupGameplayNameProvider;
            _aliasRegistryProvider = aliasRegistryProvider;
            _pickupBrowserQueryService = new PickupBrowserQueryService(
                _pickupCatalogProvider,
                _aliasRegistryProvider,
                _pickupGameplayNameProvider);
            _pickupShortcutRegistry = pickupShortcutRegistryProvider != null
                ? pickupShortcutRegistryProvider()
                : PickupShortcutRegistry.Parse(string.Empty);
            _pickupShortcutConfigSetter = pickupShortcutConfigSetter;
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
            _commandPanelCloseButtonShownProvider = commandPanelCloseButtonShownProvider;
            _commandPanelCloseButtonShownSetter = commandPanelCloseButtonShownSetter;
            _revealMapEveryFloorProvider = revealMapEveryFloorProvider;
            _revealMapEveryFloorSetter = revealMapEveryFloorSetter;
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
            _mapFeatureRuntimeCoordinator = new MapFeatureRuntimeCoordinator(
                _roomDebugCommandService,
                _performanceLogger,
                _mapTeleportVerboseLoggingEnabledProvider,
                GetCurrentMapFeatureActivationKey,
                GetMapDirectTeleportRoomKey,
                LogMapRevealTransitionDiagnostics,
                LogGamepadShortcutState,
                ResetMapDirectTeleportDiagnostics);
            _commandPanelLifecycleCoordinator = new CommandPanelLifecycleCoordinator(
                () => _isVisible,
                GetCurrentPlayer,
                () => _currentPage.ToString(),
                LogCommandPanelHealthDiagnostic,
                LogGamepadShortcutState);
            string persistedRoomEnemyRefreshMethod = _roomEnemyRefreshMethodProvider != null ? _roomEnemyRefreshMethodProvider() : "rewind";
            _roomEnemyRefreshMethod = string.Equals(persistedRoomEnemyRefreshMethod, "respawn", System.StringComparison.OrdinalIgnoreCase)
                ? RoomEnemyRefreshMethod.RespawnEnemies
                : RoomEnemyRefreshMethod.Rewind;
            _showPlayerStatsPanel = _playerStatsPanelShownProvider != null && _playerStatsPanelShownProvider();
            _showCommandPanelCloseButton = _commandPanelCloseButtonShownProvider == null || _commandPanelCloseButtonShownProvider();
            _revealMapEveryFloor = _revealMapEveryFloorProvider != null && _revealMapEveryFloorProvider();
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
            _mapFeatureRuntimeCoordinator.Update();
            LogMapRevealTransitionDiagnostics("update_before_auto_reveal");
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
                if (!_isVisible)
                {
                    BeginCommandPanelPerformanceTrace(keyboardTogglePressed ? "Keyboard" : "Controller");
                }
                Toggle();
            }

            TryHandleRoomEnemyRewindShortcut();
            TryHandlePickupShortcut();

            if (!_isVisible)
            {
                return;
            }

            ProcessLoadoutPreviewIconWarmup();

            LogMouseButtonAttempts();
            LogDisabledKeyboardNavigationKeyAttempts();
        }

        public void OnGUI(PlayerController player, BepInEx.Logging.ManualLogSource logger)
        {
            LogCommandPanelPerformanceStage("OnGUI.begin");
            long stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            EnsureStyles();
            WarmUpCommandPageTitleTextIfNeeded();
            LogCommandPanelPerformanceStage("EnsureStyles", stageStartedAtTimestamp);
            stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
            ReleaseGuiFocusIfPending();
            string currentLanguageCode = GuiText.CurrentLanguageCode;
            if (!string.Equals(_lastGuiLanguageCode, currentLanguageCode, System.StringComparison.Ordinal))
            {
                _lastGuiLanguageCode = currentLanguageCode;
                HandleLanguageChanged();
            }
            // Shortcut capture is shared by the catalog and currency pickup pages.
            // Handle it at the common IMGUI entry point so the active page cannot
            // accidentally omit keyboard capture.
            HandlePickupShortcutCapture();
            LogCommandPanelPerformanceStage("LanguageAndFocus", stageStartedAtTimestamp);

            FoyerCharacterOption[] characterOptions = EmptyCharacterOptions;
            string characterAvailability = _cachedCharacterAvailability;
            float panelHeight = GetCommandPanelHeight();
            PreparePageDataForGui(ref characterOptions, ref characterAvailability, ref panelHeight);

            Matrix4x4 previousGuiMatrix = GUI.matrix;
            GUI.matrix = GetAutoScaledGuiMatrix();
            LogCommandPanelPerformanceStage("GuiMatrix", BeginCommandPanelPerformanceStage());
            try
            {
                DrawPanelContent(panelHeight, characterOptions, characterAvailability, player, logger);
            }
            finally
            {
                CompleteCommandPanelPerformanceTrace("OnGUI.complete");
                GUI.matrix = previousGuiMatrix;
            }
        }

        private void PreparePageDataForGui(
            ref FoyerCharacterOption[] characterOptions,
            ref string characterAvailability,
            ref float panelHeight)
        {
            long stageStartedAtTimestamp = BeginCommandPanelPerformanceStage();
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
        }

        private void DrawPanelContent(
            float panelHeight,
            FoyerCharacterOption[] characterOptions,
            string characterAvailability,
            PlayerController player,
            BepInEx.Logging.ManualLogSource logger)
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
            if (_currentPage != PanelPage.Command && _showCommandPanelCloseButton)
            {
                Rect closeButtonRect = new Rect(panelRect.x + panelRect.width - 44f, panelRect.y + 12f, 30f, 30f);
                if (DrawCloseButton(closeButtonRect, "cmd.close"))
                {
                    Close();
                    return;
                }
            }

            DrawCurrentPage(panelRect, characterOptions, characterAvailability, player, logger);
        }

        private void DrawCurrentPage(
            Rect panelRect,
            FoyerCharacterOption[] characterOptions,
            string characterAvailability,
            PlayerController player,
            BepInEx.Logging.ManualLogSource logger)
        {
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

        private void Toggle()
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                SyncPanelInputOverride();
                _focusInputField = false;
                _focusPickupSearchField = false;
                RequestGuiFocusRelease();
                LogCommandPanelPerformanceStage("Toggle.open.ready");
                return;
            }

            CompleteCommandPanelPerformanceTrace("Toggle.close");
            CancelPanelEndToEndTrace("ClosedBeforeRepaint");
            ResetClosedPanelState();
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
            return ControllerFocusNavigator.Move(entries, currentControlId, direction);
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
            _commandPageTitleTextRenderingWarmedUp = false;
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
            _commandPanelLifecycleCoordinator.RequestGuiFocusRelease();
        }

        private void ReleaseGuiFocusIfPending()
        {
            _commandPanelLifecycleCoordinator.ReleaseGuiFocusIfPending();
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
