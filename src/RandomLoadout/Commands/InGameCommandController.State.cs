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
        }

        private enum CommandMenuCategory
        {
            General,
            Combat,
            Player,
        }

        private const KeyCode ToggleKey = KeyCode.F7;
        private const string InputControlName = "RandomLoadoutCommandInput";
        private const string PickupSearchControlName = "RandomLoadoutPickupSearch";
        private const float StatusDurationSeconds = 4f;
        private const float PanelWidth = 612f;
        private const float BasePanelHeight = 284f;
        private const float PlayerStatsPanelWidth = 244f;
        private const float PlayerStatsPanelHeight = 376f;
        private const float TeleportPanelWidth = 286f;
        private const float TeleportPanelHeight = 500f;
        private const float PlayerStatsRowHeight = 21f;
        private const float PlayerStatsRowGap = 3f;
        private const float PickupBrowserPanelHeight = 496f;
        private const float LoadoutEditorPanelHeight = 440f;
        private const float AboutPanelHeight = 326f;
        private const float CharacterPanelBaseHeaderHeight = 126f;
        private const float CharacterPanelFooterHeight = 26f;
        private const float CurrencyPanelHeight = 208f;
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
        private static readonly Color PanelBorderColor = new Color(0.97f, 0.63f, 0.10f, 0.96f);
        private static readonly Color InputBackgroundColor = new Color(0.11f, 0.12f, 0.15f, 0.96f);
        private static readonly Color ButtonBackgroundColor = new Color(0.19f, 0.14f, 0.08f, 0.96f);
        private static readonly Color ButtonHoverColor = new Color(0.40f, 0.27f, 0.08f, 0.98f);
        private static readonly Color ButtonActiveColor = new Color(0.78f, 0.50f, 0.10f, 1f);
        private static readonly Color PrimaryTextColor = new Color(0.90f, 0.87f, 0.79f, 1f);
        private static readonly Color PlayerStatsTextColor = Color.white;
        private static readonly Color SecondaryTextColor = new Color(0.65f, 0.62f, 0.54f, 1f);
        private static readonly Color SuccessBackgroundColor = new Color(0.23f, 0.31f, 0.22f, 0.95f);
        private static readonly Color ErrorBackgroundColor = new Color(0.44f, 0.24f, 0.21f, 0.95f);
        private static readonly AmmonomiconAnimationToggleService AmmonomiconAnimationToggleService = new AmmonomiconAnimationToggleService();
        private static readonly FoyerCharacterOption[] EmptyCharacterOptions = new FoyerCharacterOption[0];
        private static readonly PickupBrowserEntry[] EmptyPickupBrowserEntries = new PickupBrowserEntry[0];
        private static readonly LoadoutRuleEditorEntry[] EmptyLoadoutRuleEditorEntries = new LoadoutRuleEditorEntry[0];
        private static readonly LoadoutPresetEditorEntry[] EmptyLoadoutPresetEditorEntries = new LoadoutPresetEditorEntry[0];
        private static readonly LoadoutRandomPoolEditorEntry[] EmptyLoadoutRandomPoolEditorEntries = new LoadoutRandomPoolEditorEntry[0];
        private static readonly TeleportOption[] TeleportOptions =
        {
            new TeleportOption("keep", "tt_castle", "label.teleport.floor.keep", "load_level keep"),
            new TeleportOption("oubliette", "tt_sewer", "label.teleport.floor.oubliette", "load_level oubliette"),
            new TeleportOption("proper", "tt5", "label.teleport.floor.proper", "load_level proper"),
            new TeleportOption("abbey", "tt_cathedral", "label.teleport.floor.abbey", "load_level abbey"),
            new TeleportOption("mine", "tt_mines", "label.teleport.floor.mine", "load_level mine"),
            new TeleportOption("ratden", "tt_resourcefulrat", "label.teleport.floor.ratden", "load_level ratden"),
            new TeleportOption("hollow", "tt_catacombs", "label.teleport.floor.hollow", "load_level hollow"),
            new TeleportOption("R&G_Dept", "tt_nakatomi", "label.teleport.floor.rng_dept", "load_level R&G_Dept"),
            new TeleportOption("forge", "tt_forge", "label.teleport.floor.forge", "load_level forge"),
            new TeleportOption("heli", "tt_bullethell", "label.teleport.floor.heli", "load_level heli"),
        };

        private readonly GrantCommandParser _parser = new GrantCommandParser();
        private readonly GrantCommandService _commandService;
        private readonly PlayerDebugCommandService _playerDebugCommandService;
        private readonly FoyerCharacterSwitchService _foyerCharacterSwitchService;
        private readonly BossRushService _bossRushService;
        private readonly RapidFireToggleService _rapidFireToggleService;
        private readonly AutoReloadToggleService _autoReloadToggleService;
        private readonly InvincibilityToggleService _invincibilityToggleService;
        private readonly NoAmmoConsumptionToggleService _noAmmoConsumptionToggleService;
        private readonly LoadoutRuleEditorService _loadoutRuleEditorService;
        private readonly Func<EtgPickupCatalogEntry[]> _pickupCatalogProvider;
        private readonly Func<PickupAliasRegistry> _aliasRegistryProvider;
        private readonly Func<string> _languageProvider;
        private readonly Action<string> _languageSetter;

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _playerStatsTextStyle;
        private GUIStyle _wrappedHintStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _statusSuccessStyle;
        private GUIStyle _statusErrorStyle;
        private GUIStyle _pickupRowStyle;
        private GUIStyle _pickupRowButtonStyle;
        private GUIStyle _pickupPrimaryTextStyle;
        private GUIStyle _pickupSecondaryTextStyle;
        private GUIStyle _pickupFilterButtonStyle;
        private GUIStyle _pickupFilterActiveButtonStyle;
        private GUIStyle _pickupIconFallbackStyle;

        private bool _isVisible;
        private bool _showTeleportPanel;
        private bool _showPlayerStatsPanel = true;
        private bool _focusInputField;
        private bool _focusPickupSearchField;
        private PanelPage _currentPage;
        private string _lastGuiLanguageCode = string.Empty;
        private string _inputText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private float _statusExpiresAt;
        private string _lastPlayerStatsVisibilityLog = string.Empty;
        private float _nextPlayerStatsVisibilityLogAt;
        private string _lastCharacterAvailabilityLog = string.Empty;
        private CharacterActionMode _characterActionMode = CharacterActionMode.SwitchOnly;
        private FoyerCharacterOption[] _cachedCharacterOptions = EmptyCharacterOptions;
        private string _cachedCharacterAvailability = string.Empty;
        private float _nextCharacterPageRefreshAt;
        private PickupBrowserEntry[] _cachedPickupEntries = EmptyPickupBrowserEntries;
        private LoadoutRuleEditorEntry[] _cachedLoadoutRuleEntries = EmptyLoadoutRuleEditorEntries;
        private LoadoutPresetEditorEntry[] _cachedLoadoutPresetEntries = EmptyLoadoutPresetEditorEntries;
        private LoadoutRandomPoolEditorEntry[] _cachedLoadoutRandomPoolEntries = EmptyLoadoutRandomPoolEditorEntries;
        private LoadoutEditorMode _loadoutEditorMode = LoadoutEditorMode.PresetList;
        private CommandMenuCategory _commandMenuCategory = CommandMenuCategory.General;
        private string _loadoutPresetRenameText = string.Empty;
        private int _loadoutRandomPoolRuleIndex = -1;
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
        private readonly Dictionary<int, PickupIconData> _pickupIconCache = new Dictionary<int, PickupIconData>();

        private sealed class TeleportOption
        {
            public TeleportOption(string commandToken, string sceneName, string labelKey, string commandText)
            {
                CommandToken = commandToken ?? string.Empty;
                SceneName = sceneName ?? string.Empty;
                LabelKey = labelKey ?? string.Empty;
                CommandText = commandText ?? string.Empty;
            }

            public string CommandToken { get; private set; }

            public string SceneName { get; private set; }

            public string LabelKey { get; private set; }

            public string CommandText { get; private set; }
        }
    }
}
