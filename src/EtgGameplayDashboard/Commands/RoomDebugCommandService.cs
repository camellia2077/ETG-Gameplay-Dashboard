// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EtgGameplayDashboard
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

    internal sealed class RoomBossOption
    {
        public RoomBossOption(PrototypeDungeonRoom bossRoomPrototype, string bossName)
        {
            BossRoomPrototype = bossRoomPrototype;
            BossName = bossName;
        }

        public PrototypeDungeonRoom BossRoomPrototype { get; private set; }
        public string BossName { get; set; }
    }

    internal sealed partial class RoomDebugCommandService
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo CellAreaPrototypeRoomField = typeof(CellArea).GetField("m_prototypeRoom", InstanceFlags);
        private static readonly FieldInfo GameManagerNextLevelIndexField = typeof(GameManager).GetField("nextLevelIndex", InstanceFlags);
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
        private readonly System.Func<bool> _roomEnemyReplayVerboseLoggingEnabledProvider;
        private readonly System.Func<bool> _playerRewindEnabledProvider;
        private readonly System.Action<bool> _playerRewindEnabledSetter;
        private readonly System.Func<bool> _roomRewindCleanupEnabledProvider;
        private readonly System.Action<bool> _roomRewindCleanupEnabledSetter;
        private readonly System.Func<bool> _bossSelectionVerboseLoggingEnabledProvider;
        private readonly BossNameCatalog _bossNameCatalog;
        private readonly ManualLogSource _logger;
        private readonly RoomEnemyReplayService _roomEnemyReplayService;
        private bool _gunberMuncherSpawnQueued;
        private bool _evilMuncherSpawnQueued;
        private string _lastBossSelectionOptionsDiagnostic;
        private string _lastBossSelectionOptionsBuiltDiagnostic;
        private int _bossSelectionOptionsCallCount;
        private int _bossSelectionOptionsSlowCallCount;
        private long _bossSelectionOptionsTotalTicks;
        private float _bossSelectionOptionsWindowStartedAt;
        private BossManager _cachedBossSelectionManager;
        private Dungeon _cachedBossSelectionDungeon;
        private GlobalDungeonData.ValidTilesets _cachedBossSelectionTileset;
        private List<RoomBossOption> _cachedBossSelectionOptions;
        private List<RoomBossOption> _cachedUniqueBossSelectionOptions;
        private List<RoomBossOption> _cachedUniqueBossSelectionSource;
        private string _cachedBossRoomOptionsName = string.Empty;
        private List<RoomBossOption> _cachedBossRoomOptions;
        private List<RoomBossOption> _cachedBossRoomOptionsSource;
        private GameManager _cachedTargetResolutionGameManager;
        private int _cachedTargetResolutionCurrentFloor;
        private int _cachedTargetResolutionNextLevelIndex;
        private bool _cachedTargetResolutionIsFoyer;
        private bool _cachedTargetResolutionIsLoading;
        private string _cachedTargetResolutionScene;
        private Dungeon _cachedTargetResolutionGeneratingDungeon;
        private Dungeon _cachedTargetResolutionDungeon;

        public RoomDebugCommandService(
            System.Func<bool> mapTeleportVerboseLoggingEnabledProvider,
            System.Func<bool> muncherVerboseLoggingEnabledProvider,
            RoomEnemyReplayService roomEnemyReplayService,
            System.Func<bool> roomEnemyReplayVerboseLoggingEnabledProvider,
            System.Func<bool> playerRewindEnabledProvider,
            System.Action<bool> playerRewindEnabledSetter,
            System.Func<bool> roomRewindCleanupEnabledProvider,
            System.Action<bool> roomRewindCleanupEnabledSetter,
            System.Func<bool> bossSelectionVerboseLoggingEnabledProvider,
            BossNameCatalog bossNameCatalog,
            ManualLogSource logger)
        {
            _mapTeleportVerboseLoggingEnabledProvider = mapTeleportVerboseLoggingEnabledProvider;
            _muncherVerboseLoggingEnabledProvider = muncherVerboseLoggingEnabledProvider;
            _roomEnemyReplayService = roomEnemyReplayService;
            _roomEnemyReplayVerboseLoggingEnabledProvider = roomEnemyReplayVerboseLoggingEnabledProvider;
            _playerRewindEnabledProvider = playerRewindEnabledProvider;
            _playerRewindEnabledSetter = playerRewindEnabledSetter;
            _roomRewindCleanupEnabledProvider = roomRewindCleanupEnabledProvider;
            _roomRewindCleanupEnabledSetter = roomRewindCleanupEnabledSetter;
            _bossSelectionVerboseLoggingEnabledProvider = bossSelectionVerboseLoggingEnabledProvider;
            _bossNameCatalog = bossNameCatalog;
            _logger = logger;
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

    }
}
