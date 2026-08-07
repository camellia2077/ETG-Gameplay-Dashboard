// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using Dungeonator;
using UnityEngine;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Records the enemy waves that vanilla actually selected for a room, then replays that
    /// recording.  Recording the result is intentional: room definitions contain random
    /// variants and probability checks, neither of which can be reconstructed after clear.
    /// </summary>
    internal sealed class RoomEnemyReplayService
    {
        // Die() is called immediately before the vanilla death animation starts. Keep the
        // room unavailable long enough for the fixed Boss exit/reward transition to settle.
        private const float BossDeathRewindCooldownSeconds = 7f;
        private readonly ManualLogSource _logger;
        private readonly Func<bool> _verboseLoggingEnabledProvider;
        private readonly Func<bool> _playerRewindEnabledProvider;
        private readonly Func<bool> _roomRewindCleanupEnabledProvider;
        private readonly Action<bool> _recordingEnabledSetter;
        private readonly RoomRewindCleanupService _roomRewindCleanupService;
        private readonly RoomPlayerStateRestorer _roomPlayerStateRestorer;
        private readonly BossRoomDecorationRestorer _bossRoomDecorationRestorer;
        private readonly RoomEnemyWaveSpawner _roomEnemyWaveSpawner;
        private readonly Dictionary<RoomHandler, RoomEnemyReplaySnapshot> _snapshots =
            new Dictionary<RoomHandler, RoomEnemyReplaySnapshot>();
        private readonly HashSet<RoomHandler> _replayingRooms = new HashSet<RoomHandler>();
        private readonly HashSet<RoomHandler> _bossClearRewardsHandled = new HashSet<RoomHandler>();
        private readonly Dictionary<RoomHandler, float> _bossDeathRewindBlockedUntil =
            new Dictionary<RoomHandler, float>();
        private static readonly BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo MinimapTargetField =
            typeof(MinimapUIController).GetField("m_currentTeleportTarget", InstancePrivateFlags);
        private static readonly FieldInfo MinimapIconField =
            typeof(MinimapUIController).GetField("m_currentTeleportIconSprite", InstancePrivateFlags);
        private bool _recordingEnabled;

        public RoomEnemyReplayService(
            ManualLogSource logger,
            Func<bool> verboseLoggingEnabledProvider,
            Func<bool> playerRewindEnabledProvider,
            Func<bool> roomRewindCleanupEnabledProvider,
            Action<bool> recordingEnabledSetter)
        {
            _logger = logger;
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            _playerRewindEnabledProvider = playerRewindEnabledProvider;
            _roomRewindCleanupEnabledProvider = roomRewindCleanupEnabledProvider;
            _recordingEnabledSetter = recordingEnabledSetter;
            _roomRewindCleanupService = new RoomRewindCleanupService(
                IsRoomRewindCleanupEnabled,
                GetRoomLabel,
                IsBossRoom,
                Log);
            _roomPlayerStateRestorer = new RoomPlayerStateRestorer(Log, LogWarning);
            _bossRoomDecorationRestorer = new BossRoomDecorationRestorer(
                GetRoomLabel,
                _verboseLoggingEnabledProvider,
                Log,
                LogWarning);
            _roomEnemyWaveSpawner = new RoomEnemyWaveSpawner(
                GetRoomLabel,
                Log,
                LogAlways,
                LogWarning);
        }

        public bool IsRecordingEnabled
        {
            get { return _recordingEnabled; }
        }

        public int SnapshotCount
        {
            get { return _snapshots.Count; }
        }

        public bool ToggleRecording()
        {
            _recordingEnabled = !_recordingEnabled;
            if (_recordingEnabledSetter != null)
            {
                _recordingEnabledSetter(_recordingEnabled);
            }
            if (!_recordingEnabled)
            {
                int snapshotCount = _snapshots.Count;
                _bossRoomDecorationRestorer.DestroyTemplates(_snapshots.Values);
                _snapshots.Clear();
                _replayingRooms.Clear();
                _bossDeathRewindBlockedUntil.Clear();
                Log("Disabled room enemy replay recording and cleared snapshots. SnapshotCount=" + snapshotCount + ".");
            }

            return _recordingEnabled;
        }

        public void SetRecordingEnabled(bool enabled)
        {
            _recordingEnabled = enabled;
        }

        public bool EnsureRecordingEnabled()
        {
            _recordingEnabled = true;
            return _recordingEnabled;
        }

        public void RecordInitialWave(RoomHandler room, PlayerController player)
        {
            bool canTrack = CanTrack(room);
            bool alreadyRecorded = (object)room != null && _snapshots.ContainsKey(room);
            if (!_recordingEnabled || !canTrack || alreadyRecorded)
            {
                LogAlways(
                    "Skipped room-entry capture. Reason=" +
                    (!_recordingEnabled ? "RecordingDisabled" : !canTrack ? "RoomNotTrackable" : "SnapshotAlreadyExists") +
                    ", Room=" + GetRoomLabel(room) +
                    ", RoomId=" + GetRoomInstanceId(room) +
                    ", IsBossRoom=" + IsBossRoom(room) +
                    ", SnapshotCount=" + _snapshots.Count + ".");
                return;
            }

            RoomEnemyReplaySnapshot snapshot = new RoomEnemyReplaySnapshot();
            snapshot.Waves.Add(CaptureActiveEnemies(room));
            if (IsBossRoom(room))
            {
                snapshot.PlayerHasTakenDamageInThisRoom = room.PlayerHasTakenDamageInThisRoom;
                snapshot.HasGivenMasteryToken = GameManager.Instance != null &&
                    GameManager.Instance.Dungeon != null &&
                    GameManager.Instance.Dungeon.HasGivenMasteryToken;
                snapshot.Decorations = _bossRoomDecorationRestorer.Capture(room);
            }
            bool playerRewindEnabled = IsPlayerRewindEnabled();
            if (playerRewindEnabled)
            {
                PlayerController snapshotPlayer = player;
                if ((object)snapshotPlayer == null && GameManager.Instance != null)
                {
                    snapshotPlayer = GameManager.Instance.PrimaryPlayer;
                }

                snapshot.Player = _roomPlayerStateRestorer.Capture(snapshotPlayer);
                if (snapshot.Player == null)
                {
                    LogWarning("Room-entry player capture returned null. Room=" + GetRoomLabel(room) + ", RoomId=" + GetRoomInstanceId(room) + ".");
                }
                else
                {
                    Log("Recorded room-entry player state. Room=" + GetRoomLabel(room) + ", " + _roomPlayerStateRestorer.Describe(snapshot.Player) + ".");
                }
            }
            _snapshots.Add(room, snapshot);
            LogAlways(
                "Recorded initial room snapshot. " + Describe(room, snapshot) +
                ", RoomId=" + GetRoomInstanceId(room) +
                ", CurrentFloor=" + GetCurrentFloor() +
                ", IsBossRoom=" + IsBossRoom(room) +
                ", PlayerRewindEnabled=" + playerRewindEnabled +
                ", PlayerSnapshotCaptured=" + (snapshot.Player != null) +
                ", ActiveEnemyCount=" + snapshot.Waves[0].Count +
                ", SnapshotCount=" + _snapshots.Count +
                ", Entries=[" + RoomReplayWaveDiagnostics.DescribeWave(snapshot.Waves[0]) + "].");
        }

        public List<AIActor> BeginReinforcementCapture(RoomHandler room)
        {
            if (!_recordingEnabled || !CanTrack(room) || IsReplaying(room))
            {
                return null;
            }

            RecordInitialWave(room, GameManager.Instance != null ? GameManager.Instance.PrimaryPlayer : null);
            return CopyActiveEnemies(room);
        }

        public void CompleteReinforcementCapture(RoomHandler room, List<AIActor> beforeEnemies)
        {
            if (!_recordingEnabled || !CanTrack(room) || beforeEnemies == null || IsReplaying(room))
            {
                return;
            }

            RoomEnemyReplaySnapshot snapshot;
            if (!_snapshots.TryGetValue(room, out snapshot))
            {
                return;
            }

            List<RoomEnemyReplayEntry> wave = CaptureNewEnemies(room, beforeEnemies);
            if (wave.Count == 0)
            {
                Log("Vanilla reinforcement wave did not add replayable enemies. Room=" + GetRoomLabel(room) + ".");
                return;
            }

            snapshot.Waves.Add(wave);
            Log("Recorded reinforcement wave " + (snapshot.Waves.Count - 1) + ". " + Describe(room, snapshot) + " Entries=[" + RoomReplayWaveDiagnostics.DescribeWave(wave) + "].");
        }

        public GrantCommandExecutionResult Refresh(RoomHandler room, PlayerController player)
        {
            bool timingEnabled = _verboseLoggingEnabledProvider != null && _verboseLoggingEnabledProvider();
            long timingStart = timingEnabled ? Stopwatch.GetTimestamp() : 0L;
            LogAlways(
                "Rewind request received. Room=" + GetRoomLabel(room) +
                ", RoomId=" + GetRoomInstanceId(room) +
                ", CurrentFloor=" + GetCurrentFloor() +
                ", RecordingEnabled=" + _recordingEnabled +
                ", SnapshotCount=" + _snapshots.Count +
                ", PlayerRewindEnabled=" + IsPlayerRewindEnabled() +
                ", PlayerAvailable=" + ((object)player != null) +
                ", PlayerCurrentRoomMatches=" + ((object)player != null && player.CurrentRoom == room) +
                ", IsLoadingLevel=" + IsLoadingLevel() + ".");
            if (!_recordingEnabled)
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind.recording_disabled");
            }

            if (!CanTrack(room))
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            }

            string pendingBossDeath;
            float bossDeathBlockedUntil;
            if (TryGetBossDeathRewindCooldown(room, out bossDeathBlockedUntil))
            {
                LogAlways(
                    "Rejected rewind during Boss death cooldown. Room=" + GetRoomLabel(room) +
                    ", RemainingSeconds=" + Mathf.Max(0f, bossDeathBlockedUntil - Time.unscaledTime).ToString("F2") +
                    ", CurrentFloor=" + GetCurrentFloor() + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind.boss_death_animation_pending");
            }

            if (TryGetPendingBossDeathAnimation(room, out pendingBossDeath))
            {
                LogAlways(
                    "Rejected rewind while Boss death animation is pending. Room=" +
                    GetRoomLabel(room) +
                    ", PendingBoss=" + pendingBossDeath +
                    ", CurrentFloor=" + GetCurrentFloor() + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind.boss_death_animation_pending");
            }

            if (room.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.room_not_cleared");
            }

            // A Boss can have zero active enemies for a short interval while vanilla is
            // still processing the clear reward and door animation. Rewinding in that
            // interval re-enters Boss generation before the room-clear state is stable.
            if (IsBossRoom(room) && !_bossClearRewardsHandled.Contains(room))
            {
                LogWarning(
                    "Rejected Boss rewind before vanilla clear reward completed. Room=" +
                    GetRoomLabel(room) +
                    ", RoomId=" + GetRoomInstanceId(room) +
                    ", CurrentFloor=" + GetCurrentFloor() +
                    ", IsSealed=" + room.IsSealed + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind.boss_clear_pending");
            }

            RoomEnemyReplaySnapshot snapshot;
            if (!_snapshots.TryGetValue(room, out snapshot) || snapshot.Waves.Count == 0)
            {
                LogWarning(
                    "Room enemy replay snapshot lookup failed. Room=" + GetRoomLabel(room) +
                    ", RoomId=" + GetRoomInstanceId(room) +
                    ", CurrentFloor=" + GetCurrentFloor() +
                    ", SnapshotCount=" + _snapshots.Count +
                    ", IsLoadingLevel=" + IsLoadingLevel() +
                    ", Reason=" + (snapshot == null ? "SnapshotMissing" : "SnapshotHasNoWaves") + ".");
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.no_snapshot");
            }

            LogAlways(
                "Room enemy replay snapshot matched. Room=" + GetRoomLabel(room) +
                ", RoomId=" + GetRoomInstanceId(room) +
                ", Waves=" + snapshot.Waves.Count +
                ", RecordedPlayerSnapshot=" + (snapshot.Player != null) +
                ", RecordedEnemyCount=" + RoomReplayWaveDiagnostics.CountSnapshotEnemies(snapshot) + ".");

            if (!RoomReplayWaveDiagnostics.SnapshotContainsEnemies(snapshot))
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind.no_enemies");
            }

            ClearRoomRewindObjects(room);
            double cleanupMilliseconds = GetElapsedMilliseconds(timingStart);
            if (IsBossRoom(room))
            {
                _bossRoomDecorationRestorer.Restore(room, snapshot);
            }
            double decorationRestoreMilliseconds = GetElapsedMilliseconds(timingStart) - cleanupMilliseconds;
            snapshot.NextWaveIndex = 1;
            _replayingRooms.Add(room);
            List<RoomEnemyReplayEntry> actualWave;
            int spawned = _roomEnemyWaveSpawner.SpawnWave(room, snapshot.Waves[0], out actualWave);
            double spawnMilliseconds = GetElapsedMilliseconds(timingStart) - cleanupMilliseconds - decorationRestoreMilliseconds;
            if (spawned <= 0)
            {
                _replayingRooms.Remove(room);
                LogReplayTiming(
                    room,
                    timingEnabled,
                    cleanupMilliseconds,
                    decorationRestoreMilliseconds,
                    spawnMilliseconds,
                    0d,
                    0d,
                    GetElapsedMilliseconds(timingStart),
                    0,
                    snapshot.Decorations != null ? snapshot.Decorations.Count : 0);
                LogWarning("Room enemy replay could not spawn the recorded initial wave. " + Describe(room, snapshot));
                return GrantCommandExecutionResult.Localized(false, "result.room.refresh_enemies.failed");
            }

            if (IsBossRoom(room))
            {
                // RoomHandler.OnEnemiesCleared invokes HandleRoomClearReward, but vanilla
                // guards it with m_hasGivenReward after the first boss clear. Re-arm that
                // state so the replayed boss can generate its normal reward again. The
                // Master Round has a second, dungeon-wide guard which must be restored too.
                ArmBossRoomRewardForReplay(room, snapshot);
            }

            if (IsPlayerRewindEnabled() && snapshot.Player != null)
            {
                _roomPlayerStateRestorer.Restore(player, snapshot.Player);
            }
            double playerRestoreMilliseconds = GetElapsedMilliseconds(timingStart) -
                cleanupMilliseconds - decorationRestoreMilliseconds - spawnMilliseconds;

            room.SealRoom();
            LogRoomTeleportEligibility(room, "AfterRewindSetup");
            SkipBossReplayIntro(room);
            ScheduleDeferredBossSpriteMaterialDiagnostics(room);
            double bossIntroMilliseconds = GetElapsedMilliseconds(timingStart) -
                cleanupMilliseconds - decorationRestoreMilliseconds - spawnMilliseconds - playerRestoreMilliseconds;
            LogReplayTiming(
                room,
                timingEnabled,
                cleanupMilliseconds,
                decorationRestoreMilliseconds,
                spawnMilliseconds,
                playerRestoreMilliseconds,
                bossIntroMilliseconds,
                GetElapsedMilliseconds(timingStart),
                spawned,
                snapshot.Decorations != null ? snapshot.Decorations.Count : 0);
            LogReplayVerification(room, 0, snapshot.Waves[0], actualWave);
            LogAlways("Started recorded room enemy replay. Spawned=" + spawned + ", " + Describe(room, snapshot));
            return GrantCommandExecutionResult.Localized(true, "result.room.refresh_enemies.success", spawned);
        }

        private void LogReplayTiming(
            RoomHandler room,
            bool timingEnabled,
            double cleanupMilliseconds,
            double decorationRestoreMilliseconds,
            double spawnMilliseconds,
            double playerRestoreMilliseconds,
            double bossIntroMilliseconds,
            double totalMilliseconds,
            int spawnedEnemies,
            int decorationCount)
        {
            if (!timingEnabled)
            {
                return;
            }

            Log(
                "Boss rewind timing. Room=" + GetRoomLabel(room) +
                ", CleanupMs=" + cleanupMilliseconds.ToString("0.00") +
                ", DecorationRestoreMs=" + decorationRestoreMilliseconds.ToString("0.00") +
                ", SpawnWaveMs=" + spawnMilliseconds.ToString("0.00") +
                ", PlayerRestoreMs=" + playerRestoreMilliseconds.ToString("0.00") +
                ", BossIntroMs=" + bossIntroMilliseconds.ToString("0.00") +
                ", TotalMs=" + totalMilliseconds.ToString("0.00") +
                ", SpawnedEnemies=" + spawnedEnemies +
                ", DecorationCount=" + decorationCount + ".");
        }

        public void TrySpawnNextWaveBeforeClear(RoomHandler room, AIActor removedEnemy)
        {
            if (!IsReplaying(room) || removedEnemy == null || removedEnemy.IgnoreForRoomClear)
            {
                return;
            }

            Log(
                "Recorded replay clear check. Room=" + GetRoomLabel(room) +
                ", Removing=" + DescribeActiveEnemy(removedEnemy) +
                ", ActiveEnemies=" + DescribeActiveEnemies(room) + ".");

            if (room.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.RoomClear) != 1)
            {
                return;
            }

            RoomEnemyReplaySnapshot snapshot;
            if (!_snapshots.TryGetValue(room, out snapshot) || snapshot.NextWaveIndex >= snapshot.Waves.Count)
            {
                _replayingRooms.Remove(room);
                LogRoomTeleportEligibility(room, "AfterReplayCompleted");
                Log("Recorded room enemy replay completed. " + Describe(room, snapshot) + " ActiveEnemies=" + DescribeActiveEnemies(room) + ".");
                return;
            }

            int waveIndex = snapshot.NextWaveIndex;
            snapshot.NextWaveIndex++;
            List<RoomEnemyReplayEntry> actualWave;
            int spawned = _roomEnemyWaveSpawner.SpawnWave(room, snapshot.Waves[waveIndex], out actualWave);
            if (spawned > 0)
            {
                LogReplayVerification(room, waveIndex, snapshot.Waves[waveIndex], actualWave);
                Log("Spawned recorded room enemy wave " + waveIndex + ". Spawned=" + spawned + ", " + Describe(room, snapshot));
                return;
            }

            LogWarning("Recorded room enemy wave " + waveIndex + " did not spawn any enemies. " + Describe(room, snapshot));
        }

        public void NotifyEnemyDeregistered(RoomHandler room, AIActor enemy)
        {
            if (room == null || enemy == null || !IsBossRoom(room))
            {
                return;
            }

            LogAlways(
                "Boss enemy deregistered. Room=" + GetRoomLabel(room) +
                ", Enemy=" + DescribeActiveEnemy(enemy) +
                ", IsReplaying=" + IsReplaying(room) +
                ", ActiveEnemiesAll=" + room.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.All) +
                ", ActiveEnemiesRoomClear=" + room.GetActiveEnemiesCount(RoomHandler.ActiveEnemyType.RoomClear) +
                ", IsSealed=" + room.IsSealed + ".");
            LogRoomTeleportEligibility(room, "AfterBossEnemyDeregistered");
        }

        public void Clear(bool pluginDestroying = false)
        {
            ClearSnapshots("plugin destroy", pluginDestroying);
        }

        public int ClearRoomRewindObjects(RoomHandler room)
        {
            return _roomRewindCleanupService.ClearRoomRewindObjects(room);
        }

        public int ClearSnapshots()
        {
            return ClearSnapshots("new floor");
        }

        public int ClearSnapshots(string reason)
        {
            return ClearSnapshots(reason, false);
        }

        private int ClearSnapshots(string reason, bool pluginDestroying)
        {
            int snapshotCount = _snapshots.Count;
            if (!pluginDestroying)
            {
                LogFloorMapTeleportState("BeforeReplaySnapshotClear");
                _bossRoomDecorationRestorer.DestroyTemplates(_snapshots.Values);
            }

            _snapshots.Clear();
            _replayingRooms.Clear();
            _bossClearRewardsHandled.Clear();
            _bossDeathRewindBlockedUntil.Clear();
            LogAlways("Cleared room enemy replay snapshots. Reason=" + (reason ?? "unspecified") + ", SnapshotCount=" + snapshotCount + ".");
            return snapshotCount;
        }

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

        public void NotifyBossClearRewardHandled(RoomHandler room)
        {
            if (!IsBossRoom(room))
            {
                return;
            }

            _bossClearRewardsHandled.Add(room);
            LogAlways(
                "Vanilla Boss clear reward completed. Room=" + GetRoomLabel(room) +
                ", RoomId=" + GetRoomInstanceId(room) +
                ", CurrentFloor=" + GetCurrentFloor() +
                ", IsSealed=" + room.IsSealed + ".");
            LogRoomTeleportEligibility(room, "AfterBossClearReward");
        }

        public void NotifyBossDeathStarted(HealthHaver healthHaver)
        {
            if ((object)healthHaver == null || !healthHaver.IsBoss || !healthHaver.IsDead ||
                (object)healthHaver.aiActor == null)
            {
                return;
            }

            RoomHandler room = healthHaver.aiActor.ParentRoom;
            if (!IsBossRoom(room) || !_snapshots.ContainsKey(room))
            {
                return;
            }

            _bossDeathRewindBlockedUntil[room] = Time.unscaledTime + BossDeathRewindCooldownSeconds;
            LogAlways(
                "Started Boss death rewind cooldown. Room=" + GetRoomLabel(room) +
                ", Boss=" + DescribeActiveEnemy(healthHaver.aiActor) +
                ", CooldownSeconds=" + BossDeathRewindCooldownSeconds +
                ", CurrentFloor=" + GetCurrentFloor() + ".");
        }

        public void NotifyRoomClearRewardHandled(RoomHandler room)
        {
            if (!IsBossRoom(room))
            {
                return;
            }

            LogAlways(
                "Vanilla Room clear reward callback completed for Boss room. Room=" +
                GetRoomLabel(room) +
                ", RoomId=" + GetRoomInstanceId(room) +
                ", CurrentFloor=" + GetCurrentFloor() + ".");
            LogRoomTeleportEligibility(room, "AfterRoomClearReward");
        }

        public void NotifyMinimapTeleportAttempted(MinimapUIController controller, bool result)
        {
            RoomHandler targetRoom = MinimapTargetField != null
                ? MinimapTargetField.GetValue(controller) as RoomHandler
                : null;
            tk2dBaseSprite iconSprite = MinimapIconField != null
                ? MinimapIconField.GetValue(controller) as tk2dBaseSprite
                : null;
            GameObject iconObject = iconSprite != null ? iconSprite.gameObject : null;
            Minimap minimap = Minimap.HasInstance ? Minimap.Instance : null;
            bool registered = minimap != null && targetRoom != null &&
                minimap.RoomToTeleportMap != null && minimap.RoomToTeleportMap.ContainsKey(targetRoom);
            bool canTeleportTo = false;
            string targetException = string.Empty;
            if (targetRoom != null)
            {
                try
                {
                    canTeleportTo = targetRoom.CanTeleportToRoom();
                }
                catch (Exception exception)
                {
                    targetException = exception.GetType().Name + ":" + exception.Message;
                }
            }

            LogAlways(
                "Minimap teleport attempt. Result=" + result +
                ", CurrentFloor=" + GetCurrentFloor() +
                ", TargetRoom=" + (targetRoom != null ? GetRoomLabel(targetRoom) : "<null>") +
                ", TargetRoomId=" + (targetRoom != null ? GetRoomInstanceId(targetRoom).ToString() : "<null>") +
                ", TargetCanTeleportTo=" + (targetRoom != null ? canTeleportTo.ToString() : "<null>") +
                ", TargetTeleportersActive=" + (targetRoom != null ? targetRoom.TeleportersActive.ToString() : "<null>") +
                ", TargetIsSealed=" + (targetRoom != null ? targetRoom.IsSealed.ToString() : "<null>") +
                ", TargetRegistered=" + registered +
                ", IconPresent=" + (iconObject != null) +
                ", IconActiveSelf=" + (iconObject != null && iconObject.activeSelf) +
                ", IconActiveInHierarchy=" + (iconObject != null && iconObject.activeInHierarchy) +
                ", IconSpriteEnabled=" + (iconSprite != null && iconSprite.enabled) +
                ", MinimapPresent=" + (minimap != null) +
                ", MinimapPreventAllTeleports=" + (minimap != null && minimap.PreventAllTeleports) +
                ", ConversationBar=" + (GameUIRoot.Instance != null && GameUIRoot.Instance.DisplayingConversationBar) +
                ", TargetException=" + (string.IsNullOrEmpty(targetException) ? "<none>" : targetException) + ".");
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

        private static bool CanTrack(RoomHandler room)
        {
            return (object)room != null && room.area != null && (room.IsStandardRoom || IsBossRoom(room));
        }

        private static bool TryGetPendingBossDeathAnimation(RoomHandler room, out string description)
        {
            description = string.Empty;
            if ((object)room == null)
            {
                return false;
            }

            List<AIActor> activeEnemies = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (activeEnemies == null)
            {
                return false;
            }

            for (int index = 0; index < activeEnemies.Count; index++)
            {
                AIActor enemy = activeEnemies[index];
                if ((object)enemy == null || (object)enemy.healthHaver == null ||
                    !enemy.healthHaver.IsBoss || !enemy.healthHaver.IsDead)
                {
                    continue;
                }

                description = DescribeActiveEnemy(enemy);
                return true;
            }

            return false;
        }

        private bool TryGetBossDeathRewindCooldown(RoomHandler room, out float blockedUntil)
        {
            blockedUntil = 0f;
            if (!IsBossRoom(room) || !_bossDeathRewindBlockedUntil.TryGetValue(room, out blockedUntil))
            {
                return false;
            }

            if (Time.unscaledTime >= blockedUntil)
            {
                _bossDeathRewindBlockedUntil.Remove(room);
                return false;
            }

            return true;
        }

        private bool IsPlayerRewindEnabled()
        {
            return _playerRewindEnabledProvider != null && _playerRewindEnabledProvider();
        }

        private bool IsRoomRewindCleanupEnabled()
        {
            return _roomRewindCleanupEnabledProvider == null || _roomRewindCleanupEnabledProvider();
        }

        private static bool IsBossRoom(RoomHandler room)
        {
            return room != null &&
                   room.area != null &&
                   room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS;
        }

        private static int GetRoomInstanceId(RoomHandler room)
        {
            return (object)room != null ? RuntimeHelpers.GetHashCode(room) : 0;
        }

        private static int GetCurrentFloor()
        {
            return GameManager.Instance != null ? GameManager.Instance.CurrentFloor : -1;
        }

        private static bool IsLoadingLevel()
        {
            return GameManager.Instance != null && GameManager.Instance.IsLoadingLevel;
        }

        private void ArmBossRoomRewardForReplay(RoomHandler room, RoomEnemyReplaySnapshot snapshot)
        {
            const string rewardGivenFieldName = "m_hasGivenReward";
            bool hadRewardState = PrivateFieldAccessor.GetPrivateBool(room, rewardGivenFieldName);
            PrivateFieldAccessor.SetPrivateBool(room, rewardGivenFieldName, false);

            // RoomHandler.HandleBossClearReward checks both the room damage flag and
            // Dungeon.HasGivenMasteryToken before spawning the Master Round. Both values
            // belong to the entry state for a rewind, not to the already-cleared attempt.
            room.PlayerHasTakenDamageInThisRoom = snapshot != null && snapshot.PlayerHasTakenDamageInThisRoom;
            bool previousMasteryState = GameManager.Instance != null &&
                GameManager.Instance.Dungeon != null &&
                GameManager.Instance.Dungeon.HasGivenMasteryToken;
            if (GameManager.Instance != null && GameManager.Instance.Dungeon != null && snapshot != null)
            {
                GameManager.Instance.Dungeon.HasGivenMasteryToken = snapshot.HasGivenMasteryToken;
            }

            Log(
                "Re-armed Boss-room clear reward for replay. Room=" + GetRoomLabel(room) +
                ", PreviousHasGivenReward=" + hadRewardState +
                ", RestoredPlayerHasTakenDamage=" + room.PlayerHasTakenDamageInThisRoom +
                ", PreviousHasGivenMasteryToken=" + previousMasteryState +
                ", RestoredHasGivenMasteryToken=" +
                (snapshot != null && snapshot.HasGivenMasteryToken) + ".");
        }

        private bool IsReplaying(RoomHandler room)
        {
            return room != null && _replayingRooms.Contains(room);
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

        private static List<AIActor> CopyActiveEnemies(RoomHandler room)
        {
            List<AIActor> active = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            return active != null ? new List<AIActor>(active) : new List<AIActor>();
        }

        private static string DescribeActiveEnemies(RoomHandler room)
        {
            List<AIActor> activeEnemies = room != null ? room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All) : null;
            if (activeEnemies == null || activeEnemies.Count == 0)
            {
                return "[]";
            }

            List<string> descriptions = new List<string>();
            for (int index = 0; index < activeEnemies.Count; index++)
            {
                descriptions.Add(DescribeActiveEnemy(activeEnemies[index]));
            }

            return "[" + string.Join(";", descriptions.ToArray()) + "]";
        }

        private static string DescribeActiveEnemy(AIActor enemy)
        {
            if ((object)enemy == null)
            {
                return "<null>";
            }

            IntVector2 worldPosition = enemy.transform.position.IntXY();
            RoomHandler parentRoom = enemy.ParentRoom;
            return
                "Guid=" + enemy.EnemyGuid +
                " Placed=" + enemy.PlacedPosition.x + "," + enemy.PlacedPosition.y +
                " World=" + worldPosition.x + "," + worldPosition.y +
                " ParentRoom=" + GetRoomLabel(parentRoom) +
                " IgnoreForRoomClear=" + enemy.IgnoreForRoomClear;
        }

        private List<RoomEnemyReplayEntry> CaptureActiveEnemies(RoomHandler room)
        {
            return CaptureNewEnemies(room, new List<AIActor>());
        }

        private List<RoomEnemyReplayEntry> CaptureNewEnemies(RoomHandler room, List<AIActor> beforeEnemies)
        {
            List<RoomEnemyReplayEntry> entries = new List<RoomEnemyReplayEntry>();
            List<AIActor> active = room.GetActiveEnemies(RoomHandler.ActiveEnemyType.All);
            if (active == null)
            {
                return entries;
            }

            for (int index = 0; index < active.Count; index++)
            {
                AIActor enemy = active[index];
                if ((object)enemy == null || beforeEnemies.Contains(enemy) || string.IsNullOrEmpty(enemy.EnemyGuid))
                {
                    continue;
                }

                // PlacedPosition is normally the vanilla placement anchor, but a small set of
                // spawned enemies leave it at its default (0, 0). Replaying that value creates
                // an invisible, room-owned enemy outside the dungeon and permanently seals the
                // room. Fall back to the occupied world cell in that case.
                IntVector2 placedPosition = enemy.PlacedPosition;
                IntVector2 worldPosition = enemy.transform.position.IntXY();
                IntVector2 spawnPosition;
                if (room.ContainsPosition(placedPosition))
                {
                    spawnPosition = placedPosition;
                }
                else if (room.ContainsPosition(worldPosition))
                {
                    spawnPosition = worldPosition;
                    Log(
                        "Recorded enemy uses world-cell replay anchor. Room=" + GetRoomLabel(room) +
                        ", Guid=" + enemy.EnemyGuid +
                        ", InvalidPlaced=" + placedPosition.x + "," + placedPosition.y +
                        ", World=" + worldPosition.x + "," + worldPosition.y + ".");
                }
                else
                {
                    LogWarning(
                        "Skipped recorded enemy with no valid room position. Room=" + GetRoomLabel(room) +
                        ", Guid=" + enemy.EnemyGuid +
                        ", Placed=" + placedPosition.x + "," + placedPosition.y +
                        ", World=" + worldPosition.x + "," + worldPosition.y + ".");
                    continue;
                }

                entries.Add(new RoomEnemyReplayEntry(
                    enemy.EnemyGuid,
                    spawnPosition,
                    worldPosition,
                    enemy.IgnoreForRoomClear));
            }

            return entries;
        }

        private string Describe(RoomHandler room, RoomEnemyReplaySnapshot snapshot)
        {
            int waveCount = snapshot != null ? snapshot.Waves.Count : 0;
            int nextWave = snapshot != null ? snapshot.NextWaveIndex : -1;
            return "Room=" + GetRoomLabel(room) + ", WaveCount=" + waveCount + ", NextWaveIndex=" + nextWave + ".";
        }

        private static string GetRoomLabel(RoomHandler room)
        {
            string roomName = room != null ? room.GetRoomName() : null;
            return string.IsNullOrEmpty(roomName) ? "<unnamed>" : roomName;
        }

        private void Log(string message)
        {
            if (_logger != null && _verboseLoggingEnabledProvider != null && _verboseLoggingEnabledProvider())
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command(message));
            }
        }

        private void LogAlways(string message)
        {
            if (_logger != null)
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Command(message));
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning(EtgGameplayDashboardLog.Command(message));
            }
        }

        private static double GetElapsedMilliseconds(long startedAtTimestamp)
        {
            if (startedAtTimestamp == 0L)
            {
                return 0d;
            }

            return (Stopwatch.GetTimestamp() - startedAtTimestamp) * 1000d / Stopwatch.Frequency;
        }

    }
}
