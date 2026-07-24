# Boss Room Rewind

This document describes the current Boss-room rewind contract for future runtime work. The implementation is shared with standard-room enemy replay, but Boss rooms additionally restore player state and restart the native Boss reward flow.

## Entry points

The main implementation is `src/EtgGameplayDashboard/Runtime/RoomEnemyReplayService.cs`.

- `RoomEnemyReplayHooks.OnEnteredPrefix` records the first active enemy wave. The real vanilla parameter name is `p`; Harmony binds ordinary patch arguments by name, so this must not be renamed to `player`.
- `TriggerReinforcementLayerPrefix/Postfix` records later vanilla reinforcement waves.
- `DeregisterEnemyPrefix` inserts the next recorded wave before vanilla sees the room as clear.
- `RoomDebugCommandService.RefreshCurrentRoomEnemies` validates and requests rewind from the command panel or shortcut.
- `InGameCommandController` owns the selected `Rewind`/`Respawn` mode and the execute button. The selected mode applies only to standard combat rooms; a Boss-room execution always routes to `RoomDebugCommandService.RefreshCurrentRoomEnemies` so Boss replay cannot enter the template respawn path.

## Configuration and activation

The persistent settings are stored by BepInEx in `BepInEx/config/randomgun.etg-gameplay-dashboard.cfg`:

```ini
[UI]
RoomEnemyRewindKey = C
RoomEnemyRefreshRecordingEnabled = false
RoomEnemyRefreshMethod = rewind
PlayerRewindEnabled = false
RoomRewindCleanupEnabled = true
```

Recording is intentionally off by default. When it is enabled, only rooms entered after activation are recorded; this avoids work and startup/room-entry cost before the user opts in. The execute button itself has no persistent pressed state.

`PlayerRewindEnabled` restores the player snapshot for every tracked standard or Boss room. `RoomRewindCleanupEnabled` controls the visual/drop cleanup for both standard and Boss rooms.

### Design rationale: Keybinding selection (`C` Key)

The default keybinding for room rewind is set to `C` (`KeyCode.C`) based on combat ergonomics:
- **Vanilla Keymap Safety**: `C` is not used by any default player action in Enter the Gungeon's standard keyboard layout.
- **Ergonomics & Hand Positioning**: For mouse and keyboard players, the left hand rests naturally on the WASD movement keys on the left side of the keyboard while the right hand remains on the mouse.
- **Zero Hand-Movement During Rewind**: `C` can be conveniently reached by the left thumb or index finger without lifting or repositioning either hand away from WASD or mouse controls. Binding to a key on the right side of the keyboard would require moving a hand to press the shortcut and then moving back, creating awkward delays when instantly re-engaging combat.


## Boss-room timeline

1. On first entry, the service records the vanilla-selected enemy wave. If Player Rewind is enabled, it captures the player snapshot at the same entry point.
2. The service also records Boss-room destructibles at entry: `FlippableCover` tables, standalone `MajorBreakable` objects, `MinorBreakable` objects such as explosive barrels and the circular `default bush` grass, and `TallGrassPatch` cell lists from `StaticReferenceManager.AllGrasses`, including their world-cell anchor and break/flip/fire state.
3. The player can fight and clear the Boss room normally. Vanilla may create reward pedestals, currency, item pickups, floor decals, corpse sprites, death VFX, and projectile objects.
4. A valid rewind request requires a cleared tracked room, a non-empty snapshot, and (for Boss rooms) completion of vanilla's clear-reward callback. The service also rejects a request while a Boss `HealthHaver` is dead but still registered in the room: vanilla only calls `DeathAnimationComplete`, then `FinalizeDeath`, `OnDeath`, and finally `RoomHandler.DeregisterEnemy` after the death animation finishes. These gates prevent a rapid post-kill rewind from racing Boss death animation, reward generation, and door animation. The service cleans room-local replay artifacts before restoring the saved Boss-room destructibles and spawning anything.
5. Existing destructible instances are reactivated and their runtime state is restored. The replay builds one current-room lookup keyed by decoration kind and world position before walking the snapshot, so each captured object does not rescan the room or the global breakable registries. `MinorBreakable` objects are special: vanilla `Break()` sets the private `m_isBroken` flag, disables the `SpeculativeRigidbody`, and can leave the break-animation sprite in a changed state. Therefore every captured minor stores an inactive intact runtime template. If a captured minor is still present but broken, rewind replaces it with that template and reinitializes its room placement/collision; if it was destroyed, the same template or matching predefined-room `PrototypePlacedObjectData` is instantiated at the captured world position. Restoration is best-effort and reports missing prototype entries in the feature log.

## Grass and explosive barrel restoration

The circular grass in the Gatling Gull Boss room is not `TallGrassPatch`. The game creates each visible patch as a `MinorBreakable` named `default bush`; explosive barrels are also `MinorBreakable` instances. Both are therefore captured by `GetRoomMinorBreakables`, which combines the native room component scan with `StaticReferenceManager.AllMinorBreakables` and filters by the room's absolute cell.

At room entry, each minor records its world cell, transform, broken state, prototype information, and an intact hidden clone of the original GameObject. On rewind:

- a missing object is instantiated from its captured template and configured with `ConfigureOnPlacement(room)`, followed by `SpeculativeRigidbody.Reinitialize()`;
- an object that still exists but was hit is detected through `MinorBreakable.IsBroken`, destroyed, and replaced with the intact template;
- the replacement is positioned with the captured transform and re-enabled, so the original sprite/animation, private broken flag, and collision state are restored together;
- `TallGrassPatch`, when present in other rooms, remains handled separately by restoring its captured `cells`, clearing its private fire data, and calling the native `BuildPatch()` method.

This distinction is important: setting only `m_isBroken = false` is insufficient because vanilla `MinorBreakable.Break()` also disables the rigidbody and changes or removes the visible break state.
6. The recorded enemy wave is instantiated using the exact recorded enemy GUIDs and room-relative placement anchors. Boss replay does not call the native `GenericIntroDoer` a second time: Boss-specific intro coroutines such as `BashelliskIntroDoer.PlayerWalkedIn` are not repeat-safe. Instead, replay explicitly restores the Boss to `AIActor.ActorState.Normal`, disables `invisibleUntilAwaken`, enables child sprites, clears `IsGone`, restores collision, and makes the Boss vulnerable.
7. If enabled, the player snapshot is restored. The restore includes health/max-health state, armor, blanks, stats, guns and ammo, passive items, active items, selected gun/active slots, and active-item charge/cooldown state.
8. The replay restores the Boss-room entry values of `RoomHandler.PlayerHasTakenDamageInThisRoom` and `Dungeon.HasGivenMasteryToken`, then resets `RoomHandler.m_hasGivenReward` through the existing reflection helper. This lets vanilla's `OnEnemiesCleared -> HandleRoomClearReward` path create the normal Boss reward, including the floor's Master Round when the replay is flawless, after the replayed Boss dies.
9. Reinforcement waves are inserted from the saved wave list before the final room-clear enemy is deregistered, preserving vanilla wave order and door sealing.

## Player snapshot details

The snapshot is captured for every tracked room when `PlayerRewindEnabled` is already on before entering that room. It is not captured retroactively when the toggle is enabled after entry.

The snapshot contains:

- current and maximum health;
- armor and blanks;
- player stats;
- all guns, their pickup IDs, ammo, and the selected gun index;
- passive and active pickup IDs;
- selected active index;
- active-item charge/cooldown state.

Restoration reuses the live inventory in place when gun, passive, and active pickup IDs and ordering still match the captured snapshot; this restores ammo, cooldowns, and selected slots without triggering a full pickup rebuild. If the inventory structure differs, it uses the existing ETG pickup-grant path for inventory differences and then restores numeric/runtime state. Missing pickup IDs are logged and skipped rather than aborting the entire room replay.

## Visual and drop cleanup

`ClearRoomRewindObjects` is deliberately room-scoped. It does not change the recorded enemy formation or snapshots in other rooms.

Cleanup sources:

| Source | Purpose | Handling |
| --- | --- | --- |
| `Projectile` | bullets and projectile visuals | `OnDespawned`, then `SpawnManager.Despawn` |
| `DecalObject` | floor decals/impact marks | destruction trigger |
| `PickupObject` with `DebrisObject` or `CurrencyPickup` | item/currency drops | destroy scene drop |
| Boss `RewardPedestal` | previous Boss reward pedestal | destroy before replay |
| `StaticReferenceManager.AllCorpses` | vanilla corpse roots | room-filtered, hide immediately, then despawn and unregister |
| room `DebrisObject.IsCorpse` | corpse prefabs not retained in `AllCorpses` | hide immediately, then despawn and unregister |
| room `CorpseSpawnController` | corpse prefabs with controller-driven fallen-body visuals | hide immediately, then despawn |
| room `PersistentVFXBehaviour` | standalone death/floor VFX | hide/despawn unless owned by a player, gun, pickup, or room-destructible hierarchy |

The order matters. Corpse debris must be handled before the generic `EphemeralObject.TriggerDestruction(true)` path, because that path starts a fade/pitfall lifecycle and can leave a corpse sprite visible while replay spawning is already underway. The `removedObjects` set prevents double-despawn when the same object appears in multiple ETG component lists.

Persistent VFX are not globally deleted. Before removing one, the cleanup checks whether the object belongs to a `PlayerController`, `Gun`, `PickupObject`, `FlippableCover`, `TallGrassPatch`, `MajorBreakable`, `MinorBreakable`, `BreakableColumn`, `BreakableObject`, or `BreakableSprite` hierarchy. This guard exists because player/weapon rendering effects and table/building/grass child VFX can also be represented by `PersistentVFXBehaviour`; broad deletion previously hid player visuals or removed the room decoration before restoration.

## Diagnostics

Enable:

```ini
[Debug]
EnableRoomEnemyReplayVerboseLogs = true
```

The most useful lines are:

```text
Rewind cleanup scan. ... CorpsesFound=..., CorpseDebrisFound=..., CorpseControllersFound=...
Cleared rewind-room objects before replay. ... RemovedCorpses=..., RemovedRoomPersistentVfx=..., PersistentVfxSkipped=...
Room enemy replay verification. ... Match=True
Restored room-entry player state. ...
Re-armed Boss-room clear reward for replay. ...
```

Interpretation:

- `Match=True` verifies recorded enemy GUIDs and placement cells, not visual state.
- `RemovedCorpses` should increase when a defeated enemy left a corpse in the room.
- `RemovedRoomPersistentVfx` reports standalone death/floor VFX removed by the current cleanup pass.
- `PersistentVfxSkipped` should mostly be player/gun/pickup-owned effects that were intentionally preserved.
- `Boss sprite material state` reports each replayed Boss sprite's collection, sprite definition, material, shader, main texture, renderer state, and current animation clip. It is sampled immediately after spawn and again after 1, 5, and 30 frames, so it can distinguish a missing material/texture from a disabled or inactive sprite object and from a later animation/update mutation.

## Performance optimizations

Boss rewind currently includes two targeted performance optimizations:

1. Boss-room destructible restoration builds one current-room lookup keyed by decoration kind and world position before walking the snapshot. Each captured decoration then uses a dictionary lookup instead of rescanning the room and the global `MinorBreakable`/grass registries.
2. Boss player restoration uses an in-place path when the current gun, passive, and active pickup IDs and ordering match the entry snapshot. The path updates ammo, cooldowns, selected slots, health, armor, blanks, and runtime stat values without destroying and re-picking up the player's inventory. The full pickup-based restoration remains the fallback when the inventory structure differs.

The measured evidence came from `BepInEx/LogOutput.log` with `EnableRoomEnemyReplayVerboseLogs = true` in `GatlingGullRoom01`:

| Capture | CleanupMs | DecorationRestoreMs | PlayerRestoreMs | TotalMs |
| --- | ---: | ---: | ---: | ---: |
| Before decoration lookup optimization | 71.93 | 2941.74 | 47.86 | 3070.26 |
| After decoration lookup optimization | 72.93 | 77.62 | 58.68 | 216.71 |
| After in-place inventory restoration | 66.09 | 81.36 | 5.11 | 163.13 |
| Second in-place inventory capture | 74.86 | 187.31 | 6.77 | 276.61 |

The first optimization reduced the dominant decoration phase from roughly 2.9 seconds to tens of milliseconds. The second reduced player restoration from roughly 55 milliseconds to roughly 5–7 milliseconds, including cases where gun ammo changed. Total time still varies with room-local VFX, drops, and decoration replacements, so the total values are evidence from individual captures rather than a fixed benchmark.

## Known boundaries

- Snapshots are keyed by live `RoomHandler` instances and cleared when `GameManager.IsLoadingLevel` first becomes true for the next floor, while the old floor is entering its unload transition. The new-floor-loaded callback does not clear snapshots, so rooms entered on the new floor can be recorded normally.
- The room must be entered after recording was enabled; enabling it after entry cannot reconstruct the earlier player or enemy snapshot.
- Dynamically summoned enemies are not guaranteed to be part of the initial recorded formation unless they are observed as a vanilla reinforcement wave.
- Co-op restoration is limited to the player passed through the room-entry hook; the feature does not restore an independent second-player snapshot.
- Cleanup is visual/object cleanup. It does not rewind arbitrary global ETG systems or unrelated rooms.
