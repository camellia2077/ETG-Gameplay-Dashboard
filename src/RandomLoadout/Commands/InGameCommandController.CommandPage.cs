using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void DrawCommandPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            const float controlHeight = 34f;
            const float categoryButtonWidth = 92f;
            const float categoryButtonHeight = 28f;
            const float contentButtonWidth = 132f;
            Rect languageButtonRect = new Rect(panelRect.x + panelRect.width - LanguageButtonWidth - 14f, panelRect.y + 12f, LanguageButtonWidth, 30f);
            Rect settingsButtonRect = new Rect(languageButtonRect.x - ButtonGap - ButtonWidth, languageButtonRect.y, ButtonWidth, 30f);
            Rect aboutButtonRect = new Rect(settingsButtonRect.x - ButtonGap - ButtonWidth, languageButtonRect.y, ButtonWidth, 30f);

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, aboutButtonRect.x - panelRect.x - 28f, 24f),
                GuiText.Get("gui.command.title"),
                _titleStyle);
            if (DrawControllerButton(aboutButtonRect, "cmd.about", GuiText.Get("gui.command.button.about"), _buttonStyle))
            {
                OpenAboutPage();
            }

            if (DrawControllerButton(settingsButtonRect, "cmd.settings", GuiText.Get("gui.command.button.settings"), _buttonStyle))
            {
                OpenSettingsPage();
            }

            if (DrawControllerButton(languageButtonRect, "cmd.language", GetLanguageButtonLabel(), _buttonStyle))
            {
                ExecuteToggleLanguage(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.toggle"),
                _hintStyle);

            float categoryTop = panelRect.y + 72f;
            float segmentLeft = panelRect.x + 14f;
            Rect generalCategoryButtonRect = new Rect(segmentLeft, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect combatCategoryButtonRect = new Rect(generalCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect playerCategoryButtonRect = new Rect(combatCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            Rect roomCategoryButtonRect = new Rect(playerCategoryButtonRect.xMax + 2f, categoryTop, categoryButtonWidth, categoryButtonHeight);
            DrawCommandCategoryButton(generalCategoryButtonRect, "cmd.category.general", CommandMenuCategory.General, GuiText.Get("gui.command.category.general"));
            DrawCommandCategoryButton(combatCategoryButtonRect, "cmd.category.combat", CommandMenuCategory.Combat, GuiText.Get("gui.command.category.combat"));
            DrawCommandCategoryButton(playerCategoryButtonRect, "cmd.category.player", CommandMenuCategory.Player, GuiText.Get("gui.command.category.player"));
            DrawCommandCategoryButton(roomCategoryButtonRect, "cmd.category.room", CommandMenuCategory.Room, GetLocalizedFallback("gui.command.category.room", "Room", "房间"));

            Rect contentRect = new Rect(panelRect.x + 14f, generalCategoryButtonRect.yMax + 14f, panelRect.width - 28f, panelRect.height - (generalCategoryButtonRect.yMax - panelRect.y) - 56f);
            DrawCommandCategoryContent(contentRect, contentButtonWidth, controlHeight, player, logger);

        }

        private float GetCommandPanelHeight()
        {
            const float contentTopOffset = 114f;
            const float controlHeight = 34f;
            const float footerReserveHeight = 42f;
            int rowCount = GetCommandCategoryRowCount();
            float contentHeight = (rowCount * controlHeight) + (Mathf.Max(0, rowCount - 1) * ButtonGap);
            return Mathf.Max(BasePanelHeight, contentTopOffset + contentHeight + footerReserveHeight);
        }

        private int GetCommandCategoryRowCount()
        {
            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.Combat:
                    return 3;
                case CommandMenuCategory.Player:
                    return 3;
                case CommandMenuCategory.Room:
                    return 5;
                case CommandMenuCategory.General:
                default:
                    return 2;
            }
        }

        private void DrawCommandCategoryButton(Rect rect, string controlId, CommandMenuCategory category, string label)
        {
            GUIStyle style = _commandMenuCategory == category || IsControllerFocusActive("cmd", controlId)
                ? _pickupFilterActiveButtonStyle
                : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _commandMenuCategory = category;
            }
        }

        private void DrawCommandCategoryContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float secondColumnX = contentRect.x + buttonWidth + ButtonGap;
            float thirdColumnX = secondColumnX + buttonWidth + ButtonGap;
            float fourthColumnX = thirdColumnX + buttonWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

            if (_commandMenuCategory == CommandMenuCategory.General)
            {
                if (DrawControllerButton(new Rect(contentRect.x, firstRowY, buttonWidth, controlHeight), "cmd.general.pickups", GuiText.Get("gui.command.button.pickups"), _buttonStyle))
                {
                    OpenPickupPage(logger);
                }

                if (DrawControllerButton(new Rect(secondColumnX, firstRowY, buttonWidth, controlHeight), "cmd.general.loadout", GuiText.Get("gui.command.button.loadout"), _buttonStyle))
                {
                    OpenLoadoutEditorPage(logger);
                }

                if (DrawControllerButton(new Rect(thirdColumnX, firstRowY, buttonWidth, controlHeight), "cmd.general.currency", GuiText.Get("gui.command.button.currency"), _buttonStyle))
                {
                    OpenCurrencyPage(logger);
                }

                if (DrawControllerButton(new Rect(fourthColumnX, firstRowY, buttonWidth, controlHeight), "cmd.general.teleport", GuiText.Get("gui.command.button.teleport"), _buttonStyle))
                {
                    ToggleTeleportPanel();
                }

                if (DrawControllerButton(new Rect(contentRect.x, secondRowY, buttonWidth, controlHeight), "cmd.general.characters", GuiText.Get("gui.command.button.characters"), _buttonStyle))
                {
                    OpenCharacterPage(logger);
                }

                if (DrawControllerButton(new Rect(secondColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.boss_rush", GuiText.Get("gui.command.button.boss_rush"), _buttonStyle))
                {
                    OpenBossRushPage(logger);
                }

                GUIStyle revealMapButtonStyle = IsRevealMapActive() ? _enabledButtonStyle : _buttonStyle;
                if (DrawControllerButton(new Rect(thirdColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.reveal_map", GetLocalizedFallback("gui.command.button.reveal_map", "Reveal Map", "地图全开"), revealMapButtonStyle))
                {
                    ExecuteRevealCurrentFloorMap(player, logger);
                }

                if (DrawControllerButton(new Rect(fourthColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.random_item", GuiText.Get("gui.command.button.random"), _buttonStyle))
                {
                    ExecuteRandom(player, logger);
                }

                return;
            }

            if (_commandMenuCategory == CommandMenuCategory.Combat)
            {
                DrawCombatSettings(contentRect, controlHeight, player, logger);

                return;
            }

            if (_commandMenuCategory == CommandMenuCategory.Room)
            {
                DrawRoomContent(contentRect, buttonWidth, controlHeight, player, logger);
                return;
            }

            if (DrawControllerButton(new Rect(contentRect.x, firstRowY, buttonWidth, controlHeight), "cmd.player.heal_half", GuiText.Get("gui.command.button.heal_half"), _buttonStyle))
            {
                ExecuteHealHalfHeart(player, logger);
            }

            if (DrawControllerButton(new Rect(secondColumnX, firstRowY, buttonWidth, controlHeight), "cmd.player.full_heal", GuiText.Get("gui.command.button.full_heal"), _buttonStyle))
            {
                ExecuteFullHeal(player, logger);
            }

            if (DrawControllerButton(new Rect(thirdColumnX, firstRowY, buttonWidth, controlHeight), "cmd.player.add_max_health", GetLocalizedFallback("gui.command.button.add_max_health", "+1 Max HP", "+1 血量上限"), _buttonStyle))
            {
                ExecuteAddMaxHealth(player, logger);
            }

            if (DrawControllerButton(new Rect(fourthColumnX, firstRowY, buttonWidth, controlHeight), "cmd.player.add_armor", GuiText.Get("gui.command.button.add_armor"), _buttonStyle))
            {
                ExecuteAddArmor(player, logger);
            }

            if (DrawControllerButton(new Rect(contentRect.x, secondRowY, buttonWidth, controlHeight), "cmd.player.add_blank", GetLocalizedFallback("gui.command.button.add_blank", "+1 Blank", "+1 空包弹"), _buttonStyle))
            {
                ExecuteAddBlank(player, logger);
            }

            if (DrawControllerButton(new Rect(secondColumnX, secondRowY, buttonWidth, controlHeight), "cmd.player.refill_blanks", GuiText.Get("gui.command.button.refill_blanks"), _buttonStyle))
            {
                ExecuteRefillBlanks(player, logger);
            }

            if (DrawControllerButton(new Rect(contentRect.x, thirdRowY, buttonWidth, controlHeight), "cmd.player.clear_curse", GuiText.Get("gui.command.button.clear_curse"), _buttonStyle))
            {
                ExecuteClearCurse(player, logger);
            }

            string statsButtonLabel = _showPlayerStatsPanel
                ? GuiText.Get("gui.command.button.stats_on")
                : GuiText.Get("gui.command.button.stats_off");
            GUIStyle statsButtonStyle = _showPlayerStatsPanel ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(secondColumnX, thirdRowY, buttonWidth, controlHeight), "cmd.player.stats", statsButtonLabel, statsButtonStyle))
            {
                SetPlayerStatsPanelShown(!_showPlayerStatsPanel);
            }
        }

        private void DrawCombatSettings(Rect contentRect, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float settingColumnWidth = 270f;
            const float settingLabelWidth = 146f;
            const float settingButtonWidth = 116f;
            float secondSettingColumnX = contentRect.x + settingColumnWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

            DrawCombatSettingRows(
                new[]
                {
                    CreateRapidFireCombatSetting(
                        new Rect(contentRect.x, firstRowY, settingColumnWidth, controlHeight),
                        player,
                        logger),
                    CreateAutoReloadCombatSetting(
                        new Rect(secondSettingColumnX, firstRowY, settingColumnWidth, controlHeight),
                        logger),
                    CreateAmmoModeCombatSetting(
                        new Rect(contentRect.x, secondRowY, settingColumnWidth, controlHeight),
                        logger),
                    CreateInvincibilityCombatSetting(
                        new Rect(secondSettingColumnX, secondRowY, settingColumnWidth, controlHeight),
                        player,
                        logger),
                    CreateAmmonomiconCombatSetting(
                        new Rect(contentRect.x, thirdRowY, settingColumnWidth, controlHeight),
                        logger),
                },
                settingLabelWidth,
                settingButtonWidth);

            if (DrawControllerButton(new Rect(secondSettingColumnX + settingLabelWidth, thirdRowY, settingButtonWidth, controlHeight), "cmd.combat.full_ammo", GuiText.Get("gui.command.button.full_ammo"), _buttonStyle))
            {
                ExecuteRefillCurrentGunAmmo(player, logger);
            }
        }

        private CombatSettingRow CreateRapidFireCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            bool isEnabled = IsRapidFireEnabledFor(player);
            return new CombatSettingRow(
                rect,
                "cmd.combat.rapid",
                GetLocalizedFallback("gui.command.setting.rapid", "Hold Rapid", "\u6309\u4f4f\u8fde\u53d1"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleRapidFire(player, logger); });
        }

        private CombatSettingRow CreateAutoReloadCombatSetting(Rect rect, ManualLogSource logger)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.auto_reload",
                GetLocalizedFallback("gui.command.setting.auto_reload", "Auto Reload", "\u81ea\u52a8\u6362\u5f39"),
                GetAutoReloadStatusLabel(),
                IsAutoReloadEnabled(),
                delegate { ExecuteToggleAutoReload(logger); });
        }

        private CombatSettingRow CreateAmmoModeCombatSetting(Rect rect, ManualLogSource logger)
        {
            return new CombatSettingRow(
                rect,
                "cmd.combat.ammo_mode",
                GetLocalizedFallback("gui.command.setting.ammo_mode", "Ammo Mode", "\u5f39\u836f\u6a21\u5f0f"),
                GetAmmoModeStatusLabel(),
                IsAmmoModeEnabled(),
                delegate { ExecuteCycleAmmoMode(logger); });
        }

        private CombatSettingRow CreateInvincibilityCombatSetting(Rect rect, PlayerController player, ManualLogSource logger)
        {
            bool isEnabled = IsInvincibilityEnabled();
            return new CombatSettingRow(
                rect,
                "cmd.combat.invincible",
                GetLocalizedFallback("gui.command.setting.invincible", "Invincibility", "\u65e0\u654c"),
                GetOnOffStatusLabel(isEnabled),
                isEnabled,
                delegate { ExecuteToggleInvincibility(player, logger); });
        }

        private CombatSettingRow CreateAmmonomiconCombatSetting(Rect rect, ManualLogSource logger)
        {
            bool isEnabled = IsAmmonomiconFastOpenEnabled();
            return new CombatSettingRow(
                rect,
                "cmd.combat.ammonomicon",
                GetLocalizedFallback("gui.command.setting.ammonomicon", "Ammo Book", "\u56fe\u9274"),
                GetAmmonomiconFastOpenStatusLabel(),
                isEnabled,
                delegate { ExecuteToggleAmmonomiconFastOpen(logger); });
        }

        private void DrawCombatSettingRows(CombatSettingRow[] rows, float labelWidth, float buttonWidth)
        {
            for (int index = 0; index < rows.Length; index++)
            {
                DrawCombatSettingRow(rows[index], labelWidth, buttonWidth);
            }
        }

        private void DrawCombatSettingRow(CombatSettingRow row, float labelWidth, float buttonWidth)
        {
            GUI.Label(new Rect(row.Rect.x, row.Rect.y + 7f, labelWidth, 20f), row.Label, _hintStyle);
            GUIStyle buttonStyle = row.IsActive ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(row.Rect.x + labelWidth, row.Rect.y, buttonWidth, row.Rect.height), row.ControlId, row.Status, buttonStyle))
            {
                if (row.OnClick != null)
                {
                    row.OnClick();
                }
            }
        }

        private bool DrawControllerButton(Rect rect, string controlId, string label, GUIStyle normalStyle)
        {
            return GUI.Button(rect, label, GetControllerButtonStyle(controlId, normalStyle));
        }

        private GUIStyle GetControllerButtonStyle(string controlId, GUIStyle normalStyle)
        {
            if (!IsControllerFocusActive("cmd", controlId) &&
                !IsControllerFocusActive("settings", controlId) &&
                !IsControllerFocusActive("characters", controlId) &&
                !IsControllerFocusActive("loadout", controlId) &&
                !IsControllerFocusActive("pickups", controlId))
            {
                return normalStyle;
            }

            if (normalStyle == _pickupFilterButtonStyle || normalStyle == _pickupFilterDisabledButtonStyle)
            {
                return _pickupFilterActiveButtonStyle;
            }

            return _enabledButtonStyle;
        }

        private bool IsControllerFocusActive(string pagePrefix, string controlId)
        {
            if (_currentPage == PanelPage.Command && string.Equals(pagePrefix, "cmd", System.StringComparison.Ordinal))
            {
                return string.Equals(_commandPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Settings && string.Equals(pagePrefix, "settings", System.StringComparison.Ordinal))
            {
                return string.Equals(_settingsPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Characters && string.Equals(pagePrefix, "characters", System.StringComparison.Ordinal))
            {
                return string.Equals(_characterPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.LoadoutEditor && string.Equals(pagePrefix, "loadout", System.StringComparison.Ordinal))
            {
                return string.Equals(_loadoutEditorFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            if (_currentPage == PanelPage.Pickups && string.Equals(pagePrefix, "pickups", System.StringComparison.Ordinal))
            {
                return string.Equals(_pickupPageFocusedControlId, controlId, System.StringComparison.Ordinal);
            }

            return false;
        }

        private ControllerFocusEntry[] GetCommandPageFocusEntries()
        {
            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.Combat:
                    return BuildCommandPageFocusEntries(CombatCommandPageFocusEntries);
                case CommandMenuCategory.Player:
                    return BuildCommandPageFocusEntries(PlayerCommandPageFocusEntries);
                case CommandMenuCategory.Room:
                    return GetRoomCommandPageFocusEntries();
                case CommandMenuCategory.General:
                default:
                    return BuildCommandPageFocusEntries(GeneralCommandPageFocusEntries);
            }
        }

        private void ExecuteCommandPageFocusedControl()
        {
            PlayerController player = GetCurrentPlayer();
            switch (_commandPageFocusedControlId)
            {
                case "cmd.about":
                    OpenAboutPage();
                    return;
                case "cmd.settings":
                    OpenSettingsPage();
                    return;
                case "cmd.language":
                    ExecuteToggleLanguage(null);
                    return;
                case "cmd.category.general":
                    SetCommandMenuCategory(CommandMenuCategory.General);
                    return;
                case "cmd.category.combat":
                    SetCommandMenuCategory(CommandMenuCategory.Combat);
                    return;
                case "cmd.category.player":
                    SetCommandMenuCategory(CommandMenuCategory.Player);
                    return;
                case "cmd.category.room":
                    SetCommandMenuCategory(CommandMenuCategory.Room);
                    return;
                case "cmd.room.section.chest":
                    _roomMenuSection = RoomMenuSection.Chest;
                    return;
                case "cmd.room.section.neutral":
                    _roomMenuSection = RoomMenuSection.Neutral;
                    return;
                case "cmd.room.section.enemies":
                    if (IsExperimentalModeEnabled())
                    {
                        _roomMenuSection = RoomMenuSection.Enemies;
                    }
                    return;
                case "cmd.room.section.state":
                    if (IsExperimentalModeEnabled())
                    {
                        _roomMenuSection = RoomMenuSection.State;
                    }
                    return;
                case "cmd.room.chest_tier.brown":
                    ExecuteSpawnChest(player, null, RoomChestTier.Brown);
                    return;
                case "cmd.room.chest_tier.blue":
                    ExecuteSpawnChest(player, null, RoomChestTier.Blue);
                    return;
                case "cmd.room.chest_tier.green":
                    ExecuteSpawnChest(player, null, RoomChestTier.Green);
                    return;
                case "cmd.room.chest_tier.red":
                    ExecuteSpawnChest(player, null, RoomChestTier.Red);
                    return;
                case "cmd.room.chest_tier.black":
                    ExecuteSpawnChest(player, null, RoomChestTier.Black);
                    return;
                case "cmd.room.chest_tier.synergy":
                    ExecuteSpawnChest(player, null, RoomChestTier.Synergy);
                    return;
                case "cmd.room.chest_tier.rainbow":
                    ExecuteSpawnChest(player, null, RoomChestTier.Rainbow);
                    return;
                case "cmd.room.refresh_enemies":
                    ExecuteRefreshRoomEnemies(player, null);
                    return;
                case "cmd.room.spawn_gunber_muncher":
                    ExecuteSpawnGunberMuncher(player, null);
                    return;
                case "cmd.room.spawn_evil_muncher":
                    ExecuteSpawnEvilMuncher(player, null);
                    return;
                case "cmd.general.pickups":
                    OpenPickupPage(null);
                    return;
                case "cmd.general.loadout":
                    OpenLoadoutEditorPage(null);
                    return;
                case "cmd.general.currency":
                    OpenCurrencyPage(null);
                    return;
                case "cmd.general.teleport":
                    ToggleTeleportPanel();
                    return;
                case "cmd.general.characters":
                    OpenCharacterPage(null);
                    return;
                case "cmd.general.boss_rush":
                    OpenBossRushPage(null);
                    return;
                case "cmd.general.reveal_map":
                    ExecuteRevealCurrentFloorMap(player, null);
                    return;
                case "cmd.general.random_item":
                    ExecuteRandom(player, null);
                    return;
                case "cmd.combat.rapid":
                    ExecuteToggleRapidFire(player, null);
                    return;
                case "cmd.combat.auto_reload":
                    ExecuteToggleAutoReload(null);
                    return;
                case "cmd.combat.ammo_mode":
                    ExecuteCycleAmmoMode(null);
                    return;
                case "cmd.combat.invincible":
                    ExecuteToggleInvincibility(player, null);
                    return;
                case "cmd.combat.ammonomicon":
                    ExecuteToggleAmmonomiconFastOpen(null);
                    return;
                case "cmd.combat.full_ammo":
                    ExecuteRefillCurrentGunAmmo(player, null);
                    return;
                case "cmd.player.heal_half":
                    ExecuteHealHalfHeart(player, null);
                    return;
                case "cmd.player.full_heal":
                    ExecuteFullHeal(player, null);
                    return;
                case "cmd.player.add_max_health":
                    ExecuteAddMaxHealth(player, null);
                    return;
                case "cmd.player.add_armor":
                    ExecuteAddArmor(player, null);
                    return;
                case "cmd.player.add_blank":
                    ExecuteAddBlank(player, null);
                    return;
                case "cmd.player.refill_blanks":
                    ExecuteRefillBlanks(player, null);
                    return;
                case "cmd.player.clear_curse":
                    ExecuteClearCurse(player, null);
                    return;
                case "cmd.player.stats":
                    SetPlayerStatsPanelShown(!_showPlayerStatsPanel);
                    return;
                default:
                    return;
            }
        }

        private void CycleCommandCategory(int direction)
        {
            int currentIndex = 0;
            for (int i = 0; i < CommandMenuCategoryOrder.Length; i++)
            {
                if (CommandMenuCategoryOrder[i] == _commandMenuCategory)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + direction + CommandMenuCategoryOrder.Length) % CommandMenuCategoryOrder.Length;
            SetCommandMenuCategory(CommandMenuCategoryOrder[nextIndex]);
            _commandPageFocusedControlId = GetDefaultCommandFocusForCategory(_commandMenuCategory);
        }

        private void SetCommandMenuCategory(CommandMenuCategory category)
        {
            _commandMenuCategory = category;
        }

        private static string GetDefaultCommandFocusForCategory(CommandMenuCategory category)
        {
            switch (category)
            {
                case CommandMenuCategory.Combat:
                    return "cmd.combat.rapid";
                case CommandMenuCategory.Player:
                    return "cmd.player.heal_half";
                case CommandMenuCategory.Room:
                    return "cmd.category.room";
                case CommandMenuCategory.General:
                default:
                    return "cmd.general.pickups";
            }
        }

        private PlayerController GetCurrentPlayer()
        {
            GameManager gameManager = GameManager.Instance;
            return (object)gameManager != null ? gameManager.PrimaryPlayer : null;
        }

        private static ControllerFocusEntry[] BuildCommandPageFocusEntries(params ControllerFocusEntry[][] contentGroups)
        {
            int entryCount = CommandPageSharedFocusEntries.Length;
            for (int groupIndex = 0; groupIndex < contentGroups.Length; groupIndex++)
            {
                entryCount += contentGroups[groupIndex].Length;
            }

            ControllerFocusEntry[] entries = new ControllerFocusEntry[entryCount];
            int writeIndex = CopyFocusEntries(CommandPageSharedFocusEntries, entries, 0);
            for (int groupIndex = 0; groupIndex < contentGroups.Length; groupIndex++)
            {
                writeIndex = CopyFocusEntries(contentGroups[groupIndex], entries, writeIndex);
            }

            return entries;
        }

        private static int CopyFocusEntries(ControllerFocusEntry[] source, ControllerFocusEntry[] destination, int writeIndex)
        {
            for (int index = 0; index < source.Length; index++)
            {
                destination[writeIndex] = source[index];
                writeIndex++;
            }

            return writeIndex;
        }

        private string GetAutoReloadStatusLabel()
        {
            if (_autoReloadToggleService == null)
            {
                return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }

            switch (_autoReloadToggleService.Mode)
            {
                case AutoReloadMode.Instant:
                    return GetLocalizedFallback("gui.command.status.instant", "Instant", "\u77ac\u95f4");
                case AutoReloadMode.Animated:
                    return GetLocalizedFallback("gui.command.status.animated", "Animated", "\u52a8\u753b");
                default:
                    return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }
        }

        private string GetAmmoModeStatusLabel()
        {
            if (_ammoModeToggleService == null)
            {
                return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }

            switch (_ammoModeToggleService.Mode)
            {
                case AmmoMode.NoConsume:
                    return GetLocalizedFallback("gui.command.status.ammo_mode.no_consume", "No Consume", "\u4e0d\u8017\u5f39");
                case AmmoMode.InfiniteReserve:
                    return GetLocalizedFallback("gui.command.status.ammo_mode.infinite_reserve", "Inf Reserve", "\u5907\u5f39\u65e0\u9650");
                case AmmoMode.Off:
                default:
                    return GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            }
        }

        private static string GetOnOffStatusLabel(bool isEnabled)
        {
            return isEnabled
                ? GetLocalizedFallback("gui.command.status.on", "ON", "\u5f00")
                : GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
        }

        private string GetAmmonomiconFastOpenStatusLabel()
        {
            return IsAmmonomiconFastOpenEnabled()
                ? GetLocalizedFallback("gui.command.status.instant", "Instant", "\u77ac\u5f00")
                : GetLocalizedFallback("gui.command.status.animated", "Animated", "\u52a8\u753b");
        }

        private bool IsRapidFireEnabledFor(PlayerController player)
        {
            return _rapidFireToggleService != null && _rapidFireToggleService.IsEnabledFor(player);
        }

        private bool IsAutoReloadEnabled()
        {
            return _autoReloadToggleService != null && _autoReloadToggleService.Mode != AutoReloadMode.Off;
        }

        private bool IsAmmoModeEnabled()
        {
            return _ammoModeToggleService != null && _ammoModeToggleService.Mode != AmmoMode.Off;
        }

        private bool IsInvincibilityEnabled()
        {
            return _invincibilityToggleService != null && _invincibilityToggleService.IsEnabled;
        }

        private bool IsAmmonomiconFastOpenEnabled()
        {
            return _ammonomiconFastOpenToggleService != null && AmmonomiconFastOpenToggleService.IsFastOpenEnabled;
        }

        private struct CombatSettingRow
        {
            public CombatSettingRow(Rect rect, string controlId, string label, string status, bool isActive, System.Action onClick)
            {
                Rect = rect;
                ControlId = controlId ?? string.Empty;
                Label = label ?? string.Empty;
                Status = status ?? string.Empty;
                IsActive = isActive;
                OnClick = onClick;
            }

            public Rect Rect;

            public string ControlId;

            public string Label;

            public string Status;

            public bool IsActive;

            public System.Action OnClick;
        }

        private static string GetLocalizedFallback(string key, string englishFallback, string simplifiedChineseFallback)
        {
            string value = GuiText.Get(key);
            if (!string.Equals(value, key, System.StringComparison.Ordinal))
            {
                return value;
            }

            return string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase)
                ? simplifiedChineseFallback
                : englishFallback;
        }

        private static string GetLocalizedFormattedFallback(string key, string englishFallback, string simplifiedChineseFallback, params object[] args)
        {
            string template = GetLocalizedFallback(key, englishFallback, simplifiedChineseFallback);
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, args);
            }
            catch
            {
                return template;
            }
        }

        private bool IsExperimentalModeEnabled()
        {
            return _experimentalModeProvider != null && _experimentalModeProvider();
        }

        private void SetPlayerStatsPanelShown(bool isEnabled)
        {
            _showPlayerStatsPanel = isEnabled;
            if (_playerStatsPanelShownSetter != null)
            {
                _playerStatsPanelShownSetter(isEnabled);
            }
        }

        private string GetLanguageButtonLabel()
        {
            string language = GetConfiguredLanguage();
            if (string.Equals(language, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("gui.command.button.language_zh");
            }

            if (string.Equals(language, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("gui.command.button.language_en");
            }

            return GuiText.Get("gui.command.button.language_auto");
        }

        private void ExecuteToggleLanguage(ManualLogSource logger)
        {
            if (_languageSetter == null)
            {
                return;
            }

            string nextLanguage = GetNextLanguage(GetConfiguredLanguage());
            _languageSetter(nextLanguage);
            _lastGuiLanguageCode = string.Empty;
            HandleLanguageChanged();
            ShowStatus(GuiText.Get("result.language.changed", GetLanguageDisplayName(nextLanguage)), false);
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(GuiText.GetEnglish("result.language.changed", GetEnglishLanguageDisplayName(nextLanguage))));
            }
        }

        private string GetConfiguredLanguage()
        {
            return _languageProvider != null ? GuiText.NormalizeLanguageOverride(_languageProvider()) : "auto";
        }

        private static string GetNextLanguage(string currentLanguage)
        {
            if (string.Equals(currentLanguage, "auto", System.StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            if (string.Equals(currentLanguage, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            return "auto";
        }

        private static string GetLanguageDisplayName(string language)
        {
            if (string.Equals(language, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("label.language.zh");
            }

            if (string.Equals(language, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.Get("label.language.en");
            }

            return GuiText.Get("label.language.auto");
        }

        private static string GetEnglishLanguageDisplayName(string language)
        {
            if (string.Equals(language, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.GetEnglish("label.language.zh");
            }

            if (string.Equals(language, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return GuiText.GetEnglish("label.language.en");
            }

            return GuiText.GetEnglish("label.language.auto");
        }
    }
}
