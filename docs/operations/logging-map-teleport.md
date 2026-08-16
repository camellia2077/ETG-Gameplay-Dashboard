# Map Reveal Logging

Use this page when you need the detailed runtime diagnostics for `Reveal Map` and teleporter promotion.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableMapTeleportVerboseLogs = true
```

Default:

- `false`

## What It Adds

When enabled, the plugin emits high-frequency `[EtgGameplayDashboard][Command]` lines for:

- map reveal request entry
- room-scan progress
- minimap teleporter registration state
- direct teleporter-promotion attempts
- room-transition observation while promotion is active
- runtime sampling after reveal
- floor-transition timeline around automatic Reveal Map, including frame number, scene names, player position, current room, Dungeon/Minimap readiness, and teleporter state

Typical useful lines include:

- `Reveal map button pressed`
- `Map reveal requested`
- `Map reveal room scan`
- `Map direct teleport room promotion applied`
- `Map reveal completed`
- `Map direct teleport activated`
- `Map direct teleport room transition`
- `Map direct teleport runtime sample`
- `Map reveal transition diagnostic`
- `Map reveal teleport lifecycle` (records map state before General -> Teleport, after teleport succeeds, and after teleport cleanup)
- `Room map teleport eligibility` (always logged at rewind setup, Boss/room clear-reward completion, and replay completion; includes `CanTeleportFromRoom`, `IsSealed`, and active-enemy counts)
- `Floor map teleporter state` (always logged before replay snapshots are cleared and after a new floor finishes loading; includes room counts, minimap registration count, active teleporter count, revealed-room count, and per-room registration/activation state)
- `Minimap teleport attempt` (always logged from the game's private map-click entry; includes the selected target room, target eligibility, icon registration/activation, global teleport prevention, and whether the game accepted the attempt)
- `AfterBossEnemyDeregistered` (always logged after each Boss is removed; useful for paired Boss rooms because it records the active-enemy and sealed-door state immediately after removal)

## What Still Logs When Disabled

When `EnableMapTeleportVerboseLogs = false`, the high-frequency room-scan and runtime-sample lines stay silent.

The `Room map teleport eligibility` lines are emitted independently of that verbose switch so rewind-related teleport failures can still be diagnosed.

The `Floor map teleporter state` lines are also emitted independently of that switch. Compare `BeforeReplaySnapshotClear` with `AfterNewLevelFullyLoaded` to determine whether a floor transition removed teleporter room state, minimap registrations, or only the room-level eligibility flags.

If `Minimap teleport attempt` is absent, the mouse/controller release did not reach the game's teleport attempt method or no map interaction was active. If it is present with `TargetRoom=<null>`, the icon was not selected. If it has a target but `TargetRegistered=False`, `IconPresent=False`, or `IconActiveInHierarchy=False`, the Minimap registration/icon is the failure point.

Failure-oriented warnings still remain visible, such as:

- `Map reveal unavailable`
- `Map reveal failed`
- coroutine exceptions or ETG-side runtime warnings

## Typical Workflow

1. Set `EnableMapTeleportVerboseLogs = true` and restart the game.
2. Set `Settings -> Display -> Reveal Map Mode` to `Current Floor`.
3. In the foyer, turn `Reveal Map` ON and confirm the foyer map is revealed.
4. Open `General -> Teleport` and select the first floor.
5. Reproduce the failure once, then stop; do not repeat the action while collecting the same log.
6. Capture the lines `Map reveal teleport lifecycle` for `teleport_requested`, `teleport_succeeded_before_map_state_clear`, and `teleport_succeeded_after_map_state_clear`.
7. Also capture the surrounding `Map reveal transition diagnostic` lines, especially `floor_scene_changed_before_reset`, `auto_reveal_before_request`, and `auto_reveal_completed`.
8. Pay particular attention to whether `PendingDungeonReveal` changes during teleport, whether the destination scene becomes a dungeon scene, and whether `DungeonReady`/`MinimapHasInstance` become true before the automatic reveal runs.

Automatic Reveal Map waits for a non-null `CurrentRoom` to remain stable for 10 frames and for vanilla player control to be restored (`PlayerInputOverridden=false`, `PlayerInputState=AllInput`) before running. `CurrentRoom` can become valid while the elevator arrival sequence is still holding the player in `NoInput`, so the input state is part of the readiness gate.

The transition diagnostic also performs a sampled scan of active scene components whose object or type names contain `Elevator`, `Stair`, or `Entrance`. PlayMaker FSM components are read through reflection so the diagnostic does not add a compile-time PlayMaker dependency. This scan is diagnostic-only and does not enable, disable, or modify the discovered objects.

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Runtime behavior reference:
  [../reference/map-teleport.md](../reference/map-teleport.md)
