// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections.Generic;
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
            new ControllerFocusEntry("cmd.room.section.rewind", 2, 3),
            new ControllerFocusEntry("cmd.room.section.boss", 2, 4),
            new ControllerFocusEntry("cmd.room.section.state", 2, 5),
        };

        private static readonly ControllerFocusEntry[] RoomStandardSectionCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.section.chest", 2, 0),
            new ControllerFocusEntry("cmd.room.section.neutral", 2, 1),
            new ControllerFocusEntry("cmd.room.section.enemies", 2, 2),
            new ControllerFocusEntry("cmd.room.section.rewind", 2, 3),
            new ControllerFocusEntry("cmd.room.section.boss", 2, 4),
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

        private static readonly ControllerFocusEntry[] RoomRewindCommandPageFocusEntries =
        {
            new ControllerFocusEntry("cmd.room.enemy_refresh_recording", 3, 0),
            new ControllerFocusEntry("cmd.room.enemy_refresh_method", 3, 1),
            new ControllerFocusEntry("cmd.room.player_rewind", 4, 0),
            new ControllerFocusEntry("cmd.room.rewind_cleanup", 4, 1),
            new ControllerFocusEntry("cmd.room.enemy_refresh_execute", 5, 0),
        };

        private static readonly ControllerFocusEntry[] EmptyRoomBossCommandPageFocusEntries = new ControllerFocusEntry[0];

        private void DrawRoomContent(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            const float sectionButtonWidth = 92f;
            const float sectionButtonHeight = 28f;
            bool experimentalModeEnabled = IsExperimentalModeEnabled();
            Rect chestSectionRect = new Rect(contentRect.x, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect neutralSectionRect = new Rect(chestSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect enemiesSectionRect = new Rect(neutralSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect rewindSectionRect = new Rect(enemiesSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect bossSectionRect = new Rect(rewindSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            Rect stateSectionRect = new Rect(bossSectionRect.xMax + 2f, contentRect.y, sectionButtonWidth, sectionButtonHeight);
            DrawRoomSectionButton(chestSectionRect, "cmd.room.section.chest", RoomMenuSection.Chest, GetLocalizedFallback("gui.room.section.chest", "Chest", "宝箱"));
            DrawRoomSectionButton(neutralSectionRect, "cmd.room.section.neutral", RoomMenuSection.Neutral, GetLocalizedFallback("gui.room.section.neutral", "Neutral", "中立生物"));
            DrawRoomSectionButton(enemiesSectionRect, "cmd.room.section.enemies", RoomMenuSection.Enemies, GetLocalizedFallback("gui.room.section.enemies", "Enemies", "怪物"));
            DrawRoomSectionButton(rewindSectionRect, "cmd.room.section.rewind", RoomMenuSection.Rewind, GetLocalizedFallback("gui.room.section.rewind", "Rewind", "回溯"));
            DrawRoomSectionButton(bossSectionRect, "cmd.room.section.boss", RoomMenuSection.Boss, GetLocalizedFallback("gui.room.section.boss", "Boss", "Boss"));
            DrawRoomSectionButton(stateSectionRect, "cmd.room.section.state", RoomMenuSection.State, GetLocalizedFallback("gui.room.section.state", "State", "状态"), experimentalModeEnabled);

            Rect sectionContentRect = new Rect(contentRect.x, contentRect.y + sectionButtonHeight + 12f, contentRect.width, contentRect.height - sectionButtonHeight - 12f);
            if (!experimentalModeEnabled && _roomMenuSection == RoomMenuSection.State)
            {
                SetRoomMenuSection(RoomMenuSection.Chest);
            }

            switch (_roomMenuSection)
            {
                case RoomMenuSection.Neutral:
                    DrawRoomNeutralSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.Enemies:
                    DrawRoomPlaceholderSection(
                        sectionContentRect,
                        GetLocalizedFallback("gui.room.section.enemies", "Enemies", "怪物"),
                        GetLocalizedFallback("gui.room.placeholder.enemies", "Enemy tools will go here next.", "后续会在这里加入怪物相关功能。"));
                    return;
                case RoomMenuSection.Rewind:
                    DrawRoomRewindSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
                    return;
                case RoomMenuSection.Boss:
                    DrawRoomBossSection(sectionContentRect, buttonWidth, controlHeight, player, logger);
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

        private void DrawRoomRewindSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            float firstRowY = contentRect.y;
            float firstButtonY = firstRowY + 78f;
            float secondButtonY = firstButtonY + controlHeight + ButtonGap;
            float thirdButtonY = secondButtonY + controlHeight + ButtonGap;
            float rewindButtonWidth = buttonWidth * 2f + ButtonGap;
            float secondColumnX = contentRect.x + rewindButtonWidth + ButtonGap;
            bool recordingEnabled = _roomDebugCommandService != null && _roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled;
            bool playerRewindEnabled = _roomDebugCommandService != null && _roomDebugCommandService.IsPlayerRewindEnabled;
            bool rewindCleanupEnabled = _roomDebugCommandService == null || _roomDebugCommandService.IsRoomRewindCleanupEnabled;

            GUI.Label(
                new Rect(contentRect.x, firstRowY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.rewind.title", "Rewind", "回溯"),
                _hintStyle);

            GUI.Label(
                new Rect(contentRect.x, firstRowY + 22f, contentRect.width, 20f),
                GetLocalizedFallback(
                    "gui.room.rewind.shortcut",
                    "Shortcut: C",
                    "快捷键：C"),
                _hintStyle);

            GUI.Label(
                new Rect(contentRect.x, firstRowY + 44f, contentRect.width, 30f),
                GetRoomEnemyRefreshMethodDescription(),
                _wrappedHintStyle);

            if (DrawControllerButton(
                new Rect(contentRect.x, firstButtonY, rewindButtonWidth, controlHeight),
                "cmd.room.enemy_refresh_recording",
                GetLocalizedFallback(
                    recordingEnabled ? "gui.room.rewind.toggle.on" : "gui.room.rewind.toggle.off",
                    recordingEnabled ? "Rewind: ON" : "Rewind: OFF",
                    recordingEnabled ? "回溯：开启" : "回溯：关闭"),
                // Keep the toggle fill identical in both states. The enabled
                // state uses the selected border, while the disabled state
                // falls back to the ordinary button's unselected border.
                recordingEnabled ? _cursorColorSelectedButtonStyle : _buttonStyle))
            {
                ExecuteToggleRoomEnemyRefreshRecording(logger);
            }

            if (DrawControllerButton(
                new Rect(secondColumnX, firstButtonY, rewindButtonWidth, controlHeight),
                "cmd.room.enemy_refresh_method",
                GetRoomEnemyRefreshMethodLabel(),
                _buttonStyle))
            {
                ExecuteCycleRoomEnemyRefreshMethod(logger);
            }

            if (DrawControllerButton(
                new Rect(contentRect.x, secondButtonY, rewindButtonWidth, controlHeight),
                "cmd.room.player_rewind",
                GetLocalizedFallback(
                    playerRewindEnabled ? "gui.room.player_rewind.on" : "gui.room.player_rewind.off",
                    playerRewindEnabled ? "Player Rewind: ON" : "Player Rewind: OFF",
                    playerRewindEnabled ? "玩家回溯：开启" : "玩家回溯：关闭"),
                playerRewindEnabled ? _cursorColorSelectedButtonStyle : _buttonStyle))
            {
                ExecuteTogglePlayerRewind(logger);
            }

            if (DrawControllerButton(
                new Rect(secondColumnX, secondButtonY, rewindButtonWidth, controlHeight),
                "cmd.room.rewind_cleanup",
                GetLocalizedFallback(
                    rewindCleanupEnabled ? "gui.room.rewind_cleanup.on" : "gui.room.rewind_cleanup.off",
                    rewindCleanupEnabled ? "Rewind Cleanup: ON" : "Rewind Cleanup: OFF",
                    rewindCleanupEnabled ? "回溯清理：开启" : "回溯清理：关闭"),
                rewindCleanupEnabled ? _cursorColorSelectedButtonStyle : _buttonStyle))
            {
                ExecuteToggleRoomRewindCleanup(logger);
            }

            if (DrawControllerButton(
                new Rect(contentRect.x, thirdButtonY, rewindButtonWidth * 2f + ButtonGap, controlHeight),
                "cmd.room.enemy_refresh_execute",
                GetLocalizedFallback("gui.room.rewind.execute", "Spawn", "生成"),
                _buttonStyle))
            {
                ExecuteSelectedRoomEnemyRefresh(player, logger);
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

        private void DrawRoomBossSection(Rect contentRect, float buttonWidth, float controlHeight, PlayerController player, ManualLogSource logger)
        {
            long drawStartedAtTimestamp = BeginBossSelectionPagePerformanceStage();
            int bossOptionCount = 0;
            try
            {
                long optionsStartedAtTimestamp = BeginBossSelectionPagePerformanceStage();
                List<RoomBossOption> bossOptions = GetBossSelectionBossOptions();
                bossOptionCount = bossOptions.Count;
                LogBossSelectionPagePerformanceStage("Options", optionsStartedAtTimestamp, bossOptionCount);
            string currentBossNames = _roomDebugCommandService != null
                ? _roomDebugCommandService.GetSelectedBossName()
                : "Random";
            string currentFloorBossName = _roomDebugCommandService != null
                ? _roomDebugCommandService.GetCurrentFloorBossName()
                : "None";
            GUI.Label(
                new Rect(contentRect.x, contentRect.y, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.boss.current_floor_prefix", "Current floor Boss: ", "本层 Boss：") + currentFloorBossName,
                _hintStyle);
            GUI.Label(
                new Rect(contentRect.x, contentRect.y + 24f, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.boss.next_floor_prefix", "Next floor Boss: ", "下一层 Boss：") + currentBossNames,
                _hintStyle);
            GUI.Label(
                new Rect(contentRect.x, contentRect.y + 48f, contentRect.width, 20f),
                GetLocalizedFallback(
                    "gui.room.boss.hint",
                    "Select a Boss before the next floor is generated; no selection uses the first Boss.",
                    "在下一层生成前选择 Boss；不选择时使用第一个 Boss。"),
                _hintStyle);

            if (bossOptions.Count == 0)
            {
                GUI.Label(
                    new Rect(contentRect.x, contentRect.y + 76f, contentRect.width, 20f),
                    GetLocalizedFallback("gui.room.boss.empty", "No Boss options are available for the next floor.", "下一层没有可选择的 Boss。"),
                    _hintStyle);
                return;
            }

            const int bossOptionsPerRow = 4;
            float optionsTop = contentRect.y + 76f;
            for (int index = 0; index < bossOptions.Count; index++)
            {
                int row = index / bossOptionsPerRow;
                int column = index % bossOptionsPerRow;
                Rect buttonRect = new Rect(
                    contentRect.x + (column * (buttonWidth + ButtonGap)),
                    optionsTop + (row * (controlHeight + ButtonGap)),
                    buttonWidth,
                    controlHeight);
                RoomBossOption bossOption = bossOptions[index];
                GUIStyle style = (object)player != null && string.Equals(bossOption.BossName, currentBossNames, System.StringComparison.Ordinal)
                    ? _enabledButtonStyle
                    : _buttonStyle;
                if (DrawControllerButton(buttonRect, GetBossRoomControlId(index), GetBossOptionLabel(bossOption, index, bossOptions), style))
                {
                    ExecuteSwitchBoss(player, bossOption, logger);
                }
            }

            string selectedBossName = currentBossNames;
            List<RoomBossOption> roomOptions = !string.Equals(selectedBossName, "Random", System.StringComparison.Ordinal)
                ? GetBossRoomOptions(selectedBossName)
                : new List<RoomBossOption>();
            if (roomOptions.Count <= 1)
            {
                return;
            }

            int bossRowCount = (bossOptions.Count + bossOptionsPerRow - 1) / bossOptionsPerRow;
            float roomTitleY = optionsTop + (bossRowCount * (controlHeight + ButtonGap)) + 4f;
            GUI.Label(
                new Rect(contentRect.x, roomTitleY, contentRect.width, 20f),
                GetLocalizedFallback("gui.room.boss.room_title", "Room layout", "房间布局"),
                _hintStyle);
            float roomOptionsTop = roomTitleY + 24f;
            for (int index = 0; index < roomOptions.Count; index++)
            {
                int row = index / bossOptionsPerRow;
                int column = index % bossOptionsPerRow;
                Rect buttonRect = new Rect(
                    contentRect.x + (column * (buttonWidth + ButtonGap)),
                    roomOptionsTop + (row * (controlHeight + ButtonGap)),
                    buttonWidth,
                    controlHeight);
                RoomBossOption roomOption = roomOptions[index];
                GUIStyle style = BossManager.PriorFloorSelectedBossRoom == roomOption.BossRoomPrototype
                    ? _enabledButtonStyle
                    : _buttonStyle;
                string roomLabel = _roomDebugCommandService != null
                    ? _roomDebugCommandService.GetBossRoomDisplayName(roomOption)
                    : "Unknown Room";
                if (DrawControllerButton(buttonRect, GetBossRoomVariantControlId(index), roomLabel, style))
                {
                    ExecuteSwitchBoss(player, roomOption, logger);
                }
            }
            }
            finally
            {
                CompleteBossSelectionPagePerformanceTrace("Draw.complete", bossOptionCount, drawStartedAtTimestamp);
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
                SetRoomMenuSection(section);
            }
        }

        private void SetRoomMenuSection(RoomMenuSection section)
        {
            if (_roomMenuSection == section)
            {
                return;
            }

            if (_roomMenuSection == RoomMenuSection.Boss)
            {
                CancelBossSelectionPagePerformanceTrace();
            }

            _roomMenuSection = section;
            if (section == RoomMenuSection.Boss)
            {
                BeginBossSelectionPagePerformanceTrace();
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
            ControllerFocusEntry[] sectionFocusEntries = experimentalModeEnabled
                ? RoomSectionCommandPageFocusEntries
                : RoomStandardSectionCommandPageFocusEntries;
            if (_roomMenuSection == RoomMenuSection.Neutral)
            {
                return BuildCommandPageFocusEntries(
                    sectionFocusEntries,
                    RoomNeutralCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.Rewind)
            {
                return BuildCommandPageFocusEntries(sectionFocusEntries, RoomRewindCommandPageFocusEntries);
            }

            if (_roomMenuSection == RoomMenuSection.Boss)
            {
                return BuildCommandPageFocusEntries(sectionFocusEntries, BuildRoomBossCommandPageFocusEntries());
            }

            if (_roomMenuSection == RoomMenuSection.State && experimentalModeEnabled)
            {
                return BuildCommandPageFocusEntries(RoomSectionCommandPageFocusEntries);
            }

            return BuildCommandPageFocusEntries(sectionFocusEntries, RoomChestCommandPageFocusEntries);
        }

        private CommandPageActionBinding[] GetRoomCommandPageActionBindings(PlayerController player)
        {
            if (_roomMenuSection == RoomMenuSection.Boss)
            {
                List<CommandPageActionBinding> bossBindings = new List<CommandPageActionBinding>();
                List<RoomBossOption> bossOptions = GetBossSelectionBossOptions();
                for (int index = 0; index < bossOptions.Count; index++)
                {
                    RoomBossOption bossOption = bossOptions[index];
                    bossBindings.Add(new CommandPageActionBinding(
                        GetBossRoomControlId(index),
                        delegate { ExecuteSwitchBoss(player, bossOption, null); }));
                }

                string selectedBossName = _roomDebugCommandService != null
                    ? _roomDebugCommandService.GetSelectedBossName()
                    : "Random";
                List<RoomBossOption> roomOptions = !string.Equals(selectedBossName, "Random", System.StringComparison.Ordinal)
                    ? GetBossRoomOptions(selectedBossName)
                    : new List<RoomBossOption>();
                if (roomOptions.Count > 1)
                {
                    for (int index = 0; index < roomOptions.Count; index++)
                    {
                        RoomBossOption roomOption = roomOptions[index];
                        bossBindings.Add(new CommandPageActionBinding(
                            GetBossRoomVariantControlId(index),
                            delegate { ExecuteSwitchBoss(player, roomOption, null); }));
                    }
                }

                return bossBindings.ToArray();
            }

            return new[]
            {
                new CommandPageActionBinding("cmd.room.chest_tier.brown", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Brown); }),
                new CommandPageActionBinding("cmd.room.chest_tier.blue", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Blue); }),
                new CommandPageActionBinding("cmd.room.chest_tier.green", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Green); }),
                new CommandPageActionBinding("cmd.room.chest_tier.red", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Red); }),
                new CommandPageActionBinding("cmd.room.chest_tier.black", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Black); }),
                new CommandPageActionBinding("cmd.room.chest_tier.synergy", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Synergy); }),
                new CommandPageActionBinding("cmd.room.chest_tier.rainbow", delegate { ExecuteSpawnChest(player, null, RoomChestTier.Rainbow); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_recording", delegate { ExecuteToggleRoomEnemyRefreshRecording(null); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_method", delegate { ExecuteCycleRoomEnemyRefreshMethod(null); }),
                new CommandPageActionBinding("cmd.room.rewind_cleanup", delegate { ExecuteToggleRoomRewindCleanup(null); }),
                new CommandPageActionBinding("cmd.room.player_rewind", delegate { ExecuteTogglePlayerRewind(null); }),
                new CommandPageActionBinding("cmd.room.enemy_refresh_execute", delegate { ExecuteSelectedRoomEnemyRefresh(player, null); }),
                new CommandPageActionBinding("cmd.room.spawn_gunber_muncher", delegate { ExecuteSpawnGunberMuncher(player, null); }),
                new CommandPageActionBinding("cmd.room.spawn_evil_muncher", delegate { ExecuteSpawnEvilMuncher(player, null); }),
            };
        }

        private List<RoomBossOption> GetBossSelectionBossOptions()
        {
            return _roomDebugCommandService != null
                ? _roomDebugCommandService.GetBossSelectionBossOptions()
                : new List<RoomBossOption>();
        }

        private List<RoomBossOption> GetBossRoomOptions(string bossName)
        {
            return _roomDebugCommandService != null
                ? _roomDebugCommandService.GetBossRoomOptions(bossName)
                : new List<RoomBossOption>();
        }

        private static string GetBossRoomControlId(int index)
        {
            return "cmd.room.boss." + index;
        }

        private static string GetBossRoomVariantControlId(int index)
        {
            return "cmd.room.boss.room." + index;
        }

        private static string GetBossOptionLabel(RoomBossOption bossOption, int index, List<RoomBossOption> allOptions)
        {
            string bossName = bossOption != null && !string.IsNullOrEmpty(bossOption.BossName)
                ? bossOption.BossName
                : "Unknown Boss";
            int duplicateNumber = 1;
            if (allOptions != null)
            {
                for (int optionIndex = 0; optionIndex < index; optionIndex++)
                {
                    RoomBossOption previousOption = allOptions[optionIndex];
                    if (previousOption != null && string.Equals(previousOption.BossName, bossName, System.StringComparison.Ordinal))
                    {
                        duplicateNumber++;
                    }
                }
            }

            if (duplicateNumber > 1 || HasDuplicateBossName(bossName, allOptions))
            {
                string roomName = bossOption != null && bossOption.BossRoomPrototype != null
                    ? bossOption.BossRoomPrototype.name
                    : duplicateNumber.ToString();
                return bossName + " - " + roomName;
            }

            return bossName;
        }

        private static bool HasDuplicateBossName(string bossName, List<RoomBossOption> allOptions)
        {
            if (allOptions == null)
            {
                return false;
            }

            int matchCount = 0;
            for (int index = 0; index < allOptions.Count; index++)
            {
                RoomBossOption option = allOptions[index];
                if (option != null && string.Equals(option.BossName, bossName, System.StringComparison.Ordinal))
                {
                    matchCount++;
                    if (matchCount > 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private ControllerFocusEntry[] BuildRoomBossCommandPageFocusEntries()
        {
            List<RoomBossOption> bossOptions = GetBossSelectionBossOptions();
            if (bossOptions.Count == 0)
            {
                return EmptyRoomBossCommandPageFocusEntries;
            }

            const int bossOptionsPerRow = 4;
            List<ControllerFocusEntry> entries = new List<ControllerFocusEntry>();
            for (int index = 0; index < bossOptions.Count; index++)
            {
                entries.Add(new ControllerFocusEntry(
                    GetBossRoomControlId(index),
                    3 + (index / bossOptionsPerRow),
                    index % bossOptionsPerRow));
            }

            string selectedBossName = _roomDebugCommandService != null
                ? _roomDebugCommandService.GetSelectedBossName()
                : "Random";
            List<RoomBossOption> roomOptions = !string.Equals(selectedBossName, "Random", System.StringComparison.Ordinal)
                ? GetBossRoomOptions(selectedBossName)
                : new List<RoomBossOption>();
            if (roomOptions.Count > 1)
            {
                int bossRowCount = (bossOptions.Count + bossOptionsPerRow - 1) / bossOptionsPerRow;
                int roomStartRow = 4 + bossRowCount;
                for (int index = 0; index < roomOptions.Count; index++)
                {
                    entries.Add(new ControllerFocusEntry(
                        GetBossRoomVariantControlId(index),
                        roomStartRow + (index / bossOptionsPerRow),
                        index % bossOptionsPerRow));
                }
            }

            return entries.ToArray();
        }

        private int GetRoomCommandPageRowCount()
        {
            if (_roomMenuSection != RoomMenuSection.Boss)
            {
                return 5;
            }

            List<RoomBossOption> bossOptions = GetBossSelectionBossOptions();
            if (bossOptions.Count == 0)
            {
                return 5;
            }

            const int optionsPerRow = 4;
            int bossRowCount = (bossOptions.Count + optionsPerRow - 1) / optionsPerRow;
            int requiredRows = 4 + bossRowCount;
            string selectedBossName = _roomDebugCommandService != null
                ? _roomDebugCommandService.GetSelectedBossName()
                : "Random";
            List<RoomBossOption> roomOptions = !string.Equals(selectedBossName, "Random", System.StringComparison.Ordinal)
                ? GetBossRoomOptions(selectedBossName)
                : new List<RoomBossOption>();
            if (roomOptions.Count > 1)
            {
                requiredRows += 1 + ((roomOptions.Count + optionsPerRow - 1) / optionsPerRow);
            }

            return Mathf.Max(5, requiredRows);
        }

        private void ExecuteSwitchBoss(PlayerController player, RoomBossOption bossOption, ManualLogSource logger)
        {
            long startedAtTimestamp = BeginBossSelectionPagePerformanceStage();
            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.SelectBoss(bossOption, logger)
                : GrantCommandExecutionResult.Localized(false, "result.room.boss_room.unavailable");
            LogBossSelectionActionPerformance("SelectBoss", startedAtTimestamp, result);
            ShowRoomActionResult(result, logger);
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

        private void ExecuteSelectedRoomEnemyRefresh(PlayerController player, ManualLogSource logger)
        {
            if (_roomEnemyRefreshMethod == RoomEnemyRefreshMethod.RespawnEnemies)
            {
                ExecuteRefreshTemplateRoomEnemies(player, logger);
                return;
            }

            if (_roomDebugCommandService == null || !_roomDebugCommandService.IsRoomEnemyRefreshRecordingEnabled)
            {
                ShowStatus(
                    GetLocalizedFallback(
                        "result.room.rewind.recording_required",
                        "Enable Rewind before using C to rewind the room.",
                        "请先开启回溯功能，再使用 C 回溯房间。"),
                    true);
                return;
            }

            ExecuteRefreshRoomEnemies(player, logger);
        }

        private void ExecuteToggleRoomEnemyRefreshRecording(ManualLogSource logger)
        {
            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.ToggleRoomEnemyRefreshRecording()
                : GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            ShowRoomActionResult(result, logger);
        }

        private void ExecuteTogglePlayerRewind(ManualLogSource logger)
        {
            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.TogglePlayerRewind()
                : GrantCommandExecutionResult.Localized(false, "result.room.player_rewind.unavailable");
            ShowRoomActionResult(result, logger);
        }

        private void ExecuteToggleRoomRewindCleanup(ManualLogSource logger)
        {
            GrantCommandExecutionResult result = _roomDebugCommandService != null
                ? _roomDebugCommandService.ToggleRoomRewindCleanup()
                : GrantCommandExecutionResult.Localized(false, "result.room.rewind_cleanup.unavailable");
            ShowRoomActionResult(result, logger);
        }

        private void ExecuteRefreshTemplateRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            ShowRoomActionResult(_roomDebugCommandService.RefreshCurrentRoomEnemiesFromTemplate(player, logger), logger);
        }

        private void ExecuteCycleRoomEnemyRefreshMethod(ManualLogSource logger)
        {
            if (_roomDebugCommandService != null)
            {
                _roomDebugCommandService.EnsureRoomEnemyRefreshRecordingEnabled();
            }

            _roomEnemyRefreshMethod = _roomEnemyRefreshMethod == RoomEnemyRefreshMethod.Rewind
                ? RoomEnemyRefreshMethod.RespawnEnemies
                : RoomEnemyRefreshMethod.Rewind;
            if (_roomEnemyRefreshMethodSetter != null)
            {
                _roomEnemyRefreshMethodSetter(_roomEnemyRefreshMethod == RoomEnemyRefreshMethod.Rewind ? "rewind" : "respawn");
            }
            ShowStatus(GuiText.Get("result.room.rewind.method_changed", GetRoomEnemyRefreshMethodName()), false);

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Room enemy refresh method changed to " + _roomEnemyRefreshMethod + "."));
            }
        }

        private string GetRoomEnemyRefreshMethodLabel()
        {
            return GuiText.Get("gui.room.rewind.method", GetRoomEnemyRefreshMethodName());
        }

        private string GetRoomEnemyRefreshMethodDescription()
        {
            return _roomEnemyRefreshMethod == RoomEnemyRefreshMethod.RespawnEnemies
                ? GetLocalizedFallback(
                    "gui.room.rewind.mode.respawn",
                    "Respawn: generates enemies in this room; batches, types, and positions may differ.",
                    "重新生成：在当前房间重新生成敌人，批次、类型和站位可能不同。")
                : GetLocalizedFallback(
                    "gui.room.rewind.mode.rewind",
                    "Rewind: restores the same enemy batches, types, and positions as recorded.",
                    "回溯：恢复与记录时相同的敌人批次、类型和站位。");
        }

        private string GetRoomEnemyRefreshMethodName()
        {
            return _roomEnemyRefreshMethod == RoomEnemyRefreshMethod.RespawnEnemies
                ? GetLocalizedFallback("gui.room.button.refresh_template_enemies", "Respawn Enemies", "重新生成怪物")
                : GetLocalizedFallback("gui.room.button.refresh_enemies", "Rewind Room", "回溯房间");
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
            ShowStatus(executionResult.Message, GetRoomActionStatusSeverity(executionResult));

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

        private static StatusSeverity GetRoomActionStatusSeverity(GrantCommandExecutionResult executionResult)
        {
            if (executionResult != null && executionResult.Succeeded)
            {
                return StatusSeverity.Success;
            }

            string key = executionResult != null ? executionResult.LocalizationKey : string.Empty;
            if (key == "result.room.refresh_enemies.room_not_cleared" ||
                key == "result.room.rewind.boss_clear_pending" ||
                key == "result.room.rewind.boss_death_animation_pending" ||
                key == "result.room.refresh_enemies.no_snapshot" ||
                key == "result.room.rewind.recording_disabled" ||
                key == "result.room.rewind.no_enemies" ||
                key == "result.room.respawn_enemies.no_enemies" ||
                key == "result.room.enemy_refresh.corridor" ||
                key == "result.room.enemy_refresh.player_not_in_room")
            {
                return StatusSeverity.Warning;
            }

            return StatusSeverity.Failure;
        }
    }
}
