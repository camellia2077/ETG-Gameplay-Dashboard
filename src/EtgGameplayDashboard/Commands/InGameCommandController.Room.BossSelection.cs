// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
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
    }
}
