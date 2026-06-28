using UnityEngine;
using Dungeonator;

namespace RandomLoadout
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
            InvincibilityToggleService invincibilityToggleService,
            AmmoModeToggleService ammoModeToggleService,
            AmmonomiconFastOpenToggleService ammonomiconFastOpenToggleService,
            LoadoutRuleEditorService loadoutRuleEditorService,
            System.Func<EtgPickupCatalogEntry[]> pickupCatalogProvider,
            System.Func<PickupAliasRegistry> aliasRegistryProvider,
            System.Func<string> languageProvider,
            System.Action<string> languageSetter,
            System.Action<string> inputLogHandler,
            System.Func<KeyCode> toggleKeyProvider,
            System.Func<string> toggleKeyNameProvider,
            System.Action<string> toggleKeySetter,
            System.Func<string> uiScalePresetProvider,
            System.Action<string> uiScalePresetSetter,
            System.Func<bool> playerStatsPanelShownProvider,
            System.Action<bool> playerStatsPanelShownSetter,
            System.Func<bool> experimentalModeProvider,
            System.Action<bool> experimentalModeSetter,
            System.Action<bool> ammonomiconFastOpenEnabledSetter,
            System.Func<bool> mapTeleportVerboseLoggingEnabledProvider,
            System.Func<bool> floorTeleportVerboseLoggingEnabledProvider,
            System.Func<EtgFloorDefinition, string, string, bool> deferredTeleportRequestHandler)
        {
            _commandService = commandService;
            _playerDebugCommandService = playerDebugCommandService;
            _roomDebugCommandService = roomDebugCommandService;
            _foyerCharacterSwitchService = foyerCharacterSwitchService;
            _bossRushService = bossRushService;
            _rapidFireToggleService = rapidFireToggleService;
            _autoReloadToggleService = autoReloadToggleService;
            _invincibilityToggleService = invincibilityToggleService;
            _ammoModeToggleService = ammoModeToggleService;
            _ammonomiconFastOpenToggleService = ammonomiconFastOpenToggleService;
            _loadoutRuleEditorService = loadoutRuleEditorService;
            _pickupCatalogProvider = pickupCatalogProvider;
            _aliasRegistryProvider = aliasRegistryProvider;
            _languageProvider = languageProvider;
            _languageSetter = languageSetter;
            _inputLogHandler = inputLogHandler;
            _toggleKeyProvider = toggleKeyProvider;
            _toggleKeyNameProvider = toggleKeyNameProvider;
            _toggleKeySetter = toggleKeySetter;
            _uiScalePresetProvider = uiScalePresetProvider;
            _uiScalePresetSetter = uiScalePresetSetter;
            _playerStatsPanelShownProvider = playerStatsPanelShownProvider;
            _playerStatsPanelShownSetter = playerStatsPanelShownSetter;
            _experimentalModeProvider = experimentalModeProvider;
            _experimentalModeSetter = experimentalModeSetter;
            _ammonomiconFastOpenEnabledSetter = ammonomiconFastOpenEnabledSetter;
            _mapTeleportVerboseLoggingEnabledProvider = mapTeleportVerboseLoggingEnabledProvider;
            _floorTeleportVerboseLoggingEnabledProvider = floorTeleportVerboseLoggingEnabledProvider;
            _deferredTeleportRequestHandler = deferredTeleportRequestHandler;
            _showPlayerStatsPanel = _playerStatsPanelShownProvider != null && _playerStatsPanelShownProvider();
            if (_bossRushService != null)
            {
                _bossRushService.StatusRaised += OnBossRushStatusRaised;
            }
        }

        public void Update()
        {
            LogJoystickButtonStateChanges();
            UpdateMapFeatureActivationState();
            LogMapDirectTeleportRoomTransitionIfNeeded();
            LogMapDirectTeleportRuntimeStateIfNeeded();

            HandleControllerNavigation();

            if (Input.GetKeyDown(GetToggleKey()) || IsGamepadToggleShortcutPressed())
            {
                Toggle();
            }

            if (!_isVisible)
            {
                return;
            }
        }

        public void OnGUI(PlayerController player, BepInEx.Logging.ManualLogSource logger)
        {
            EnsureStyles();
            ReleaseGuiFocusIfPending();
            string currentLanguageCode = GuiText.CurrentLanguageCode;
            if (!string.Equals(_lastGuiLanguageCode, currentLanguageCode, System.StringComparison.Ordinal))
            {
                _lastGuiLanguageCode = currentLanguageCode;
                HandleLanguageChanged();
            }

            FoyerCharacterOption[] characterOptions = EmptyCharacterOptions;
            string characterAvailability = _cachedCharacterAvailability;
            float panelHeight = GetCommandPanelHeight();
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
            else if (_isVisible && _currentPage == PanelPage.ControllerHelp)
            {
                panelHeight = ControllerHelpPanelHeight;
            }

            Matrix4x4 previousGuiMatrix = GUI.matrix;
            GUI.matrix = GetAutoScaledGuiMatrix();
            try
            {
                DrawPlayerStatsPanelIfEnabled(player);
                DrawStatusOverlay(panelHeight);
                if (!_isVisible)
                {
                    return;
                }

                Rect panelRect = GetMainPanelRect(panelHeight);
                GUI.Box(panelRect, GUIContent.none, _panelStyle);
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

                if (_currentPage == PanelPage.ControllerHelp)
                {
                    DrawControllerHelpPage(panelRect);
                    return;
                }

                if (_currentPage == PanelPage.Settings)
                {
                    DrawSettingsPage(panelRect, logger);
                }
                else
                {
                    DrawCommandPage(panelRect, player, logger);
                }

                DrawExperimentalModeConfirmDialog(panelRect, logger);
            }
            finally
            {
                GUI.matrix = previousGuiMatrix;
            }
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                _focusInputField = false;
                _focusPickupSearchField = false;
                RequestGuiFocusRelease();
                return;
            }

            ResetClosedPanelState();
        }

        private void HandleControllerNavigation()
        {
            if (!_isVisible)
            {
                ResetControllerNavigationAxes();
                return;
            }

            bool isControllerBackPressed = IsControllerBackPressed();
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

                if (IsControllerConfirmPressed())
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
                case PanelPage.ControllerHelp:
                    HandleControllerHelpPageControllerNavigation(isControllerBackPressed);
                    return;
                case PanelPage.Pickups:
                    HandlePickupPageControllerNavigation(isControllerBackPressed);
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
                LogGamepadShortcutState(
                    "Command page controller navigation moved focus. Direction=" +
                    navigationDirection.Value +
                    ", From=" +
                    previousControlId +
                    ", To=" +
                    _commandPageFocusedControlId +
                    ".");
            }

            if (IsControllerConfirmPressed())
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

            if (IsControllerConfirmPressed())
            {
                ExecuteSettingsPageFocusedControl();
            }
        }

        private void HandleControllerHelpPageControllerNavigation(bool isControllerBackPressed)
        {
            if (isControllerBackPressed || IsControllerConfirmPressed())
            {
                LogGamepadShortcutState("Controller navigation is returning from controller help to settings.");
                OpenSettingsPage();
                return;
            }

            ResetControllerNavigationAxes();
        }

        private void HandlePickupPageControllerNavigation(bool isControllerBackPressed)
        {
            if (!isControllerBackPressed)
            {
                ResetControllerNavigationAxes();
                return;
            }

            if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
            {
                LogGamepadShortcutState("Controller back press is returning from pickup browser to loadout editor preset detail.");
                _currentPage = PanelPage.LoadoutEditor;
                _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                RefreshLoadoutEditorEntries();
            }
            else if (_pickupBrowserMode == PickupBrowserMode.AddToRandomPool)
            {
                LogGamepadShortcutState("Controller back press is returning from pickup browser to loadout editor random pool detail.");
                _currentPage = PanelPage.LoadoutEditor;
                _loadoutEditorMode = LoadoutEditorMode.RandomPoolDetail;
                RefreshLoadoutEditorEntries();
                RefreshLoadoutRandomPoolEntries();
            }
            else
            {
                LogGamepadShortcutState("Controller back press is returning from pickup browser to the command page.");
                _currentPage = PanelPage.Command;
                _focusInputField = true;
            }

            _focusPickupSearchField = false;
            RequestGuiFocusRelease();
            ResetControllerNavigationAxes();
        }

        private void HandleLoadoutEditorPageControllerNavigation(bool isControllerBackPressed)
        {
            if (!isControllerBackPressed)
            {
                ResetControllerNavigationAxes();
                return;
            }

            switch (_loadoutEditorMode)
            {
                case LoadoutEditorMode.RandomPoolDetail:
                    LogGamepadShortcutState("Controller back press is returning from loadout random pool detail to preset detail.");
                    _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                    RefreshLoadoutEditorEntries();
                    break;
                case LoadoutEditorMode.PresetPickupsDetail:
                    LogGamepadShortcutState("Controller back press is returning from loadout preset pickups detail to preset detail.");
                    _loadoutEditorMode = LoadoutEditorMode.PresetDetail;
                    _loadoutPickupCountEditIndex = -1;
                    _loadoutPickupCountEditText = string.Empty;
                    RefreshLoadoutEditorEntries();
                    break;
                case LoadoutEditorMode.PresetDetail:
                    LogGamepadShortcutState("Controller back press is returning from loadout preset detail to preset list.");
                    _loadoutEditorMode = LoadoutEditorMode.PresetList;
                    RefreshLoadoutPresetEntries();
                    break;
                case LoadoutEditorMode.PresetList:
                default:
                    LogGamepadShortcutState("Controller back press is returning from loadout editor to the command page.");
                    _currentPage = PanelPage.Command;
                    _focusInputField = true;
                    break;
            }

            RequestGuiFocusRelease();
            ResetControllerNavigationAxes();
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

            if (IsControllerConfirmPressed())
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
            ControllerNavDirection? braveInputDirection = GetBraveInputNavigationDirection();
            if (braveInputDirection.HasValue)
            {
                return braveInputDirection;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
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

        private ControllerNavDirection? GetBraveInputNavigationDirection()
        {
            BraveInput braveInput = BraveInput.PrimaryPlayerInstance;
            if ((object)braveInput == null)
            {
                braveInput = BraveInput.PlayerlessInstance;
            }

            if ((object)braveInput == null || braveInput.ActiveActions == null)
            {
                return null;
            }

            GungeonActions actions = braveInput.ActiveActions;
            if (WasNavigationActionPressed(actions.SelectLeft))
            {
                LogGamepadShortcutState("Detected controller navigation from BraveInput.SelectLeft.");
                return ControllerNavDirection.Left;
            }

            if (WasNavigationActionPressed(actions.SelectRight))
            {
                LogGamepadShortcutState("Detected controller navigation from BraveInput.SelectRight.");
                return ControllerNavDirection.Right;
            }

            if (WasNavigationActionPressed(actions.SelectUp))
            {
                LogGamepadShortcutState("Detected controller navigation from BraveInput.SelectUp.");
                return ControllerNavDirection.Up;
            }

            if (WasNavigationActionPressed(actions.SelectDown))
            {
                LogGamepadShortcutState("Detected controller navigation from BraveInput.SelectDown.");
                return ControllerNavDirection.Down;
            }

            return null;
        }

        private static bool WasNavigationActionPressed(InControl.PlayerAction action)
        {
            return action != null &&
                   (action.WasPressed ||
                    action.WasPressedRepeating ||
                    action.WasPressedAsDpad ||
                    action.WasPressedAsDpadRepeating);
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

        private bool IsControllerConfirmPressed()
        {
            return Input.GetKeyDown(GetJoystickButtonKeyCode(0));
        }

        private bool IsControllerBackPressed()
        {
            return Input.GetKeyDown(GetJoystickButtonKeyCode(1));
        }

        private bool IsGamepadToggleShortcutPressed()
        {
            const int leftStickButtonIndex = 8;
            const int rightStickButtonIndex = 9;
            KeyCode leftStickButtonKeyCode = GetJoystickButtonKeyCode(leftStickButtonIndex);
            KeyCode rightStickButtonKeyCode = GetJoystickButtonKeyCode(rightStickButtonIndex);
            bool isLeftStickPressed = Input.GetKey(leftStickButtonKeyCode);
            bool isRightStickPressed = Input.GetKeyDown(rightStickButtonKeyCode);

            if (_isVisible || isLeftStickPressed || !isRightStickPressed)
            {
                if (isRightStickPressed)
                {
                    LogGamepadShortcutState(
                        "Ignored command panel R3 short press. Visible=" +
                        _isVisible +
                        ", L3Pressed=" +
                        isLeftStickPressed +
                        ", R3Down=" +
                        isRightStickPressed +
                        ".");
                }

                return false;
            }

            LogGamepadShortcutState("Detected command panel R3 short press. Opening command panel.");
            return true;
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

        private static KeyCode GetJoystickButtonKeyCode(int buttonIndex)
        {
            return KeyCode.JoystickButton0 + buttonIndex;
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
            _focusInputField = false;
            _focusPickupSearchField = false;
            _currentPage = PanelPage.Command;
            _commandPageFocusedControlId = "cmd.settings";
            _settingsPageFocusedControlId = "settings.toggle_key";
            _inputText = string.Empty;
            CloseTeleportPanel();
            ResetPickupBrowserState();
            ResetCharacterPageCache();
            ResetControllerNavigationAxes();
            RequestGuiFocusRelease();
        }

        private void RequestGuiFocusRelease()
        {
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
            ReleaseGuiFocus();
        }

        private static void ReleaseGuiFocus()
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
        }

        private KeyCode GetToggleKey()
        {
            return _toggleKeyProvider != null ? _toggleKeyProvider() : KeyCode.F7;
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
            return new Rect(
                (GetScaledScreenWidth() - PanelWidth) * 0.5f,
                GetScaledScreenHeight() - PanelBottomMargin - panelHeight,
                PanelWidth,
                panelHeight);
        }

        private float GetUiScaleMultiplier()
        {
            return UiScalePresetCatalog.GetScaleMultiplier(GetConfiguredUiScalePreset());
        }
    }
}
