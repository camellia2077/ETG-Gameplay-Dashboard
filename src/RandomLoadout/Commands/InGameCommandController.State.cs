using System;
using System.Collections.Generic;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
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
            ControllerHelp,
        }

        private enum CharacterActionMode
        {
            SwitchOnly,
            Unlock,
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
            Combat,
            Player,
            Room,
        }

        private enum RoomMenuSection
        {
            Chest,
            Neutral,
            Enemies,
            State,
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
        private const float StatusDurationSeconds = 4f;
        private const float ReferenceScreenWidth = 1920f;
        private const float ReferenceScreenHeight = 1080f;
        private const float MinimumUiScale = 0.70f;
        private const float MaximumUiScale = 1.50f;
        private const float PanelWidth = 612f;
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
        private const float SettingsPanelHeight = 608f;
        private const float ControllerHelpPanelHeight = 332f;
        private const float CharacterPanelBaseHeaderHeight = 126f;
        private const float CharacterPanelFooterHeight = 26f;
        private const float CurrencyPanelHeight = 252f;
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
        private const float LoadoutRuleRowHeight = 60f;
        private const float PickupIconSize = 32f;
        private const float PickupGrantButtonWidth = 72f;

        private static readonly Color PanelBackgroundColor = new Color(0.07f, 0.08f, 0.10f, 0.88f);
        private static readonly Color PlayerStatsPanelBackgroundColor = new Color(0.03f, 0.04f, 0.05f, 0.97f);
        private static readonly Color PlayerStatsRowBackgroundColor = new Color(0.01f, 0.01f, 0.02f, 0.78f);
        private static readonly Color PanelBorderColor = new Color(0.97f, 0.63f, 0.10f, 0.96f);
        private static readonly Color InputBackgroundColor = new Color(0.11f, 0.12f, 0.15f, 0.96f);
        private static readonly Color ButtonBackgroundColor = new Color(0.19f, 0.14f, 0.08f, 0.96f);
        private static readonly Color ButtonHoverColor = new Color(0.40f, 0.27f, 0.08f, 0.98f);
        private static readonly Color ButtonActiveColor = new Color(0.78f, 0.50f, 0.10f, 1f);
        private static readonly Color EnabledButtonBackgroundColor = new Color(0.69f, 0.45f, 0.10f, 0.98f);
        private static readonly Color EnabledButtonHoverColor = new Color(0.84f, 0.55f, 0.10f, 1f);
        private static readonly Color EnabledButtonActiveColor = new Color(0.97f, 0.63f, 0.10f, 1f);
        private static readonly Color PrimaryTextColor = new Color(0.90f, 0.87f, 0.79f, 1f);
        private static readonly Color PlayerStatsTextColor = Color.white;
        private static readonly Color SecondaryTextColor = new Color(0.65f, 0.62f, 0.54f, 1f);
        private static readonly Color SuccessBackgroundColor = new Color(0.23f, 0.31f, 0.22f, 0.95f);
        private static readonly Color ErrorBackgroundColor = new Color(0.44f, 0.24f, 0.21f, 0.95f);
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
            CommandMenuCategory.Combat,
            CommandMenuCategory.Player,
            CommandMenuCategory.Room,
        };

        private static readonly ControllerFocusEntry[] CommandPageSharedFocusEntries =
        {
            new ControllerFocusEntry("cmd.about", 0, 0),
            new ControllerFocusEntry("cmd.settings", 0, 1),
            new ControllerFocusEntry("cmd.language", 0, 2),
            new ControllerFocusEntry("cmd.category.general", 1, 0),
            new ControllerFocusEntry("cmd.category.combat", 1, 1),
            new ControllerFocusEntry("cmd.category.player", 1, 2),
            new ControllerFocusEntry("cmd.category.room", 1, 3),
        };

        private static readonly ControllerFocusEntry[] GeneralCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.general.pickups", 2, 0),
            new ControllerFocusEntry("cmd.general.loadout", 2, 1),
            new ControllerFocusEntry("cmd.general.currency", 2, 2),
            new ControllerFocusEntry("cmd.general.teleport", 2, 3),
            new ControllerFocusEntry("cmd.general.characters", 3, 0),
            new ControllerFocusEntry("cmd.general.boss_rush", 3, 1),
            new ControllerFocusEntry("cmd.general.reveal_map", 3, 2),
        };

        private static readonly ControllerFocusEntry[] CombatCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.combat.rapid", 2, 0),
            new ControllerFocusEntry("cmd.combat.auto_reload", 2, 1),
            new ControllerFocusEntry("cmd.combat.ammo_mode", 3, 0),
            new ControllerFocusEntry("cmd.combat.invincible", 3, 1),
            new ControllerFocusEntry("cmd.combat.ammonomicon", 4, 0),
            new ControllerFocusEntry("cmd.combat.full_ammo", 4, 1),
        };

        private static readonly ControllerFocusEntry[] PlayerCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.player.heal_half", 2, 0),
            new ControllerFocusEntry("cmd.player.full_heal", 2, 1),
            new ControllerFocusEntry("cmd.player.add_max_health", 2, 2),
            new ControllerFocusEntry("cmd.player.add_armor", 2, 3),
            new ControllerFocusEntry("cmd.player.add_blank", 3, 0),
            new ControllerFocusEntry("cmd.player.refill_blanks", 3, 1),
            new ControllerFocusEntry("cmd.player.clear_curse", 4, 0),
            new ControllerFocusEntry("cmd.player.stats", 4, 1),
        };

        private static readonly ControllerFocusEntry[] RoomSectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.section.chest", 2, 0),
            new ControllerFocusEntry("cmd.room.section.neutral", 2, 1),
            new ControllerFocusEntry("cmd.room.section.enemies", 2, 2),
            new ControllerFocusEntry("cmd.room.section.state", 2, 3),
        };

        private static readonly ControllerFocusEntry[] RoomChestOnlySectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.section.chest", 2, 0),
            new ControllerFocusEntry("cmd.room.section.neutral", 2, 1),
        };

        private static readonly ControllerFocusEntry[] RoomChestCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.chest_tier.brown", 3, 0),
            new ControllerFocusEntry("cmd.room.chest_tier.blue", 3, 1),
            new ControllerFocusEntry("cmd.room.chest_tier.green", 3, 2),
            new ControllerFocusEntry("cmd.room.chest_tier.red", 3, 3),
            new ControllerFocusEntry("cmd.room.chest_tier.black", 4, 0),
            new ControllerFocusEntry("cmd.room.chest_tier.synergy", 4, 1),
            new ControllerFocusEntry("cmd.room.chest_tier.rainbow", 4, 2),
        };

        private static readonly ControllerFocusEntry[] RoomNeutralCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.spawn_gunber_muncher", 3, 0),
            new ControllerFocusEntry("cmd.room.spawn_evil_muncher", 3, 1),
        };

        private static readonly ControllerFocusEntry[] RoomEnemiesCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.refresh_enemies", 3, 0),
        };

        private static readonly ControllerFocusEntry[] SettingsPageFocusEntries =
        {
            new ControllerFocusEntry("settings.back", 0, 0),
            new ControllerFocusEntry("settings.toggle_key", 1, 0),
            new ControllerFocusEntry("settings.controller_help", 2, 0),
            new ControllerFocusEntry("settings.ui_scale", 3, 0),
            new ControllerFocusEntry("settings.language", 4, 0),
            new ControllerFocusEntry("settings.experimental_mode", 5, 0),
        };

        private readonly GrantCommandParser _parser = new GrantCommandParser();
        private readonly GrantCommandService _commandService;
        private readonly PlayerDebugCommandService _playerDebugCommandService;
        private readonly RoomDebugCommandService _roomDebugCommandService;
        private readonly FoyerCharacterSwitchService _foyerCharacterSwitchService;
        private readonly BossRushService _bossRushService;
        private readonly RapidFireToggleService _rapidFireToggleService;
        private readonly AutoReloadToggleService _autoReloadToggleService;
        private readonly InvincibilityToggleService _invincibilityToggleService;
        private readonly AmmoModeToggleService _ammoModeToggleService;
        private readonly LoadoutRuleEditorService _loadoutRuleEditorService;
        private readonly Func<EtgPickupCatalogEntry[]> _pickupCatalogProvider;
        private readonly Func<PickupAliasRegistry> _aliasRegistryProvider;
        private readonly Func<string> _languageProvider;
        private readonly Action<string> _languageSetter;
        private readonly Action<string> _inputLogHandler;
        private readonly Func<KeyCode> _toggleKeyProvider;
        private readonly Func<string> _toggleKeyNameProvider;
        private readonly Action<string> _toggleKeySetter;
        private readonly Func<string> _uiScalePresetProvider;
        private readonly Action<string> _uiScalePresetSetter;
        private readonly Func<bool> _playerStatsPanelShownProvider;
        private readonly Action<bool> _playerStatsPanelShownSetter;
        private readonly Func<bool> _experimentalModeProvider;
        private readonly Action<bool> _experimentalModeSetter;
        private readonly Action<bool> _ammonomiconFastOpenEnabledSetter;
        private readonly Func<bool> _mapTeleportVerboseLoggingEnabledProvider;
        private readonly Func<bool> _floorTeleportVerboseLoggingEnabledProvider;
        private readonly Func<EtgFloorDefinition, string, string, bool> _deferredTeleportRequestHandler;

        private GUIStyle _panelStyle;
        private GUIStyle _playerStatsPanelStyle;
        private GUIStyle _playerStatsRowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _playerStatsTextStyle;
        private GUIStyle _wrappedHintStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _enabledButtonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _statusSuccessStyle;
        private GUIStyle _statusErrorStyle;
        private GUIStyle _pickupRowStyle;
        private GUIStyle _pickupRowButtonStyle;
        private GUIStyle _pickupPrimaryTextStyle;
        private GUIStyle _pickupSecondaryTextStyle;
        private GUIStyle _pickupSecondaryActiveTextStyle;
        private GUIStyle _pickupFilterButtonStyle;
        private GUIStyle _pickupFilterActiveButtonStyle;
        private GUIStyle _pickupFilterDisabledButtonStyle;
        private GUIStyle _pickupIconFallbackStyle;
        private GUIStyle _modalOverlayStyle;
        private GUIStyle _modalPanelStyle;
        private GUIStyle _modalBodyStyle;
        private GUIStyle _settingsInfoTextStyle;
        private GUIStyle _controllerHelpTitleStyle;
        private GUIStyle _controllerHelpTextStyle;
        private GUIStyle _scrollViewStyle;
        private GUIStyle _scrollbarBackgroundStyle;
        private GUIStyle _verticalScrollbarThumbStyle;
        private GUIStyle _horizontalScrollbarThumbStyle;
        private GUIStyle _verticalScrollbarButtonStyle;
        private GUIStyle _horizontalScrollbarButtonStyle;

        private bool _isVisible;
        private bool _showTeleportPanel;
        private bool _showPlayerStatsPanel;
        private bool _showExperimentalModeConfirmDialog;
        private bool _focusInputField;
        private bool _focusPickupSearchField;
        private bool _releaseGuiFocusPending;
        private PanelPage _currentPage;
        private string _commandPageFocusedControlId = "cmd.settings";
        private string _settingsPageFocusedControlId = "settings.toggle_key";
        private string _lastGuiLanguageCode = string.Empty;
        private string _revealMapActivatedSceneName = string.Empty;
        private string _mapDirectTeleportActivatedSceneName = string.Empty;
        private float _nextMapDirectTeleportDebugLogAt;
        private string _lastMapDirectTeleportRoomKey = string.Empty;
        private string _inputText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private float _statusExpiresAt;
        private string _lastCharacterAvailabilityLog = string.Empty;
        private CharacterActionMode _characterActionMode = CharacterActionMode.SwitchOnly;
        private RoomChestTier _selectedRoomChestTier = RoomChestTier.Brown;
        private RoomMenuSection _roomMenuSection = RoomMenuSection.Chest;
        private FoyerCharacterOption[] _cachedCharacterOptions = EmptyCharacterOptions;
        private string _cachedCharacterAvailability = string.Empty;
        private float _nextCharacterPageRefreshAt;
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
        private Vector2 _pickupScrollPosition = Vector2.zero;
        private Vector2 _loadoutEditorScrollPosition = Vector2.zero;
        private Vector2 _loadoutPresetScrollPosition = Vector2.zero;
        private int _teleportSelectedIndex;
        private float _lastLoggedControllerHorizontalAxis = float.NaN;
        private float _lastLoggedControllerVerticalAxis = float.NaN;
        private bool _wasControllerHorizontalNavigationActive;
        private bool _wasControllerVerticalNavigationActive;
        private readonly bool[] _wasJoystickButtonPressed = new bool[20];
        private readonly Dictionary<int, PickupIconData> _pickupIconCache = new Dictionary<int, PickupIconData>();
        private static readonly string[] CommandPanelKeyOptions =
        {
            "F7",
            "F8",
            "F9",
            "Insert",
            "BackQuote",
        };
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
