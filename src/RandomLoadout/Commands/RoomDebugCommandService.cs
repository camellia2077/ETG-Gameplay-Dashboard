// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal enum RoomChestTier
    {
        Brown,
        Blue,
        Green,
        Red,
        Black,
        Synergy,
        Rainbow,
    }

    internal sealed class RoomDebugCommandService
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo CellAreaPrototypeRoomField = typeof(CellArea).GetField("m_prototypeRoom", InstanceFlags);
        private const string LoadingDungeonSceneName = "LoadingDungeon";
        private const string GunberMuncherAssetBundleName = "shared_auto_002";
        private const string EvilMuncherAssetBundleName = "shared_auto_001";
        private const string GunberMuncherPrefabAssetPath = "Assets/data/prefabs/npcs/NPC_GunberMuncher.prefab";
        private const string GunberMuncherRoomAssetPath = "Assets/data/rooms/shop rooms/SubShop_Muncher_01.asset";
        private const string EvilMuncherPrefabAssetPath = "Assets/data/prefabs/npcs/NPC_GunberMuncher_Evil.prefab";
        private const string EvilMuncherRoomAssetPath = "Assets/data/rooms/shop rooms/SubShop_EvilMuncher_01.asset";
        private static readonly IntVector2[] MuncherSpawnOffsetCandidates = new[]
        {
            IntVector2.Zero,
            new IntVector2(5, 0),
            new IntVector2(-5, 0),
            new IntVector2(0, 5),
            new IntVector2(0, -5),
            new IntVector2(5, 5),
            new IntVector2(-5, 5),
            new IntVector2(5, -5),
            new IntVector2(-5, -5),
            new IntVector2(8, 0),
            new IntVector2(-8, 0),
            new IntVector2(0, 8),
            new IntVector2(0, -8),
        };
        private static readonly string[] GunberMuncherPrefabCandidates = new[]
        {
            "NPC_GunberMuncher",
            "Npc_GunberMuncher",
            "npc_gunbermuncher",
        };
        private readonly System.Func<bool> _mapTeleportVerboseLoggingEnabledProvider;
        private static System.Func<bool> _muncherVerboseLoggingEnabledProvider;
        private bool _gunberMuncherSpawnQueued;
        private bool _evilMuncherSpawnQueued;

        public RoomDebugCommandService(System.Func<bool> mapTeleportVerboseLoggingEnabledProvider, System.Func<bool> muncherVerboseLoggingEnabledProvider)
        {
            _mapTeleportVerboseLoggingEnabledProvider = mapTeleportVerboseLoggingEnabledProvider;
            _muncherVerboseLoggingEnabledProvider = muncherVerboseLoggingEnabledProvider;
        }

        public GrantCommandExecutionResult SpawnChest(PlayerController player, RoomChestTier chestTier)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if ((object)currentRoom == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            RewardManager rewards = GameManager.Instance != null ? GameManager.Instance.RewardManager : null;
            if ((object)rewards == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.reward_manager_not_ready");
            }

            Chest chestPrefab = ResolveChestPrefab(rewards, chestTier);
            if ((object)chestPrefab == null)
            {
                return CreateChestResult(false, "result.room.spawn_chest.prefab_missing", chestTier);
            }

            IntVector2 spawnLocation = currentRoom.GetBestRewardLocation(new IntVector2(2, 1), RoomHandler.RewardLocationStyle.PlayerCenter, true);
            Chest spawnedChest = Chest.Spawn(chestPrefab, spawnLocation, currentRoom, true);
            if ((object)spawnedChest == null)
            {
                return CreateChestResult(false, "result.room.spawn_chest.failed", chestTier);
            }

            spawnedChest.ForceUnlock();
            return CreateChestResult(true, "result.room.spawn_chest.success", chestTier);
        }

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
                        logger.LogWarning(RandomLoadoutLog.Command("Deferred Gunber Muncher spawn failed. " + executionResult.LogMessage));
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
                        logger.LogWarning(RandomLoadoutLog.Command("Deferred Evil Muncher spawn failed. " + executionResult.LogMessage));
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

        public GrantCommandExecutionResult RefreshCurrentRoomEnemies(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            RoomHandler currentRoom = player.CurrentRoom;
            if ((object)currentRoom == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.room_not_ready");
            }

            LogRoomRefreshInfo(logger, "Room enemy refresh requested. Before=" + DescribeRoomState(currentRoom) + ".");
            if (currentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                LogRoomRefreshWarning(logger, "Room enemy refresh blocked because active enemies remain. Snapshot=" + DescribeRoomState(currentRoom) + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.room_not_cleared");
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
                    LogRoomRefreshWarning(
                        logger,
                        "Room enemy refresh could not resolve prototype room data. " +
                        "AreaPresent=" + ((object)currentRoom.area != null) +
                        ", PropertyPrototypePresent=" + (((object)currentRoom.area != null && (object)currentRoom.area.prototypeRoom != null)) +
                        ", FieldPrototypePresent=" + HasPrototypeRoomFieldValue(currentRoom.area) +
                        ", RuntimePrototypePresent=" + ((object)runtimePrototypeData != null) +
                        ", Snapshot=" + DescribeRoomState(currentRoom) +
                        ".");
                    return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
                }

                int prototypeObjectCount = placedObjects.Count;
                int prototypePositionCount = placedObjectPositions.Count;
                int scannedEnemySlots = 0;
                int spawnedEnemyCount = 0;
                int missingEnemyPrefabs = 0;
                List<string> spawnedEnemyGuids = new List<string>();

                for (int index = 0; index < prototypeObjectCount && index < prototypePositionCount; index++)
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
                    AIActor spawnedEnemy = AIActor.Spawn(enemyPrefab, spawnCell, currentRoom, true, default(AIActor.AwakenAnimationType), true);
                    if ((object)spawnedEnemy == null)
                    {
                        continue;
                    }

                    spawnedEnemyCount++;
                    spawnedEnemyGuids.Add(string.IsNullOrEmpty(resolvedEnemyGuid) ? "<non_database>" : resolvedEnemyGuid);
                }

                LogRoomRefreshInfo(
                    logger,
                    "Room enemy refresh invoked prototype spawn chain. " +
                    "Source=" + ((object)prototypeRoom != null ? "PrototypeRoom" : "RuntimePrototypeData") +
                    ", " +
                    "PrototypeObjectCount=" + prototypeObjectCount +
                    ", PrototypePositionCount=" + prototypePositionCount +
                    ", ScannedEnemySlots=" + scannedEnemySlots +
                    ", SpawnedEnemyCount=" + spawnedEnemyCount +
                    ", MissingEnemyPrefabs=" + missingEnemyPrefabs +
                    ", SpawnedEnemyGuids=[" + string.Join(",", spawnedEnemyGuids.ToArray()) + "]" +
                    ", AfterSpawn=" + DescribeRoomState(currentRoom) +
                    ".");

                if (spawnedEnemyCount > 0 || currentRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All) > 0)
                {
                    currentRoom.SealRoom();
                }

                int activeEnemyCount = currentRoom.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All);
                if (activeEnemyCount > 0)
                {
                    LogRoomRefreshInfo(logger, "Room enemy refresh succeeded. ActiveEnemyCount=" + activeEnemyCount + ", Snapshot=" + DescribeRoomState(currentRoom) + ".");
                    return GrantCommandExecutionResult.Localized(true, "result.room.refresh_enemies.success", activeEnemyCount);
                }

                LogRoomRefreshWarning(logger, "Room enemy refresh completed but no active enemies were detected. Snapshot=" + DescribeRoomState(currentRoom) + ".");
                return GrantCommandExecutionResult.Localized(true, "result.room.refresh_enemies.no_enemies_spawned");
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(logger, "Room enemy refresh threw an exception. Snapshot=" + DescribeRoomState(currentRoom) + ", Exception=" + exception + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            }
        }

        public GrantCommandExecutionResult RevealCurrentFloorMap(PlayerController player, ManualLogSource logger)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
            }

            GameManager gameManager = GameManager.Instance;
            Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
            List<RoomHandler> rooms = dungeon != null && dungeon.data != null ? dungeon.data.rooms : null;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            if ((object)dungeon == null || dungeon.data == null || rooms == null || (object)minimap == null)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Map reveal unavailable. " +
                    "GameManagerPresent=" + ((object)gameManager != null) +
                    ", DungeonPresent=" + ((object)dungeon != null) +
                    ", DungeonDataPresent=" + ((object)dungeon != null && dungeon.data != null) +
                    ", RoomsPresent=" + (rooms != null) +
                    ", MinimapPresent=" + ((object)minimap != null) +
                    ".");
                return GrantCommandExecutionResult.Localized(false, "result.map_reveal.unavailable");
            }

            int revealedRoomCount = 0;
            int teleporterActivatedCount = 0;
            int teleportableRoomCount = 0;
            int processedRoomCount = 0;
            int promotedVisitedRoomCount = 0;
            int promotedForcedTeleporterRoomCount = 0;
            int promotedDeferredActivationRoomCount = 0;
            int minimapTeleportEntryCountBefore = GetMinimapTeleportEntryCount(minimap);
            int minimapTeleportEntryCountAfter = minimapTeleportEntryCountBefore;

            try
            {
                LogMapTeleportInfo(
                    logger,
                    "Map reveal requested. " +
                    "UnityScene=" + GetCurrentSceneName() +
                    ", LastLoadedDungeonScene=" + GetLastLoadedDungeonSceneName(gameManager) +
                    ", CurrentRoom=" + DescribeRoomState(player.CurrentRoom) +
                    ", CurrentRoomCanTeleportFrom=" + ((object)player.CurrentRoom != null ? player.CurrentRoom.CanTeleportFromRoom().ToString() : "<unknown>") +
                    ", CurrentRoomMinimapTeleportRegistered=" + IsMinimapTeleportRegistered(minimap, player.CurrentRoom) +
                    ", CurrentRoomConnectedRooms=[" + DescribeConnectedRooms(player.CurrentRoom, minimap) + "]" +
                    ", DungeonAllRoomsVisited=" + dungeon.AllRoomsVisited +
                    ", RoomsCount=" + rooms.Count +
                    ", MinimapTeleportEntriesBefore=" + minimapTeleportEntryCountBefore +
                    ", MinimapHasInstance=" + Minimap.HasInstance +
                    ".");
                player.EverHadMap = true;
                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomHandler room = rooms[index];
                    if ((object)room == null)
                    {
                        continue;
                    }

                    processedRoomCount++;
                    bool revealedBefore = room.RevealedOnMap;
                    bool canTeleportToRoom = room.CanTeleportToRoom();
                    bool teleportersActiveBefore = room.TeleportersActive;
                    if (!room.RevealedOnMap)
                    {
                        room.RevealedOnMap = true;
                        revealedRoomCount++;
                    }

                    if (!canTeleportToRoom)
                    {
                        bool wasPromoted =
                            TryPromoteRoomForMapDirectTeleport(
                                gameManager,
                                player,
                                minimap,
                                room,
                                logger,
                                ref promotedVisitedRoomCount,
                                ref promotedForcedTeleporterRoomCount,
                                ref promotedDeferredActivationRoomCount);
                        LogMapTeleportInfo(
                            logger,
                            "Map reveal room scan. " +
                            "Index=" + index +
                            ", Room=" + DescribeMapRoom(room) +
                            ", RevealedBefore=" + revealedBefore +
                            ", RevealedAfter=" + room.RevealedOnMap +
                            ", CanTeleportToRoom=" + canTeleportToRoom +
                            ", TeleportersActiveBefore=" + teleportersActiveBefore +
                            ", TeleportersActiveAfter=" + room.TeleportersActive +
                            ", HasEverBeenVisited=" + room.hasEverBeenVisited +
                            ", ForceTeleportersActive=" + room.forceTeleportersActive +
                            ", MinimapTeleportRegistered=" + IsMinimapTeleportRegistered(minimap, room) +
                            ", Action=" + (wasPromoted ? "PromotedForMapDirectTeleport" : "SkippedNotTeleportable") + ".");
                        continue;
                    }

                    teleportableRoomCount++;
                    if (room.TeleportersActive)
                    {
                        LogMapTeleportInfo(
                            logger,
                            "Map reveal room scan. " +
                            "Index=" + index +
                            ", Room=" + DescribeMapRoom(room) +
                            ", RevealedBefore=" + revealedBefore +
                            ", RevealedAfter=" + room.RevealedOnMap +
                            ", CanTeleportToRoom=" + canTeleportToRoom +
                            ", TeleportersActiveBefore=" + teleportersActiveBefore +
                            ", TeleportersActiveAfter=" + room.TeleportersActive +
                            ", HasEverBeenVisited=" + room.hasEverBeenVisited +
                            ", ForceTeleportersActive=" + room.forceTeleportersActive +
                            ", MinimapTeleportRegistered=" + IsMinimapTeleportRegistered(minimap, room) +
                            ", Action=SkippedAlreadyActive.");
                        continue;
                    }

                    room.AddProceduralTeleporterToRoom();
                    if (room.TeleportersActive)
                    {
                        teleporterActivatedCount++;
                    }

                    LogMapTeleportInfo(
                        logger,
                        "Map reveal room scan. " +
                        "Index=" + index +
                        ", Room=" + DescribeMapRoom(room) +
                        ", RevealedBefore=" + revealedBefore +
                        ", RevealedAfter=" + room.RevealedOnMap +
                        ", CanTeleportToRoom=" + canTeleportToRoom +
                        ", TeleportersActiveBefore=" + teleportersActiveBefore +
                        ", TeleportersActiveAfter=" + room.TeleportersActive +
                        ", HasEverBeenVisited=" + room.hasEverBeenVisited +
                        ", ForceTeleportersActive=" + room.forceTeleportersActive +
                        ", MinimapTeleportRegistered=" + IsMinimapTeleportRegistered(minimap, room) +
                        ", Action=AddProceduralTeleporterToRoom.");
                }

                minimap.RevealAllRooms(true);
                if ((object)player.CurrentRoom != null)
                {
                    minimap.RevealMinimapRoom(player.CurrentRoom, true, true, true);
                }
                minimapTeleportEntryCountAfter = GetMinimapTeleportEntryCount(minimap);

                LogMapTeleportInfo(
                    logger,
                    "Map reveal completed. " +
                    "ProcessedRooms=" + processedRoomCount +
                    ", NewlyRevealedRooms=" + revealedRoomCount +
                    ", TeleportableRooms=" + teleportableRoomCount +
                    ", NewlyActivatedTeleporters=" + teleporterActivatedCount +
                    ", PromotedVisitedRooms=" + promotedVisitedRoomCount +
                    ", PromotedForcedTeleporterRooms=" + promotedForcedTeleporterRoomCount +
                    ", PromotedDeferredActivationRooms=" + promotedDeferredActivationRoomCount +
                    ", MinimapTeleportEntriesBefore=" + minimapTeleportEntryCountBefore +
                    ", MinimapTeleportEntriesAfter=" + minimapTeleportEntryCountAfter +
                    ", MinimapTeleportRooms=[" + DescribeMinimapTeleportRooms(minimap) + "]" +
                    ", CurrentRoom=" + DescribeRoomState(player.CurrentRoom) +
                    ", CurrentRoomMinimapTeleportRegistered=" + IsMinimapTeleportRegistered(minimap, player.CurrentRoom) +
                    ", CurrentRoomConnectedRooms=[" + DescribeConnectedRooms(player.CurrentRoom, minimap) + "]" +
                    ".");
                return GrantCommandExecutionResult.Localized(
                    true,
                    "result.map_reveal.success",
                    processedRoomCount,
                    teleporterActivatedCount);
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Map reveal failed. " +
                    "ProcessedRooms=" + processedRoomCount +
                    ", NewlyRevealedRooms=" + revealedRoomCount +
                    ", TeleportableRooms=" + teleportableRoomCount +
                    ", NewlyActivatedTeleporters=" + teleporterActivatedCount +
                    ", PromotedVisitedRooms=" + promotedVisitedRoomCount +
                    ", PromotedForcedTeleporterRooms=" + promotedForcedTeleporterRoomCount +
                    ", PromotedDeferredActivationRooms=" + promotedDeferredActivationRoomCount +
                    ", MinimapTeleportEntriesBefore=" + minimapTeleportEntryCountBefore +
                    ", MinimapTeleportEntriesAfter=" + minimapTeleportEntryCountAfter +
                    ", Exception=" + exception +
                    ".");
                return GrantCommandExecutionResult.Localized(false, "result.map_reveal.failed");
            }
        }

        private static Chest ResolveChestPrefab(RewardManager rewards, RoomChestTier chestTier)
        {
            switch (chestTier)
            {
                case RoomChestTier.Brown:
                    return rewards.D_Chest;
                case RoomChestTier.Blue:
                    return rewards.C_Chest;
                case RoomChestTier.Green:
                    return rewards.B_Chest;
                case RoomChestTier.Red:
                    return rewards.A_Chest;
                case RoomChestTier.Black:
                    return rewards.S_Chest;
                case RoomChestTier.Synergy:
                    return rewards.Synergy_Chest;
                case RoomChestTier.Rainbow:
                    return rewards.Rainbow_Chest;
                default:
                    return null;
            }
        }

        private static GrantCommandExecutionResult CreateChestResult(bool succeeded, string messageKey, RoomChestTier chestTier)
        {
            string label = GetChestTierLabel(chestTier, false);
            string englishLabel = GetChestTierLabel(chestTier, true);
            return new GrantCommandExecutionResult(
                succeeded,
                ResolveMessage(messageKey, label, false),
                ResolveMessage(messageKey, englishLabel, true));
        }

        private static GrantCommandExecutionResult CreateSpawnGunberMuncherResult(bool succeeded, string messageKey)
        {
            return new GrantCommandExecutionResult(
                succeeded,
                ResolveSpawnGunberMuncherMessage(messageKey, false),
                ResolveSpawnGunberMuncherMessage(messageKey, true));
        }

        private static GrantCommandExecutionResult CreateSpawnEvilMuncherResult(bool succeeded, string messageKey)
        {
            return new GrantCommandExecutionResult(
                succeeded,
                ResolveSpawnEvilMuncherMessage(messageKey, false),
                ResolveSpawnEvilMuncherMessage(messageKey, true));
        }

        private static string ResolveMessage(string messageKey, string chestTierLabel, bool forceEnglish)
        {
            string resolved = forceEnglish
                ? GuiText.GetEnglish(messageKey, chestTierLabel)
                : GuiText.Get(messageKey, chestTierLabel);
            if (!string.Equals(resolved, messageKey, System.StringComparison.Ordinal))
            {
                return resolved;
            }

            if (string.Equals(messageKey, "result.room.spawn_chest.success", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "Spawned an unlocked " + chestTierLabel + " chest."
                    : "已生成一个已解锁的" + chestTierLabel + "。";
            }

            if (string.Equals(messageKey, "result.room.spawn_chest.prefab_missing", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "The prefab for the " + chestTierLabel + " chest was unavailable."
                    : chestTierLabel + "的预制体当前不可用。";
            }

            return forceEnglish
                ? "Failed to spawn the " + chestTierLabel + " chest."
                : "生成" + chestTierLabel + "失败。";
        }

        private static string ResolveSpawnGunberMuncherMessage(string messageKey, bool forceEnglish)
        {
            string resolved = forceEnglish
                ? GuiText.GetEnglish(messageKey)
                : GuiText.Get(messageKey);
            if (!string.Equals(resolved, messageKey, System.StringComparison.Ordinal))
            {
                return resolved;
            }

            if (string.Equals(messageKey, "result.room.spawn_gunber_muncher.success", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "Spawned a Gunber Muncher in the current room."
                    : "已在当前房间生成吃枪怪。";
            }

            if (string.Equals(messageKey, "result.room.spawn_gunber_muncher.queued", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "The room is still loading. Gunber Muncher spawn has been queued and will retry automatically."
                    : "当前仍在加载楼层，已将吃枪怪生成请求加入队列，稍后会自动重试。";
            }

            if (string.Equals(messageKey, "result.room.spawn_gunber_muncher.prefab_missing", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "The Gunber Muncher prefab is currently unavailable."
                    : "吃枪怪预制体当前不可用。";
            }

            return forceEnglish
                ? "Failed to spawn the Gunber Muncher."
                : "生成吃枪怪失败。";
        }

        private static string ResolveSpawnEvilMuncherMessage(string messageKey, bool forceEnglish)
        {
            string resolved = forceEnglish
                ? GuiText.GetEnglish(messageKey)
                : GuiText.Get(messageKey);
            if (!string.Equals(resolved, messageKey, System.StringComparison.Ordinal))
            {
                return resolved;
            }

            if (string.Equals(messageKey, "result.room.spawn_evil_muncher.success", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "Spawned an Evil Muncher in the current room."
                    : "已在当前房间生成邪恶吃枪怪。";
            }

            if (string.Equals(messageKey, "result.room.spawn_evil_muncher.queued", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "The room is still loading. Evil Muncher spawn has been queued and will retry automatically."
                    : "当前仍在加载楼层，已将邪恶吃枪怪生成请求加入队列，稍后会自动重试。";
            }

            if (string.Equals(messageKey, "result.room.spawn_evil_muncher.prefab_missing", System.StringComparison.Ordinal))
            {
                return forceEnglish
                    ? "The Evil Muncher prefab is currently unavailable."
                    : "邪恶吃枪怪预制体当前不可用。";
            }

            return forceEnglish
                ? "Failed to spawn the Evil Muncher."
                : "生成邪恶吃枪怪失败。";
        }

        private static DungeonPlaceableBehaviour ResolveGunberMuncherBehaviour(ManualLogSource logger)
        {
            DungeonPlaceableBehaviour resolved = ResolveGunberMuncherBehaviourFromOriginalRoomAsset(logger);
            if ((object)resolved != null)
            {
                return resolved;
            }

            return ResolveGunberMuncherBehaviourFromDungeon(logger);
        }

        private static DungeonPlaceableBehaviour ResolveEvilMuncherBehaviour(ManualLogSource logger)
        {
            return ResolveMuncherBehaviourFromOriginalRoomAsset(logger, EvilMuncherAssetBundleName, EvilMuncherRoomAssetPath, "EvilMuncherOriginalRoomAsset");
        }

        private static GameObject ResolveGunberMuncherPrefab(ManualLogSource logger)
        {
            GameObject directPrefab = ResolveGunberMuncherPrefabFromAssetBundle(logger);
            if ((object)directPrefab != null)
            {
                return directPrefab;
            }

            for (int i = 0; i < GunberMuncherPrefabCandidates.Length; i++)
            {
                string candidate = GunberMuncherPrefabCandidates[i];
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                Object braveResource = null;
                try
                {
                    braveResource = BraveResources.Load(candidate, ".prefab");
                }
                catch (System.Exception exception)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Gunber Muncher prefab resolve BraveResources.Load threw. " +
                        "Candidate=" + candidate +
                        ", Exception=" + exception +
                        ".");
                }

                GameObject prefab = braveResource as GameObject;
                LogMuncherInfo(
                    logger,
                    "Gunber Muncher prefab resolve attempt. " +
                    "Candidate=" + candidate +
                    ", BraveResourceType=" + DescribeUnityObject(braveResource) +
                    ", BravePrefabResolved=" + ((object)prefab != null) +
                    ".");

                if ((object)prefab == null)
                {
                    Object resource = null;
                    try
                    {
                        resource = Resources.Load(candidate);
                    }
                    catch (System.Exception exception)
                    {
                        LogRoomRefreshWarning(
                            logger,
                            "Gunber Muncher prefab resolve Resources.Load threw. " +
                            "Candidate=" + candidate +
                            ", Exception=" + exception +
                            ".");
                    }

                    prefab = resource as GameObject;
                    LogMuncherInfo(
                        logger,
                        "Gunber Muncher prefab resolve fallback attempt. " +
                        "Candidate=" + candidate +
                        ", ResourceType=" + DescribeUnityObject(resource) +
                        ", ResourcePrefabResolved=" + ((object)prefab != null) +
                        ".");
                }

                if ((object)prefab != null)
                {
                    LogMuncherInfo(
                        logger,
                        "Gunber Muncher prefab resolve succeeded. " +
                        "Candidate=" + candidate +
                        ", Prefab=" + DescribeGameObject(prefab) +
                        ".");
                    return prefab;
                }
            }

            return null;
        }

        private static GameObject ResolveEvilMuncherPrefab(ManualLogSource logger)
        {
            return ResolveMuncherPrefabFromAssetBundle(logger, EvilMuncherAssetBundleName, EvilMuncherPrefabAssetPath, "Evil Muncher");
        }

        private static DungeonPlaceableBehaviour ResolveGunberMuncherBehaviourFromOriginalRoomAsset(ManualLogSource logger)
        {
            return ResolveMuncherBehaviourFromOriginalRoomAsset(logger, GunberMuncherAssetBundleName, GunberMuncherRoomAssetPath, "OriginalRoomAsset");
        }

        private static GameObject ResolveGunberMuncherPrefabFromAssetBundle(ManualLogSource logger)
        {
            return ResolveMuncherPrefabFromAssetBundle(logger, GunberMuncherAssetBundleName, GunberMuncherPrefabAssetPath, "Gunber Muncher");
        }

        private static DungeonPlaceableBehaviour ResolveMuncherBehaviourFromOriginalRoomAsset(ManualLogSource logger, string bundleName, string roomAssetPath, string sourceLabel)
        {
            AssetBundle assetBundle = null;
            PrototypeDungeonRoom prototypeRoom = null;
            try
            {
                assetBundle = ResourceManager.LoadAssetBundle(bundleName);
                if ((object)assetBundle != null)
                {
                    prototypeRoom = assetBundle.LoadAsset<PrototypeDungeonRoom>(roomAssetPath);
                }
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Muncher original room asset load threw. " +
                    "Bundle=" + bundleName +
                    ", AssetPath=" + roomAssetPath +
                    ", Exception=" + exception +
                    ".");
            }

            LogMuncherInfo(
                logger,
                "Muncher original room asset resolve attempt. " +
                "Bundle=" + bundleName +
                ", AssetBundleLoaded=" + ((object)assetBundle != null) +
                ", PrototypeRoomResolved=" + ((object)prototypeRoom != null) +
                ", SourceLabel=" + sourceLabel +
                ".");

            if ((object)prototypeRoom == null)
            {
                return null;
            }

            int scannedPlacedObjectCount = 0;
            return ResolveGunberMuncherBehaviourFromRoomData(
                prototypeRoom,
                prototypeRoom.name,
                sourceLabel,
                ref scannedPlacedObjectCount,
                logger);
        }

        private static GameObject ResolveMuncherPrefabFromAssetBundle(ManualLogSource logger, string bundleName, string prefabAssetPath, string muncherLabel)
        {
            AssetBundle assetBundle = null;
            GameObject prefab = null;
            try
            {
                assetBundle = ResourceManager.LoadAssetBundle(bundleName);
                if ((object)assetBundle != null)
                {
                    prefab = assetBundle.LoadAsset<GameObject>(prefabAssetPath);
                }
            }
            catch (System.Exception exception)
            {
                LogRoomRefreshWarning(
                    logger,
                    muncherLabel + " direct prefab asset load threw. " +
                    "Bundle=" + bundleName +
                    ", AssetPath=" + prefabAssetPath +
                    ", Exception=" + exception +
                    ".");
            }

            LogMuncherInfo(
                logger,
                muncherLabel + " direct prefab asset resolve attempt. " +
                "Bundle=" + bundleName +
                ", AssetBundleLoaded=" + ((object)assetBundle != null) +
                ", PrefabResolved=" + ((object)prefab != null) +
                ", Prefab=" + DescribeGameObject(prefab) +
                ".");
            return prefab;
        }

        private static DungeonPlaceableBehaviour ResolveGunberMuncherBehaviourFromDungeon(ManualLogSource logger)
        {
            GameManager gameManager = GameManager.Instance;
            Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
            List<RoomHandler> rooms = dungeon != null && dungeon.data != null ? dungeon.data.rooms : null;
            if (rooms == null || rooms.Count == 0)
            {
                LogRoomRefreshWarning(
                    logger,
                    "Gunber Muncher dungeon source scan unavailable. " +
                    "DungeonPresent=" + ((object)dungeon != null) +
                    ", DungeonDataPresent=" + ((object)dungeon != null && dungeon.data != null) +
                    ".");
                return null;
            }

            int scannedRoomCount = 0;
            int scannedPlacedObjectCount = 0;
            for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                RoomHandler room = rooms[roomIndex];
                scannedRoomCount++;

                DungeonPlaceableBehaviour resolved =
                    ResolveGunberMuncherBehaviourFromRoomData(
                        ResolvePrototypeRoom(room),
                        DescribeMapRoom(room),
                        "PrototypeRoom",
                        ref scannedPlacedObjectCount,
                        logger);
                if ((object)resolved != null)
                {
                    return resolved;
                }

                RuntimePrototypeRoomData runtimePrototypeData = ResolveRuntimePrototypeData(room);
                resolved =
                    ResolveGunberMuncherBehaviourFromRoomData(
                        runtimePrototypeData,
                        DescribeMapRoom(room),
                        "RuntimePrototypeData",
                        ref scannedPlacedObjectCount,
                        logger);
                if ((object)resolved != null)
                {
                    return resolved;
                }
            }

            LogRoomRefreshWarning(
                logger,
                "Gunber Muncher dungeon source scan completed without a match. " +
                "ScannedRooms=" + scannedRoomCount +
                ", ScannedPlacedObjects=" + scannedPlacedObjectCount +
                ".");
            return null;
        }

        private static DungeonPlaceableBehaviour ResolveGunberMuncherBehaviourFromRoomData(
            PrototypeDungeonRoom prototypeRoom,
            string roomLabel,
            string sourceLabel,
            ref int scannedPlacedObjectCount,
            ManualLogSource logger)
        {
            if ((object)prototypeRoom == null)
            {
                return null;
            }

            DungeonPlaceableBehaviour resolved =
                ResolveGunberMuncherBehaviourFromPlacedObjects(
                    prototypeRoom.placedObjects,
                    roomLabel,
                    sourceLabel + ".placedObjects",
                    ref scannedPlacedObjectCount,
                    logger);
            if ((object)resolved != null)
            {
                return resolved;
            }

            if (prototypeRoom.additionalObjectLayers == null)
            {
                return null;
            }

            for (int layerIndex = 0; layerIndex < prototypeRoom.additionalObjectLayers.Count; layerIndex++)
            {
                PrototypeRoomObjectLayer layer = prototypeRoom.additionalObjectLayers[layerIndex];
                resolved =
                    ResolveGunberMuncherBehaviourFromPlacedObjects(
                        layer != null ? layer.placedObjects : null,
                        roomLabel,
                        sourceLabel + ".additionalObjectLayers[" + layerIndex + "]",
                        ref scannedPlacedObjectCount,
                        logger);
                if ((object)resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static DungeonPlaceableBehaviour ResolveGunberMuncherBehaviourFromRoomData(
            RuntimePrototypeRoomData runtimePrototypeData,
            string roomLabel,
            string sourceLabel,
            ref int scannedPlacedObjectCount,
            ManualLogSource logger)
        {
            if ((object)runtimePrototypeData == null)
            {
                return null;
            }

            DungeonPlaceableBehaviour resolved =
                ResolveGunberMuncherBehaviourFromPlacedObjects(
                    runtimePrototypeData.placedObjects,
                    roomLabel,
                    sourceLabel + ".placedObjects",
                    ref scannedPlacedObjectCount,
                    logger);
            if ((object)resolved != null)
            {
                return resolved;
            }

            if (runtimePrototypeData.additionalObjectLayers == null)
            {
                return null;
            }

            for (int layerIndex = 0; layerIndex < runtimePrototypeData.additionalObjectLayers.Count; layerIndex++)
            {
                PrototypeRoomObjectLayer layer = runtimePrototypeData.additionalObjectLayers[layerIndex];
                resolved =
                    ResolveGunberMuncherBehaviourFromPlacedObjects(
                        layer != null ? layer.placedObjects : null,
                        roomLabel,
                        sourceLabel + ".additionalObjectLayers[" + layerIndex + "]",
                        ref scannedPlacedObjectCount,
                        logger);
                if ((object)resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static DungeonPlaceableBehaviour ResolveGunberMuncherBehaviourFromPlacedObjects(
            List<PrototypePlacedObjectData> placedObjects,
            string roomLabel,
            string sourceLabel,
            ref int scannedPlacedObjectCount,
            ManualLogSource logger)
        {
            if (placedObjects == null)
            {
                return null;
            }

            for (int objectIndex = 0; objectIndex < placedObjects.Count; objectIndex++)
            {
                PrototypePlacedObjectData placedObject = placedObjects[objectIndex];
                if ((object)placedObject == null || (object)placedObject.nonenemyBehaviour == null)
                {
                    continue;
                }

                scannedPlacedObjectCount++;
                DungeonPlaceableBehaviour nonenemyBehaviour = placedObject.nonenemyBehaviour;
                if (!IsGunberMuncherBehaviour(nonenemyBehaviour))
                {
                    continue;
                }

                LogMuncherInfo(
                    logger,
                    "Gunber Muncher dungeon source matched. " +
                    "Room=" + roomLabel +
                    ", Source=" + sourceLabel +
                    ", ObjectIndex=" + objectIndex +
                    ", Behaviour=" + DescribeGameObject(nonenemyBehaviour.gameObject) +
                    ".");
                return nonenemyBehaviour;
            }

            return null;
        }

        private static bool IsGunberMuncherBehaviour(DungeonPlaceableBehaviour nonenemyBehaviour)
        {
            if ((object)nonenemyBehaviour == null)
            {
                return false;
            }

            if (nonenemyBehaviour.GetComponent<GunberMuncherController>() != null)
            {
                return true;
            }

            GameObject gameObject = nonenemyBehaviour.gameObject;
            if ((object)gameObject == null || string.IsNullOrEmpty(gameObject.name))
            {
                return false;
            }

            return gameObject.name.IndexOf("GunberMuncher", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IntVector2 FindMuncherSpawnCell(RoomHandler currentRoom, out bool success, out int existingMuncherCount)
        {
            success = false;
            existingMuncherCount = CountExistingMunchersInRoom(currentRoom);
            if ((object)currentRoom == null)
            {
                return IntVector2.Zero;
            }

            IntVector2 baseSpawnCell = currentRoom.GetCenteredVisibleClearSpot(3, 3, out success, restrictive: true);
            if (!success)
            {
                baseSpawnCell = currentRoom.GetBestRewardLocation(new IntVector2(3, 3), RoomHandler.RewardLocationStyle.PlayerCenter, giveChestBuffer: false);
            }

            for (int i = 0; i < MuncherSpawnOffsetCandidates.Length; i++)
            {
                IntVector2 candidateCell = baseSpawnCell + MuncherSpawnOffsetCandidates[i];
                if (!IsSpawnCellInRoom(currentRoom, candidateCell))
                {
                    continue;
                }

                if (IsSpawnCellTooCloseToExistingMuncher(currentRoom, candidateCell, 4f))
                {
                    continue;
                }

                return candidateCell;
            }

            return baseSpawnCell;
        }

        private static int CountExistingMunchersInRoom(RoomHandler room)
        {
            List<GunberMuncherController> munchers = room != null ? room.GetComponentsAbsoluteInRoom<GunberMuncherController>() : null;
            return munchers != null ? munchers.Count : 0;
        }

        private static bool IsSpawnCellInRoom(RoomHandler room, IntVector2 candidateCell)
        {
            if ((object)room == null)
            {
                return false;
            }

            Dungeon dungeon = GameManager.Instance != null ? GameManager.Instance.Dungeon : null;
            DungeonData dungeonData = dungeon != null ? dungeon.data : null;
            if ((object)dungeonData == null)
            {
                return true;
            }

            return dungeonData.GetAbsoluteRoomFromPosition(candidateCell) == room;
        }

        private static bool IsSpawnCellTooCloseToExistingMuncher(RoomHandler room, IntVector2 candidateCell, float minimumDistance)
        {
            if ((object)room == null)
            {
                return false;
            }

            List<GunberMuncherController> munchers = room.GetComponentsAbsoluteInRoom<GunberMuncherController>();
            if (munchers == null || munchers.Count == 0)
            {
                return false;
            }

            float minimumDistanceSquared = minimumDistance * minimumDistance;
            Vector2 candidateCenter = new Vector2(candidateCell.x + 0.5f, candidateCell.y + 0.5f);
            for (int i = 0; i < munchers.Count; i++)
            {
                GunberMuncherController muncher = munchers[i];
                if ((object)muncher == null)
                {
                    continue;
                }

                Vector3 muncherPosition = muncher.transform.position;
                float deltaX = muncherPosition.x - candidateCenter.x;
                float deltaY = muncherPosition.y - candidateCenter.y;
                float distanceSquared = deltaX * deltaX + deltaY * deltaY;
                if (distanceSquared < minimumDistanceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InitializeSpawnedRigidbodies(GameObject gameObject)
        {
            if ((object)gameObject == null)
            {
                return;
            }

            SpeculativeRigidbody[] rigidbodies = gameObject.GetComponentsInChildren<SpeculativeRigidbody>(true);
            if (rigidbodies == null)
            {
                return;
            }

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                SpeculativeRigidbody rigidbody = rigidbodies[i];
                if ((object)rigidbody == null)
                {
                    continue;
                }

                rigidbody.Initialize();
                rigidbody.Reinitialize();
            }
        }

        private static int RegisterRoomInteractables(RoomHandler room, GameObject gameObject)
        {
            if ((object)room == null || (object)gameObject == null)
            {
                return 0;
            }

            IPlayerInteractable[] interactables = gameObject.GetInterfacesInChildren<IPlayerInteractable>();
            if (interactables == null)
            {
                return 0;
            }

            int registeredCount = 0;
            for (int i = 0; i < interactables.Length; i++)
            {
                if ((object)interactables[i] != null)
                {
                    room.RegisterInteractable(interactables[i]);
                    registeredCount++;
                }
            }

            return registeredCount;
        }

        private static string DescribeGameObject(GameObject gameObject)
        {
            if ((object)gameObject == null)
            {
                return "<null>";
            }

            int componentCount = 0;
            Component[] components = gameObject.GetComponents<Component>();
            if (components != null)
            {
                componentCount = components.Length;
            }

            return
                gameObject.name +
                "{Active=" + gameObject.activeSelf +
                ", Layer=" + gameObject.layer +
                ", Components=" + componentCount +
                "}";
        }

        private static string DescribeSpawnedObjectState(GameObject gameObject, RoomHandler expectedRoom)
        {
            if ((object)gameObject == null)
            {
                return "<null>";
            }

            Transform transform = gameObject.transform;
            Vector3 position = transform != null ? transform.position : Vector3.zero;
            string parentName = transform != null && transform.parent != null ? transform.parent.name : "<none>";
            string layerName = LayerMask.LayerToName(gameObject.layer);
            if (string.IsNullOrEmpty(layerName))
            {
                layerName = gameObject.layer.ToString();
            }

            tk2dBaseSprite sprite = gameObject.GetComponentInChildren<tk2dBaseSprite>(true);
            Renderer renderer = sprite != null ? sprite.renderer : gameObject.GetComponentInChildren<Renderer>(true);
            SpeculativeRigidbody speculativeRigidbody = gameObject.GetComponentInChildren<SpeculativeRigidbody>(true);
            DungeonPlaceableBehaviour placeableBehaviour = gameObject.GetComponent<DungeonPlaceableBehaviour>();
            TalkDoerLite talkDoer = gameObject.GetComponent<TalkDoerLite>();
            GunberMuncherController muncherController = gameObject.GetComponent<GunberMuncherController>();
            RoomHandler resolvedRoom = null;

            if ((object)talkDoer != null)
            {
                try
                {
                    resolvedRoom = talkDoer.ParentRoom;
                }
                catch
                {
                    resolvedRoom = null;
                }
            }

            bool roomMatches = (object)expectedRoom != null && (object)resolvedRoom != null && resolvedRoom == expectedRoom;
            return
                "Name=" + gameObject.name +
                ", ActiveSelf=" + gameObject.activeSelf +
                ", ActiveInHierarchy=" + gameObject.activeInHierarchy +
                ", Position=" + position.x + "," + position.y + "," + position.z +
                ", Parent=" + parentName +
                ", Layer=" + layerName +
                ", SpritePresent=" + ((object)sprite != null) +
                ", SpriteRendererEnabled=" + ((object)renderer != null && renderer.enabled) +
                ", SpriteScale=" + ((object)sprite != null ? sprite.scale.ToString() : "<none>") +
                ", RigidbodyPresent=" + ((object)speculativeRigidbody != null) +
                ", RigidbodyEnabled=" + ((object)speculativeRigidbody != null && speculativeRigidbody.enabled) +
                ", TalkDoerPresent=" + ((object)talkDoer != null) +
                ", PlaceablePresent=" + ((object)placeableBehaviour != null) +
                ", MuncherControllerPresent=" + ((object)muncherController != null) +
                ", ParentRoom=" + DescribeMapRoom(resolvedRoom) +
                ", ParentRoomMatchesExpected=" + roomMatches;
        }

        private static string DescribeUnityObject(Object unityObject)
        {
            if ((object)unityObject == null)
            {
                return "<null>";
            }

            return unityObject.GetType().FullName + ":" + unityObject.name;
        }

        private static string GetChestTierLabel(RoomChestTier chestTier, bool forceEnglish)
        {
            switch (chestTier)
            {
                case RoomChestTier.Brown:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.brown") : GuiText.Get("label.room.chest_tier.brown");
                case RoomChestTier.Blue:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.blue") : GuiText.Get("label.room.chest_tier.blue");
                case RoomChestTier.Green:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.green") : GuiText.Get("label.room.chest_tier.green");
                case RoomChestTier.Red:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.red") : GuiText.Get("label.room.chest_tier.red");
                case RoomChestTier.Black:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.black") : GuiText.Get("label.room.chest_tier.black");
                case RoomChestTier.Synergy:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.synergy") : GuiText.Get("label.room.chest_tier.synergy");
                case RoomChestTier.Rainbow:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.rainbow") : GuiText.Get("label.room.chest_tier.rainbow");
                default:
                    return forceEnglish ? GuiText.GetEnglish("label.room.chest_tier.brown") : GuiText.Get("label.room.chest_tier.brown");
            }
        }

        private static string DescribeRoomState(RoomHandler room)
        {
            if ((object)room == null)
            {
                return "Room=<null>";
            }

            string roomCategory = "<unknown>";
            string roomName = "<unnamed>";
            IntVector2 basePosition = IntVector2.Zero;

            if (room.area != null)
            {
                roomCategory = room.area.PrototypeRoomCategory.ToString();
                basePosition = room.area.basePosition;
            }

            string resolvedRoomName = room.GetRoomName();
            if (!string.IsNullOrEmpty(resolvedRoomName))
            {
                roomName = resolvedRoomName;
            }

            int activeAllCount = SafeGetActiveEnemyCount(room, RoomHandler.ActiveEnemyType.All);
            int activeRoomClearCount = SafeGetActiveEnemyCount(room, RoomHandler.ActiveEnemyType.RoomClear);

            return
                "Room=" + roomName +
                ", Cell=" + basePosition.x + "," + basePosition.y +
                ", Category=" + roomCategory +
                ", IsStandardRoom=" + room.IsStandardRoom +
                ", IsSealed=" + room.IsSealed +
                ", TeleportersActive=" + room.TeleportersActive +
                ", WillSealOnEntry=" + room.WillSealOnEntry() +
                ", ActiveEnemiesAll=" + activeAllCount +
                ", ActiveEnemiesRoomClear=" + activeRoomClearCount;
        }

        private static string DescribeMapRoom(RoomHandler room)
        {
            if ((object)room == null)
            {
                return "<null>";
            }

            string roomName = room.GetRoomName();
            IntVector2 basePosition = room.area != null ? room.area.basePosition : IntVector2.Zero;
            string category = room.area != null ? room.area.PrototypeRoomCategory.ToString() : "<unknown>";
            return
                (string.IsNullOrEmpty(roomName) ? "<unnamed>" : roomName) +
                "@" +
                basePosition.x +
                "," +
                basePosition.y +
                "#" +
                category;
        }

        private static int GetMinimapTeleportEntryCount(Minimap minimap)
        {
            Dictionary<RoomHandler, GameObject> roomToTeleportMap = minimap != null ? minimap.RoomToTeleportMap : null;
            return roomToTeleportMap != null ? roomToTeleportMap.Count : -1;
        }

        private static bool IsMinimapTeleportRegistered(Minimap minimap, RoomHandler room)
        {
            Dictionary<RoomHandler, GameObject> roomToTeleportMap = minimap != null ? minimap.RoomToTeleportMap : null;
            return roomToTeleportMap != null && room != null && roomToTeleportMap.ContainsKey(room);
        }

        private static string DescribeMinimapTeleportRooms(Minimap minimap)
        {
            Dictionary<RoomHandler, GameObject> roomToTeleportMap = minimap != null ? minimap.RoomToTeleportMap : null;
            if (roomToTeleportMap == null || roomToTeleportMap.Count == 0)
            {
                return string.Empty;
            }

            List<string> roomLabels = new List<string>();
            foreach (KeyValuePair<RoomHandler, GameObject> entry in roomToTeleportMap)
            {
                roomLabels.Add(
                    DescribeMapRoom(entry.Key) +
                    "=" +
                    ((object)entry.Value != null ? "IconPresent" : "IconNull"));
            }

            return string.Join("; ", roomLabels.ToArray());
        }

        private static string DescribeConnectedRooms(RoomHandler room, Minimap minimap)
        {
            if ((object)room == null || room.connectedRooms == null || room.connectedRooms.Count == 0)
            {
                return string.Empty;
            }

            List<string> roomLabels = new List<string>();
            for (int index = 0; index < room.connectedRooms.Count; index++)
            {
                RoomHandler connectedRoom = room.connectedRooms[index];
                roomLabels.Add(
                    DescribeMapRoom(connectedRoom) +
                    "{CanTo=" +
                    ((object)connectedRoom != null ? connectedRoom.CanTeleportToRoom().ToString() : "<unknown>") +
                    ", TeleActive=" +
                    ((object)connectedRoom != null ? connectedRoom.TeleportersActive.ToString() : "<unknown>") +
                    ", Revealed=" +
                    ((object)connectedRoom != null ? connectedRoom.RevealedOnMap.ToString() : "<unknown>") +
                    ", Registered=" +
                    IsMinimapTeleportRegistered(minimap, connectedRoom) +
                    "}");
            }

            return string.Join("; ", roomLabels.ToArray());
        }

        private static string GetCurrentSceneName()
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null)
            {
                try
                {
                    GameLevelDefinition levelDefinition = gameManager.GetLastLoadedLevelDefinition();
                    if (levelDefinition != null && !string.IsNullOrEmpty(levelDefinition.dungeonSceneName))
                    {
                        return SceneNameNormalizer.Normalize(levelDefinition.dungeonSceneName);
                    }
                }
                catch (System.NullReferenceException)
                {
                    // ETG can briefly expose GameManager before its level definition is stable.
                }
            }

#pragma warning disable 618
            string sceneName = Application.loadedLevelName ?? string.Empty;
#pragma warning restore 618
            string normalizedSceneName = SceneNameNormalizer.Normalize(sceneName);
            return string.IsNullOrEmpty(normalizedSceneName) ? "<empty>" : normalizedSceneName;
        }

        private static string GetLastLoadedDungeonSceneName(GameManager gameManager)
        {
            if ((object)gameManager == null)
            {
                return "<no_game_manager>";
            }

            try
            {
                GameLevelDefinition levelDefinition = gameManager.GetLastLoadedLevelDefinition();
                if (levelDefinition == null || string.IsNullOrEmpty(levelDefinition.dungeonSceneName))
                {
                    return "<unknown>";
                }

                return levelDefinition.dungeonSceneName;
            }
            catch (System.Exception exception)
            {
                return "<exception:" + exception.GetType().Name + ">";
            }
        }

        private static int SafeGetActiveEnemyCount(RoomHandler room, RoomHandler.ActiveEnemyType activeEnemyType)
        {
            if ((object)room == null)
            {
                return -1;
            }

            try
            {
                return room.GetActiveEnemiesCount(activeEnemyType);
            }
            catch
            {
                return -1;
            }
        }

        private bool TryPromoteRoomForMapDirectTeleport(
            GameManager gameManager,
            PlayerController player,
            Minimap minimap,
            RoomHandler room,
            ManualLogSource logger,
            ref int promotedVisitedRoomCount,
            ref int promotedForcedTeleporterRoomCount,
            ref int promotedDeferredActivationRoomCount)
        {
            if ((object)room == null || !IsMinimapTeleportRegistered(minimap, room))
            {
                return false;
            }

            bool changed = false;
            if (!room.hasEverBeenVisited)
            {
                room.hasEverBeenVisited = true;
                promotedVisitedRoomCount++;
                changed = true;
            }

            if (!room.forceTeleportersActive)
            {
                room.forceTeleportersActive = true;
                promotedForcedTeleporterRoomCount++;
                changed = true;
            }

            if ((object)minimap != null)
            {
                minimap.RevealMinimapRoom(room, true, true, false);
            }

            if ((object)gameManager != null && (object)player != null)
            {
                try
                {
                    gameManager.StartCoroutine(room.DeferredMarkVisibleRoomsActive(player));
                    promotedDeferredActivationRoomCount++;
                    changed = true;
                }
                catch (System.Exception exception)
                {
                    LogRoomRefreshWarning(
                        logger,
                        "Map direct teleport room promotion coroutine failed. " +
                        "Room=" +
                        DescribeMapRoom(room) +
                        ", Exception=" +
                        exception +
                        ".");
                }
            }

            if (changed)
            {
                LogMapTeleportInfo(
                    logger,
                    "Map direct teleport room promotion applied. " +
                    "Room=" +
                    DescribeMapRoom(room) +
                    ", HasEverBeenVisited=" +
                    room.hasEverBeenVisited +
                    ", ForceTeleportersActive=" +
                    room.forceTeleportersActive +
                    ", TeleportersActive=" +
                    room.TeleportersActive +
                    ", MinimapTeleportRegistered=" +
                    IsMinimapTeleportRegistered(minimap, room) +
                    ".");
            }

            return changed;
        }

        private static PrototypeDungeonRoom ResolvePrototypeRoom(RoomHandler room)
        {
            if ((object)room == null || (object)room.area == null)
            {
                return null;
            }

            PrototypeDungeonRoom prototypeRoom = room.area.prototypeRoom;
            if ((object)prototypeRoom != null)
            {
                return prototypeRoom;
            }

            if (CellAreaPrototypeRoomField == null)
            {
                return null;
            }

            try
            {
                return CellAreaPrototypeRoomField.GetValue(room.area) as PrototypeDungeonRoom;
            }
            catch
            {
                return null;
            }
        }

        private static RuntimePrototypeRoomData ResolveRuntimePrototypeData(RoomHandler room)
        {
            if ((object)room == null || (object)room.area == null)
            {
                return null;
            }

            return room.area.runtimePrototypeData;
        }

        private static AIActor ResolveEnemyPrefab(PrototypePlacedObjectData placedObject, out string resolvedEnemyGuid)
        {
            resolvedEnemyGuid = null;
            if ((object)placedObject == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(placedObject.enemyBehaviourGuid))
            {
                resolvedEnemyGuid = placedObject.enemyBehaviourGuid;
                return EnemyDatabase.GetOrLoadByGuid(resolvedEnemyGuid);
            }

            DungeonPlaceable placeableContents = placedObject.placeableContents;
            if ((object)placeableContents == null || !placeableContents.ContainsEnemy || placeableContents.variantTiers == null)
            {
                return null;
            }

            for (int index = 0; index < placeableContents.variantTiers.Count; index++)
            {
                DungeonPlaceableVariant variant = placeableContents.variantTiers[index];
                if ((object)variant == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(variant.enemyPlaceableGuid))
                {
                    resolvedEnemyGuid = variant.enemyPlaceableGuid;
                    AIActor enemyFromGuid = EnemyDatabase.GetOrLoadByGuid(resolvedEnemyGuid);
                    if ((object)enemyFromGuid != null)
                    {
                        return enemyFromGuid;
                    }
                }

                GameObject nonDatabasePlaceable = variant.nonDatabasePlaceable;
                if ((object)nonDatabasePlaceable == null)
                {
                    continue;
                }

                AIActor enemyFromPrefab = nonDatabasePlaceable.GetComponent<AIActor>();
                if ((object)enemyFromPrefab != null)
                {
                    return enemyFromPrefab;
                }
            }

            return null;
        }

        private static bool HasPrototypeRoomFieldValue(CellArea area)
        {
            if ((object)area == null || CellAreaPrototypeRoomField == null)
            {
                return false;
            }

            try
            {
                return CellAreaPrototypeRoomField.GetValue(area) != null;
            }
            catch
            {
                return false;
            }
        }

        private bool ShouldLogMapTeleportVerbose()
        {
            return _mapTeleportVerboseLoggingEnabledProvider != null && _mapTeleportVerboseLoggingEnabledProvider();
        }

        private static bool ShouldLogMuncherVerbose()
        {
            return _muncherVerboseLoggingEnabledProvider != null && _muncherVerboseLoggingEnabledProvider();
        }

        private void LogMapTeleportInfo(ManualLogSource logger, string message)
        {
            if (!ShouldLogMapTeleportVerbose())
            {
                return;
            }

            LogRoomRefreshInfo(logger, message);
        }

        private static void LogMuncherInfo(ManualLogSource logger, string message)
        {
            if (!ShouldLogMuncherVerbose())
            {
                return;
            }

            LogRoomRefreshInfo(logger, message);
        }

        private static void LogRoomRefreshInfo(ManualLogSource logger, string message)
        {
            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(message));
            }
        }

        private static void LogRoomRefreshWarning(ManualLogSource logger, string message)
        {
            if (logger != null)
            {
                logger.LogWarning(RandomLoadoutLog.Command(message));
            }
        }
    }
}
