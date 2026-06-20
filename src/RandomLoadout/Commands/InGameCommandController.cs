using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        public InGameCommandController(
            GrantCommandService commandService,
            PlayerDebugCommandService playerDebugCommandService,
            FoyerCharacterSwitchService foyerCharacterSwitchService,
            BossRushService bossRushService,
            RapidFireToggleService rapidFireToggleService,
            AutoReloadToggleService autoReloadToggleService,
            InvincibilityToggleService invincibilityToggleService,
            NoAmmoConsumptionToggleService noAmmoConsumptionToggleService,
            LoadoutRuleEditorService loadoutRuleEditorService,
            System.Func<EtgPickupCatalogEntry[]> pickupCatalogProvider,
            System.Func<PickupAliasRegistry> aliasRegistryProvider,
            System.Func<string> languageProvider,
            System.Action<string> languageSetter)
        {
            _commandService = commandService;
            _playerDebugCommandService = playerDebugCommandService;
            _foyerCharacterSwitchService = foyerCharacterSwitchService;
            _bossRushService = bossRushService;
            _rapidFireToggleService = rapidFireToggleService;
            _autoReloadToggleService = autoReloadToggleService;
            _invincibilityToggleService = invincibilityToggleService;
            _noAmmoConsumptionToggleService = noAmmoConsumptionToggleService;
            _loadoutRuleEditorService = loadoutRuleEditorService;
            _pickupCatalogProvider = pickupCatalogProvider;
            _aliasRegistryProvider = aliasRegistryProvider;
            _languageProvider = languageProvider;
            _languageSetter = languageSetter;
            if (_bossRushService != null)
            {
                _bossRushService.StatusRaised += OnBossRushStatusRaised;
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                Toggle();
            }

            if (!_isVisible)
            {
                return;
            }
        }

        public void OnGUI(PlayerController player, ManualLogSource logger)
        {
            EnsureStyles();
            string currentLanguageCode = GuiText.CurrentLanguageCode;
            if (!string.Equals(_lastGuiLanguageCode, currentLanguageCode, System.StringComparison.Ordinal))
            {
                _lastGuiLanguageCode = currentLanguageCode;
                ResetPickupBrowserState();
                ResetCharacterPageCache();
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

            DrawPlayerStatsPanelIfEnabled(player, logger);
            DrawStatusOverlay(panelHeight);
            if (!_isVisible)
            {
                return;
            }

            Rect panelRect = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                Screen.height - PanelBottomMargin - panelHeight,
                PanelWidth,
                panelHeight);

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

            DrawCommandPage(panelRect, player, logger);
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            _focusInputField = _isVisible;
            if (!_isVisible)
            {
                _currentPage = PanelPage.Command;
                _inputText = string.Empty;
                _showTeleportPanel = false;
                ResetPickupBrowserState();
                ResetCharacterPageCache();
            }
        }

        private void Close()
        {
            _isVisible = false;
            _currentPage = PanelPage.Command;
            _inputText = string.Empty;
            _showTeleportPanel = false;
            ResetPickupBrowserState();
            ResetCharacterPageCache();
        }
    }
}
