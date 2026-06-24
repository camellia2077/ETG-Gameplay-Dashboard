using UnityEngine;

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
            System.Func<string> gamepadPresetProvider,
            System.Action<string> gamepadPresetSetter,
            System.Func<string> uiScalePresetProvider,
            System.Action<string> uiScalePresetSetter,
            System.Func<bool> experimentalModeProvider,
            System.Action<bool> experimentalModeSetter,
            System.Action<bool> ammonomiconFastOpenEnabledSetter,
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
            _gamepadPresetProvider = gamepadPresetProvider;
            _gamepadPresetSetter = gamepadPresetSetter;
            _uiScalePresetProvider = uiScalePresetProvider;
            _uiScalePresetSetter = uiScalePresetSetter;
            _experimentalModeProvider = experimentalModeProvider;
            _experimentalModeSetter = experimentalModeSetter;
            _ammonomiconFastOpenEnabledSetter = ammonomiconFastOpenEnabledSetter;
            _deferredTeleportRequestHandler = deferredTeleportRequestHandler;
            if (_bossRushService != null)
            {
                _bossRushService.StatusRaised += OnBossRushStatusRaised;
            }
        }

        public void Update()
        {
            LogJoystickButtonStateChanges();

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

            if (_showExperimentalModeConfirmDialog && _currentPage == PanelPage.Settings)
            {
                if (IsControllerBackPressed())
                {
                    _showExperimentalModeConfirmDialog = false;
                }

                if (IsControllerConfirmPressed())
                {
                    SetExperimentalModeEnabled(true, null);
                }

                ResetControllerNavigationAxes();
                return;
            }

            switch (_currentPage)
            {
                case PanelPage.Command:
                    HandleCommandPageControllerNavigation();
                    return;
                case PanelPage.Settings:
                    HandleSettingsPageControllerNavigation();
                    return;
                default:
                    ResetControllerNavigationAxes();
                    return;
            }
        }

        private void HandleCommandPageControllerNavigation()
        {
            if (IsControllerBackPressed())
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(GetJoystickButtonKeyCode(4)))
            {
                CycleCommandCategory(-1);
            }
            else if (Input.GetKeyDown(GetJoystickButtonKeyCode(5)))
            {
                CycleCommandCategory(1);
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                _commandPageFocusedControlId = MoveControllerFocus(GetCommandPageFocusEntries(), _commandPageFocusedControlId, navigationDirection.Value);
            }

            if (IsControllerConfirmPressed())
            {
                ExecuteCommandPageFocusedControl();
            }
        }

        private void HandleSettingsPageControllerNavigation()
        {
            if (IsControllerBackPressed())
            {
                _currentPage = PanelPage.Command;
                return;
            }

            ControllerNavDirection? navigationDirection = GetControllerNavigationDirection();
            if (navigationDirection.HasValue)
            {
                _settingsPageFocusedControlId = MoveControllerFocus(GetSettingsPageFocusEntries(), _settingsPageFocusedControlId, navigationDirection.Value);
            }

            if (IsControllerConfirmPressed())
            {
                ExecuteSettingsPageFocusedControl();
            }
        }

        private ControllerNavDirection? GetControllerNavigationDirection()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) < 0.5f)
            {
                _wasControllerHorizontalNavigationActive = false;
            }
            else if (!_wasControllerHorizontalNavigationActive)
            {
                _wasControllerHorizontalNavigationActive = true;
                return horizontal > 0f ? ControllerNavDirection.Right : ControllerNavDirection.Left;
            }

            float vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(vertical) < 0.5f)
            {
                _wasControllerVerticalNavigationActive = false;
            }
            else if (!_wasControllerVerticalNavigationActive)
            {
                _wasControllerVerticalNavigationActive = true;
                return vertical > 0f ? ControllerNavDirection.Up : ControllerNavDirection.Down;
            }

            return null;
        }

        private void ResetControllerNavigationAxes()
        {
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
            KeyCode backButtonKeyCode;
            KeyCode startButtonKeyCode;
            GetGamepadShortcutKeyCodes(out backButtonKeyCode, out startButtonKeyCode);
            bool isBackPressed = Input.GetKey(backButtonKeyCode);
            bool isStartPressed = Input.GetKey(startButtonKeyCode);
            bool didBackChange = isBackPressed != _wasGamepadBackPressed;
            bool didStartChange = isStartPressed != _wasGamepadStartPressed;

            if (didBackChange || didStartChange)
            {
                LogGamepadShortcutState(
                    "Observed gamepad shortcut state change. BackOrSelect=" +
                    isBackPressed +
                    ", Start=" +
                    isStartPressed +
                    ", BackDown=" +
                    Input.GetKeyDown(backButtonKeyCode) +
                    ", StartDown=" +
                    Input.GetKeyDown(startButtonKeyCode) +
                    ", BackUp=" +
                    Input.GetKeyUp(backButtonKeyCode) +
                    ", StartUp=" +
                    Input.GetKeyUp(startButtonKeyCode) +
                    ", Preset=" +
                    GetConfiguredGamepadPreset() +
                    ".");
            }

            _wasGamepadBackPressed = isBackPressed;
            _wasGamepadStartPressed = isStartPressed;

            bool isShortcutPressed =
                (isBackPressed && Input.GetKeyDown(startButtonKeyCode)) ||
                (isStartPressed && Input.GetKeyDown(backButtonKeyCode));
            if (isShortcutPressed)
            {
                LogGamepadShortcutState("Detected gamepad shortcut press. Toggling command panel.");
            }

            return isShortcutPressed;
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

        private void GetGamepadShortcutKeyCodes(out KeyCode backButtonKeyCode, out KeyCode startButtonKeyCode)
        {
            if (string.Equals(GetConfiguredGamepadPreset(), "Legacy", System.StringComparison.OrdinalIgnoreCase))
            {
                backButtonKeyCode = KeyCode.JoystickButton8;
                startButtonKeyCode = KeyCode.JoystickButton9;
                return;
            }

            backButtonKeyCode = KeyCode.JoystickButton6;
            startButtonKeyCode = KeyCode.JoystickButton7;
        }

        private string GetConfiguredGamepadPreset()
        {
            return _gamepadPresetProvider != null ? GetNormalizedGamepadPresetName(_gamepadPresetProvider()) : "Xbox";
        }

        private void LogGamepadShortcutState(string message)
        {
            if (_inputLogHandler != null)
            {
                _inputLogHandler(message);
            }
        }

        private static string GetNormalizedGamepadPresetName(string presetName)
        {
            return string.Equals(presetName, "Legacy", System.StringComparison.OrdinalIgnoreCase)
                ? "Legacy"
                : "Xbox";
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
            _showTeleportPanel = false;
            ResetPickupBrowserState();
            ResetCharacterPageCache();
            ResetControllerNavigationAxes();
            RequestGuiFocusRelease();
        }

        private void RequestGuiFocusRelease()
        {
            _releaseGuiFocusPending = true;
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
