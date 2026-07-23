// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections.Generic;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class RoomDebugCommandService
    {
        public GrantCommandExecutionResult RefreshCurrentRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            GrantCommandExecutionResult roomValidationResult = ValidateEnemyRefreshRoom(player, currentRoom, true);
            LogRoomEnemyReplayDiagnostic(logger, "Rewind request validation. " + DescribeEnemyRefreshValidation(player, currentRoom, roomValidationResult));
            if (roomValidationResult != null)
            {
                return roomValidationResult;
            }

            if (_roomEnemyReplayService == null)
            {
                LogRoomRefreshWarning(logger, "Room enemy replay service is unavailable. Snapshot=" + DescribeRoomState(currentRoom) + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            }

            GrantCommandExecutionResult replayResult = _roomEnemyReplayService.Refresh(currentRoom, player);
            LogRoomEnemyReplayDiagnostic(logger, "Rewind request result. " + DescribeEnemyRefreshValidation(player, currentRoom, replayResult));
            return replayResult;
        }

        public bool IsRoomEnemyRefreshRecordingEnabled
        {
            get { return _roomEnemyReplayService != null && _roomEnemyReplayService.IsRecordingEnabled; }
        }

        public GrantCommandExecutionResult ToggleRoomEnemyRefreshRecording()
        {
            if (_roomEnemyReplayService == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            }

            bool enabled = _roomEnemyReplayService.ToggleRecording();
            return GrantCommandExecutionResult.Localized(
                true,
                enabled ? "result.room.enemy_refresh_recording.enabled" : "result.room.enemy_refresh_recording.disabled");
        }

        public void EnsureRoomEnemyRefreshRecordingEnabled()
        {
            if (_roomEnemyReplayService != null)
            {
                _roomEnemyReplayService.EnsureRecordingEnabled();
            }
        }

        public bool IsPlayerRewindEnabled
        {
            get { return _playerRewindEnabledProvider != null && _playerRewindEnabledProvider(); }
        }

        public GrantCommandExecutionResult TogglePlayerRewind()
        {
            if (_playerRewindEnabledSetter == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.player_rewind.unavailable");
            }

            bool enabled = !IsPlayerRewindEnabled;
            _playerRewindEnabledSetter(enabled);
            EnsureRoomEnemyRefreshRecordingEnabled();
            return GrantCommandExecutionResult.Localized(
                true,
                enabled ? "result.room.player_rewind.enabled" : "result.room.player_rewind.disabled");
        }

        public bool IsRoomRewindCleanupEnabled
        {
            get { return _roomRewindCleanupEnabledProvider == null || _roomRewindCleanupEnabledProvider(); }
        }

        public GrantCommandExecutionResult ToggleRoomRewindCleanup()
        {
            if (_roomRewindCleanupEnabledSetter == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind_cleanup.unavailable");
            }

            bool enabled = !IsRoomRewindCleanupEnabled;
            _roomRewindCleanupEnabledSetter(enabled);
            EnsureRoomEnemyRefreshRecordingEnabled();
            return GrantCommandExecutionResult.Localized(
                true,
                enabled ? "result.room.rewind_cleanup.enabled" : "result.room.rewind_cleanup.disabled");
        }

        public GrantCommandExecutionResult RefreshCurrentRoomEnemiesFromTemplate(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if (IsBossRoom(currentRoom))
            {
                LogRoomRefreshInfo(
                    logger,
                    "Boss room refresh method override. Requested=RespawnEnemies, Effective=Rewind, Room=" +
                    (currentRoom != null ? currentRoom.GetRoomName() : "<null>" ) + ".");
                return RefreshCurrentRoomEnemies(player, logger);
            }

            GrantCommandExecutionResult roomValidationResult = ValidateEnemyRefreshRoom(player, currentRoom, false);
            LogRoomEnemyReplayDiagnostic(logger, "Template respawn request validation. " + DescribeEnemyRefreshValidation(player, currentRoom, roomValidationResult));
            if (roomValidationResult != null)
            {
                return roomValidationResult;
            }

            LogRoomRefreshInfo(logger, "Template room enemy refresh requested. Before=" + DescribeRoomState(currentRoom) + ".");
            if (currentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                LogRoomRefreshWarning(logger, "Template room enemy refresh blocked because active enemies remain. Snapshot=" + DescribeRoomState(currentRoom) + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.room_not_cleared");
            }

            if (_roomEnemyReplayService != null)
            {
                _roomEnemyReplayService.ClearRoomRewindObjects(currentRoom);
            }

            try
            {
                PrototypeDungeonRoom prototypeRoom = ResolvePrototypeRoom(currentRoom);
                RuntimePrototypeRoomData runtimePrototypeData = ResolveRuntimePrototypeData(currentRoom);
                List<PrototypePlacedObjectData> placedObjects = null;
                List<Vector2> placedObjectPositions = null;

                if ((object)prototypeRoom != null)
                {
                    placedObjects = prototypeRoom.placedObjects;
                    placedObjectPositions = prototypeRoom.placedObjectPositions;
                }
                else if ((object)runtimePrototypeData != null)
                {
                    placedObjects = runtimePrototypeData.placedObjects;
                    placedObjectPositions = runtimePrototypeData.placedObjectPositions;
                }

                if (placedObjects == null || placedObjectPositions == null)
                {
                    LogRoomRefreshWarning(logger, "Template room enemy refresh could not resolve prototype room data. Snapshot=" + DescribeRoomState(currentRoom) + ".");
                    return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
                }

                int scannedEnemySlots = 0;
                int spawnedEnemyCount = 0;
                int missingEnemyPrefabs = 0;
                for (int index = 0; index < placedObjects.Count && index < placedObjectPositions.Count; index++)
                {
                    PrototypePlacedObjectData placedObject = placedObjects[index];
                    if ((object)placedObject == null || placedObject.spawnChance <= 0f)
                    {
                        continue;
                    }

                    string resolvedEnemyGuid = null;
                    AIActor enemyPrefab = ResolveEnemyPrefab(placedObject, out resolvedEnemyGuid);
                    if ((object)enemyPrefab == null && string.IsNullOrEmpty(resolvedEnemyGuid))
                    {
                        continue;
                    }

                    scannedEnemySlots++;
                    if ((object)enemyPrefab == null)
                    {
                        missingEnemyPrefabs++;
                        continue;
                    }

                    Vector2 relativePosition = placedObjectPositions[index];
                    IntVector2 spawnCell = new IntVector2(
                        currentRoom.area.basePosition.x + Mathf.RoundToInt(relativePosition.x),
                        currentRoom.area.basePosition.y + Mathf.RoundToInt(relativePosition.y));
                    if ((object)AIActor.Spawn(enemyPrefab, spawnCell, currentRoom, true, default(AIActor.AwakenAnimationType), true) != null)
                    {
                        spawnedEnemyCount++;
                    }
                }

                if (spawnedEnemyCount > 0 || currentRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All) > 0)
                {
                    currentRoom.SealRoom();
                }

                int activeEnemyCount = currentRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All);
                LogRoomRefreshInfo(
                    logger,
                    "Template room enemy refresh completed. Source=" + ((object)prototypeRoom != null ? "PrototypeRoom" : "RuntimePrototypeData") +
                    ", ScannedEnemySlots=" + scannedEnemySlots +
                    ", SpawnedEnemyCount=" + spawnedEnemyCount +
                    ", MissingEnemyPrefabs=" + missingEnemyPrefabs +
                    ", After=" + DescribeRoomState(currentRoom) + ".");
                return activeEnemyCount > 0
                    ? GrantCommandExecutionResult.Localized(true, "result.room.respawn_enemies.success", activeEnemyCount)
                    : GrantCommandExecutionResult.Localized(false, "result.room.respawn_enemies.no_enemies");
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(logger, "Template room enemy refresh threw. Snapshot=" + DescribeRoomState(currentRoom) + ", Exception=" + exception + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            }
        }

        private static GrantCommandExecutionResult ValidateEnemyRefreshRoom(PlayerController player, RoomHandler room, bool allowBossRoom)
        {
            if ((object)room == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            if (!room.IsStandardRoom && (!allowBossRoom || !IsBossRoom(room)))
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.enemy_refresh.corridor");
            }

            if (!room.CellsWithoutExits.Contains(player.transform.position.IntXY()))
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.enemy_refresh.player_not_in_room");
            }

            return null;
        }

        private static string DescribeEnemyRefreshValidation(PlayerController player, RoomHandler room, GrantCommandExecutionResult result)
        {
            IntVector2 playerPosition = (object)player != null ? player.transform.position.IntXY() : IntVector2.Zero;
            CellData playerCell = GameManager.Instance != null && GameManager.Instance.Dungeon != null && GameManager.Instance.Dungeon.data != null
                ? GameManager.Instance.Dungeon.data[playerPosition]
                : null;
            bool isBossRoom = IsBossRoom(room);
            return
                DescribeRoomState(room) +
                ", IsBossRoom=" + isBossRoom +
                ", PlayerCell=" + playerPosition.x + "," + playerPosition.y +
                ", ContainsPlayerRaw=" + ((object)room != null && room.ContainsPosition(playerPosition)) +
                ", ContainsPlayerInterior=" + ((object)room != null && room.CellsWithoutExits.Contains(playerPosition)) +
                ", PlayerCellIsExit=" + (playerCell != null && playerCell.isExitCell) +
                ", RejectionReason=" + GetEnemyRefreshRejectionReason(room, player, result) +
                ", ResultKey=" + (result != null ? result.LocalizationKey : "<allowed>") + ".";
        }

        private static string GetEnemyRefreshRejectionReason(RoomHandler room, PlayerController player, GrantCommandExecutionResult result)
        {
            if (result == null)
            {
                return "<none>";
            }

            if ((object)room == null)
            {
                return "RoomUnavailable";
            }

            if (!room.IsStandardRoom && !IsBossRoom(room))
            {
                return "NonStandardRoom";
            }

            if ((object)player != null && !room.CellsWithoutExits.Contains(player.transform.position.IntXY()))
            {
                return "PlayerOutsideRoomInterior";
            }

            return "CommandOrReplayService";
        }

        private static bool IsBossRoom(RoomHandler room)
        {
            return room != null &&
                   room.area != null &&
                   room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS;
        }
    }
}
