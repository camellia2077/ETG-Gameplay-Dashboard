// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class RoomDebugCommandService
    {
        public GrantCommandExecutionResult SpawnGunberMuncher(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if ((object)currentRoom == null || (object)currentRoom.area == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            string currentSceneName = GetCurrentSceneName();
            if (string.Equals(currentSceneName, LoadingDungeonSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return QueueSpawnGunberMuncher(player, logger, currentSceneName);
            }

            return SpawnGunberMuncherNow(player, logger);
        }

        public GrantCommandExecutionResult SpawnEvilMuncher(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if ((object)currentRoom == null || (object)currentRoom.area == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            string currentSceneName = GetCurrentSceneName();
            if (string.Equals(currentSceneName, LoadingDungeonSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return QueueSpawnEvilMuncher(player, logger, currentSceneName);
            }

            return SpawnEvilMuncherNow(player, logger);
        }

        private GrantCommandExecutionResult QueueSpawnGunberMuncher(PlayerController player, ManualLogSource logger, string currentSceneName)
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Gunber Muncher spawn queue failed because GameManager was unavailable. " +
                    "CurrentScene=" + currentSceneName +
                    ".");
                return CreateSpawnGunberMuncherResult(false, "result.room.spawn_gunber_muncher.failed");
            }

            if (_gunberMuncherSpawnQueued)
            {
                LogMuncherInfo(
                    logger,
                    "Gunber Muncher spawn request ignored because a queued retry is already pending. " +
                    "CurrentScene=" + currentSceneName +
                    ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                    ".");
                return CreateSpawnGunberMuncherResult(true, "result.room.spawn_gunber_muncher.queued");
            }

            _gunberMuncherSpawnQueued = true;
            LogMuncherInfo(
                logger,
                "Gunber Muncher spawn deferred because the game is still in the loading scene. " +
                "CurrentScene=" + currentSceneName +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                ", Room=" + DescribeRoomState(player.CurrentRoom) +
                ".");
            gameManager.StartCoroutine(SpawnGunberMuncherWhenSceneReady(player, logger));
            return CreateSpawnGunberMuncherResult(true, "result.room.spawn_gunber_muncher.queued");
        }

        private IEnumerator SpawnGunberMuncherWhenSceneReady(PlayerController requestedPlayer, ManualLogSource logger)
        {
            const int maxRetryCount = 20;
            const float retryDelaySeconds = 0.25f;

            for (int attempt = 1; attempt <= maxRetryCount; attempt++)
            {
                yield return new WaitForSeconds(retryDelaySeconds);

                GameManager gameManager = GameManager.Instance;
                PlayerController player = (object)requestedPlayer != null ? requestedPlayer : (gameManager != null ? gameManager.PrimaryPlayer : null);
                string currentSceneName = GetCurrentSceneName();
                RoomHandler currentRoom = (object)player != null ? player.CurrentRoom : null;
                bool isSceneReady =
                    !string.Equals(currentSceneName, LoadingDungeonSceneName, System.StringComparison.OrdinalIgnoreCase) &&
                    (object)player != null &&
                    (object)currentRoom != null &&
                    (object)currentRoom.area != null;

                LogMuncherInfo(
                    logger,
                    "Gunber Muncher deferred spawn poll. " +
                    "Attempt=" + attempt +
                    "/" + maxRetryCount +
                    ", CurrentScene=" + currentSceneName +
                    ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                    ", PlayerPresent=" + ((object)player != null) +
                    ", RoomReady=" + ((object)currentRoom != null && (object)currentRoom.area != null) +
                    ".");

                if (!isSceneReady)
                {
                    continue;
                }

                _gunberMuncherSpawnQueued = false;
                GrantCommandExecutionResult executionResult = SpawnGunberMuncherNow(player, logger);
                if (logger != null)
                {
                    if (executionResult.Succeeded)
                    {
                        LogMuncherInfo(logger, "Deferred Gunber Muncher spawn completed. " + executionResult.LogMessage);
                    }
                    else
                    {
                        logger.LogWarning(EtgGameplayDashboardLog.Command("Deferred Gunber Muncher spawn failed. " + executionResult.LogMessage));
                    }
                }

                yield break;
            }

            _gunberMuncherSpawnQueued = false;
            LogRoomRefreshWarning(
                logger,
                "Gunber Muncher deferred spawn timed out before the scene became ready. " +
                "CurrentScene=" + GetCurrentSceneName() +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(GameManager.Instance) +
                ".");
        }

        private GrantCommandExecutionResult QueueSpawnEvilMuncher(PlayerController player, ManualLogSource logger, string currentSceneName)
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Evil Muncher spawn queue failed because GameManager was unavailable. " +
                    "CurrentScene=" + currentSceneName +
                    ".");
                return CreateSpawnEvilMuncherResult(false, "result.room.spawn_evil_muncher.failed");
            }

            if (_evilMuncherSpawnQueued)
            {
                LogMuncherInfo(
                    logger,
                    "Evil Muncher spawn request ignored because a queued retry is already pending. " +
                    "CurrentScene=" + currentSceneName +
                    ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                    ".");
                return CreateSpawnEvilMuncherResult(true, "result.room.spawn_evil_muncher.queued");
            }

            _evilMuncherSpawnQueued = true;
            LogMuncherInfo(
                logger,
                "Evil Muncher spawn deferred because the game is still in the loading scene. " +
                "CurrentScene=" + currentSceneName +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                ", Room=" + DescribeRoomState(player.CurrentRoom) +
                ".");
            gameManager.StartCoroutine(SpawnEvilMuncherWhenSceneReady(player, logger));
            return CreateSpawnEvilMuncherResult(true, "result.room.spawn_evil_muncher.queued");
        }

        private IEnumerator SpawnEvilMuncherWhenSceneReady(PlayerController requestedPlayer, ManualLogSource logger)
        {
            const int maxRetryCount = 20;
            const float retryDelaySeconds = 0.25f;

            for (int attempt = 1; attempt <= maxRetryCount; attempt++)
            {
                yield return new WaitForSeconds(retryDelaySeconds);

                GameManager gameManager = GameManager.Instance;
                PlayerController player = (object)requestedPlayer != null ? requestedPlayer : (gameManager != null ? gameManager.PrimaryPlayer : null);
                string currentSceneName = GetCurrentSceneName();
                RoomHandler currentRoom = (object)player != null ? player.CurrentRoom : null;
                bool isSceneReady =
                    !string.Equals(currentSceneName, LoadingDungeonSceneName, System.StringComparison.OrdinalIgnoreCase) &&
                    (object)player != null &&
                    (object)currentRoom != null &&
                    (object)currentRoom.area != null;

                LogMuncherInfo(
                    logger,
                    "Evil Muncher deferred spawn poll. " +
                    "Attempt=" + attempt +
                    "/" + maxRetryCount +
                    ", CurrentScene=" + currentSceneName +
                    ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                    ", PlayerPresent=" + ((object)player != null) +
                    ", RoomReady=" + ((object)currentRoom != null && (object)currentRoom.area != null) +
                    ".");

                if (!isSceneReady)
                {
                    continue;
                }

                _evilMuncherSpawnQueued = false;
                GrantCommandExecutionResult executionResult = SpawnEvilMuncherNow(player, logger);
                if (logger != null)
                {
                    if (executionResult.Succeeded)
                    {
                        LogMuncherInfo(logger, "Deferred Evil Muncher spawn completed. " + executionResult.LogMessage);
                    }
                    else
                    {
                        logger.LogWarning(EtgGameplayDashboardLog.Command("Deferred Evil Muncher spawn failed. " + executionResult.LogMessage));
                    }
                }

                yield break;
            }

            _evilMuncherSpawnQueued = false;
            LogRoomRefreshWarning(
                logger,
                "Evil Muncher deferred spawn timed out before the scene became ready. " +
                "CurrentScene=" + GetCurrentSceneName() +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(GameManager.Instance) +
                ".");
        }

        private GrantCommandExecutionResult SpawnGunberMuncherNow(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if ((object)currentRoom == null || (object)currentRoom.area == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            LogMuncherInfo(
                logger,
                "Gunber Muncher spawn requested. " +
                "CurrentScene=" + GetCurrentSceneName() +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(GameManager.Instance) +
                ", Room=" + DescribeRoomState(currentRoom) +
                ".");

            bool success;
            int existingMuncherCount;
            IntVector2 spawnCell = FindMuncherSpawnCell(currentRoom, out success, out existingMuncherCount);

            IntVector2 relativeSpawnCell = spawnCell - currentRoom.area.basePosition + IntVector2.One;
            GameObject spawnedObject = null;
            DungeonPlaceableBehaviour sourceBehaviour = ResolveGunberMuncherBehaviour(logger);
            if ((object)sourceBehaviour != null)
            {
                LogMuncherInfo(
                    logger,
                    "Gunber Muncher source behaviour resolved. " +
                    "Source=" + DescribeGameObject(sourceBehaviour.gameObject) +
                    ", SourceDetails=" + DescribeSpawnedObjectState(sourceBehaviour.gameObject, currentRoom) +
                    ", SpawnCellSelectionSucceeded=" + success +
                    ", ExistingMuncherCountInRoom=" + existingMuncherCount +
                    ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                    ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                    ", RoomBasePosition=" + currentRoom.area.basePosition.x + "," + currentRoom.area.basePosition.y +
                    ".");

                try
                {
                    spawnedObject = sourceBehaviour.InstantiateObject(currentRoom, relativeSpawnCell, deferConfiguration: false);
                }
                catch (System.Exception exception)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Gunber Muncher source behaviour instantiate threw an exception. " +
                        "Source=" + DescribeGameObject(sourceBehaviour.gameObject) +
                        ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                        ", Room=" + DescribeRoomState(currentRoom) +
                        ", Exception=" + exception +
                        ".");
                }
            }

            if ((object)spawnedObject == null)
            {
                GameObject prefab = ResolveGunberMuncherPrefab(logger);
                if ((object)prefab == null)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Gunber Muncher resolution failed. " +
                        "No dungeon source behaviour or prefab candidate produced a spawnable object. " +
                        "Room=" + DescribeRoomState(currentRoom) +
                        ".");
                    return CreateSpawnGunberMuncherResult(false, "result.room.spawn_gunber_muncher.prefab_missing");
                }

                LogMuncherInfo(
                    logger,
                    "Gunber Muncher prefab resolved. " +
                    "Prefab=" + DescribeGameObject(prefab) +
                    ", PrefabDetails=" + DescribeSpawnedObjectState(prefab, currentRoom) +
                    ", SpawnCellSelectionSucceeded=" + success +
                    ", ExistingMuncherCountInRoom=" + existingMuncherCount +
                    ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                    ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                    ", RoomBasePosition=" + currentRoom.area.basePosition.x + "," + currentRoom.area.basePosition.y +
                    ".");

                try
                {
                    spawnedObject = DungeonPlaceableUtility.InstantiateDungeonPlaceable(prefab, currentRoom, relativeSpawnCell, deferConfiguration: false);
                }
                catch (System.Exception exception)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Gunber Muncher instantiate threw an exception. " +
                        "Prefab=" + DescribeGameObject(prefab) +
                        ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                        ", Room=" + DescribeRoomState(currentRoom) +
                        ", Exception=" + exception +
                        ".");
                    return CreateSpawnGunberMuncherResult(false, "result.room.spawn_gunber_muncher.failed");
                }
            }

            try
            {
                InitializeSpawnedRigidbodies(spawnedObject);
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Gunber Muncher rigidbody initialization threw an exception. " +
                    "SpawnedObject=" + DescribeGameObject(spawnedObject) +
                    ", SpawnedDetails=" + DescribeSpawnedObjectState(spawnedObject, currentRoom) +
                    ", Room=" + DescribeRoomState(currentRoom) +
                    ", Exception=" + exception +
                    ".");
            }

            if ((object)spawnedObject == null)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Gunber Muncher instantiate returned null. " +
                    "RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                    ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                    ", Room=" + DescribeRoomState(currentRoom) +
                    ".");
                return CreateSpawnGunberMuncherResult(false, "result.room.spawn_gunber_muncher.failed");
            }

            LogMuncherInfo(
                logger,
                "Gunber Muncher post-instantiate snapshot. " +
                "SpawnedObject=" + DescribeGameObject(spawnedObject) +
                ", SpawnedDetails=" + DescribeSpawnedObjectState(spawnedObject, currentRoom) +
                ", ExistingMuncherCountBeforeSpawn=" + existingMuncherCount +
                ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                ".");

            int interactableCount = RegisterRoomInteractables(currentRoom, spawnedObject);
            LogMuncherInfo(
                logger,
                "Gunber Muncher spawned successfully. " +
                "SpawnedObject=" + DescribeGameObject(spawnedObject) +
                ", SpawnedDetails=" + DescribeSpawnedObjectState(spawnedObject, currentRoom) +
                ", ExistingMuncherCountBeforeSpawn=" + existingMuncherCount +
                ", InteractableCount=" + interactableCount +
                ", Room=" + DescribeRoomState(currentRoom) +
                ".");
            return CreateSpawnGunberMuncherResult(true, "result.room.spawn_gunber_muncher.success");
        }

        private GrantCommandExecutionResult SpawnEvilMuncherNow(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if ((object)currentRoom == null || (object)currentRoom.area == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            LogMuncherInfo(
                logger,
                "Evil Muncher spawn requested. " +
                "CurrentScene=" + GetCurrentSceneName() +
                ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(GameManager.Instance) +
                ", Room=" + DescribeRoomState(currentRoom) +
                ".");

            bool success;
            int existingMuncherCount;
            IntVector2 spawnCell = FindMuncherSpawnCell(currentRoom, out success, out existingMuncherCount);

            IntVector2 relativeSpawnCell = spawnCell - currentRoom.area.basePosition + IntVector2.One;
            GameObject spawnedObject = null;
            DungeonPlaceableBehaviour sourceBehaviour = ResolveEvilMuncherBehaviour(logger);
            if ((object)sourceBehaviour != null)
            {
                LogMuncherInfo(
                    logger,
                    "Evil Muncher source behaviour resolved. " +
                    "Source=" + DescribeGameObject(sourceBehaviour.gameObject) +
                    ", SourceDetails=" + DescribeSpawnedObjectState(sourceBehaviour.gameObject, currentRoom) +
                    ", SpawnCellSelectionSucceeded=" + success +
                    ", ExistingMuncherCountInRoom=" + existingMuncherCount +
                    ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                    ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                    ", RoomBasePosition=" + currentRoom.area.basePosition.x + "," + currentRoom.area.basePosition.y +
                    ".");

                try
                {
                    spawnedObject = sourceBehaviour.InstantiateObject(currentRoom, relativeSpawnCell, deferConfiguration: false);
                }
                catch (System.Exception exception)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Evil Muncher source behaviour instantiate threw an exception. " +
                        "Source=" + DescribeGameObject(sourceBehaviour.gameObject) +
                        ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                        ", Room=" + DescribeRoomState(currentRoom) +
                        ", Exception=" + exception +
                        ".");
                }
            }

            if ((object)spawnedObject == null)
            {
                GameObject prefab = ResolveEvilMuncherPrefab(logger);
                if ((object)prefab == null)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Evil Muncher resolution failed. " +
                        "No source behaviour or prefab produced a spawnable object. " +
                        "Room=" + DescribeRoomState(currentRoom) +
                        ".");
                    return CreateSpawnEvilMuncherResult(false, "result.room.spawn_evil_muncher.prefab_missing");
                }

                LogMuncherInfo(
                    logger,
                    "Evil Muncher prefab resolved. " +
                    "Prefab=" + DescribeGameObject(prefab) +
                    ", PrefabDetails=" + DescribeSpawnedObjectState(prefab, currentRoom) +
                    ", SpawnCellSelectionSucceeded=" + success +
                    ", ExistingMuncherCountInRoom=" + existingMuncherCount +
                    ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                    ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                    ", RoomBasePosition=" + currentRoom.area.basePosition.x + "," + currentRoom.area.basePosition.y +
                    ".");

                try
                {
                    spawnedObject = DungeonPlaceableUtility.InstantiateDungeonPlaceable(prefab, currentRoom, relativeSpawnCell, deferConfiguration: false);
                }
                catch (System.Exception exception)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Evil Muncher instantiate threw an exception. " +
                        "Prefab=" + DescribeGameObject(prefab) +
                        ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                        ", Room=" + DescribeRoomState(currentRoom) +
                        ", Exception=" + exception +
                        ".");
                    return CreateSpawnEvilMuncherResult(false, "result.room.spawn_evil_muncher.failed");
                }
            }

            try
            {
                InitializeSpawnedRigidbodies(spawnedObject);
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Evil Muncher rigidbody initialization threw an exception. " +
                    "SpawnedObject=" + DescribeGameObject(spawnedObject) +
                    ", SpawnedDetails=" + DescribeSpawnedObjectState(spawnedObject, currentRoom) +
                    ", Room=" + DescribeRoomState(currentRoom) +
                    ", Exception=" + exception +
                    ".");
            }

            if ((object)spawnedObject == null)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Evil Muncher instantiate returned null. " +
                    "RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                    ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                    ", Room=" + DescribeRoomState(currentRoom) +
                    ".");
                return CreateSpawnEvilMuncherResult(false, "result.room.spawn_evil_muncher.failed");
            }

            LogMuncherInfo(
                logger,
                "Evil Muncher post-instantiate snapshot. " +
                "SpawnedObject=" + DescribeGameObject(spawnedObject) +
                ", SpawnedDetails=" + DescribeSpawnedObjectState(spawnedObject, currentRoom) +
                ", ExistingMuncherCountBeforeSpawn=" + existingMuncherCount +
                ", AbsoluteSpawnCell=" + spawnCell.x + "," + spawnCell.y +
                ", RelativeSpawnCell=" + relativeSpawnCell.x + "," + relativeSpawnCell.y +
                ".");

            int interactableCount = RegisterRoomInteractables(currentRoom, spawnedObject);
            LogMuncherInfo(
                logger,
                "Evil Muncher spawned successfully. " +
                "SpawnedObject=" + DescribeGameObject(spawnedObject) +
                ", SpawnedDetails=" + DescribeSpawnedObjectState(spawnedObject, currentRoom) +
                ", ExistingMuncherCountBeforeSpawn=" + existingMuncherCount +
                ", InteractableCount=" + interactableCount +
                ", Room=" + DescribeRoomState(currentRoom) +
                ".");
            return CreateSpawnEvilMuncherResult(true, "result.room.spawn_evil_muncher.success");
        }

    }
}

