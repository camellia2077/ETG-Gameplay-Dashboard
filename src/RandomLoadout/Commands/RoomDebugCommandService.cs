using BepInEx.Logging;
using Dungeonator;
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
