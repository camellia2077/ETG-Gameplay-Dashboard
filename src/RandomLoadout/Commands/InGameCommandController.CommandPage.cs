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
                GuiText.Get("gui.command.hint.input"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.toggle"),
                _hintStyle);

            GUI.SetNextControlName(InputControlName);
            float textFieldWidth = panelRect.width - 54f - (ButtonWidth * 2f) - ButtonGap - 12f;
            Rect textFieldRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, textFieldWidth, controlHeight);
            Rect grantButtonRect = new Rect(textFieldRect.xMax + 12f, textFieldRect.y, ButtonWidth, controlHeight);
            Rect randomButtonRect = new Rect(grantButtonRect.xMax + ButtonGap, textFieldRect.y, ButtonWidth, controlHeight);
            _inputText = GUI.TextField(textFieldRect, _inputText, 256, _textFieldStyle);

            if (_focusInputField)
            {
                GUI.FocusControl(InputControlName);
                _focusInputField = false;
            }

            bool shouldSubmit = false;
            Event currentEvent = Event.current;
            if (currentEvent != null &&
                currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                shouldSubmit = true;
                currentEvent.Use();
            }

            if (GUI.Button(grantButtonRect, GuiText.Get("gui.command.button.grant"), _buttonStyle))
            {
                shouldSubmit = true;
            }

            if (GUI.Button(randomButtonRect, GuiText.Get("gui.command.button.random"), _buttonStyle))
            {
                ExecuteRandom(player, logger);
            }

            float categoryTop = panelRect.y + 132f;
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

            if (shouldSubmit)
            {
                Submit(player, logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + panelRect.height - 28f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.command.hint.submit"),
                _hintStyle);
        }

        private float GetCommandPanelHeight()
        {
            const float contentTopOffset = 174f;
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
                    return 4;
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
                    _showTeleportPanel = !_showTeleportPanel;
                }

                if (DrawControllerButton(new Rect(contentRect.x, secondRowY, buttonWidth, controlHeight), "cmd.general.characters", GuiText.Get("gui.command.button.characters"), _buttonStyle))
                {
                    OpenCharacterPage(logger);
                }

                if (DrawControllerButton(new Rect(secondColumnX, secondRowY, buttonWidth, controlHeight), "cmd.general.boss_rush", GuiText.Get("gui.command.button.boss_rush"), _buttonStyle))
                {
                    OpenBossRushPage(logger);
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
            if (DrawControllerButton(new Rect(secondColumnX, thirdRowY, buttonWidth, controlHeight), "cmd.player.stats", statsButtonLabel, _buttonStyle))
            {
                _showPlayerStatsPanel = !_showPlayerStatsPanel;
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

            DrawCombatSettingRow(
                new Rect(contentRect.x, firstRowY, settingColumnWidth, controlHeight),
                "cmd.combat.rapid",
                GetLocalizedFallback("gui.command.setting.rapid", "Hold Rapid", "\u6309\u4f4f\u8fde\u53d1"),
                GetOnOffStatusLabel(_rapidFireToggleService != null && _rapidFireToggleService.IsEnabledFor(player)),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleRapidFire(player, logger); });

            DrawCombatSettingRow(
                new Rect(secondSettingColumnX, firstRowY, settingColumnWidth, controlHeight),
                "cmd.combat.auto_reload",
                GetLocalizedFallback("gui.command.setting.auto_reload", "Auto Reload", "\u81ea\u52a8\u6362\u5f39"),
                GetAutoReloadStatusLabel(),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleAutoReload(logger); });

            DrawCombatSettingRow(
                new Rect(contentRect.x, secondRowY, settingColumnWidth, controlHeight),
                "cmd.combat.ammo_mode",
                GetLocalizedFallback("gui.command.setting.ammo_mode", "Ammo Mode", "\u5f39\u836f\u6a21\u5f0f"),
                GetAmmoModeStatusLabel(),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteCycleAmmoMode(logger); });

            DrawCombatSettingRow(
                new Rect(secondSettingColumnX, secondRowY, settingColumnWidth, controlHeight),
                "cmd.combat.invincible",
                GetLocalizedFallback("gui.command.setting.invincible", "Invincibility", "\u65e0\u654c"),
                GetOnOffStatusLabel(_invincibilityToggleService != null && _invincibilityToggleService.IsEnabled),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleInvincibility(player, logger); });

            DrawCombatSettingRow(
                new Rect(contentRect.x, thirdRowY, settingColumnWidth, controlHeight),
                "cmd.combat.ammonomicon",
                GetLocalizedFallback("gui.command.setting.ammonomicon", "Ammo Book", "\u56fe\u9274"),
                GetAmmonomiconFastOpenStatusLabel(),
                settingLabelWidth,
                settingButtonWidth,
                delegate { ExecuteToggleAmmonomiconFastOpen(logger); });

            if (DrawControllerButton(new Rect(secondSettingColumnX + settingLabelWidth, thirdRowY, settingButtonWidth, controlHeight), "cmd.combat.full_ammo", GuiText.Get("gui.command.button.full_ammo"), _buttonStyle))
            {
                ExecuteRefillCurrentGunAmmo(player, logger);
            }
        }

        private void DrawCombatSettingRow(Rect rowRect, string controlId, string label, string status, float labelWidth, float buttonWidth, System.Action onClick)
        {
            GUI.Label(new Rect(rowRect.x, rowRect.y + 7f, labelWidth, 20f), label, _hintStyle);
            GUIStyle buttonStyle = IsEnabledStatusLabel(status) ? _enabledButtonStyle : _buttonStyle;
            if (DrawControllerButton(new Rect(rowRect.x + labelWidth, rowRect.y, buttonWidth, rowRect.height), controlId, status, buttonStyle))
            {
                if (onClick != null)
                {
                    onClick();
                }
            }
        }

        private bool DrawControllerButton(Rect rect, string controlId, string label, GUIStyle normalStyle)
        {
            return GUI.Button(rect, label, GetControllerButtonStyle(controlId, normalStyle));
        }

        private GUIStyle GetControllerButtonStyle(string controlId, GUIStyle normalStyle)
        {
            if (!IsControllerFocusActive("cmd", controlId) && !IsControllerFocusActive("settings", controlId))
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

            return false;
        }

        private void DrawRoomContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            bool experimentalModeEnabled = IsExperimentalModeEnabled();
            Rect chestSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect enemiesSectionRect = new Rect(chestSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect stateSectionRect = new Rect(enemiesSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawRoomSectionButton(chestSectionRect, RoomMenuSection.Chest, GetLocalizedFallback("gui.room.section.chest", "Chest", "宝箱"));
            DrawRoomSectionButton(enemiesSectionRect, RoomMenuSection.Enemies, GetLocalizedFallback("gui.room.section.enemies", "Enemies", "怪物"), experimentalModeEnabled);
            DrawRoomSectionButton(stateSectionRect, RoomMenuSection.State, GetLocalizedFallback("gui.room.section.state", "State", "状态"), experimentalModeEnabled);

            Rect sectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 12f, contentRect.width, contentRect.height - sectionButtonHeight - 12f);
            if (!experimentalModeEnabled && _roomMenuSection != RoomMenuSection.Chest)
            {
                _roomMenuSection = RoomMenuSection.Chest;
            }

            switch (_roomMenuSection)
            {
                case RoomMenuSection.Enemies:
                    DrawRoomEnemiesSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.State:
                    DrawRoomPlaceholderSection(
                        sectionContentRect,
                        GetLocalizedFallback("gui.room.section.state", "State", "状态"),
                        GetLocalizedFallback("gui.room.placeholder.state", "Room-state tools will go here next.", "后续会在这里加入房间状态相关功能。"));
                    return;
                case RoomMenuSection.Chest:
                default:
                    DrawRoomChestSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
            }
        }

        private void DrawRoomEnemiesSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + 24f + controlHeight + ButtonGap;

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.enemies.title", "Refresh Current Room", "刷新当前房间"),
                _hintStyle);

            GUI.Label(
                new Rect(contentRect.x, firstRowY + 24f, contentRect.width, 36f),
                GetLocalizedFallback(
                    "gui.room.enemies.hint",
                    "Respawns the room's predefined enemies after the room has been cleared. The exact spawn pattern may differ from the first entry.",
                    "在清空当前房间后，重新刷出该房间的预定义敌人。不保证与第一次进房时的站位和波次完全一致。"),
                _wrappedHintStyle);

            if (GUI.Button(
                new Rect(contentRect.x, secondRowY + 18f, buttonWidth * 2f + ButtonGap, controlHeight),
                GetLocalizedFallback("gui.room.button.refresh_enemies", "Refresh Room Enemies", "刷新房间怪物"),
                _buttonStyle))
            {
                ExecuteRefreshRoomEnemies(player, logger);
            }
        }

        private void DrawRoomChestSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float secondColumnX = contentRect.x + buttonWidth + ButtonGap;
            float thirdColumnX = secondColumnX + buttonWidth + ButtonGap;
            float fourthColumnX = thirdColumnX + buttonWidth + ButtonGap;
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + controlHeight + ButtonGap;
            float thirdRowY = secondRowY + controlHeight + ButtonGap;

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.chest_tier.title", "Chest Tier", "宝箱等级"),
                _hintStyle);

            float optionsTop = firstRowY + 24f;
            float optionsSecondRowY = optionsTop + controlHeight + ButtonGap;
            DrawRoomChestTierButton(new Rect(contentRect.x, optionsTop, buttonWidth, controlHeight), RoomChestTier.Brown);
            DrawRoomChestTierButton(new Rect(secondColumnX, optionsTop, buttonWidth, controlHeight), RoomChestTier.Blue);
            DrawRoomChestTierButton(new Rect(thirdColumnX, optionsTop, buttonWidth, controlHeight), RoomChestTier.Green);
            DrawRoomChestTierButton(new Rect(fourthColumnX, optionsTop, buttonWidth, controlHeight), RoomChestTier.Red);

            DrawRoomChestTierButton(new Rect(contentRect.x, optionsSecondRowY, buttonWidth, controlHeight), RoomChestTier.Black);
            DrawRoomChestTierButton(new Rect(secondColumnX, optionsSecondRowY, buttonWidth, controlHeight), RoomChestTier.Synergy);
            DrawRoomChestTierButton(new Rect(thirdColumnX, optionsSecondRowY, buttonWidth, controlHeight), RoomChestTier.Rainbow);

            GUI.Label(
                new Rect(contentRect.x, thirdRowY + 24f, contentRect.width, 20f),
                GetLocalizedFormattedFallback("gui.room.selected_tier", "Selected tier: {0}", "当前等级：{0}", GetRoomChestTierLabel(_selectedRoomChestTier)),
                _hintStyle);

            string spawnButtonLabel = GetLocalizedFallback("gui.room.button.spawn_chest", "Spawn Chest", "生成宝箱");
            if (GUI.Button(new Rect(fourthColumnX, thirdRowY + 14f, buttonWidth, controlHeight), spawnButtonLabel, _buttonStyle))
            {
                ExecuteSpawnChest(player, logger);
            }
        }

        private void DrawRoomSectionButton(Rect rect, RoomMenuSection section, string label)
        {
            DrawRoomSectionButton(rect, section, label, true);
        }

        private void DrawRoomSectionButton(Rect rect, RoomMenuSection section, string label, bool isEnabled)
        {
            GUIStyle style = !isEnabled
                ? _pickupFilterDisabledButtonStyle
                : (_roomMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle);
            if (GUI.Button(rect, label, style) && isEnabled)
            {
                _roomMenuSection = section;
            }
        }

        private void DrawRoomPlaceholderSection(Rect contentRect, string title, string hint)
        {
            GUI.Label(
                new Rect(contentRect.x, contentRect.y, contentRect.width, 20f),
                title,
                _titleStyle);
            GUI.Label(
                new Rect(contentRect.x, contentRect.y + 28f, contentRect.width, 20f),
                hint,
                _hintStyle);
        }

        private ControllerFocusEntry[] GetCommandPageFocusEntries()
        {
            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.Combat:
                    return new[]
                    {
                        new ControllerFocusEntry("cmd.about", 0, 0),
                        new ControllerFocusEntry("cmd.settings", 0, 1),
                        new ControllerFocusEntry("cmd.language", 0, 2),
                        new ControllerFocusEntry("cmd.category.general", 1, 0),
                        new ControllerFocusEntry("cmd.category.combat", 1, 1),
                        new ControllerFocusEntry("cmd.category.player", 1, 2),
                        new ControllerFocusEntry("cmd.category.room", 1, 3),
                        new ControllerFocusEntry("cmd.combat.rapid", 2, 0),
                        new ControllerFocusEntry("cmd.combat.auto_reload", 2, 1),
                        new ControllerFocusEntry("cmd.combat.ammo_mode", 3, 0),
                        new ControllerFocusEntry("cmd.combat.invincible", 3, 1),
                        new ControllerFocusEntry("cmd.combat.ammonomicon", 4, 0),
                        new ControllerFocusEntry("cmd.combat.full_ammo", 4, 1),
                    };
                case CommandMenuCategory.Player:
                    return new[]
                    {
                        new ControllerFocusEntry("cmd.about", 0, 0),
                        new ControllerFocusEntry("cmd.settings", 0, 1),
                        new ControllerFocusEntry("cmd.language", 0, 2),
                        new ControllerFocusEntry("cmd.category.general", 1, 0),
                        new ControllerFocusEntry("cmd.category.combat", 1, 1),
                        new ControllerFocusEntry("cmd.category.player", 1, 2),
                        new ControllerFocusEntry("cmd.category.room", 1, 3),
                        new ControllerFocusEntry("cmd.player.heal_half", 2, 0),
                        new ControllerFocusEntry("cmd.player.full_heal", 2, 1),
                        new ControllerFocusEntry("cmd.player.add_max_health", 2, 2),
                        new ControllerFocusEntry("cmd.player.add_armor", 2, 3),
                        new ControllerFocusEntry("cmd.player.add_blank", 3, 0),
                        new ControllerFocusEntry("cmd.player.refill_blanks", 3, 1),
                        new ControllerFocusEntry("cmd.player.clear_curse", 4, 0),
                        new ControllerFocusEntry("cmd.player.stats", 4, 1),
                    };
                case CommandMenuCategory.Room:
                    return new[]
                    {
                        new ControllerFocusEntry("cmd.about", 0, 0),
                        new ControllerFocusEntry("cmd.settings", 0, 1),
                        new ControllerFocusEntry("cmd.language", 0, 2),
                        new ControllerFocusEntry("cmd.category.general", 1, 0),
                        new ControllerFocusEntry("cmd.category.combat", 1, 1),
                        new ControllerFocusEntry("cmd.category.player", 1, 2),
                        new ControllerFocusEntry("cmd.category.room", 1, 3),
                    };
                case CommandMenuCategory.General:
                default:
                    return new[]
                    {
                        new ControllerFocusEntry("cmd.about", 0, 0),
                        new ControllerFocusEntry("cmd.settings", 0, 1),
                        new ControllerFocusEntry("cmd.language", 0, 2),
                        new ControllerFocusEntry("cmd.category.general", 1, 0),
                        new ControllerFocusEntry("cmd.category.combat", 1, 1),
                        new ControllerFocusEntry("cmd.category.player", 1, 2),
                        new ControllerFocusEntry("cmd.category.room", 1, 3),
                        new ControllerFocusEntry("cmd.general.pickups", 2, 0),
                        new ControllerFocusEntry("cmd.general.loadout", 2, 1),
                        new ControllerFocusEntry("cmd.general.currency", 2, 2),
                        new ControllerFocusEntry("cmd.general.teleport", 2, 3),
                        new ControllerFocusEntry("cmd.general.characters", 3, 0),
                        new ControllerFocusEntry("cmd.general.boss_rush", 3, 1),
                    };
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
                    _commandMenuCategory = CommandMenuCategory.General;
                    return;
                case "cmd.category.combat":
                    _commandMenuCategory = CommandMenuCategory.Combat;
                    return;
                case "cmd.category.player":
                    _commandMenuCategory = CommandMenuCategory.Player;
                    return;
                case "cmd.category.room":
                    _commandMenuCategory = CommandMenuCategory.Room;
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
                    _showTeleportPanel = !_showTeleportPanel;
                    return;
                case "cmd.general.characters":
                    OpenCharacterPage(null);
                    return;
                case "cmd.general.boss_rush":
                    OpenBossRushPage(null);
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
                    _showPlayerStatsPanel = !_showPlayerStatsPanel;
                    return;
                default:
                    return;
            }
        }

        private void CycleCommandCategory(int direction)
        {
            CommandMenuCategory[] categories =
            {
                CommandMenuCategory.General,
                CommandMenuCategory.Combat,
                CommandMenuCategory.Player,
                CommandMenuCategory.Room,
            };

            int currentIndex = 0;
            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] == _commandMenuCategory)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + direction + categories.Length) % categories.Length;
            _commandMenuCategory = categories[nextIndex];
            switch (_commandMenuCategory)
            {
                case CommandMenuCategory.Combat:
                    _commandPageFocusedControlId = "cmd.combat.rapid";
                    return;
                case CommandMenuCategory.Player:
                    _commandPageFocusedControlId = "cmd.player.heal_half";
                    return;
                case CommandMenuCategory.Room:
                    _commandPageFocusedControlId = "cmd.category.room";
                    return;
                case CommandMenuCategory.General:
                default:
                    _commandPageFocusedControlId = "cmd.general.pickups";
                    return;
            }
        }

        private PlayerController GetCurrentPlayer()
        {
            GameManager gameManager = GameManager.Instance;
            return (object)gameManager != null ? gameManager.PrimaryPlayer : null;
        }

        private void DrawRoomChestTierButton(Rect rect, RoomChestTier chestTier)
        {
            GUIStyle style = _selectedRoomChestTier == chestTier ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, GetRoomChestTierLabel(chestTier), style))
            {
                _selectedRoomChestTier = chestTier;
            }
        }

        private string GetRoomChestTierLabel(RoomChestTier chestTier)
        {
            switch (chestTier)
            {
                case RoomChestTier.Brown:
                    return GetLocalizedFallback("label.room.chest_tier.brown", "Brown", "棕箱");
                case RoomChestTier.Blue:
                    return GetLocalizedFallback("label.room.chest_tier.blue", "Blue", "蓝箱");
                case RoomChestTier.Green:
                    return GetLocalizedFallback("label.room.chest_tier.green", "Green", "绿箱");
                case RoomChestTier.Red:
                    return GetLocalizedFallback("label.room.chest_tier.red", "Red", "红箱");
                case RoomChestTier.Black:
                    return GetLocalizedFallback("label.room.chest_tier.black", "Black", "黑箱");
                case RoomChestTier.Synergy:
                    return GetLocalizedFallback("label.room.chest_tier.synergy", "Synergy", "协同箱");
                case RoomChestTier.Rainbow:
                    return GetLocalizedFallback("label.room.chest_tier.rainbow", "Rainbow", "彩虹箱");
                default:
                    return GetLocalizedFallback("label.room.chest_tier.brown", "Brown", "棕箱");
            }
        }

        private void ExecuteSpawnChest(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _roomDebugCommandService.SpawnChest(player, _selectedRoomChestTier);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void ExecuteRefreshRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _roomDebugCommandService.RefreshCurrentRoomEnemies(player, logger);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private void DrawCombatSettingRow(Rect rowRect, string label, string statusLabel, float labelWidth, float buttonWidth, System.Action onClick)
        {
            GUI.Label(new Rect(rowRect.x, rowRect.y + 7f, labelWidth, 20f), label, _hintStyle);
            if (GUI.Button(new Rect(rowRect.x + labelWidth, rowRect.y, buttonWidth, rowRect.height), statusLabel, GetCombatStatusButtonStyle(statusLabel)) && onClick != null)
            {
                onClick();
            }
        }

        private GUIStyle GetCombatStatusButtonStyle(string statusLabel)
        {
            if (string.IsNullOrEmpty(statusLabel))
            {
                return _buttonStyle;
            }

            string offLabel = GetLocalizedFallback("gui.command.status.off", "OFF", "\u5173");
            return string.Equals(statusLabel, offLabel, System.StringComparison.OrdinalIgnoreCase)
                ? _buttonStyle
                : _enabledButtonStyle;
        }

        private string GetAutoReloadButtonLabel()
        {
            if (_autoReloadToggleService == null)
            {
                return GuiText.Get("gui.command.button.auto_reload_off");
            }

            switch (_autoReloadToggleService.Mode)
            {
                case AutoReloadMode.Instant:
                    return GuiText.Get("gui.command.button.auto_reload_instant");
                case AutoReloadMode.Animated:
                    return GuiText.Get("gui.command.button.auto_reload_animated");
                default:
                    return GuiText.Get("gui.command.button.auto_reload_off");
            }
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

        private bool IsAmmonomiconFastOpenEnabled()
        {
            return _ammonomiconFastOpenToggleService != null && AmmonomiconFastOpenToggleService.IsFastOpenEnabled;
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
