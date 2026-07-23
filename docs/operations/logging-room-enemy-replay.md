# Room Enemy Replay Logging

Use this page when diagnosing `Room -> Enemies -> Rewind Room`.

## Enable

Set this in `BepInEx\config\randomgun.randomloadout.cfg`:

```ini
[Debug]
EnableRoomEnemyReplayVerboseLogs = true
```

Default: `false`.

## Implementation Model

There are two deliberately different actions:

- `Rewind Room` is the exact-mode path. It records the result chosen by vanilla during a standard or Boss room's first entry: each enemy GUID, replay anchor, `IgnoreForRoomClear` flag, initial wave, and every later vanilla reinforcement wave. It does not reroll the room template.
- `Respawn Enemies` is the legacy/template path for standard combat rooms. In a Boss room, the command automatically falls back to the exact Boss rewind path, regardless of the selected mode, because template spawning cannot restore Boss reward and intro state safely.

The replay lifecycle is:

1. A Harmony prefix on `RoomHandler.OnEntered` captures the vanilla initial wave after the room becomes active.
2. Prefix/postfix hooks around `RoomHandler.TriggerReinforcementLayer` compare the active-enemy sets and append only newly added actors as the next recorded wave.
3. After a cleared room is rewound, replay spawns the saved initial wave, seals the room, and marks it as replaying.
4. A prefix on `RoomHandler.DeregisterEnemy` inserts the next saved wave before vanilla removes the final room-clear enemy. This preserves wave order and prevents ETG from declaring the room clear between waves.
5. The first frame of `GameManager.IsLoadingLevel` clears the old floor's snapshots before unload. The completed floor-load callback leaves the service intact so `RoomHandler.OnEntered` can record the new floor normally. Snapshots are keyed by live `RoomHandler` objects and are valid only for the current floor.

Replay uses the enemy prefab's `DungeonPlaceableBehaviour.InstantiateObject(room, localCell)` path instead of `AIActor.Spawn`. That is the same placement family ETG uses for an explicit `enemyBehaviourGuid` and preserves placement anchors for enemy prefabs that use a different `AIActor.Spawn` anchor calculation.

For Boss rooms, replay does not start the Boss's native `GenericIntroDoer` a second time because Boss-specific intro coroutines are not reliably repeat-safe. The replay spawn explicitly marks the actor as engaged. A rewind is also rejected until vanilla's original `HandleBossClearReward` callback has completed; this closes the short post-kill window in which the Boss is gone but reward and door state are still settling. Before the replayed Boss is killed, rewind re-arms `RoomHandler.m_hasGivenReward`; this allows the vanilla `OnEnemiesCleared -> HandleRoomClearReward` chain to generate the normal Boss reward pedestal again. When `RoomRewindCleanupEnabled` is on (the default), earlier reward pedestals, currency, scene drops, room-local `DecalObject` floor decals, corpse roots, corpse debris, corpse controllers, and standalone death/floor VFX are removed before replay. Player, gun, and pickup-owned persistent VFX are preserved. The same cleanup applies to standard rooms, except standard rooms have no Boss reward pedestal.

## What It Captures

`Enemy Refresh Recording` is off by default and is persisted as `RoomEnemyRefreshRecordingEnabled`. When turned on, the feature records the enemies actually selected by vanilla on a standard or Boss room's first entry on the current floor. It records the initial wave and each vanilla reinforcement wave, then `Rewind Room` restores those saved GUIDs and positions after a room clear. Boss replays trigger the boss's native intro sequence, remove the previous reward objects, and re-arm vanilla's clear-reward state so the replayed Boss can generate a new normal reward. Turning recording off clears all saved snapshots immediately. The current floor's snapshots are cleared when the next floor enters its unload transition.

The selected room refresh mode is persisted as `RoomEnemyRefreshMethod` (`rewind` or `respawn`). The selected mode applies to standard combat rooms only; Boss rooms always use rewind. The rewind shortcut key remains separately configurable as `RoomEnemyRewindKey`; the in-panel execute button does not persist a pressed state.

`Player Rewind` is a separate persistent toggle in the Room panel and defaults to off (`PlayerRewindEnabled`). When enabled before entering a tracked room, the room-entry snapshot records the player's health, armor, blanks, stats, guns/ammo, passives, actives, selected slots, and charge state. Rewinding the room restores that player snapshot.

`Room Rewind Cleanup` is a separate persistent toggle in the Room panel and defaults to on. It controls removal of room-local decals, scene drops, currency, Boss reward pedestals, corpse visuals, and standalone death/floor VFX immediately before either rewind mode respawns enemies. See [Boss Room Rewind](../architecture/boss-room-rewind.md) for the object-source and player-VFX ownership rules.

When enabled, `[RandomLoadout][Command]` logs include:

- initial-wave recording (always logged);
- skipped room-entry captures and their reason (`RecordingDisabled`, `RoomNotTrackable`, or `SnapshotAlreadyExists`) (always logged);
- room-entry snapshot details, including `CurrentFloor`, `RoomId`, active enemy count, player snapshot capture status, and total snapshot count (always logged);
- replayed Boss visibility state, including AI state, `InvisibleUntilAwaken`, `IsGone`, renderer/sprite status, collision, and vulnerability;
- stale corpse cleanup entries are skipped when room-position resolution is unavailable, so cleanup continues instead of aborting replay;
- rewind request state and snapshot lookup status, including `CurrentFloor`, `RoomId`, `RecordingEnabled`, `SnapshotCount`, and whether the current player room matches;
- each vanilla reinforcement wave recorded;
- replay start and the initial-wave spawn count;
- each replayed later-wave spawn count;
- a per-wave `Room enemy replay verification` line containing both the recorded and the spawned GUID/position lists plus `Match=True` or `Match=False`;
- replay completion.
- snapshot cleanup when the old floor enters its unload transition, including the cleanup reason and count.
- startup configuration confirmation, including the effective verbose value, bound section/key, and the active BepInEx config path.
- rewind/respawn request validation and final result are always logged. Rewind request receipt, snapshot match, replay start, floor snapshot cleanup, and Boss clear-reward completion are also always logged; detailed wave, object, player, and timing diagnostics remain controlled by `EnableRoomEnemyReplayVerboseLogs`.
- Player snapshot capture and restore summaries, including health/max health, armor, blanks, gun/passive/active counts, selected slots, and the restore-before/after summaries. Missing pickup IDs are logged as warnings.
- Player restore reports `InPlaceInventory=True` when the live inventory matched the snapshot and was restored without clearing and re-picking up every item.
- each Rewind Room or Respawn Enemies request's room category, Boss-room flag, player cell, raw-room and interior-room membership, exit-cell state, explicit rejection reason, and final result key. This identifies Boss/non-standard-room, corridor/stale-room guards, and no-enemy outcomes.
- each replayed enemy's expected room, `ParentRoom`, placement cell, and world cell; plus the active-enemy list before each enemy is deregistered and at replay completion. This identifies foreign-room spawns and stale active enemies that keep doors sealed.
- Boss-intro skip decision and active Boss count, plus whether vanilla Boss-reward suppression is active for the replay.
- Boss-room destructible capture and restore summaries, including the number of `FlippableCover` tables, `MajorBreakable` objects, `MinorBreakable` objects, and `TallGrassPatch` objects captured, plus restored, respawned, missing, and failed counts. A per-object respawn line includes the object kind and world cell.
- Boss-room destructible restore uses one current-room lookup keyed by decoration kind and world position; the lookup is built once before restoring the captured decoration list.
- Circular Boss-room grass and explosive barrels are both logged under `MinorBreakable`. In the Gatling Gull room, circular grass appears as `Captured Boss-room named minor ... Name=default bush`; the capture summary's `Minors` count includes these bushes and the explosive barrels.
- `Boss-room grass scan` reports `VisibleComponents`, `GlobalRegistry`, and `RoomGrass` for the separate native `TallGrassPatch` path. It can legitimately report zero in the Gatling Gull room because its circular bushes are `MinorBreakable` objects instead.
- When an existing captured minor was broken, rewind restores it from its intact template so the log's `Restored` count can include a full visual/collision replacement, not only a flag reset. `Respawned` counts only objects that had no current instance at lookup time.
- Rewind cleanup scan and result counts for room-local projectiles, `EphemeralObject` instances, registered corpses, corpse debris/controllers, persistent VFX, reward pedestals, and total removed objects. Persistent VFX are removed only when they are not owned by a player, gun, pickup, or room-destructible hierarchy; this preserves table/building child VFX during Boss rewind.
- Boss rewind phase timing, when enabled: `CleanupMs`, `DecorationRestoreMs`, `SpawnWaveMs`, `PlayerRestoreMs`, `BossIntroMs`, and `TotalMs`, together with the spawned enemy and captured decoration counts.

For Boss-room stutter diagnosis, look for:

```text
Boss rewind timing. ... CleanupMs=..., DecorationRestoreMs=..., SpawnWaveMs=..., PlayerRestoreMs=..., BossIntroMs=..., TotalMs=...
```

The values are measured in milliseconds on the game thread. `CleanupMs` covers room-local drops, corpses, VFX, decals, and Boss reward cleanup. `DecorationRestoreMs` covers Boss-room destructible lookup, replacement, and state restoration. `SpawnWaveMs` covers replay enemy instantiation. `PlayerRestoreMs` covers the optional Boss player snapshot restore. `BossIntroMs` covers room sealing and the native Boss intro trigger. The timing line is emitted only while `EnableRoomEnemyReplayVerboseLogs` is enabled.

## Recorded optimization evidence

The current Boss rewind implementation has two performance optimizations documented in [Boss Room Rewind](../architecture/boss-room-rewind.md):

- Destructible restoration uses one room scan plus a type-and-world-cell lookup instead of rescanning the room for every captured decoration.
- Matching player inventories use in-place runtime restoration instead of clearing and re-granting all guns, passives, and active items.

Observed log values from `GatlingGullRoom01`:

```text
Before lookup optimization:        DecorationRestoreMs=2941.74, TotalMs=3070.26
After lookup optimization:         DecorationRestoreMs=77.62,   TotalMs=216.71
After in-place inventory restore:  PlayerRestoreMs=5.11,        TotalMs=163.13, InPlaceInventory=True
Second in-place capture:           PlayerRestoreMs=6.77,        TotalMs=276.61, InPlaceInventory=True
```

The second in-place capture had a higher total because `DecorationRestoreMs` rose to `187.31ms`; this is room-state variation and does not indicate that the inventory fast path failed.

Warnings remain enabled even when verbose logging is off for missing snapshots, unavailable enemy prefabs, and failed recorded spawns.

## Debugging History And Guards

The following checks are intentional; do not remove them as simplifications without reproducing the associated ETG behavior.

| Observed failure | Cause | Current guard or fix |
| --- | --- | --- |
| Rewind differed in enemy type, count, position, or reinforcement order. | Re-reading a room template reruns random variants and does not reproduce the original vanilla selection. | Record the actual initial and reinforcement waves, then replay their GUIDs and anchors. |
| A room remained sealed with an enemy marker in another room after many clears. | Some enemies report a default `PlacedPosition` of `(0,0)` even though their world cell is valid. Replaying that anchor creates a room-owned enemy outside the dungeon. | Use `PlacedPosition` only when it is inside the expected room; otherwise use the actual world cell. Skip the entry if neither position is valid. Log expected room, parent room, placement, and world cells. |
| Replayed enemies appeared offset or in a different tile. | `AIActor.Spawn` has different placement-anchor behavior for some prefab types. | Exact replay uses `InstantiateObject` with a room-relative anchor; the template mode remains intentionally separate and may differ. |
| Replayed enemies stood idle until damaged. | Vanilla's player-enter event had already occurred before the replay spawn. | Mark each replayed actor `HasDonePlayerEnterCheck` and `HasBeenEngaged` after instantiation. |
| Doors opened after the first replay wave even though more waves were recorded. | If the final enemy is deregistered first, vanilla immediately considers the room clear. | Spawn the next saved wave in the `DeregisterEnemy` prefix, before vanilla's clear check. |
| Using rewind in an A/B-room corridor sealed B while the player was stranded in the corridor. | `Player.CurrentRoom` may already select a neighboring room while the player's cell is still in a connecting exit/corridor. | Require the player cell to be in `CellsWithoutExits`; reject corridor, exit-cell, stale-room, and non-combat requests with a yellow warning before spawning or sealing. |
| Boss replay broke Boss presentation or did not produce a reward. | A Boss needs its native intro flow, and `m_hasGivenReward` remains true after the original clear. | Allow exact replay for Boss rooms only, call the native intro after spawn, remove old reward objects, and re-arm `m_hasGivenReward` so vanilla `HandleRoomClearReward` runs after the replayed clear. Template respawn remains standard-room only. |

## Limits

- The room must be entered after the plugin has loaded; rooms entered earlier in the same run have no snapshot.
- Exact rewind restores the room-entry player's recorded health, armor, blanks, stats, gun inventory and ammo, passive/active items, selected slots, and active-item cooldown/charge state. It does not restore other players in co-op unless they were the player recorded by the room-entry hook.
- Standard rooms and Boss rooms are tracked. Tutorial and other special rooms are deliberately excluded.
- The feature replays original room enemies and reinforcement waves. Enemies dynamically summoned during combat are not part of the recorded formation.

## Read Next

- [Logging overview](./logging.md)
- [Commands](../reference/commands.md)
- [Boss Room Rewind](../architecture/boss-room-rewind.md)
