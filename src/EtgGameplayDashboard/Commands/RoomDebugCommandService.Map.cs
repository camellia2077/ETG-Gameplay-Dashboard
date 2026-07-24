// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using System.Collections.Generic;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class RoomDebugCommandService
    {
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
    }
}

