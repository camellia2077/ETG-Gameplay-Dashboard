// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class RoomDebugCommandService
    {
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

        private static string GetRuntimeSceneName()
        {
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

        private void LogRoomEnemyReplayDiagnostic(ManualLogSource logger, string message)
        {
            ManualLogSource targetLogger = logger ?? _logger;
            if (targetLogger != null)
            {
                // Request validation and final results are lifecycle evidence, not verbose
                // detail. Always record them so a failed or ignored cfg switch cannot make
                // a rewind attempt disappear from LogOutput.log.
                targetLogger.LogInfo(RandomLoadoutLog.Command(message));
            }
        }

        private bool IsBossSelectionVerboseLoggingEnabled()
        {
            return _bossSelectionVerboseLoggingEnabledProvider != null && _bossSelectionVerboseLoggingEnabledProvider();
        }

        private void LogBossSelectionDiagnostic(ManualLogSource logger, string message)
        {
            if (!IsBossSelectionVerboseLoggingEnabled())
            {
                return;
            }

            ManualLogSource targetLogger = logger ?? _logger;
            if (targetLogger != null)
            {
                targetLogger.LogInfo(RandomLoadoutLog.Command(message));
            }
        }

        private void LogBossSelectionOptionsPerformance(long startedAt, int optionCount, Dungeon targetDungeon, bool cacheHit)
        {
            long elapsedTicks = System.Diagnostics.Stopwatch.GetTimestamp() - startedAt;
            double elapsedMilliseconds = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            _bossSelectionOptionsCallCount++;
            _bossSelectionOptionsTotalTicks += elapsedTicks;
            if (elapsedMilliseconds >= 2.0)
            {
                _bossSelectionOptionsSlowCallCount++;
            }

            float now = Time.realtimeSinceStartup;
            if (_bossSelectionOptionsWindowStartedAt <= 0f)
            {
                _bossSelectionOptionsWindowStartedAt = now;
            }

            if (now - _bossSelectionOptionsWindowStartedAt < 0.5f)
            {
                return;
            }

            double totalMilliseconds = _bossSelectionOptionsTotalTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            double averageMilliseconds = _bossSelectionOptionsCallCount > 0
                ? totalMilliseconds / _bossSelectionOptionsCallCount
                : 0.0;
            if (_logger != null)
            {
                _logger.LogInfo(RandomLoadoutLog.Command(
                    "Boss selection options performance window. " +
                    "Calls=" + _bossSelectionOptionsCallCount +
                    ", SlowCalls(>=2ms)=" + _bossSelectionOptionsSlowCallCount +
                    ", TotalMs=" + totalMilliseconds.ToString("0.##") +
                    ", AverageMs=" + averageMilliseconds.ToString("0.##") +
                    ", LastMs=" + elapsedMilliseconds.ToString("0.##") +
                    ", OptionCount=" + optionCount +
                    ", TargetDungeon=" + (targetDungeon != null ? targetDungeon.name : "<null>") +
                    ", CatalogLoaded=" + (_bossNameCatalog != null) +
                    ", CacheHit=" + cacheHit + "."));
            }

            _bossSelectionOptionsCallCount = 0;
            _bossSelectionOptionsSlowCallCount = 0;
            _bossSelectionOptionsTotalTicks = 0;
            _bossSelectionOptionsWindowStartedAt = now;
        }

        private void LogBossSelectionOptionsDiagnostic(GameManager gameManager, Dungeon targetDungeon, string phase)
        {
            if (!IsBossSelectionVerboseLoggingEnabled())
            {
                return;
            }

            string currentSceneName = GetCurrentSceneName();
            string runtimeSceneName = GetRuntimeSceneName();
            bool isFoyer = (object)gameManager != null &&
                (gameManager.IsFoyer ||
                 string.Equals(currentSceneName, "foyer", System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(runtimeSceneName, "foyer", System.StringComparison.OrdinalIgnoreCase));
            string message =
                "Boss selection options " + phase +
                ". CurrentFloor=" + ((object)gameManager != null ? gameManager.CurrentFloor.ToString() : "<null>") +
                ", ReflectedNextLevelIndex=" + DescribeNextLevelIndex(gameManager) +
                ", DungeonFloorCount=" + ((object)gameManager != null && gameManager.dungeonFloors != null ? gameManager.dungeonFloors.Count.ToString() : "<null>") +
                ", IsFoyer=" + isFoyer +
                ", IsLoadingLevel=" + ((object)gameManager != null && gameManager.IsLoadingLevel) +
                ", Scene=" + currentSceneName +
                ", RuntimeScene=" + runtimeSceneName +
                ", TargetDungeon=" + DescribeBossSelectionDungeon(gameManager) + ".";
            if (!string.Equals(_lastBossSelectionOptionsDiagnostic, message, System.StringComparison.Ordinal))
            {
                _lastBossSelectionOptionsDiagnostic = message;
                LogBossSelectionDiagnostic(null, message);
            }
        }

        private static string DescribeNextLevelIndex(GameManager gameManager)
        {
            if ((object)gameManager == null || GameManagerNextLevelIndexField == null)
            {
                return "<unavailable>";
            }

            object value = GameManagerNextLevelIndexField.GetValue(gameManager);
            return value is int ? ((int)value).ToString() : "<null>";
        }

        private static string DescribeBossOptions(List<RoomBossOption> options)
        {
            if (options == null || options.Count == 0)
            {
                return "<empty>";
            }

            List<string> descriptions = new List<string>();
            for (int index = 0; index < options.Count; index++)
            {
                RoomBossOption option = options[index];
                if (option != null)
                {
                    descriptions.Add(
                        option.BossName +
                        "(" + (option.BossRoomPrototype != null ? option.BossRoomPrototype.name : "<null>") + ")");
                }
            }

            return descriptions.Count > 0 ? string.Join(", ", descriptions.ToArray()) : "<empty>";
        }

        private static string DescribeBossSelectionDungeon(GameManager gameManager)
        {
            Dungeon targetDungeon = GetBossSelectionDungeon(gameManager);
            if ((object)targetDungeon == null || targetDungeon.tileIndices == null)
            {
                return "<unavailable>";
            }

            return targetDungeon.name + ", Tileset=" + targetDungeon.tileIndices.tilesetId + ".";
        }

        private static string DescribeBossPrototype(PrototypeDungeonRoom prototype)
        {
            if (prototype == null)
            {
                return "<null>";
            }

            return "Name=" + prototype.name +
                   ", GUID=" + prototype.GUID +
                   ", Category=" + prototype.category +
                   ", BossSubcategory=" + prototype.subCategoryBoss;
        }
    }
}

