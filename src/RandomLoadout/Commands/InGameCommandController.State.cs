// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        internal bool IsVisibleForDiagnostics
        {
            get { return _isVisible; }
        }

        private enum PanelPage
        {
            Command,
            Pickups,
            Characters,
            Currency,
            BossRush,
            LoadoutEditor,
            About,
            Settings,
            PickupInfoConfig,
            AdvancedTools,
            ControllerHelp,
            KeyboardHelp,
            CursorColor,
        }

        private enum CharacterActionMode
        {
            SwitchOnly,
            Unlock,
        }

        private enum CharacterSwitchTarget
        {
            PrimaryPlayer,
            SecondaryPlayer,
            BothPlayers,
        }

        private enum PickupBrowserFilter
        {
            All,
            Gun,
            Passive,
            Active,
        }

        private enum PickupQualityFilter
        {
            All,
            D,
            C,
            B,
            A,
            S,
            Special,
            Excluded,
        }

        private enum PickupGunClassFilter
        {
            All,
            Pistol,
            FullAuto,
            Shotgun,
            Rifle,
            Beam,
            Charge,
            Explosive,
            Elemental,
            Special,
        }

        private enum PickupPassiveSubcategoryFilter
        {
            All,
            Bullet,
        }

        private enum PickupActiveCooldownFilter
        {
            All,
            Uses,
            Damage,
            Time,
            Room,
        }

        private enum PickupBrowserMode
        {
            Grant,
            AddToStartItems,
            AddToRandomPool,
        }

        private enum LoadoutEditorMode
        {
            PresetList,
            PresetDetail,
            RandomPoolDetail,
            PresetPickupsDetail,
        }

        private enum CommandMenuCategory
        {
            General,
            Player,
            Room,
        }

        private enum RoomMenuSection
        {
            Chest,
            Neutral,
            Enemies,
            Rewind,
            Boss,
            State,
        }

        private enum RoomEnemyRefreshMethod
        {
            Rewind,
            RespawnEnemies,
        }

        private enum PlayerMenuSection
        {
            Character,
            Combat,
        }

        private enum CharacterMenuSection
        {
            Pickups,
            Stats,
        }

        private enum StatusSeverity
        {
            Success,
            Failure,
            Warning,
            Information,
        }

        private enum ControllerNavDirection
        {
            Left,
            Right,
            Up,
            Down,
        }

        private const string InputControlName = "RandomLoadoutCommandInput";
        private const string PickupSearchControlName = "RandomLoadoutPickupSearch";
        private const string PanelInputOverrideReason = "randomloadout_command_panel";
        private const KeyCode RoomEnemyRewindShortcutKey = KeyCode.C;
        private const float StatusDurationSeconds = 4f;
        private const float KeyboardNavigationRepeatDelaySeconds = 0.35f;
        private const float KeyboardNavigationRepeatIntervalSeconds = 0.08f;
        private const float ReferenceScreenWidth = 1920f;
        private const float ReferenceScreenHeight = 1080f;
        private const float MinimumUiScale = 0.70f;
        private const float MaximumUiScale = 1.50f;
        private const float PanelWidth = 612f;
        private const float LoadoutEditorPanelWidth = 900f;
        private const float SettingsPanelWidth = 1100f;
        private const float BasePanelHeight = 284f;
        private const float PlayerStatsPanelWidth = 160f;
        private const float PlayerStatsPanelHeight = 330f;
        private const float TeleportPanelWidth = 286f;
        private const float TeleportPanelHeight = 410f;
        private const float PlayerStatsRowHeight = 23f;
        private const float PlayerStatsRowGap = 3f;
        private const float PickupBrowserPanelHeight = 496f;
        private const float LoadoutEditorPanelHeight = 440f;
        private const float AboutPanelHeight = 404f;
        private const float SettingsPanelHeight = 560f;
        private const float PickupInfoConfigPanelHeight = 428f;
        private const float AdvancedToolsPanelHeight = 254f;
        private const float ControllerHelpPanelHeight = 356f;
        private const float KeyboardHelpPanelHeight = 356f;
        private const float CursorColorPanelHeight = 430f;
        private const float CharacterPanelBaseHeaderHeight = 126f;
        private const float CharacterPanelFooterHeight = 26f;
        private const float CurrencyPanelHeight = 430f;
        private const float BossRushPanelHeight = 226f;
        private const float PanelBottomMargin = 92f;
        private const float StatusMaxWidth = 560f;
        private const float StatusMinHeight = 40f;
        private const float StatusGap = 14f;
        private const float ButtonWidth = 92f;
        private const float ButtonGap = 8f;
        private const float StatsButtonWidth = 108f;
        private const float LanguageButtonWidth = 108f;
        private const float BossRushMenuButtonWidth = 108f;
        private const float BossRushActionButtonWidth = 180f;
        private const float CurrencyMenuButtonWidth = 108f;
        private const float PickupMenuButtonWidth = 108f;
        private const float CurrencyActionButtonWidth = 180f;
        private const float CharacterButtonWidth = 108f;
        private const int CharacterButtonsPerRow = 5;
        private const float CharacterModeButtonWidth = 180f;
        private const float CharacterPageRefreshIntervalSeconds = 0.2f;
        private const float PickupFilterButtonWidth = 78f;
        private const float PickupFilterSmallButtonWidth = 58f;
        private const float PickupFilterGunClassButtonWidth = 82f;
        private const float PickupRowHeight = 48f;
        private const float PickupBrowserRowHeight = 48f;
        private const float PickupBrowserRowGap = 6f;
        private const float LoadoutPresetRowHeight = 48f;
        private const float LoadoutPresetPreviewRowHeight = 24f;
        private const int LoadoutPresetColumnCount = 2;
        private const float LoadoutRuleRowHeight = 60f;
        private const float PickupIconSize = 32f;
        private const float PickupBrowserIconWidth = 64f;
        private const float PickupBrowserIconHeight = 40f;
        private const float PickupGrantButtonWidth = 72f;

        private static Color PanelBackgroundColor { get { return DashboardTheme.PanelBackground; } }
        private static Color SecondaryColor { get { return DashboardTheme.Secondary; } }
        private static Color PlayerStatsPanelBackgroundColor { get { return DashboardTheme.PanelBackground; } }
        private static Color PlayerStatsRowBackgroundColor { get { return DashboardTheme.PanelRowBackground; } }
        private static Color PanelBorderColor { get { return DashboardTheme.PanelOuterBorder; } }
        private static Color InnerBorderColor { get { return DashboardTheme.PanelInnerBorder; } }
        private static Color InputBackgroundColor { get { return DashboardTheme.InputBackground; } }
        private static Color ButtonBackgroundColor { get { return DashboardTheme.ButtonBackground; } }
        private static Color ButtonBorderColor { get { return DashboardTheme.ButtonBorder; } }
        private static Color ButtonSelectedBorderColor { get { return DashboardTheme.ButtonSelectedBorder; } }
        private static Color ButtonHoverColor { get { return DashboardTheme.ButtonHoverBackground; } }
        private static Color ButtonActiveColor { get { return DashboardTheme.ButtonActiveBackground; } }
        private static Color CommandCategoryButtonBackgroundColor { get { return DashboardTheme.CommandCategoryNormalBackground; } }
        private static Color CommandCategoryButtonBorderColor { get { return DashboardTheme.CommandCategoryNormalBorder; } }
        private static Color CommandCategoryHoverButtonBackgroundColor { get { return DashboardTheme.CommandCategoryHoverBackground; } }
        private static Color CommandCategoryHoverButtonBorderColor { get { return DashboardTheme.CommandCategoryHoverBorder; } }
        private static Color CommandCategoryActiveButtonBackgroundColor { get { return DashboardTheme.CommandCategorySelectedBackground; } }
        private static Color CommandCategoryActiveButtonBorderColor { get { return DashboardTheme.CommandCategorySelectedBorder; } }
        private static Color DisabledButtonBackgroundColor { get { return DashboardTheme.DisabledButtonBackground; } }
        private static Color EnabledButtonBackgroundColor { get { return DashboardTheme.EnabledButtonBackground; } }
        private static Color EnabledButtonHoverColor { get { return DashboardTheme.EnabledButtonHoverBackground; } }
        private static Color EnabledButtonActiveColor { get { return DashboardTheme.EnabledButtonActiveBackground; } }
        private static Color PrimaryTextColor { get { return DashboardTheme.PrimaryText; } }
        private static Color PlayerStatsTextColor { get { return Color.white; } }
        private static Color SecondaryTextColor { get { return DashboardTheme.SecondaryText; } }
        // Status colors stay fixed across themes so their meaning remains immediately recognizable.
        private static readonly Color StatusSuccessBackgroundColor = new Color32(31, 122, 69, 230);
        private static readonly Color StatusFailureBackgroundColor = new Color32(185, 56, 56, 230);
        private static readonly Color StatusWarningBackgroundColor = new Color32(154, 103, 0, 230);
        private static readonly Color StatusInformationBackgroundColor = new Color32(29, 95, 155, 230);
        private readonly AmmonomiconFastOpenToggleService _ammonomiconFastOpenToggleService;
        private static readonly FoyerCharacterOption[] EmptyCharacterOptions = new FoyerCharacterOption[0];
        private static readonly PickupBrowserEntry[] EmptyPickupBrowserEntries = new PickupBrowserEntry[0];
        private static readonly LoadoutRuleEditorEntry[] EmptyLoadoutRuleEditorEntries = new LoadoutRuleEditorEntry[0];
        private static readonly LoadoutPresetEditorEntry[] EmptyLoadoutPresetEditorEntries = new LoadoutPresetEditorEntry[0];
        private static readonly LoadoutRandomPoolEditorEntry[] EmptyLoadoutRandomPoolEditorEntries = new LoadoutRandomPoolEditorEntry[0];
        private static readonly LoadoutRuleEditorEntry[] EmptyLoadoutPickupEditorEntries = new LoadoutRuleEditorEntry[0];
        private static readonly TeleportOption[] TeleportOptions =
        {
            new TeleportOption("keep", "label.teleport.floor.keep", "load_level keep"),
            new TeleportOption("oubliette", "label.teleport.floor.oubliette", "load_level oubliette"),
            new TeleportOption("proper", "label.teleport.floor.proper", "load_level proper"),
            new TeleportOption("abbey", "label.teleport.floor.abbey", "load_level abbey"),
            new TeleportOption("mine", "label.teleport.floor.mine", "load_level mine"),
            new TeleportOption("ratden", "label.teleport.floor.ratden", "load_level ratden"),
            new TeleportOption("hollow", "label.teleport.floor.hollow", "load_level hollow"),
            new TeleportOption("R&G_Dept", "label.teleport.floor.rng_dept", "load_level R&G_Dept"),
            new TeleportOption("forge", "label.teleport.floor.forge", "load_level forge"),
            new TeleportOption("heli", "label.teleport.floor.heli", "load_level heli"),
        };

        private static readonly CommandMenuCategory[] CommandMenuCategoryOrder =
        {
            CommandMenuCategory.General,
            CommandMenuCategory.Player,
            CommandMenuCategory.Room,
        };

        private static readonly ControllerFocusEntry[] SettingsPageFocusEntries =
        {
            new ControllerFocusEntry("settings.back", 0, 0),
            new ControllerFocusEntry("settings.toggle_key", 1, 0),
            new ControllerFocusEntry("settings.keyboard_help", 2, 0),
            new ControllerFocusEntry("settings.controller_shortcut", 3, 0),
            new ControllerFocusEntry("settings.controller_shortcut_enabled", 4, 0),
            new ControllerFocusEntry("settings.controller_help", 5, 0),
            new ControllerFocusEntry("settings.about", 10, 0),
            new ControllerFocusEntry("settings.ui_scale", 6, 0),
            new ControllerFocusEntry("settings.language", 7, 0),
            new ControllerFocusEntry("settings.advanced_tools", 8, 0),
            new ControllerFocusEntry("settings.experimental_mode", 9, 0),
        };

        private static readonly ControllerFocusEntry[] CursorColorPageFocusEntries =
        {
            new ControllerFocusEntry("cursor_color.back", 0, 0),
            new ControllerFocusEntry("cursor_color.toggle", 1, 0),
            new ControllerFocusEntry("cursor_color.preset_01", 2, 0),
            new ControllerFocusEntry("cursor_color.preset_02", 2, 1),
            new ControllerFocusEntry("cursor_color.preset_03", 3, 0),
            new ControllerFocusEntry("cursor_color.preset_04", 3, 1),
            new ControllerFocusEntry("cursor_color.preset_05", 4, 0),
            new ControllerFocusEntry("cursor_color.preset_06", 4, 1),
            new ControllerFocusEntry("cursor_color.preset_07", 5, 0),
            new ControllerFocusEntry("cursor_color.preset_08", 5, 1),
        };

        private static readonly ControllerFocusEntry[] PickupInfoConfigPageFocusEntries =
        {
            new ControllerFocusEntry("pickup_info_config.back", 0, 0),
            new ControllerFocusEntry("pickup_info_config.quality", 1, 0),
            new ControllerFocusEntry("pickup_info_config.type", 2, 0),
            new ControllerFocusEntry("pickup_info_config.effects", 3, 0),
            new ControllerFocusEntry("pickup_info_config.synergies", 4, 0),
            new ControllerFocusEntry("pickup_info_config.summary", 5, 0),
            new ControllerFocusEntry("pickup_info_config.notes", 6, 0),
        };

        private static readonly ControllerFocusEntry[] CurrencyPageFocusEntries =
        {
            new ControllerFocusEntry("currency.target", 0, 0),
            new ControllerFocusEntry("currency.max_health", 1, 0),
            new ControllerFocusEntry("currency.back", 1, 1),
            new ControllerFocusEntry("currency.armor", 2, 0),
            new ControllerFocusEntry("currency.blank", 3, 0),
            new ControllerFocusEntry("currency.key", 4, 0),
            new ControllerFocusEntry("currency.rat_key", 5, 0),
            new ControllerFocusEntry("currency.casings", 6, 0),
            new ControllerFocusEntry("currency.clear_casings", 6, 1),
            new ControllerFocusEntry("currency.hegemony", 7, 0),
            new ControllerFocusEntry("currency.clear_hegemony", 7, 1),
        };

        private readonly GrantCommandParser _parser = new GrantCommandParser();
        private readonly GrantCommandService _commandService;
        private readonly PlayerDebugCommandService _playerDebugCommandService;
        private readonly RoomDebugCommandService _roomDebugCommandService;
        private readonly FoyerCharacterSwitchService _foyerCharacterSwitchService;
        private readonly BossRushService _bossRushService;
        private readonly RapidFireToggleService _rapidFireToggleService;
        private readonly AutoReloadToggleService _autoReloadToggleService;
        private readonly ArmorNoConsumeToggleService _armorNoConsumeToggleService;
        private readonly BlankNoConsumeToggleService _blankNoConsumeToggleService;
        private readonly KeyNoConsumeToggleService _keyNoConsumeToggleService;
        private readonly CurrencyNoConsumeToggleService _currencyNoConsumeToggleService;
        private readonly InvincibilityToggleService _invincibilityToggleService;
        private readonly EnemyHealthBarToggleService _enemyHealthBarToggleService;
        private readonly ControllerAimLockService _controllerAimLockService;
        private readonly KeyboardAimAssistService _keyboardAimAssistService;
        private readonly PlayerStatMultiplierService _playerStatMultiplierService;
        private readonly AmmoModeToggleService _ammoModeToggleService;
        private readonly LoadoutRuleEditorService _loadoutRuleEditorService;
        private readonly LoadoutPresetRandomService _loadoutPresetRandomService;
        private readonly Func<EtgPickupCatalogEntry[]> _pickupCatalogProvider;
        private readonly Func<int, string> _pickupGameplayNameProvider;
        private readonly Func<PickupAliasRegistry> _aliasRegistryProvider;
        private readonly Func<string> _languageProvider;
        private readonly Action<string> _languageSetter;
        private readonly Action<string> _inputLogHandler;
        private readonly Func<KeyCode> _toggleKeyProvider;
        private readonly Func<string> _toggleKeyNameProvider;
        private readonly Action<string> _toggleKeySetter;
        private readonly Func<KeyCode> _roomEnemyRewindKeyProvider;
        private readonly Func<string> _roomEnemyRefreshMethodProvider;
        private readonly Action<string> _roomEnemyRefreshMethodSetter;
        private readonly Func<string> _controllerShortcutProvider;
        private readonly Action<string> _controllerShortcutSetter;
        private readonly Func<bool> _controllerShortcutEnabledProvider;
        private readonly Action<bool> _controllerShortcutEnabledSetter;
        private readonly Func<string> _uiScalePresetProvider;
        private readonly Action<string> _uiScalePresetSetter;
        private readonly Func<string> _themeProvider;
        private readonly Action<string> _themeSetter;
        private readonly Func<bool> _startItemsPresetIconsEnabledProvider;
        private readonly Action<bool> _startItemsPresetIconsEnabledSetter;
        private readonly Func<bool> _playerStatsPanelShownProvider;
        private readonly Action<bool> _playerStatsPanelShownSetter;
        private readonly Func<bool> _pickupInfoOverlayEnabledProvider;
        private readonly Action<bool> _pickupInfoOverlayEnabledSetter;
        private readonly Func<bool> _pickupInfoQualityEnabledProvider;
        private readonly Action<bool> _pickupInfoQualityEnabledSetter;
        private readonly Func<bool> _pickupInfoTypeEnabledProvider;
        private readonly Action<bool> _pickupInfoTypeEnabledSetter;
        private readonly Func<bool> _pickupInfoEffectsEnabledProvider;
        private readonly Action<bool> _pickupInfoEffectsEnabledSetter;
        private readonly Func<bool> _pickupInfoSynergiesEnabledProvider;
        private readonly Action<bool> _pickupInfoSynergiesEnabledSetter;
        private readonly Func<bool> _pickupInfoSummaryEnabledProvider;
        private readonly Action<bool> _pickupInfoSummaryEnabledSetter;
        private readonly Func<bool> _pickupInfoNotesEnabledProvider;
        private readonly Action<bool> _pickupInfoNotesEnabledSetter;
        private readonly Func<bool> _experimentalModeProvider;
        private readonly Action<bool> _experimentalModeSetter;
        private readonly Action<bool> _ammonomiconFastOpenEnabledSetter;
        private readonly Func<bool> _mapTeleportVerboseLoggingEnabledProvider;
        private readonly Func<bool> _floorTeleportVerboseLoggingEnabledProvider;
        private readonly Func<bool> _commandPanelHealthVerboseLoggingEnabledProvider;
        private readonly Func<bool> _commandPanelCursorVerboseLoggingEnabledProvider;
        private readonly Func<bool> _commandPanelGameplayInputVerboseLoggingEnabledProvider;
        private readonly Func<bool> _commandPanelControllerGameplayInputVerboseLoggingEnabledProvider;
        private readonly Func<bool> _commandPanelShortcutVerboseLoggingEnabledProvider;
        private readonly Func<string> _combatCursorColorProvider;
        private readonly Action<string> _combatCursorColorSetter;
        private readonly Func<bool> _performanceVerboseLoggingEnabledProvider;
        private readonly BepInEx.Logging.ManualLogSource _performanceLogger;
        private readonly Func<EtgFloorDefinition, string, string, bool> _deferredTeleportRequestHandler;

        private GUIStyle _panelStyle;
        private GUIStyle _playerStatsPanelStyle;
        private GUIStyle _playerStatsRowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _combatSettingLabelStyle;
        private GUIStyle _playerStatsTextStyle;
        private GUIStyle _wrappedHintStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _cursorColorSelectedButtonStyle;
        private GUIStyle _enabledButtonStyle;
        private GUIStyle _pickupGrantButtonStyle;
        private GUIStyle _disabledToggleButtonStyle;
        private GUIStyle _commandCategoryButtonStyle;
        private GUIStyle _commandCategoryFocusButtonStyle;
        private GUIStyle _commandCategoryActiveButtonStyle;
        private GUIStyle _commandContentButtonStyle;
        private GUIStyle _commandContentFocusButtonStyle;
        private GUIStyle _commandContentActiveButtonStyle;
        private GUIStyle _commandContentActiveFocusButtonStyle;
        private GUIStyle _headerActionButtonStyle;
        private GUIStyle _headerActionFocusButtonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _statusSuccessStyle;
        private GUIStyle _statusErrorStyle;
        private GUIStyle _statusWarningStyle;
        private GUIStyle _statusInformationStyle;
        private GUIStyle _pickupRowStyle;
        private GUIStyle _loadoutEditorRowStyle;
        private GUIStyle _pickupBrowserRowStyle;
        private GUIStyle _activePresetRowStyle;
        private GUIStyle _pickupRowButtonStyle;
        private GUIStyle _pickupPrimaryTextStyle;
        private GUIStyle _pickupSecondaryTextStyle;
        private GUIStyle _pickupSecondaryActiveTextStyle;
        private GUIStyle _activePresetAccentTextStyle;
        private GUIStyle _pickupFilterButtonStyle;
        private GUIStyle _pickupFilterFocusButtonStyle;
        private GUIStyle _pickupFilterActiveButtonStyle;
        private GUIStyle _pickupFilterActiveFocusButtonStyle;
        private GUIStyle _pickupFilterDisabledButtonStyle;
        private GUIStyle _pickupIconBackgroundStyle;
        private GUIStyle _pickupIconFallbackStyle;
        private GUIStyle _modalOverlayStyle;
        private GUIStyle _modalPanelStyle;
        private GUIStyle _modalBodyStyle;
        private GUIStyle _settingsInfoTextStyle;
        private GUIStyle _controllerHelpTitleStyle;
        private GUIStyle _controllerHelpTextStyle;
        private bool _isVisible;
        private bool _showTeleportPanel;
        private bool _showPlayerStatsPanel;
        private bool _showPickupInfoOverlay;
        private bool _showPickupInfoQuality;
        private bool _showPickupInfoType;
        private bool _showPickupInfoEffects;
        private bool _showPickupInfoSynergies;
        private bool _showPickupInfoSummary;
        private bool _showPickupInfoNotes;
        private bool _showExperimentalModeConfirmDialog;
        private bool _focusInputField;
        private bool _focusPickupSearchField;
        private bool _releaseGuiFocusPending;
        private PanelPage _currentPage;
        private string _commandPageFocusedControlId = "cmd.settings";
        private string _settingsPageFocusedControlId = "settings.toggle_key";
        private string _pickupInfoConfigFocusedControlId = "pickup_info_config.quality";
        private string _characterPageFocusedControlId = "characters.mode";
        private string _loadoutEditorFocusedControlId = "loadout.back";
        private string _pickupPageFocusedControlId = "pickups.back";
        private string _currencyPageFocusedControlId = "currency.max_health";
        private string _cursorColorPageFocusedControlId = "cursor_color.back";
        private string _cursorColorCustomHexText = string.Empty;
        private ControllerNavDirection? _heldKeyboardNavigationDirection;
        private string _lastGuiLanguageCode = string.Empty;
        private string _revealMapActivatedSceneName = string.Empty;
        private string _mapDirectTeleportActivatedSceneName = string.Empty;
        private float _nextMapDirectTeleportDebugLogAt;
        private string _lastMapDirectTeleportRoomKey = string.Empty;
        private string _inputText = string.Empty;
        private string _statusMessage = string.Empty;
        private StatusSeverity _statusSeverity;
        private float _statusExpiresAt;
        private string _lastCharacterAvailabilityLog = string.Empty;
        private CharacterActionMode _characterActionMode = CharacterActionMode.SwitchOnly;
        private CharacterSwitchTarget _characterSwitchTarget = CharacterSwitchTarget.PrimaryPlayer;
        private RoomChestTier _selectedRoomChestTier = RoomChestTier.Brown;
        private RoomMenuSection _roomMenuSection = RoomMenuSection.Chest;
        private RoomEnemyRefreshMethod _roomEnemyRefreshMethod = RoomEnemyRefreshMethod.Rewind;
        private PlayerMenuSection _playerMenuSection = PlayerMenuSection.Character;
        private CharacterMenuSection _characterMenuSection = CharacterMenuSection.Pickups;
        private FoyerCharacterOption[] _cachedCharacterOptions = EmptyCharacterOptions;
        private string _cachedCharacterAvailability = string.Empty;
        private float _nextCharacterPageRefreshAt;
        private float _nextKeyboardNavigationRepeatAt;
        private PickupBrowserEntry[] _cachedPickupEntries = EmptyPickupBrowserEntries;
        private LoadoutRuleEditorEntry[] _cachedLoadoutRuleEntries = EmptyLoadoutRuleEditorEntries;
        private LoadoutPresetEditorEntry[] _cachedLoadoutPresetEntries = EmptyLoadoutPresetEditorEntries;
        private LoadoutRandomPoolEditorEntry[] _cachedLoadoutRandomPoolEntries = EmptyLoadoutRandomPoolEditorEntries;
        private LoadoutRuleEditorEntry[] _cachedLoadoutPickupEntries = EmptyLoadoutPickupEditorEntries;
        private LoadoutEditorMode _loadoutEditorMode = LoadoutEditorMode.PresetList;
        private CommandMenuCategory _commandMenuCategory = CommandMenuCategory.General;
        private string _loadoutPresetRenameText = string.Empty;
        private string _loadoutRandomPoolRenameText = string.Empty;
        private string _loadoutPickupCountEditText = string.Empty;
        private int _loadoutRandomPoolRuleIndex = -1;
        private int _loadoutPickupCountEditIndex = -1;
        private PickupBrowserFilter _pickupBrowserFilter = PickupBrowserFilter.All;
        private PickupBrowserMode _pickupBrowserMode = PickupBrowserMode.Grant;
        private PickupQualityFilter _pickupQualityFilter = PickupQualityFilter.All;
        private PickupGunClassFilter _pickupGunClassFilter = PickupGunClassFilter.All;
        private PickupPassiveSubcategoryFilter _pickupPassiveSubcategoryFilter = PickupPassiveSubcategoryFilter.All;
        private PickupActiveCooldownFilter _pickupActiveCooldownFilter = PickupActiveCooldownFilter.All;
        private string _pickupSearchText = string.Empty;
        private PickupBrowserEntry[] _filteredPickupEntriesCache;
        private string _filteredPickupEntriesCacheSearch = string.Empty;
        private PickupBrowserFilter _filteredPickupEntriesCacheFilter;
        private PickupQualityFilter _filteredPickupEntriesCacheQualityFilter;
        private PickupGunClassFilter _filteredPickupEntriesCacheGunClassFilter;
        private PickupPassiveSubcategoryFilter _filteredPickupEntriesCachePassiveFilter;
        private PickupActiveCooldownFilter _filteredPickupEntriesCacheActiveCooldownFilter;
        private Vector2 _pickupScrollPosition = Vector2.zero;
        private Vector2 _loadoutEditorScrollPosition = Vector2.zero;
        private Vector2 _loadoutPresetScrollPosition = Vector2.zero;
        private int _teleportSelectedIndex;
        private float _lastLoggedControllerHorizontalAxis = float.NaN;
        private float _lastLoggedControllerVerticalAxis = float.NaN;
        private float _lastLoggedControllerDpadHorizontalAxis = float.NaN;
        private float _lastLoggedControllerDpadVerticalAxis = float.NaN;
        private float _lastLoggedControllerLeftStickHorizontalAxis = float.NaN;
        private float _lastLoggedControllerLeftStickVerticalAxis = float.NaN;
        private float _lastLoggedControllerRightStickHorizontalAxis = float.NaN;
        private float _lastLoggedControllerRightStickVerticalAxis = float.NaN;
        private bool _hasLoggedCursorVisibilityState;
        private bool _lastLoggedCursorVisible;
        private CursorLockMode _lastLoggedCursorLockMode;
        private string _lastLoggedActiveInputDeviceName = string.Empty;
        private string _lastLoggedActiveInputDeviceClass = string.Empty;
        private bool _wasControllerHorizontalNavigationActive;
        private bool _wasControllerVerticalNavigationActive;
        private bool _hasLoggedGameplayInputState;
        private bool _lastLoggedGameplayPanelVisible;
        private bool _lastLoggedGameplayW;
        private bool _lastLoggedGameplayA;
        private bool _lastLoggedGameplayS;
        private bool _lastLoggedGameplayD;
        private bool _lastLoggedGameplayInputOverridden;
        private string _lastLoggedGameplayInputState = string.Empty;
        private bool _hasLoggedControllerGameplayInputState;
        private bool _lastLoggedControllerGameplayPanelVisible;
        private bool _lastLoggedControllerGameplayInputOverridden;
        private string _lastLoggedControllerGameplayInputState = string.Empty;
        private string _lastLoggedControllerGameplayDevice = string.Empty;
        private float _lastLoggedControllerGameplayDpadHorizontal = float.NaN;
        private float _lastLoggedControllerGameplayDpadVertical = float.NaN;
        private float _lastLoggedControllerGameplayLeftStickHorizontal = float.NaN;
        private float _lastLoggedControllerGameplayLeftStickVertical = float.NaN;
        private float _lastLoggedControllerGameplayRightStickHorizontal = float.NaN;
        private float _lastLoggedControllerGameplayRightStickVertical = float.NaN;
        private bool _hasLoggedCommandPanelShortcutState;
        private bool _lastLoggedCommandPanelKeyboardHeld;
        private bool _lastLoggedCommandPanelKeyboardDown;
        private bool _lastLoggedCommandPanelControllerDetected;
        private bool _lastLoggedCommandPanelVisible;
        private PlayerController _panelInputOverridePlayer;
        private PlayerController _lastHealthDiagnosticPlayer;
        private float _lastHealthDiagnosticCurrentHealth = float.NaN;
        private float _lastHealthDiagnosticMaxHealth = float.NaN;
        private float _lastHealthDiagnosticArmor = float.NaN;
        private int _lastHealthDiagnosticGunId = -1;
        private string _lastHealthDiagnosticGunName = string.Empty;
        private readonly bool[] _wasJoystickButtonPressed = new bool[20];
        private float _controllerShortcutR3PressedAt = -1f;
        private bool _controllerShortcutHoldTriggered;
        private readonly Dictionary<int, PickupIconData> _pickupIconCache = new Dictionary<int, PickupIconData>();
        private readonly HashSet<int> _pickupNameDiagnosticsLogged = new HashSet<int>();
        private readonly HashSet<PlayerController> _autoReloadTargetStates = new HashSet<PlayerController>();
        private readonly HashSet<PlayerController> _ammoModeTargetStates = new HashSet<PlayerController>();
        private dfAtlas _gameUiAtlas;
        private bool _hasResolvedGameUiAtlas;
        private static readonly string[] CommandPanelKeyOptions =
        {
            "F7",
            "F8",
            "F9",
            "Insert",
            "BackQuote",
        };
        private static readonly string[] ControllerShortcutOptions = { "LB+R3", "LB+X", "LB+Y", "R3" };
        private sealed class TeleportOption
        {
            public TeleportOption(string commandToken, string labelKey, string commandText)
            {
                CommandToken = commandToken ?? string.Empty;
                LabelKey = labelKey ?? string.Empty;
                CommandText = commandText ?? string.Empty;
            }

            public string CommandToken { get; private set; }

            public string LabelKey { get; private set; }

            public string CommandText { get; private set; }
        }

        private struct ControllerFocusEntry
        {
            public ControllerFocusEntry(string controlId, int row, int column)
            {
                ControlId = controlId ?? string.Empty;
                Row = row;
                Column = column;
            }

            public string ControlId;

            public int Row;

            public int Column;
        }
    }
}
