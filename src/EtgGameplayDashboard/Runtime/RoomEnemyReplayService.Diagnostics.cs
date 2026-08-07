// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class RoomEnemyReplayService
    {
        public void LogFloorMapTeleportState(string phase)
        {
            GameManager gameManager = GameManager.Instance;
            Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
            List<RoomHandler> rooms = dungeon != null && dungeon.data != null ? dungeon.data.rooms : null;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            Dictionary<RoomHandler, GameObject> teleportMap = minimap != null ? minimap.RoomToTeleportMap : null;
            int teleportableRoomCount = 0;
            int activeTeleporterCount = 0;
            int revealedRoomCount = 0;
            int registeredRoomCount = teleportMap != null ? teleportMap.Count : -1;
            List<string> roomStates = new List<string>();

            if (rooms != null)
            {
                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomHandler room = rooms[index];
                    if (room == null)
                    {
                        continue;
                    }

                    bool canTeleportTo = false;
                    try
                    {
                        canTeleportTo = room.CanTeleportToRoom();
                    }
                    catch (Exception exception)
                    {
                        roomStates.Add(GetRoomLabel(room) + "{Exception=" + exception.GetType().Name + "}");
                        continue;
                    }

                    if (canTeleportTo)
                    {
                        teleportableRoomCount++;
                    }

                    if (room.TeleportersActive)
                    {
                        activeTeleporterCount++;
                    }

                    if (room.RevealedOnMap)
                    {
                        revealedRoomCount++;
                    }

                    if (room.TeleportersActive || room.RevealedOnMap || (teleportMap != null && teleportMap.ContainsKey(room)))
                    {
                        roomStates.Add(
                            GetRoomLabel(room) +
                            "{CanTo=" + canTeleportTo +
                            ",TeleActive=" + room.TeleportersActive +
                            ",Revealed=" + room.RevealedOnMap +
                            ",Visited=" + room.hasEverBeenVisited +
                            ",Force=" + room.forceTeleportersActive +
                            ",Registered=" + (teleportMap != null && teleportMap.ContainsKey(room)) + "}");
                    }
                }
            }

            LogAlways(
                "Floor map teleporter state. Phase=" + (phase ?? "<unknown>") +
                ", CurrentFloor=" + GetCurrentFloor() +
                ", IsLoadingLevel=" + IsLoadingLevel() +
                ", DungeonPresent=" + (dungeon != null) +
                ", Rooms=" + (rooms != null ? rooms.Count : -1) +
                ", MinimapPresent=" + (minimap != null) +
                ", MinimapTeleportEntries=" + registeredRoomCount +
                ", TeleportableRooms=" + teleportableRoomCount +
                ", ActiveTeleporters=" + activeTeleporterCount +
                ", RevealedRooms=" + revealedRoomCount +
                ", RoomStates=[" + string.Join(";", roomStates.ToArray()) + "].");
        }

        private void LogRoomTeleportEligibility(RoomHandler room, string phase)
        {
            if ((object)room == null)
            {
                LogAlways(
                    "Room map teleport eligibility. Phase=" + (phase ?? "<unknown>") +
                    ", Room=<null>, CanTeleportFromRoom=<unknown>." );
                return;
            }

            bool canTeleportFromRoom = false;
            bool canTeleportToRoom = false;
            int activeEnemiesAll = -1;
            int activeEnemiesRoomClear = -1;
            string exceptionText = string.Empty;
            try
            {
                canTeleportFromRoom = room.CanTeleportFromRoom();
                canTeleportToRoom = room.CanTeleportToRoom();
                activeEnemiesAll = room.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All);
                activeEnemiesRoomClear = room.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.RoomClear);
            }
            catch (Exception exception)
            {
                exceptionText = exception.GetType().Name + ":" + exception.Message;
            }

            LogAlways(
                "Room map teleport eligibility. Phase=" + (phase ?? "<unknown>") +
                ", Room=" + GetRoomLabel(room) +
                ", RoomId=" + GetRoomInstanceId(room) +
                ", CurrentFloor=" + GetCurrentFloor() +
                ", CanTeleportFromRoom=" + canTeleportFromRoom +
                ", CanTeleportToRoom=" + canTeleportToRoom +
                ", IsSealed=" + room.IsSealed +
                ", TeleportersActive=" + room.TeleportersActive +
                ", HasEverBeenVisited=" + room.hasEverBeenVisited +
                ", ForceTeleportersActive=" + room.forceTeleportersActive +
                ", ActiveEnemiesAll=" + activeEnemiesAll +
                ", ActiveEnemiesRoomClear=" + activeEnemiesRoomClear +
                ", Exception=" + (string.IsNullOrEmpty(exceptionText) ? "<none>" : exceptionText) + ".");
        }

        private void ScheduleDeferredBossSpriteMaterialDiagnostics(RoomHandler room)
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null && room != null)
            {
                gameManager.StartCoroutine(LogDeferredBossSpriteMaterialState(room));
            }
        }

        private IEnumerator LogDeferredBossSpriteMaterialState(RoomHandler room)
        {
            int[] sampleFrames = new[] { 1, 5, 30 };
            int currentFrame = 0;
            for (int sampleIndex = 0; sampleIndex < sampleFrames.Length; sampleIndex++)
            {
                int targetFrame = sampleFrames[sampleIndex];
                while (currentFrame < targetFrame)
                {
                    yield return null;
                    currentFrame++;
                }

                if (targetFrame == 1)
                {
                    FinalizeReplayedBulletBrosIntro(room);
                }

                List<AIActor> activeEnemies = room != null
                    ? room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All)
                    : null;
                if (activeEnemies == null)
                {
                    yield break;
                }

                for (int index = 0; index < activeEnemies.Count; index++)
                {
                    AIActor enemy = activeEnemies[index];
                    if ((object)enemy == null || enemy.healthHaver == null || !enemy.healthHaver.IsBoss)
                    {
                        continue;
                    }

                    _roomEnemyWaveSpawner.LogBossSpriteMaterialState(
                        enemy,
                        enemy.GetComponentsInChildren<tk2dSprite>(true),
                        "AfterSpawnFrame" + targetFrame);
                }
            }
        }

        private void FinalizeReplayedBulletBrosIntro(RoomHandler room)
        {
            if (room == null)
            {
                return;
            }

            List<AIActor> activeEnemies = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
            {
                return;
            }

            for (int index = 0; index < activeEnemies.Count; index++)
            {
                AIActor enemy = activeEnemies[index];
                if ((object)enemy == null || enemy.healthHaver == null || !enemy.healthHaver.IsBoss)
                {
                    continue;
                }

                BulletBrosIntroDoer intro = enemy.GetComponent<BulletBrosIntroDoer>();
                if (intro == null)
                {
                    continue;
                }

                try
                {
                    // BulletBrosIntroDoer.Update hides both paired Bosses during its
                    // intro setup. Vanilla later calls EndIntro, but replay deliberately
                    // skips the native intro, leaving both Bosses permanently invisible.
                    // At this point its paired references have been initialized, so the
                    // public vanilla cleanup method is safe and restores both actors.
                    intro.EndIntro();
                    LogAlways(
                        "Finalized replayed Bullet Bros intro. Room=" +
                        GetRoomLabel(room) +
                        ", Enemy=" + enemy.EnemyGuid +
                        ", Frame=1, Action=EndIntro.");
                }
                catch (Exception exception)
                {
                    LogWarning(
                        "Failed to finalize replayed Bullet Bros intro. Room=" +
                        GetRoomLabel(room) +
                        ", Enemy=" + enemy.EnemyGuid +
                        ", Exception=" + exception.GetType().Name + ":" + exception.Message + ".");
                }

                return;
            }
        }

        private void SkipBossReplayIntro(RoomHandler room)
        {
            if (!IsBossRoom(room))
            {
                return;
            }

            List<AIActor> activeEnemies = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            int bossCount = 0;
            if (activeEnemies != null)
            {
                for (int index = 0; index < activeEnemies.Count; index++)
                {
                    AIActor enemy = activeEnemies[index];
                    if ((object)enemy == null || (object)enemy.healthHaver == null || !enemy.healthHaver.IsBoss)
                    {
                        continue;
                    }

                    bossCount++;
                }
            }

            Log(
                "Skipped native Boss replay intro. Room=" + GetRoomLabel(room) +
                ", ActiveBossCount=" + bossCount +
                ", Reason=ReplayIntroCanReenterBossSpecificCoroutine.");
        }

        private void LogReplayVerification(
            RoomHandler room,
            int waveIndex,
            List<RoomEnemyReplayEntry> expectedWave,
            List<RoomEnemyReplayEntry> actualWave)
        {
            bool matches = RoomReplayWaveDiagnostics.WavesMatch(expectedWave, actualWave);
            string message =
                "Room enemy replay verification. Room=" + GetRoomLabel(room) +
                ", Wave=" + waveIndex +
                ", Match=" + matches +
                ", Expected=[" + RoomReplayWaveDiagnostics.DescribeWave(expectedWave) + "]" +
                ", Actual=[" + RoomReplayWaveDiagnostics.DescribeWave(actualWave) + "].";
            if (matches)
            {
                Log(message);
            }
            else
            {
                LogWarning(message);
            }
        }
    }
}
