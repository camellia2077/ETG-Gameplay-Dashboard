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
        private readonly Dictionary<RoomHandler, RoomEnemyReplaySnapshot> _snapshots =
            new Dictionary<RoomHandler, RoomEnemyReplaySnapshot>();
        private readonly HashSet<RoomHandler> _replayingRooms = new HashSet<RoomHandler>();
        private readonly HashSet<RoomHandler> _bossClearRewardsHandled = new HashSet<RoomHandler>();
        private readonly Dictionary<RoomHandler, float> _bossDeathRewindBlockedUntil =
            new Dictionary<RoomHandler, float>();
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
                DestroyDecorationTemplates();
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
                snapshot.Decorations = CaptureBossRoomDecorations(room);
            }
            bool playerRewindEnabled = IsPlayerRewindEnabled();
            if (playerRewindEnabled)
            {
                PlayerController snapshotPlayer = player;
                if ((object)snapshotPlayer == null && GameManager.Instance != null)
                {
                    snapshotPlayer = GameManager.Instance.PrimaryPlayer;
                }

                snapshot.Player = CapturePlayerState(snapshotPlayer);
                if (snapshot.Player == null)
                {
                    LogWarning("Room-entry player capture returned null. Room=" + GetRoomLabel(room) + ", RoomId=" + GetRoomInstanceId(room) + ".");
                }
                else
                {
                    Log("Recorded room-entry player state. Room=" + GetRoomLabel(room) + ", " + DescribePlayerState(snapshot.Player) + ".");
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
                ", Entries=[" + DescribeWave(snapshot.Waves[0]) + "].");
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
            Log("Recorded reinforcement wave " + (snapshot.Waves.Count - 1) + ". " + Describe(room, snapshot) + " Entries=[" + DescribeWave(wave) + "].");
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
                ", RecordedEnemyCount=" + CountSnapshotEnemies(snapshot) + ".");

            if (!SnapshotContainsEnemies(snapshot))
            {
                return GrantCommandExecutionResult.Localized(false, "result.room.rewind.no_enemies");
            }

            ClearRoomRewindObjects(room);
            double cleanupMilliseconds = GetElapsedMilliseconds(timingStart);
            if (IsBossRoom(room))
            {
                RestoreBossRoomDecorations(room, snapshot);
            }
            double decorationRestoreMilliseconds = GetElapsedMilliseconds(timingStart) - cleanupMilliseconds;
            snapshot.NextWaveIndex = 1;
            _replayingRooms.Add(room);
            List<RoomEnemyReplayEntry> actualWave;
            int spawned = SpawnWave(room, snapshot.Waves[0], out actualWave);
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
                RestorePlayerState(player, snapshot.Player);
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
            int spawned = SpawnWave(room, snapshot.Waves[waveIndex], out actualWave);
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

        public void Clear()
        {
            ClearSnapshots();
        }

        public int ClearRoomRewindObjects(RoomHandler room)
        {
            if ((object)room == null || !IsRoomRewindCleanupEnabled())
            {
                return 0;
            }

            int removedCount = 0;
            int removedProjectiles = 0;
            int removedDecalsAndDebris = 0;
            int removedCorpses = 0;
            int removedRoomPersistentVfx = 0;
            int removedPedestals = 0;
            HashSet<GameObject> removedObjects = new HashSet<GameObject>();
            List<Projectile> projectiles = room.GetComponentsAbsoluteInRoom<Projectile>();
            List<EphemeralObject> ephemeralObjects = room.GetComponentsAbsoluteInRoom<EphemeralObject>();
            List<PersistentVFXBehaviour> persistentVfx = room.GetComponentsAbsoluteInRoom<PersistentVFXBehaviour>();
            List<DebrisObject> corpseDebris = room.GetComponentsAbsoluteInRoom<DebrisObject>();
            List<CorpseSpawnController> corpseControllers = room.GetComponentsAbsoluteInRoom<CorpseSpawnController>();
            List<GameObject> corpses = StaticReferenceManager.AllCorpses != null
                ? new List<GameObject>(StaticReferenceManager.AllCorpses)
                : null;
            Log(
                "Rewind cleanup scan. Room=" + GetRoomLabel(room) +
                ", IsBossRoom=" + IsBossRoom(room) +
                ", ProjectilesFound=" + CountValidObjects(projectiles) +
                ", EphemeralObjectsFound=" + CountValidObjects(ephemeralObjects) +
                ", PersistentVfxFound=" + CountValidObjects(persistentVfx) +
                ", CorpsesFound=" + CountValidObjects(corpses) +
                ", CorpseDebrisFound=" + CountCorpseDebris(corpseDebris) +
                ", CorpseControllersFound=" + CountValidObjects(corpseControllers) + ".");

            if (projectiles != null)
            {
                for (int index = 0; index < projectiles.Count; index++)
                {
                    Projectile projectile = projectiles[index];
                    if ((object)projectile == null || (object)projectile.gameObject == null || !removedObjects.Add(projectile.gameObject))
                    {
                        continue;
                    }

                    projectile.OnDespawned();
                    SpawnManager.Despawn(projectile.gameObject);
                    removedProjectiles++;
                    removedCount++;
                }
            }

            // AIActor.ForceDeath registers spawned corpse prefabs in AllCorpses. Some
            // small-enemy corpse prefabs do not expose DebrisObject, so the generic
            // EphemeralObject cleanup above cannot find them. Filter by room before
            // despawning so cleanup never touches corpses from another room.
            if (corpses != null)
            {
                for (int index = 0; index < corpses.Count; index++)
                {
                    GameObject corpse = corpses[index];
                    if ((object)corpse == null || !IsObjectInRoom(room, corpse) || !removedObjects.Add(corpse))
                    {
                        continue;
                    }

                    // Hide first because pooled debris may not be deactivated until the
                    // pool manager processes the despawn on a later frame.
                    corpse.SetActive(false);
                    SpawnManager.Despawn(corpse);
                    StaticReferenceManager.AllCorpses.Remove(corpse);
                    removedCorpses++;
                    removedCount++;
                }
            }

            // Some small-enemy prefabs retain DebrisObject.IsCorpse but are not present
            // in AllCorpses after vanilla has already pruned the static list. Catch those
            // room-local corpse debris directly as a second path.
            if (corpseDebris != null)
            {
                for (int index = 0; index < corpseDebris.Count; index++)
                {
                    DebrisObject debris = corpseDebris[index];
                    if ((object)debris == null || !debris.IsCorpse || (object)debris.gameObject == null ||
                        !removedObjects.Add(debris.gameObject))
                    {
                        continue;
                    }

                    debris.gameObject.SetActive(false);
                    SpawnManager.Despawn(debris.gameObject);
                    removedCorpses++;
                    removedCount++;
                }
            }

            // Corpse prefabs can also retain a CorpseSpawnController while their
            // DebrisObject is not returned by the room component scan. Hide and despawn
            // these controllers directly so their fallen-body sprite cannot survive.
            if (corpseControllers != null)
            {
                for (int index = 0; index < corpseControllers.Count; index++)
                {
                    CorpseSpawnController controller = corpseControllers[index];
                    if ((object)controller == null || (object)controller.gameObject == null ||
                        !removedObjects.Add(controller.gameObject))
                    {
                        continue;
                    }

                    controller.gameObject.SetActive(false);
                    SpawnManager.Despawn(controller.gameObject);
                    removedCorpses++;
                    removedCount++;
                }
            }

            // Death effects can be spawned as standalone PersistentVFXBehaviour objects
            // rather than as the corpse root. Remove room-local VFX, but preserve anything
            // owned by a player, gun, or pickup so character/weapon render effects survive.
            if (persistentVfx != null)
            {
                for (int index = 0; index < persistentVfx.Count; index++)
                {
                    PersistentVFXBehaviour vfx = persistentVfx[index];
                    if ((object)vfx == null || (object)vfx.gameObject == null ||
                        IsPlayerOwnedVfx(vfx.gameObject) || IsRoomDecorationOwnedVfx(vfx.gameObject) ||
                        !removedObjects.Add(vfx.gameObject))
                    {
                        continue;
                    }

                    vfx.gameObject.SetActive(false);
                    SpawnManager.Despawn(vfx.gameObject);
                    removedRoomPersistentVfx++;
                    removedCount++;
                }
            }

            if (ephemeralObjects != null)
            {
                for (int index = 0; index < ephemeralObjects.Count; index++)
                {
                    EphemeralObject ephemeralObject = ephemeralObjects[index];
                    if ((object)ephemeralObject == null || (object)ephemeralObject.gameObject == null || !removedObjects.Add(ephemeralObject.gameObject))
                    {
                        continue;
                    }

                    // Do not clear every EphemeralObject: player and weapon-related VFX
                    // can also live in this room. Only remove confirmed floor/debris types.
                    if (!(ephemeralObject is DecalObject) && !(ephemeralObject is DebrisObject))
                    {
                        removedObjects.Remove(ephemeralObject.gameObject);
                        continue;
                    }

                    DebrisObject debrisObject = ephemeralObject as DebrisObject;
                    if (debrisObject != null && debrisObject.IsCorpse)
                    {
                        // Do not use DebrisObject.TriggerDestruction for corpses. It
                        // starts a fade/pitfall lifecycle and can leave the fallen-body
                        // sprite visible while the replay is already spawning.
                        ephemeralObject.gameObject.SetActive(false);
                        SpawnManager.Despawn(ephemeralObject.gameObject);
                        StaticReferenceManager.AllCorpses.Remove(ephemeralObject.gameObject);
                        removedCorpses++;
                        removedCount++;
                        continue;
                    }

                    ephemeralObject.TriggerDestruction(true);
                    removedDecalsAndDebris++;
                    removedCount++;
                }
            }

            if (IsBossRoom(room))
            {
                List<RewardPedestal> pedestals = room.GetComponentsAbsoluteInRoom<RewardPedestal>();
                if (pedestals != null)
                {
                    for (int index = 0; index < pedestals.Count; index++)
                    {
                        RewardPedestal pedestal = pedestals[index];
                        if ((object)pedestal == null || (object)pedestal.gameObject == null)
                        {
                            continue;
                        }

                        if (!removedObjects.Add(pedestal.gameObject))
                        {
                            continue;
                        }

                        UnityEngine.Object.Destroy(pedestal.gameObject);
                        removedPedestals++;
                        removedCount++;
                    }
                }
            }

            List<PickupObject> pickups = room.GetComponentsAbsoluteInRoom<PickupObject>();
            if (pickups != null)
            {
                for (int index = 0; index < pickups.Count; index++)
                {
                    PickupObject pickup = pickups[index];
                    if ((object)pickup == null || (object)pickup.gameObject == null)
                    {
                        continue;
                    }

                    // Boss currency and item drops are spawned as pickup objects, normally
                    // with a DebrisObject. Remove only scene drops, not player inventory.
                    if ((object)pickup.GetComponent<DebrisObject>() == null && !(pickup is CurrencyPickup))
                    {
                        continue;
                    }

                    if (!removedObjects.Add(pickup.gameObject))
                    {
                        continue;
                    }

                    UnityEngine.Object.Destroy(pickup.gameObject);
                    removedCount++;
                }
            }

            Log(
                "Cleared rewind-room objects before replay. Room=" + GetRoomLabel(room) +
                ", IsBossRoom=" + IsBossRoom(room) +
                ", RemovedProjectiles=" + removedProjectiles +
                ", RemovedDecalsAndDebris=" + removedDecalsAndDebris +
                ", RemovedCorpses=" + removedCorpses +
                ", RemovedRoomPersistentVfx=" + removedRoomPersistentVfx +
                ", PersistentVfxSkipped=" + Math.Max(0, CountValidObjects(persistentVfx) - removedRoomPersistentVfx) +
                ", RemovedPedestals=" + removedPedestals +
                ", RemovedObjects=" + removedCount + ".");
            return removedCount;
        }

        public int ClearSnapshots()
        {
            return ClearSnapshots("new floor");
        }

        public int ClearSnapshots(string reason)
        {
            int snapshotCount = _snapshots.Count;
            LogFloorMapTeleportState("BeforeReplaySnapshotClear");
            DestroyDecorationTemplates();
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

        private static int CountSnapshotEnemies(RoomEnemyReplaySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Waves == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < snapshot.Waves.Count; index++)
            {
                if (snapshot.Waves[index] != null)
                {
                    count += snapshot.Waves[index].Count;
                }
            }

            return count;
        }

        private static bool IsObjectInRoom(RoomHandler room, GameObject gameObject)
        {
            try
            {
                GameManager gameManager = GameManager.Instance;
                Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
                DungeonData dungeonData = dungeon != null ? dungeon.data : null;
                Transform transform = (object)gameObject != null ? gameObject.transform : null;
                if ((object)room == null || (object)gameObject == null || dungeonData == null || (object)transform == null)
                {
                    return false;
                }

                return dungeonData.GetAbsoluteRoomFromPosition(transform.position.IntXY()) == room;
            }
            catch (Exception)
            {
                // AllCorpses can contain an object destroyed by vanilla before the list
                // is pruned, and some special-floor room data can be unavailable while
                // resolving the corpse's world position. Treat either stale entry as
                // out of scope for this cleanup so one corpse cannot abort the replay.
                return false;
            }
        }

        private static bool IsPlayerOwnedVfx(GameObject gameObject)
        {
            if ((object)gameObject == null)
            {
                return false;
            }

            return gameObject.GetComponentInParent<PlayerController>() != null ||
                   gameObject.GetComponentInParent<Gun>() != null ||
                   gameObject.GetComponentInParent<PickupObject>() != null;
        }

        private static bool IsRoomDecorationOwnedVfx(GameObject gameObject)
        {
            if ((object)gameObject == null)
            {
                return false;
            }

            // MajorBreakable and MinorBreakable inherit from ETG's persistent-VFX
            // behaviours. Their child VFX is part of the table/building itself, not
            // a disposable death effect. Removing it deactivates or despawns the
            // whole room decoration before the replay restore can run.
            return gameObject.GetComponentInParent<FlippableCover>() != null ||
                   gameObject.GetComponentInParent<TallGrassPatch>() != null ||
                   gameObject.GetComponentInParent<MajorBreakable>() != null ||
                   gameObject.GetComponentInParent<MinorBreakable>() != null ||
                   gameObject.GetComponentInParent<BreakableColumn>() != null ||
                   gameObject.GetComponentInParent<BreakableObject>() != null ||
                   gameObject.GetComponentInParent<BreakableSprite>() != null;
        }

        private static void ClearTallGrassFireState(TallGrassPatch grass)
        {
            FieldInfo field = grass != null ? grass.GetType().GetField("m_fireData", InstancePrivateFlags) : null;
            if (field != null)
            {
                field.SetValue(grass, Activator.CreateInstance(field.FieldType));
            }

            FieldInfo stripPool = grass != null ? grass.GetType().GetField("m_tiledSpritePool", InstancePrivateFlags) : null;
            if (stripPool != null)
            {
                stripPool.SetValue(grass, Activator.CreateInstance(stripPool.FieldType));
            }
        }

        private void ArmBossRoomRewardForReplay(RoomHandler room, RoomEnemyReplaySnapshot snapshot)
        {
            const string rewardGivenFieldName = "m_hasGivenReward";
            bool hadRewardState = GetPrivateBool(room, rewardGivenFieldName);
            SetPrivateBool(room, rewardGivenFieldName, false);

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

        private List<RoomDecorationState> CaptureBossRoomDecorations(RoomHandler room)
        {
            List<RoomDecorationState> decorations = new List<RoomDecorationState>();
            HashSet<GameObject> capturedRoots = new HashSet<GameObject>();

            List<FlippableCover> covers = room.GetComponentsAbsoluteInRoom<FlippableCover>();
            if (covers != null)
            {
                for (int index = 0; index < covers.Count; index++)
                {
                    FlippableCover cover = covers[index];
                    if ((object)cover == null || (object)cover.gameObject == null || !capturedRoots.Add(cover.gameObject))
                    {
                        continue;
                    }

                    MajorBreakable breakable = cover.GetComponentInChildren<MajorBreakable>();
                    RoomDecorationState state = RoomDecorationState.ForCover(cover, breakable);
                    state.Prototype = FindDecorationPrototype(room, state.WorldPosition, state.Kind);
                    decorations.Add(state);
                }
            }

            List<MajorBreakable> majors = room.GetComponentsAbsoluteInRoom<MajorBreakable>();
            if (majors != null)
            {
                for (int index = 0; index < majors.Count; index++)
                {
                    MajorBreakable major = majors[index];
                    if ((object)major == null || (object)major.gameObject == null ||
                        major.GetComponentInParent<FlippableCover>() != null || !capturedRoots.Add(major.gameObject))
                    {
                        continue;
                    }

                    RoomDecorationState state = RoomDecorationState.ForMajor(major);
                    state.Prototype = FindDecorationPrototype(room, state.WorldPosition, state.Kind);
                    decorations.Add(state);
                }
            }

            List<MinorBreakable> minors = GetRoomMinorBreakables(room);
            if (minors != null)
            {
                for (int index = 0; index < minors.Count; index++)
                {
                    MinorBreakable minor = minors[index];
                    if ((object)minor == null || (object)minor.gameObject == null || !capturedRoots.Add(minor.gameObject))
                    {
                        continue;
                    }

                    RoomDecorationState state = RoomDecorationState.ForMinor(minor);
                    state.Prototype = FindDecorationPrototype(room, state.WorldPosition, state.Kind);
                    // MinorBreakable keeps private broken state, disabled rigidbodies,
                    // and break-animation sprite state. Keep an intact runtime template
                    // even when a room prototype is available so rewind can restore the
                    // actual captured visual instead of only toggling m_isBroken.
                    state.Template = CreateDecorationTemplate(minor.gameObject);
                    if (_verboseLoggingEnabledProvider() &&
                        (minor.gameObject.name.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         minor.gameObject.name.IndexOf("bush", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        Log("Captured Boss-room named minor. Room=" + GetRoomLabel(room) +
                            ", Name=" + minor.gameObject.name +
                            ", Position=" + state.WorldPosition + ".");
                    }
                    decorations.Add(state);
                }
            }

            List<TallGrassPatch> grassPatches = GetRoomGrassPatches(room);
            if (_verboseLoggingEnabledProvider())
            {
                TallGrassPatch[] visibleGrass = UnityEngine.Object.FindObjectsOfType<TallGrassPatch>();
                int globalGrassCount = StaticReferenceManager.AllGrasses != null
                    ? StaticReferenceManager.AllGrasses.Count
                    : 0;
                Log("Boss-room grass scan. Room=" + GetRoomLabel(room) +
                    ", VisibleComponents=" + (visibleGrass != null ? visibleGrass.Length : 0) +
                    ", GlobalRegistry=" + globalGrassCount +
                    ", RoomGrass=" + (grassPatches != null ? grassPatches.Count : 0) + ".");
            }
            if (grassPatches != null)
            {
                for (int index = 0; index < grassPatches.Count; index++)
                {
                    TallGrassPatch grass = grassPatches[index];
                    if ((object)grass == null || (object)grass.gameObject == null || !capturedRoots.Add(grass.gameObject))
                    {
                        continue;
                    }

                    decorations.Add(RoomDecorationState.ForGrass(grass));
                }
            }

            List<BreakableObject> breakableObjects = room.GetComponentsAbsoluteInRoom<BreakableObject>();
            if (breakableObjects != null)
            {
                for (int index = 0; index < breakableObjects.Count; index++)
                {
                    BreakableObject breakable = breakableObjects[index];
                    if ((object)breakable == null || (object)breakable.gameObject == null || !capturedRoots.Add(breakable.gameObject))
                    {
                        continue;
                    }
                    RoomDecorationState state = RoomDecorationState.ForTemplate(RoomDecorationKind.BreakableObject, breakable.gameObject);
                    state.Template = CreateDecorationTemplate(breakable.gameObject);
                    decorations.Add(state);
                }
            }

            List<BreakableSprite> breakableSprites = room.GetComponentsAbsoluteInRoom<BreakableSprite>();
            if (breakableSprites != null)
            {
                for (int index = 0; index < breakableSprites.Count; index++)
                {
                    BreakableSprite breakable = breakableSprites[index];
                    if ((object)breakable == null || (object)breakable.gameObject == null || !capturedRoots.Add(breakable.gameObject))
                    {
                        continue;
                    }
                    RoomDecorationState state = RoomDecorationState.ForTemplate(RoomDecorationKind.BreakableSprite, breakable.gameObject);
                    state.Template = CreateDecorationTemplate(breakable.gameObject);
                    decorations.Add(state);
                }
            }

            Log("Captured Boss-room destructible state. Room=" + GetRoomLabel(room) +
                ", Decorations=" + decorations.Count +
                ", Covers=" + CountDecorationKind(decorations, RoomDecorationKind.Cover) +
                ", Majors=" + CountDecorationKind(decorations, RoomDecorationKind.Major) +
                ", Minors=" + CountDecorationKind(decorations, RoomDecorationKind.Minor) +
                ", Grass=" + CountDecorationKind(decorations, RoomDecorationKind.Grass) +
                ", BreakableObjects=" + CountDecorationKind(decorations, RoomDecorationKind.BreakableObject) +
                ", BreakableSprites=" + CountDecorationKind(decorations, RoomDecorationKind.BreakableSprite) + ".");
            return decorations;
        }

        private static int CountDecorationKind(List<RoomDecorationState> decorations, RoomDecorationKind kind)
        {
            int count = 0;
            if (decorations == null)
            {
                return count;
            }

            for (int index = 0; index < decorations.Count; index++)
            {
                if (decorations[index] != null && decorations[index].Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }

        private static List<TallGrassPatch> GetRoomGrassPatches(RoomHandler room)
        {
            List<TallGrassPatch> result = new List<TallGrassPatch>();
            HashSet<TallGrassPatch> seen = new HashSet<TallGrassPatch>();
            List<TallGrassPatch> roomGrass = room != null
                ? room.GetComponentsAbsoluteInRoom<TallGrassPatch>()
                : null;
            AddRoomGrassPatches(room, roomGrass, result, seen);
            List<TallGrassPatch> allGrasses = StaticReferenceManager.AllGrasses;
            AddRoomGrassPatches(room, allGrasses, result, seen);
            return result;
        }

        private static void AddRoomGrassPatches(
            RoomHandler room,
            List<TallGrassPatch> candidates,
            List<TallGrassPatch> result,
            HashSet<TallGrassPatch> seen)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                TallGrassPatch grass = candidates[index];
                if ((object)grass == null || (object)grass.gameObject == null ||
                    !seen.Add(grass) || grass.cells == null)
                {
                    continue;
                }

                for (int cellIndex = 0; cellIndex < grass.cells.Count; cellIndex++)
                {
                    if (IsCellInRoom(room, grass.cells[cellIndex]))
                    {
                        result.Add(grass);
                        break;
                    }
                }
            }

        }

        private static List<MinorBreakable> GetRoomMinorBreakables(RoomHandler room)
        {
            List<MinorBreakable> result = new List<MinorBreakable>();
            HashSet<MinorBreakable> seen = new HashSet<MinorBreakable>();
            List<MinorBreakable> roomMinors = room != null
                ? room.GetComponentsAbsoluteInRoom<MinorBreakable>()
                : null;
            AddRoomMinorBreakables(room, roomMinors, result, seen);
            AddRoomMinorBreakables(room, StaticReferenceManager.AllMinorBreakables, result, seen);
            return result;
        }

        private static void AddRoomMinorBreakables(
            RoomHandler room,
            List<MinorBreakable> candidates,
            List<MinorBreakable> result,
            HashSet<MinorBreakable> seen)
        {
            if (candidates == null)
            {
                return;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                MinorBreakable minor = candidates[index];
                if ((object)minor == null || (object)minor.gameObject == null || !seen.Add(minor) ||
                    !IsCellInRoom(room, minor.transform.position.IntXY(VectorConversions.Floor)))
                {
                    continue;
                }
                result.Add(minor);
            }
        }

        private static bool IsCellInRoom(RoomHandler room, IntVector2 cell)
        {
            if (room == null)
            {
                return false;
            }
            if ((room.Cells != null && room.Cells.Contains(cell)) ||
                (room.RawCells != null && room.RawCells.Contains(cell)))
            {
                return true;
            }

            return GameManager.Instance != null && GameManager.Instance.Dungeon != null &&
                   GameManager.Instance.Dungeon.data != null &&
                   GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(cell) == room;
        }

        private void RestoreBossRoomDecorations(RoomHandler room, RoomEnemyReplaySnapshot snapshot)
        {
            if (snapshot == null || snapshot.Decorations == null)
            {
                LogWarning("Boss-room destructible restore skipped because no decoration snapshot exists. Room=" + GetRoomLabel(room) + ".");
                return;
            }

            int restored = 0;
            int respawned = 0;
            int missing = 0;
            int failed = 0;
            Dictionary<DecorationLookupKey, GameObject> currentObjects = BuildDecorationLookup(room);
            for (int index = 0; index < snapshot.Decorations.Count; index++)
            {
                RoomDecorationState state = snapshot.Decorations[index];
                GameObject current = FindDecorationObject(currentObjects, state);
                if (current != null && state.Template != null &&
                    (state.Kind == RoomDecorationKind.BreakableObject || state.Kind == RoomDecorationKind.BreakableSprite))
                {
                    UnityEngine.Object.Destroy(current);
                    current = null;
                }
                if (current == null)
                {
                    current = RespawnDecoration(room, state);
                    if (current != null)
                    {
                        respawned++;
                    }
                }

                if (current == null)
                {
                    if (state.Prototype == null)
                    {
                        missing++;
                    }
                    else
                    {
                        failed++;
                    }
                    Log("Boss-room destructible restore missing object. Room=" + GetRoomLabel(room) +
                        ", Kind=" + state.Kind +
                        ", Position=" + state.WorldPosition.x + "," + state.WorldPosition.y +
                        ", Prototype=" + (state.Prototype != null) +
                        ", Template=" + (state.Template != null) + ".");
                    continue;
                }

                RestoreDecorationState(room, state, current);
                restored++;
            }

            Log(
                "Restored Boss-room destructible state. Room=" + GetRoomLabel(room) +
                ", SnapshotCount=" + snapshot.Decorations.Count +
                ", Restored=" + restored +
                ", Respawned=" + respawned +
                ", Missing=" + missing +
                ", Failed=" + failed + ".");
            if (missing > 0 || failed > 0)
            {
                LogWarning("Boss-room destructible restore was incomplete. Room=" + GetRoomLabel(room) +
                    ", Missing=" + missing + ", Failed=" + failed + ".");
            }
        }

        private static Dictionary<DecorationLookupKey, GameObject> BuildDecorationLookup(RoomHandler room)
        {
            Dictionary<DecorationLookupKey, GameObject> result = new Dictionary<DecorationLookupKey, GameObject>();
            if (room == null)
            {
                return result;
            }

            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<FlippableCover>(), RoomDecorationKind.Cover, false);
            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<MajorBreakable>(), RoomDecorationKind.Major, true);
            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<BreakableObject>(), RoomDecorationKind.BreakableObject, false);
            AddDecorationObjects(result, room.GetComponentsAbsoluteInRoom<BreakableSprite>(), RoomDecorationKind.BreakableSprite, false);
            AddDecorationObjects(result, GetRoomMinorBreakables(room), RoomDecorationKind.Minor, false);

            List<TallGrassPatch> grassPatches = GetRoomGrassPatches(room);
            if (grassPatches != null)
            {
                for (int index = 0; index < grassPatches.Count; index++)
                {
                    TallGrassPatch grass = grassPatches[index];
                    if (grass == null || grass.gameObject == null || grass.cells == null)
                    {
                        continue;
                    }

                    for (int cellIndex = 0; cellIndex < grass.cells.Count; cellIndex++)
                    {
                        AddDecorationObject(
                            result,
                            RoomDecorationKind.Grass,
                            grass.cells[cellIndex],
                            grass.gameObject);
                    }
                }
            }

            return result;
        }

        private static void AddDecorationObjects<T>(
            Dictionary<DecorationLookupKey, GameObject> lookup,
            List<T> objects,
            RoomDecorationKind kind,
            bool skipCovers) where T : Component
        {
            if (objects == null)
            {
                return;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                T component = objects[index];
                if (component == null || component.gameObject == null ||
                    (skipCovers && component.GetComponentInParent<FlippableCover>() != null))
                {
                    continue;
                }

                AddDecorationObject(lookup, kind, component.transform.position.IntXY(), component.gameObject);
            }
        }

        private static void AddDecorationObject(
            Dictionary<DecorationLookupKey, GameObject> lookup,
            RoomDecorationKind kind,
            IntVector2 position,
            GameObject gameObject)
        {
            if (lookup == null || gameObject == null)
            {
                return;
            }

            DecorationLookupKey key = new DecorationLookupKey(kind, position);
            if (!lookup.ContainsKey(key))
            {
                lookup.Add(key, gameObject);
            }
        }

        private static GameObject FindDecorationObject(
            Dictionary<DecorationLookupKey, GameObject> lookup,
            RoomDecorationState state)
        {
            if (lookup == null || state == null)
            {
                return null;
            }

            GameObject gameObject;
            return lookup.TryGetValue(new DecorationLookupKey(state.Kind, state.WorldPosition), out gameObject)
                ? gameObject
                : null;
        }

        private struct DecorationLookupKey : IEquatable<DecorationLookupKey>
        {
            public DecorationLookupKey(RoomDecorationKind kind, IntVector2 position)
            {
                Kind = kind;
                X = position.x;
                Y = position.y;
            }

            private readonly RoomDecorationKind Kind;
            private readonly int X;
            private readonly int Y;

            public bool Equals(DecorationLookupKey other)
            {
                return Kind == other.Kind && X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return obj is DecorationLookupKey && Equals((DecorationLookupKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)Kind;
                    hash = (hash * 397) ^ X;
                    hash = (hash * 397) ^ Y;
                    return hash;
                }
            }
        }

        private GameObject RespawnDecoration(RoomHandler room, RoomDecorationState state)
        {
            PrototypePlacedObjectData data = state.Prototype;
            if (data == null && state.Template == null)
            {
                return null;
            }

            IntVector2 location = state.WorldPosition - room.area.basePosition;
            GameObject spawned = null;
            if (data == null && state.Template != null)
            {
                spawned = UnityEngine.Object.Instantiate(state.Template);
                spawned.transform.position = state.WorldTransformPosition;
                spawned.transform.rotation = state.WorldRotation;
                spawned.transform.localScale = state.WorldScale;
                spawned.SetActive(true);
                MinorBreakable templateMinor = spawned.GetComponentInChildren<MinorBreakable>();
                if (templateMinor != null)
                {
                    templateMinor.ConfigureOnPlacement(room);
                    if (templateMinor.specRigidbody != null)
                    {
                        templateMinor.specRigidbody.Reinitialize();
                    }
                }
                Log("Respawned Boss-room destructible from captured template. Room=" + GetRoomLabel(room) +
                    ", Kind=" + state.Kind + ", Position=" + state.WorldPosition.x + "," + state.WorldPosition.y + ".");
            }
            else if (data != null && data.nonenemyBehaviour != null)
            {
                spawned = data.nonenemyBehaviour.InstantiateObject(room, location);
            }
            else if (data != null && data.placeableContents != null && !data.placeableContents.ContainsEnemy)
            {
                spawned = data.placeableContents.InstantiateObject(room, location);
            }

            if (spawned != null)
            {
                if (data != null)
                {
                    room.HandleFields(data, spawned);
                }
                Log("Respawned Boss-room destructible. Room=" + GetRoomLabel(room) +
                    ", Kind=" + state.Kind + ", Position=" + state.WorldPosition.x + "," + state.WorldPosition.y + ".");
            }
            return spawned;
        }

        private static GameObject CreateDecorationTemplate(GameObject source)
        {
            if (source == null)
            {
                return null;
            }

            GameObject template = UnityEngine.Object.Instantiate(source);
            template.name = source.name + "__RoomRewindTemplate";
            template.transform.position = new Vector3(-10000f, -10000f, -10000f);
            template.SetActive(false);
            template.hideFlags = HideFlags.HideAndDontSave;
            MinorBreakable minor = template.GetComponentInChildren<MinorBreakable>();
            if (minor != null && StaticReferenceManager.AllMinorBreakables != null)
            {
                StaticReferenceManager.AllMinorBreakables.Remove(minor);
            }
            return template;
        }

        private void DestroyDecorationTemplates()
        {
            foreach (KeyValuePair<RoomHandler, RoomEnemyReplaySnapshot> pair in _snapshots)
            {
                RoomEnemyReplaySnapshot snapshot = pair.Value;
                if (snapshot == null || snapshot.Decorations == null)
                {
                    continue;
                }

                for (int index = 0; index < snapshot.Decorations.Count; index++)
                {
                    GameObject template = snapshot.Decorations[index].Template;
                    if (template != null)
                    {
                        UnityEngine.Object.Destroy(template);
                    }
                }
            }
        }

        private static PrototypePlacedObjectData FindDecorationPrototype(RoomHandler room, IntVector2 worldPosition, RoomDecorationKind kind)
        {
            if (room == null || room.area == null || room.area.prototypeRoom == null)
            {
                return null;
            }

            PrototypeDungeonRoom prototypeRoom = room.area.prototypeRoom;
            PrototypePlacedObjectData result = FindDecorationPrototypeInList(
                prototypeRoom.placedObjects,
                prototypeRoom.placedObjectPositions,
                room.area.basePosition,
                worldPosition,
                kind);
            if (result != null)
            {
                return result;
            }

            if (prototypeRoom.runtimeAdditionalObjectLayers != null)
            {
                for (int layerIndex = 0; layerIndex < prototypeRoom.runtimeAdditionalObjectLayers.Count; layerIndex++)
                {
                    PrototypeRoomObjectLayer layer = prototypeRoom.runtimeAdditionalObjectLayers[layerIndex];
                    if (layer == null || layer.layerIsReinforcementLayer)
                    {
                        continue;
                    }

                    result = FindDecorationPrototypeInList(
                        layer.placedObjects,
                        layer.placedObjectBasePositions,
                        room.area.basePosition,
                        worldPosition,
                        kind);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            if (prototypeRoom.additionalObjectLayers != null)
            {
                for (int layerIndex = 0; layerIndex < prototypeRoom.additionalObjectLayers.Count; layerIndex++)
                {
                    PrototypeRoomObjectLayer layer = prototypeRoom.additionalObjectLayers[layerIndex];
                    if (layer == null || layer.layerIsReinforcementLayer)
                    {
                        continue;
                    }

                    result = FindDecorationPrototypeInList(
                        layer.placedObjects,
                        layer.placedObjectBasePositions,
                        room.area.basePosition,
                        worldPosition,
                        kind);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            // A few predefined-room objects carry pixel offsets or are authored
            // through a layer whose runtime base position differs by one cell.
            // If exact matching fails, use the nearest same-kind prototype within
            // a small room-local radius and still instantiate at the captured cell.
            result = FindNearestDecorationPrototypeInList(
                prototypeRoom.placedObjects,
                prototypeRoom.placedObjectPositions,
                room.area.basePosition,
                worldPosition,
                kind);
            if (result != null)
            {
                return result;
            }

            result = FindNearestDecorationPrototypeInLayers(
                prototypeRoom.runtimeAdditionalObjectLayers,
                room.area.basePosition,
                worldPosition,
                kind);
            if (result != null)
            {
                return result;
            }

            return FindNearestDecorationPrototypeInLayers(
                prototypeRoom.additionalObjectLayers,
                room.area.basePosition,
                worldPosition,
                kind);
        }

        private static PrototypePlacedObjectData FindNearestDecorationPrototypeInLayers(
            List<PrototypeRoomObjectLayer> layers,
            IntVector2 roomBase,
            IntVector2 worldPosition,
            RoomDecorationKind kind)
        {
            if (layers == null)
            {
                return null;
            }

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                PrototypeRoomObjectLayer layer = layers[layerIndex];
                if (layer == null || layer.layerIsReinforcementLayer)
                {
                    continue;
                }

                PrototypePlacedObjectData result = FindNearestDecorationPrototypeInList(
                    layer.placedObjects,
                    layer.placedObjectBasePositions,
                    roomBase,
                    worldPosition,
                    kind);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static PrototypePlacedObjectData FindNearestDecorationPrototypeInList(
            List<PrototypePlacedObjectData> objects,
            List<Vector2> positions,
            IntVector2 roomBase,
            IntVector2 worldPosition,
            RoomDecorationKind kind)
        {
            if (objects == null)
            {
                return null;
            }

            PrototypePlacedObjectData nearest = null;
            int nearestDistance = 3;
            for (int index = 0; index < objects.Count; index++)
            {
                PrototypePlacedObjectData data = objects[index];
                if (data == null || !IsDecorationPrototype(data, kind))
                {
                    continue;
                }

                IntVector2 candidate = positions != null && index < positions.Count
                    ? positions[index].ToIntVector2() + roomBase
                    : data.contentsBasePosition.ToIntVector2() + roomBase;
                int distance = Math.Abs(candidate.x - worldPosition.x) + Math.Abs(candidate.y - worldPosition.y);
                if (distance < nearestDistance)
                {
                    nearest = data;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static PrototypePlacedObjectData FindDecorationPrototypeInList(
            List<PrototypePlacedObjectData> objects,
            List<Vector2> positions,
            IntVector2 roomBase,
            IntVector2 worldPosition,
            RoomDecorationKind kind)
        {
            if (objects == null)
            {
                return null;
            }

            for (int index = 0; index < objects.Count; index++)
            {
                PrototypePlacedObjectData data = objects[index];
                if (data == null || !IsDecorationPrototype(data, kind))
                {
                    continue;
                }

                IntVector2 candidate = positions != null && index < positions.Count
                    ? positions[index].ToIntVector2() + roomBase
                    : data.contentsBasePosition.ToIntVector2() + roomBase;
                if (candidate.x == worldPosition.x && candidate.y == worldPosition.y)
                {
                    return data;
                }
            }

            return null;
        }

        private static bool IsDecorationPrototype(PrototypePlacedObjectData data, RoomDecorationKind kind)
        {
            if (data == null || !string.IsNullOrEmpty(data.enemyBehaviourGuid))
            {
                return false;
            }

            if (data.nonenemyBehaviour != null)
            {
                return ContainsDecorationType(data.nonenemyBehaviour.gameObject, kind);
            }

            if (data.placeableContents == null || data.placeableContents.variantTiers == null)
            {
                return false;
            }

            for (int index = 0; index < data.placeableContents.variantTiers.Count; index++)
            {
                Dungeonator.DungeonPlaceableVariant variant = data.placeableContents.variantTiers[index];
                if (variant != null && variant.nonDatabasePlaceable != null &&
                    ContainsDecorationType(variant.nonDatabasePlaceable, kind))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsDecorationType(GameObject prefab, RoomDecorationKind kind)
        {
            if (prefab == null)
            {
                return false;
            }

            if (kind == RoomDecorationKind.Cover)
            {
                return prefab.GetComponentInChildren<FlippableCover>() != null;
            }
            if (kind == RoomDecorationKind.Major)
            {
                return prefab.GetComponentInChildren<MajorBreakable>() != null &&
                    prefab.GetComponentInChildren<FlippableCover>() == null;
            }
            return prefab.GetComponentInChildren<MinorBreakable>() != null;
        }

        private static void RestoreDecorationState(RoomHandler room, RoomDecorationState state, GameObject root)
        {
            root.SetActive(true);
            if (root.transform.parent == null && room.hierarchyParent != null)
            {
                root.transform.SetParent(room.hierarchyParent, true);
            }

            if (state.Kind == RoomDecorationKind.Grass)
            {
                TallGrassPatch grass = root.GetComponentInChildren<TallGrassPatch>();
                if (grass != null)
                {
                    grass.cells = new List<IntVector2>(state.GrassCells);
                    ClearTallGrassFireState(grass);
                    grass.BuildPatch();
                }
                return;
            }

            if (state.Kind == RoomDecorationKind.Cover)
            {
                FlippableCover cover = root.GetComponentInChildren<FlippableCover>();
                if (cover != null)
                {
                    SetPrivateBool(cover, "m_flipped", state.WasFlipped);
                    SetPrivateEnum(cover, "m_flipDirection", state.FlipDirection);
                    if (!room.IsRegistered(cover))
                    {
                        room.RegisterInteractable(cover);
                    }
                }

                MajorBreakable breakable = root.GetComponentInChildren<MajorBreakable>();
                RestoreMajorBreakable(breakable, state);
                return;
            }

            if (state.Kind == RoomDecorationKind.Major)
            {
                RestoreMajorBreakable(root.GetComponentInChildren<MajorBreakable>(), state);
            }
            else
            {
                MinorBreakable minor = root.GetComponentInChildren<MinorBreakable>();
                if (minor != null)
                {
                    if (!state.WasBroken && minor.IsBroken && state.Template != null)
                    {
                        ReplaceBrokenMinorWithTemplate(room, state, root);
                        return;
                    }
                    SetPrivateBool(minor, "m_isBroken", state.WasBroken);
                    minor.enabled = !state.WasBroken;
                    if (minor.specRigidbody != null)
                    {
                        minor.specRigidbody.enabled = !state.WasBroken;
                        if (!state.WasBroken)
                        {
                            minor.specRigidbody.Reinitialize();
                        }
                    }
                }
            }
        }

        private static void ReplaceBrokenMinorWithTemplate(
            RoomHandler room,
            RoomDecorationState state,
            GameObject brokenRoot)
        {
            // Vanilla MinorBreakable.Break() changes more than IsBroken: it disables
            // the SpeculativeRigidbody and may leave the break animation/sprite active.
            // Replacing the live object from the intact captured template restores all
            // of that native state together, including serialized prefab components.
            if (state == null || state.Template == null || brokenRoot == null)
            {
                return;
            }

            Transform parent = brokenRoot.transform.parent;
            UnityEngine.Object.Destroy(brokenRoot);

            GameObject restored = UnityEngine.Object.Instantiate(state.Template);
            restored.name = state.Template.name.Replace("__RoomRewindTemplate", string.Empty);
            if (parent != null)
            {
                restored.transform.SetParent(parent, true);
            }
            restored.transform.position = state.WorldTransformPosition;
            restored.transform.rotation = state.WorldRotation;
            restored.transform.localScale = state.WorldScale;
            restored.SetActive(true);

            MinorBreakable minor = restored.GetComponentInChildren<MinorBreakable>();
            if (minor != null)
            {
                minor.ConfigureOnPlacement(room);
                if (minor.specRigidbody != null)
                {
                    minor.specRigidbody.enabled = true;
                    minor.specRigidbody.Reinitialize();
                }
            }
        }

        private static void RestoreMajorBreakable(MajorBreakable breakable, RoomDecorationState state)
        {
            if (breakable == null)
            {
                return;
            }

            SetPrivateBool(breakable, "m_isBroken", state.WasBroken);
            SetPrivateBool(breakable, "m_inZeroHPState", false);
            SetPrivateInt(breakable, "m_numHits", state.NumHits);
            breakable.HitPoints = state.HitPoints;
            breakable.enabled = !state.WasBroken;
            if (breakable.specRigidbody != null)
            {
                breakable.specRigidbody.enabled = !state.WasBroken;
                if (!state.WasBroken)
                {
                    breakable.specRigidbody.Reinitialize();
                }
            }
        }

        // This snapshot is intentionally taken at room entry, before the Boss fight.
        // Do not move capture into Refresh: by then the player has already spent health,
        // blanks, ammo, charges, and may have changed inventory during the fight.
        private static PlayerRoomSnapshot CapturePlayerState(PlayerController player)
        {
            if ((object)player == null || (object)player.healthHaver == null || player.stats == null)
            {
                return null;
            }

            PlayerRoomSnapshot snapshot = new PlayerRoomSnapshot();
            snapshot.CurrentHealth = player.healthHaver.GetCurrentHealth();
            snapshot.MaximumHealth = player.healthHaver.GetMaxHealth();
            snapshot.Armor = player.healthHaver.Armor;
            snapshot.Blanks = player.Blanks;
            snapshot.BaseStats = CopyList(player.stats.BaseStatValues);
            snapshot.StatValues = CopyList(GetPrivateList<float>(player.stats, "StatValues"));
            snapshot.PreviouslyActiveSynergies = CopyList(player.stats.PreviouslyActiveSynergies);

            if (player.inventory != null && player.inventory.AllGuns != null)
            {
                snapshot.SelectedGunIndex = player.inventory.AllGuns.IndexOf(player.CurrentGun);
                for (int index = 0; index < player.inventory.AllGuns.Count; index++)
                {
                    Gun gun = player.inventory.AllGuns[index];
                    if ((object)gun != null)
                    {
                        snapshot.Guns.Add(new GunRoomState(gun.PickupObjectId, gun.ammo, GetPrivateFloat(gun, "m_remainingActiveCooldownAmount")));
                    }
                }
            }

            if (player.passiveItems != null)
            {
                for (int index = 0; index < player.passiveItems.Count; index++)
                {
                    PassiveItem item = player.passiveItems[index];
                    if ((object)item != null)
                    {
                        snapshot.PassiveIds.Add(item.PickupObjectId);
                    }
                }
            }

            if (player.activeItems != null)
            {
                snapshot.SelectedActiveIndex = GetPrivateInt(player, "m_selectedItemIndex");
                for (int index = 0; index < player.activeItems.Count; index++)
                {
                    PlayerItem item = player.activeItems[index];
                    if ((object)item != null)
                    {
                        snapshot.ActiveItems.Add(new ActiveRoomState(
                            item.PickupObjectId,
                            item.CurrentRoomCooldown,
                            item.CurrentTimeCooldown,
                            item.CurrentDamageCooldown,
                            GetPrivateFloat(item, "m_activeElapsed"),
                            GetPrivateFloat(item, "m_activeDuration"),
                            GetPrivateBool(item, "m_isCurrentlyActive")));
                    }
                }
            }

            return snapshot;
        }

        // Reuse the live inventory when its structure still matches the entry snapshot;
        // otherwise restore inventory through the normal pickup path, then restore ETG's
        // runtime values. Pickup IDs can be unavailable in a partially loaded scene;
        // those entries are warned and skipped so the remaining snapshot still applies.
        private void RestorePlayerState(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            if ((object)player == null || snapshot == null)
            {
                LogWarning("Room player rewind skipped because the player or snapshot was unavailable. Player=" + ((object)player != null) + ".");
                return;
            }

            Log("Restoring room-entry player state. Before=" + DescribeLivePlayerState(player) + ", Snapshot=" + DescribePlayerState(snapshot) + ".");
            bool restoredInventoryInPlace = TryRestorePlayerInventoryInPlace(player, snapshot);
            if (!restoredInventoryInPlace && player.inventory != null)
            {
                player.inventory.DestroyAllGuns();
            }
            if (!restoredInventoryInPlace)
            {
                player.RemoveAllPassiveItems();
                player.RemoveAllActiveItems();
            }

            if (player.stats != null)
            {
                player.stats.BaseStatValues = CopyList(snapshot.BaseStats);
                player.stats.PreviouslyActiveSynergies = CopyList(snapshot.PreviouslyActiveSynergies);
            }

            if (!restoredInventoryInPlace)
            {
                for (int index = 0; index < snapshot.Guns.Count; index++)
                {
                    GunRoomState savedGun = snapshot.Guns[index];
                    Gun prefab = PickupObjectDatabase.GetById(savedGun.PickupId) as Gun;
                    if ((object)prefab == null || player.inventory == null)
                    {
                        LogWarning("Room player rewind could not restore gun. PickupId=" + savedGun.PickupId + ".");
                        continue;
                    }

                    Gun restoredGun = player.inventory.AddGunToInventory(prefab, index == snapshot.SelectedGunIndex);
                    if ((object)restoredGun != null)
                    {
                        restoredGun.ammo = savedGun.Ammo;
                        SetPrivateFloat(restoredGun, "m_remainingActiveCooldownAmount", savedGun.RemainingActiveCooldownAmount);
                    }
                }

                for (int index = 0; index < snapshot.PassiveIds.Count; index++)
                {
                    PassiveItem prefab = PickupObjectDatabase.GetById(snapshot.PassiveIds[index]) as PassiveItem;
                    if ((object)prefab == null)
                    {
                        LogWarning("Room player rewind could not restore passive. PickupId=" + snapshot.PassiveIds[index] + ".");
                        continue;
                    }

                    player.AcquirePassiveItemPrefabDirectly(prefab);
                }

                for (int index = 0; index < snapshot.ActiveItems.Count; index++)
                {
                    ActiveRoomState savedItem = snapshot.ActiveItems[index];
                    PlayerItem prefab = PickupObjectDatabase.GetById(savedItem.PickupId) as PlayerItem;
                    if ((object)prefab == null)
                    {
                        LogWarning("Room player rewind could not restore active item. PickupId=" + savedItem.PickupId + ".");
                        continue;
                    }

                    EncounterTrackable.SuppressNextNotification = true;
                    prefab.Pickup(player);
                    if (player.activeItems != null && player.activeItems.Count > index)
                    {
                        PlayerItem restoredItem = player.activeItems[index];
                        restoredItem.CurrentRoomCooldown = savedItem.RoomCooldown;
                        restoredItem.CurrentTimeCooldown = savedItem.TimeCooldown;
                        restoredItem.CurrentDamageCooldown = savedItem.DamageCooldown;
                        SetPrivateFloat(restoredItem, "m_activeElapsed", savedItem.ActiveElapsed);
                        SetPrivateFloat(restoredItem, "m_activeDuration", savedItem.ActiveDuration);
                        SetPrivateBool(restoredItem, "m_isCurrentlyActive", savedItem.IsCurrentlyActive);
                    }
                }
            }

            if (player.stats != null)
            {
                player.stats.RecalculateStats(player, true);
                SetPrivateList(player.stats, "StatValues", CopyList(snapshot.StatValues));
            }

            player.Blanks = snapshot.Blanks;
            player.healthHaver.SetHealthMaximum(snapshot.MaximumHealth, null, false);
            player.healthHaver.ForceSetCurrentHealth(snapshot.CurrentHealth);
            player.healthHaver.Armor = snapshot.Armor;
            if (player.inventory != null && snapshot.SelectedGunIndex >= 0 && snapshot.SelectedGunIndex < player.inventory.AllGuns.Count)
            {
                player.ChangeToGunSlot(snapshot.SelectedGunIndex, true);
            }
            SetPrivateInt(player, "m_selectedItemIndex", snapshot.SelectedActiveIndex);
            Log("Restored room-entry player state. InPlaceInventory=" + restoredInventoryInPlace + ", After=" + DescribeLivePlayerState(player) + ".");
        }

        private bool TryRestorePlayerInventoryInPlace(PlayerController player, PlayerRoomSnapshot snapshot)
        {
            if (player.inventory == null || player.inventory.AllGuns == null ||
                player.passiveItems == null || player.activeItems == null ||
                player.inventory.AllGuns.Count != snapshot.Guns.Count ||
                player.passiveItems.Count != snapshot.PassiveIds.Count ||
                player.activeItems.Count != snapshot.ActiveItems.Count)
            {
                return false;
            }

            for (int index = 0; index < snapshot.Guns.Count; index++)
            {
                Gun currentGun = player.inventory.AllGuns[index];
                GunRoomState savedGun = snapshot.Guns[index];
                if ((object)currentGun == null || currentGun.PickupObjectId != savedGun.PickupId)
                {
                    return false;
                }
            }

            for (int index = 0; index < snapshot.PassiveIds.Count; index++)
            {
                PassiveItem currentItem = player.passiveItems[index];
                if ((object)currentItem == null || currentItem.PickupObjectId != snapshot.PassiveIds[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < snapshot.ActiveItems.Count; index++)
            {
                PlayerItem currentItem = player.activeItems[index];
                if ((object)currentItem == null || currentItem.PickupObjectId != snapshot.ActiveItems[index].PickupId)
                {
                    return false;
                }
            }

            for (int index = 0; index < snapshot.Guns.Count; index++)
            {
                Gun currentGun = player.inventory.AllGuns[index];
                GunRoomState savedGun = snapshot.Guns[index];
                currentGun.ammo = savedGun.Ammo;
                SetPrivateFloat(currentGun, "m_remainingActiveCooldownAmount", savedGun.RemainingActiveCooldownAmount);
            }

            for (int index = 0; index < snapshot.ActiveItems.Count; index++)
            {
                PlayerItem currentItem = player.activeItems[index];
                ActiveRoomState savedItem = snapshot.ActiveItems[index];
                currentItem.CurrentRoomCooldown = savedItem.RoomCooldown;
                currentItem.CurrentTimeCooldown = savedItem.TimeCooldown;
                currentItem.CurrentDamageCooldown = savedItem.DamageCooldown;
                SetPrivateFloat(currentItem, "m_activeElapsed", savedItem.ActiveElapsed);
                SetPrivateFloat(currentItem, "m_activeDuration", savedItem.ActiveDuration);
                SetPrivateBool(currentItem, "m_isCurrentlyActive", savedItem.IsCurrentlyActive);
            }

            Log("Used in-place Boss-room inventory restore because the current inventory matches the snapshot.");
            return true;
        }

        private static string DescribePlayerState(PlayerRoomSnapshot snapshot)
        {
            return snapshot == null
                ? "PlayerSnapshot=<none>"
                : "Health=" + snapshot.CurrentHealth + "/" + snapshot.MaximumHealth +
                  ", Armor=" + snapshot.Armor + ", Blanks=" + snapshot.Blanks +
                  ", Guns=" + snapshot.Guns.Count + ", Passives=" + snapshot.PassiveIds.Count +
                  ", Actives=" + snapshot.ActiveItems.Count + ", SelectedGun=" + snapshot.SelectedGunIndex +
                  ", SelectedActive=" + snapshot.SelectedActiveIndex;
        }

        private static string DescribeLivePlayerState(PlayerController player)
        {
            return (object)player == null || (object)player.healthHaver == null
                ? "Player=<none>"
                : "Health=" + player.healthHaver.GetCurrentHealth() + "/" + player.healthHaver.GetMaxHealth() +
                  ", Armor=" + player.healthHaver.Armor + ", Blanks=" + player.Blanks +
                  ", Guns=" + (player.inventory != null && player.inventory.AllGuns != null ? player.inventory.AllGuns.Count : 0) +
                  ", Passives=" + (player.passiveItems != null ? player.passiveItems.Count : 0) +
                  ", Actives=" + (player.activeItems != null ? player.activeItems.Count : 0);
        }

        private static List<T> CopyList<T>(List<T> source)
        {
            return source != null ? new List<T>(source) : new List<T>();
        }

        private static int CountValidObjects<T>(List<T> objects) where T : UnityEngine.Object
        {
            if (objects == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < objects.Count; index++)
            {
                if ((object)objects[index] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountCorpseDebris(List<DebrisObject> debrisObjects)
        {
            if (debrisObjects == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < debrisObjects.Count; index++)
            {
                DebrisObject debris = debrisObjects[index];
                if ((object)debris != null && debris.IsCorpse)
                {
                    count++;
                }
            }

            return count;
        }

        private static readonly BindingFlags InstancePrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static List<T> GetPrivateList<T>(object target, string fieldName)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            return field != null ? field.GetValue(target) as List<T> : null;
        }

        private static void SetPrivateList<T>(object target, string fieldName, List<T> value)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static int GetPrivateInt(object target, string fieldName)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            return field != null ? (int)field.GetValue(target) : -1;
        }

        private static void SetPrivateInt(object target, string fieldName, int value)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static void SetPrivateEnum<T>(object target, string fieldName, T value) where T : struct
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            if (field != null && field.FieldType.IsEnum)
            {
                field.SetValue(target, value);
            }
        }

        private static float GetPrivateFloat(object target, string fieldName)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            return field != null ? (float)field.GetValue(target) : 0f;
        }

        private static void SetPrivateFloat(object target, string fieldName, float value)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private static bool GetPrivateBool(object target, string fieldName)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            return field != null && (bool)field.GetValue(target);
        }

        private static void SetPrivateBool(object target, string fieldName, bool value)
        {
            FieldInfo field = target != null ? target.GetType().GetField(fieldName, InstancePrivateFlags) : null;
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private bool IsReplaying(RoomHandler room)
        {
            return room != null && _replayingRooms.Contains(room);
        }

        private int SpawnWave(RoomHandler room, List<RoomEnemyReplayEntry> wave, out List<RoomEnemyReplayEntry> actualWave)
        {
            actualWave = new List<RoomEnemyReplayEntry>();
            int spawned = 0;
            for (int index = 0; index < wave.Count; index++)
            {
                RoomEnemyReplayEntry entry = wave[index];
                AIActor prefab = EnemyDatabase.GetOrLoadByGuid(entry.EnemyGuid);
                if ((object)prefab == null)
                {
                    LogWarning("Recorded enemy prefab is unavailable. Guid=" + entry.EnemyGuid + ", Room=" + GetRoomLabel(room) + ".");
                    continue;
                }

                // This is the same DungeonPlaceableBehaviour path RoomHandler uses for a
                // direct enemyBehaviourGuid in PlaceObjectsFromLayer. AIActor.Spawn has a
                // different anchor calculation for several enemy prefabs.
                GameObject spawnedObject = prefab.InstantiateObject(room, entry.SpawnPosition - room.area.basePosition);
                AIActor enemy = spawnedObject != null ? spawnedObject.GetComponent<AIActor>() : null;
                if ((object)enemy == null)
                {
                    LogWarning("Recorded enemy spawn returned null. Guid=" + entry.EnemyGuid + ", Room=" + GetRoomLabel(room) + ".");
                    continue;
                }

                enemy.PlacedPosition = entry.SpawnPosition;
                if ((object)enemy.specRigidbody != null)
                {
                    enemy.specRigidbody.Initialize();
                }

                enemy.IgnoreForRoomClear = entry.IgnoreForRoomClear;
                // Vanilla marks room enemies engaged when the player enters their room. A
                // rewind happens after that event, so explicitly complete both parts here.
                // Without this, the AI remains idle until taking damage.
                enemy.HasDonePlayerEnterCheck = true;
                enemy.HasBeenEngaged = true;
            if (enemy.healthHaver != null && enemy.healthHaver.IsBoss)
            {
                RestoreReplayedBossVisibility(enemy);
                BossAudioDiagnosticsHooks.StartReplayedTankTreaderIdle(enemy);
            }
                // Verify the actual world cell so the log compares the location the player sees.
                actualWave.Add(new RoomEnemyReplayEntry(
                    enemy.EnemyGuid,
                    entry.SpawnPosition,
                    enemy.transform.position.IntXY(),
                    enemy.IgnoreForRoomClear));
                Log(
                    "Recorded replay spawn state. ExpectedRoom=" + GetRoomLabel(room) +
                    ", Enemy=" + DescribeActiveEnemy(enemy) +
                    ", ParentRoomMatchesExpected=" + (enemy.ParentRoom == room) +
                    ", SpawnInsideExpectedRoom=" + room.ContainsPosition(enemy.transform.position.IntXY()) + ".");
                spawned++;
            }

            return spawned;
        }

        private void RestoreReplayedBossVisibility(AIActor boss)
        {
            if ((object)boss == null)
            {
                return;
            }

            tk2dSprite[] spritesBefore = boss.GetComponentsInChildren<tk2dSprite>(true);
            int disabledBefore = CountDisabledSprites(spritesBefore);
            boss.State = AIActor.ActorState.Normal;
            boss.invisibleUntilAwaken = false;
            boss.ToggleRenderers(true);
            boss.IsGone = false;
            if (boss.specRigidbody != null)
            {
                boss.specRigidbody.CollideWithOthers = true;
                boss.specRigidbody.Reinitialize();
            }

            if (boss.healthHaver != null)
            {
                boss.healthHaver.IsVulnerable = true;
            }

            // AIActor.ToggleRenderers only searches active child objects. Some paired
            // first-floor Boss prefabs keep a sprite branch inactive after their intro,
            // so enabling the component alone still leaves the Boss visually missing.
            tk2dSprite[] allSprites = boss.GetComponentsInChildren<tk2dSprite>(true);
            int activatedSpriteObjects = 0;
            for (int index = 0; index < allSprites.Length; index++)
            {
                tk2dSprite sprite = allSprites[index];
                if (sprite == null)
                {
                    continue;
                }

                if (!sprite.gameObject.activeSelf)
                {
                    sprite.gameObject.SetActive(true);
                    activatedSpriteObjects++;
                }

                sprite.enabled = true;
            }

            tk2dSprite[] spritesAfter = boss.GetComponentsInChildren<tk2dSprite>(true);
            LogBossSpriteMaterialState(boss, spritesAfter, "AfterSpawn");
            Log(
                "Restored replayed Boss visibility. Enemy=" + boss.EnemyGuid +
                ", State=" + boss.State +
                ", InvisibleUntilAwaken=" + boss.invisibleUntilAwaken +
                ", IsGone=" + boss.IsGone +
                ", RendererEnabled=" + (boss.renderer != null && boss.renderer.enabled) +
                ", Sprites=" + spritesAfter.Length +
                ", ActivatedSpriteObjects=" + activatedSpriteObjects +
                ", DisabledSpritesBefore=" + disabledBefore +
                ", DisabledSpritesAfter=" + CountDisabledSprites(spritesAfter) +
                ", CollideWithOthers=" + (boss.specRigidbody != null && boss.specRigidbody.CollideWithOthers) +
                ", IsVulnerable=" + (boss.healthHaver != null && boss.healthHaver.IsVulnerable) + ".");
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

                    LogBossSpriteMaterialState(
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

        private void LogBossSpriteMaterialState(AIActor boss, tk2dSprite[] sprites, string phase)
        {
            if (boss == null || sprites == null)
            {
                return;
            }

            for (int index = 0; index < sprites.Length; index++)
            {
                tk2dSprite sprite = sprites[index];
                if (sprite == null)
                {
                    continue;
                }

                try
                {
                    tk2dSpriteCollectionData collection = sprite.Collection;
                    tk2dSpriteDefinition definition = sprite.CurrentSprite;
                    Renderer spriteRenderer = sprite.renderer;
                    Material sharedMaterial = spriteRenderer != null ? spriteRenderer.sharedMaterial : null;
                    Texture mainTexture = sharedMaterial != null ? sharedMaterial.mainTexture : null;
                    string shaderName = sharedMaterial != null && sharedMaterial.shader != null
                        ? sharedMaterial.shader.name
                        : "<none>";
                    string animationClip = "<none>";
                    tk2dSpriteAnimator animator = sprite.GetComponent<tk2dSpriteAnimator>();
                    if (animator != null && animator.CurrentClip != null)
                    {
                        animationClip = animator.CurrentClip.name;
                    }

                    LogAlways(
                        "Boss sprite material state. Phase=" + phase + ", Enemy=" + boss.EnemyGuid +
                        ", Index=" + index +
                        ", Object=" + sprite.gameObject.name +
                        ", ActiveSelf=" + sprite.gameObject.activeSelf +
                        ", ActiveInHierarchy=" + sprite.gameObject.activeInHierarchy +
                        ", Enabled=" + sprite.enabled +
                        ", RendererPresent=" + (spriteRenderer != null) +
                        ", RendererEnabled=" + (spriteRenderer != null && spriteRenderer.enabled) +
                        ", SpriteId=" + sprite.spriteId +
                        ", SpriteDefinition=" + (definition != null ? definition.name : "<null>") +
                        ", DefinitionMaterial=" + (definition != null && definition.material != null ? definition.material.name : "<null>") +
                        ", DefinitionMaterialInst=" + (definition != null && definition.materialInst != null ? definition.materialInst.name : "<null>") +
                        ", Collection=" + (collection != null ? collection.name : "<null>") +
                        ", CollectionAsset=" + (collection != null ? collection.assetName : "<null>") +
                        ", CollectionName=" + (collection != null ? collection.spriteCollectionName : "<null>") +
                        ", CollectionDefinitions=" + (collection != null && collection.spriteDefinitions != null ? collection.spriteDefinitions.Length.ToString() : "-1") +
                        ", CollectionMaterials=" + (collection != null && collection.materials != null ? collection.materials.Length.ToString() : "-1") +
                        ", CollectionMaterialInsts=" + (collection != null && collection.materialInsts != null ? collection.materialInsts.Length.ToString() : "-1") +
                        ", CollectionTextures=" + (collection != null && collection.textures != null ? collection.textures.Length.ToString() : "-1") +
                        ", SharedMaterial=" + (sharedMaterial != null ? sharedMaterial.name : "<null>") +
                        ", Shader=" + shaderName +
                        ", MainTexture=" + (mainTexture != null ? mainTexture.name : "<null>") +
                        ", AnimationClip=" + animationClip + ".");
                }
                catch (Exception exception)
                {
                    LogAlways(
                        "Boss sprite material state failed. Phase=" + phase + ", Enemy=" + boss.EnemyGuid +
                        ", Index=" + index +
                        ", Object=" + sprite.gameObject.name +
                        ", Exception=" + exception.GetType().Name + ":" + exception.Message + ".");
                }
            }
        }

        private static int CountDisabledSprites(tk2dSprite[] sprites)
        {
            if (sprites == null)
            {
                return 0;
            }

            int disabled = 0;
            for (int index = 0; index < sprites.Length; index++)
            {
                if (sprites[index] != null && !sprites[index].enabled)
                {
                    disabled++;
                }
            }

            return disabled;
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
            bool matches = WavesMatch(expectedWave, actualWave);
            string message =
                "Room enemy replay verification. Room=" + GetRoomLabel(room) +
                ", Wave=" + waveIndex +
                ", Match=" + matches +
                ", Expected=[" + DescribeWave(expectedWave) + "]" +
                ", Actual=[" + DescribeWave(actualWave) + "].";
            if (matches)
            {
                Log(message);
            }
            else
            {
                LogWarning(message);
            }
        }

        private static bool WavesMatch(List<RoomEnemyReplayEntry> expectedWave, List<RoomEnemyReplayEntry> actualWave)
        {
            if (expectedWave == null || actualWave == null || expectedWave.Count != actualWave.Count)
            {
                return false;
            }

            for (int index = 0; index < expectedWave.Count; index++)
            {
                RoomEnemyReplayEntry expected = expectedWave[index];
                RoomEnemyReplayEntry actual = actualWave[index];
                if (!string.Equals(expected.EnemyGuid, actual.EnemyGuid, StringComparison.Ordinal) ||
                    expected.WorldPosition != actual.WorldPosition ||
                    expected.IgnoreForRoomClear != actual.IgnoreForRoomClear)
                {
                    return false;
                }
            }

            return true;
        }

        private static string DescribeWave(List<RoomEnemyReplayEntry> wave)
        {
            if (wave == null || wave.Count == 0)
            {
                return string.Empty;
            }

            List<string> entries = new List<string>();
            for (int index = 0; index < wave.Count; index++)
            {
                RoomEnemyReplayEntry entry = wave[index];
                entries.Add(
                    entry.EnemyGuid +
                    " Spawn@" + entry.SpawnPosition.x + "," + entry.SpawnPosition.y +
                    " World@" + entry.WorldPosition.x + "," + entry.WorldPosition.y +
                    ":IgnoreForRoomClear=" + entry.IgnoreForRoomClear);
            }

            return string.Join(";", entries.ToArray());
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

        private static bool SnapshotContainsEnemies(RoomEnemyReplaySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            for (int waveIndex = 0; waveIndex < snapshot.Waves.Count; waveIndex++)
            {
                List<RoomEnemyReplayEntry> wave = snapshot.Waves[waveIndex];
                if (wave != null && wave.Count > 0)
                {
                    return true;
                }
            }

            return false;
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

        private sealed class RoomEnemyReplaySnapshot
        {
            public readonly List<List<RoomEnemyReplayEntry>> Waves = new List<List<RoomEnemyReplayEntry>>();
            public List<RoomDecorationState> Decorations;
            public int NextWaveIndex;
            public PlayerRoomSnapshot Player;
            public bool PlayerHasTakenDamageInThisRoom;
            public bool HasGivenMasteryToken;
        }

        private enum RoomDecorationKind
        {
            Cover,
            Major,
            Minor,
            Grass,
            BreakableObject,
            BreakableSprite
        }

        private sealed class RoomDecorationState
        {
            private RoomDecorationState(RoomDecorationKind kind, GameObject root)
            {
                Kind = kind;
                WorldPosition = root.transform.position.IntXY();
                WorldTransformPosition = root.transform.position;
                WorldRotation = root.transform.rotation;
                WorldScale = root.transform.localScale;
                Prototype = null;
            }

            public RoomDecorationKind Kind;
            public IntVector2 WorldPosition;
            public PrototypePlacedObjectData Prototype;
            public GameObject Template;
            public Vector3 WorldTransformPosition;
            public Quaternion WorldRotation;
            public Vector3 WorldScale;
            public bool WasBroken;
            public bool WasFlipped;
            public DungeonData.Direction FlipDirection;
            public int NumHits;
            public float HitPoints;
            public readonly List<IntVector2> GrassCells = new List<IntVector2>();

            public static RoomDecorationState ForCover(FlippableCover cover, MajorBreakable breakable)
            {
                RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Cover, cover.gameObject);
                state.WasFlipped = cover.IsFlipped;
                state.FlipDirection = cover.DirectionFlipped;
                state.WasBroken = cover.IsBroken;
                if (breakable != null)
                {
                    state.NumHits = breakable.NumHits;
                    state.HitPoints = breakable.HitPoints;
                }
                return state;
            }

            public static RoomDecorationState ForMajor(MajorBreakable major)
            {
                RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Major, major.gameObject);
                state.WasBroken = major.IsDestroyed;
                state.NumHits = major.NumHits;
                state.HitPoints = major.HitPoints;
                return state;
            }

            public static RoomDecorationState ForMinor(MinorBreakable minor)
            {
                RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Minor, minor.gameObject);
                state.WasBroken = minor.IsBroken;
                return state;
            }

            public static RoomDecorationState ForGrass(TallGrassPatch grass)
            {
                RoomDecorationState state = new RoomDecorationState(RoomDecorationKind.Grass, grass.gameObject);
                if (grass.cells != null)
                {
                    state.GrassCells.AddRange(grass.cells);
                    if (state.GrassCells.Count > 0)
                    {
                        state.WorldPosition = state.GrassCells[0];
                    }
                }
                return state;
            }

            public static RoomDecorationState ForTemplate(RoomDecorationKind kind, GameObject root)
            {
                return new RoomDecorationState(kind, root);
            }
        }

        private sealed class PlayerRoomSnapshot
        {
            public float CurrentHealth;
            public float MaximumHealth;
            public float Armor;
            public int Blanks;
            public int SelectedGunIndex = -1;
            public int SelectedActiveIndex = -1;
            public List<float> BaseStats = new List<float>();
            public List<float> StatValues = new List<float>();
            public List<int> PreviouslyActiveSynergies = new List<int>();
            public readonly List<GunRoomState> Guns = new List<GunRoomState>();
            public readonly List<int> PassiveIds = new List<int>();
            public readonly List<ActiveRoomState> ActiveItems = new List<ActiveRoomState>();
        }

        private sealed class GunRoomState
        {
            public GunRoomState(int pickupId, int ammo, float remainingActiveCooldownAmount)
            {
                PickupId = pickupId;
                Ammo = ammo;
                RemainingActiveCooldownAmount = remainingActiveCooldownAmount;
            }

            public int PickupId;
            public int Ammo;
            public float RemainingActiveCooldownAmount;
        }

        private sealed class ActiveRoomState
        {
            public ActiveRoomState(int pickupId, int roomCooldown, float timeCooldown, float damageCooldown, float activeElapsed, float activeDuration, bool isCurrentlyActive)
            {
                PickupId = pickupId;
                RoomCooldown = roomCooldown;
                TimeCooldown = timeCooldown;
                DamageCooldown = damageCooldown;
                ActiveElapsed = activeElapsed;
                ActiveDuration = activeDuration;
                IsCurrentlyActive = isCurrentlyActive;
            }

            public int PickupId;
            public int RoomCooldown;
            public float TimeCooldown;
            public float DamageCooldown;
            public float ActiveElapsed;
            public float ActiveDuration;
            public bool IsCurrentlyActive;
        }

        private sealed class RoomEnemyReplayEntry
        {
            public RoomEnemyReplayEntry(
                string enemyGuid,
                IntVector2 spawnPosition,
                IntVector2 worldPosition,
                bool ignoreForRoomClear)
            {
                EnemyGuid = enemyGuid;
                SpawnPosition = spawnPosition;
                WorldPosition = worldPosition;
                IgnoreForRoomClear = ignoreForRoomClear;
            }

            public string EnemyGuid { get; private set; }
            public IntVector2 SpawnPosition { get; private set; }
            public IntVector2 WorldPosition { get; private set; }
            public bool IgnoreForRoomClear { get; private set; }
        }
    }
}
