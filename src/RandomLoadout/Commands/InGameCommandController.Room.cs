using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
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

        private void DrawRoomContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            bool experimentalModeEnabled = IsExperimentalModeEnabled();
            Rect chestSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect neutralSectionRect = new Rect(chestSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect enemiesSectionRect = new Rect(neutralSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect stateSectionRect = new Rect(enemiesSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawRoomSectionButton(chestSectionRect, "cmd.room.section.chest", RoomMenuSection.Chest, GetLocalizedFallback("gui.room.section.chest", "Chest", "宝箱"));
            DrawRoomSectionButton(neutralSectionRect, "cmd.room.section.neutral", RoomMenuSection.Neutral, GetLocalizedFallback("gui.room.section.neutral", "Neutral", "中立生物"));
            DrawRoomSectionButton(enemiesSectionRect, "cmd.room.section.enemies", RoomMenuSection.Enemies, GetLocalizedFallback("gui.room.section.enemies", "Enemies", "怪物"), experimentalModeEnabled);
            DrawRoomSectionButton(stateSectionRect, "cmd.room.section.state", RoomMenuSection.State, GetLocalizedFallback("gui.room.section.state", "State", "状态"), experimentalModeEnabled);

            Rect sectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 12f, contentRect.width, contentRect.height - sectionButtonHeight - 12f);
            if (!experimentalModeEnabled &&
                (_roomMenuSection == RoomMenuSection.Enemies || _roomMenuSection == RoomMenuSection.State))
            {
                _roomMenuSection = RoomMenuSection.Chest;
            }

            switch (_roomMenuSection)
            {
                case RoomMenuSection.Neutral:
                    DrawRoomNeutralSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
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

            if (DrawControllerButton(
                new Rect(contentRect.x, secondRowY + 18f, buttonWidth * 2f + ButtonGap, controlHeight),
                "cmd.room.refresh_enemies",
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

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.chest_tier.title", "Chest Tier", "宝箱等级"),
                _hintStyle);

            float optionsTop = firstRowY + 24f;
            float optionsSecondRowY = optionsTop + controlHeight + ButtonGap;
            DrawRoomChestTierButton(new Rect(contentRect.x, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.brown", RoomChestTier.Brown, player, logger);
            DrawRoomChestTierButton(new Rect(secondColumnX, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.blue", RoomChestTier.Blue, player, logger);
            DrawRoomChestTierButton(new Rect(thirdColumnX, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.green", RoomChestTier.Green, player, logger);
            DrawRoomChestTierButton(new Rect(fourthColumnX, optionsTop, buttonWidth, controlHeight), "cmd.room.chest_tier.red", RoomChestTier.Red, player, logger);

            DrawRoomChestTierButton(new Rect(contentRect.x, optionsSecondRowY, buttonWidth, controlHeight), "cmd.room.chest_tier.black", RoomChestTier.Black, player, logger);
            DrawRoomChestTierButton(new Rect(secondColumnX, optionsSecondRowY, buttonWidth, controlHeight), "cmd.room.chest_tier.synergy", RoomChestTier.Synergy, player, logger);
            DrawRoomChestTierButton(new Rect(thirdColumnX, optionsSecondRowY, buttonWidth, controlHeight), "cmd.room.chest_tier.rainbow", RoomChestTier.Rainbow, player, logger);
        }

        private void DrawRoomNeutralSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float thirdColumnX = contentRect.x + (buttonWidth * 2f) + (ButtonGap * 2f);
            float firstRowY = contentRect.y;
            float secondRowY = firstRowY + 24f + controlHeight + ButtonGap;

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.neutral.title", "Neutral NPCs", "中立生物"),
                _hintStyle);

            GUI.Label(
                new Rect(contentRect.x, firstRowY + 24f, contentRect.width, 36f),
                GetLocalizedFallback(
                    "gui.room.neutral.hint",
                    "Spawn utility NPC-style objects in the current room.",
                    "在当前房间生成偏中立、功能型的 NPC 对象。"),
                _wrappedHintStyle);

            float fourthRowY = secondRowY + 18f;
            string spawnGunberMuncherLabel = GetLocalizedFallback("gui.room.button.spawn_gunber_muncher", "Spawn Gunber Muncher", "生成吃枪怪");
            if (DrawControllerButton(new Rect(contentRect.x, fourthRowY, buttonWidth * 2f + ButtonGap, controlHeight), "cmd.room.spawn_gunber_muncher", spawnGunberMuncherLabel, _buttonStyle))
            {
                ExecuteSpawnGunberMuncher(player, logger);
            }

            string spawnEvilMuncherLabel = GetLocalizedFallback("gui.room.button.spawn_evil_muncher", "Spawn Evil Muncher", "生成邪恶吃枪怪");
            if (DrawControllerButton(new Rect(thirdColumnX, fourthRowY, buttonWidth * 2f, controlHeight), "cmd.room.spawn_evil_muncher", spawnEvilMuncherLabel, _buttonStyle))
            {
                ExecuteSpawnEvilMuncher(player, logger);
            }
        }

        private void DrawRoomSectionButton(Rect rect, string controlId, RoomMenuSection section, string label)
        {
            DrawRoomSectionButton(rect, controlId, section, label, true);
        }

        private void DrawRoomSectionButton(Rect rect, string controlId, RoomMenuSection section, string label, bool isEnabled)
        {
            GUIStyle style = !isEnabled
                ? _pickupFilterDisabledButtonStyle
                : (_roomMenuSection == section ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle);
            if (DrawControllerButton(rect, controlId, label, style) && isEnabled)
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

        private ControllerFocusEntry[] GetRoomCommandPageFocusEntries()
        {
            bool experimentalModeEnabled = IsExperimentalModeEnabled();
            if (_roomMenuSection == RoomMenuSection.Neutral)
            {
                return BuildCommandPageFocusEntries(
                    experimentalModeEnabled ? RoomSectionCommandPageFocusEntries : RoomChestOnlySectionCommandPageFocusEntries,
                    RoomNeutralCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.Enemies && experimentalModeEnabled)
            {
                return BuildCommandPageFocusEntries(RoomSectionCommandPageFocusEntries, RoomEnemiesCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.State && experimentalModeEnabled)
            {
                return BuildCommandPageFocusEntries(RoomSectionCommandPageFocusEntries);
            }

            if (!experimentalModeEnabled)
            {
                return BuildCommandPageFocusEntries(RoomChestOnlySectionCommandPageFocusEntries, RoomChestCommandPageFocusEntries);
            }

            return BuildCommandPageFocusEntries(RoomSectionCommandPageFocusEntries, RoomChestCommandPageFocusEntries);
        }

        private CommandPageActionBinding[] GetRoomCommandPageActionBindings(PlayerController player)
        {
            return new[]
            {
                new CommandPageActionBinding("cmd.room.chest_tier.brown", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Brown); }),
                new CommandPageActionBinding("cmd.room.chest_tier.blue", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Blue); }),
                new CommandPageActionBinding("cmd.room.chest_tier.green", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Green); }),
                new CommandPageActionBinding("cmd.room.chest_tier.red", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Red); }),
                new CommandPageActionBinding("cmd.room.chest_tier.black", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Black); }),
                new CommandPageActionBinding("cmd.room.chest_tier.synergy", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Synergy); }),
                new CommandPageActionBinding("cmd.room.chest_tier.rainbow", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Rainbow); }),
                new CommandPageActionBinding("cmd.room.refresh_enemies", delegate { ExecuteRefreshRoomEnemies(player, null); }),
                new CommandPageActionBinding("cmd.room.spawn_gunber_muncher", delegate { ExecuteSpawnGunberMuncher(player, null); }),
                new CommandPageActionBinding("cmd.room.spawn_evil_muncher", delegate { ExecuteSpawnEvilMuncher(player, null); }),
            };
        }

        private void DrawRoomChestTierButton(Rect rect, string controlId, RoomChestTier chestTier, PlayerController player, ManualLogSource logger)
        {
            GUIStyle style = _selectedRoomChestTier == chestTier ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (DrawControllerButton(rect, controlId, GetRoomChestTierLabel(chestTier), style))
            {
                _selectedRoomChestTier = chestTier;
                ExecuteSpawnChest(player, logger, chestTier);
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
            ExecuteSpawnChest(player, logger, _selectedRoomChestTier);
        }

        private void ExecuteSpawnChest(PlayerController player, ManualLogSource logger, RoomChestTier chestTier)
        {
            _selectedRoomChestTier = chestTier;
            ShowRoomActionResult(_roomDebugCommandService.SpawnChest(player, chestTier), logger);
        }

        private void ExecuteRefreshRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.RefreshCurrentRoomEnemies(player, logger), logger);
        }

        private void ExecuteSpawnGunberMuncher(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.SpawnGunberMuncher(player, logger), logger);
        }

        private void ExecuteSpawnEvilMuncher(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.SpawnEvilMuncher(player, logger), logger);
        }

        private void ShowRoomActionResult(GrantCommandExecutionResult executionResult, ManualLogSource logger)
        {
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
    }
}
