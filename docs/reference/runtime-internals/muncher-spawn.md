# Muncher Spawn

Use this page when you need to understand or extend the custom room-tool actions that spawn `Gunber Muncher（常规吃枪怪）` and `Evil Muncher（邪恶吃枪怪）`.

This feature is intentionally documented as a non-standard ETG runtime integration. The working solution does not come from a clean public API surface. It depends on traced vanilla assets, decompiled runtime behavior, and a spawn path that preserves room-owned ETG setup.

## Why This Exists

The command panel now exposes two room actions:

- `Spawn Gunber Muncher`
- `Spawn Evil Muncher`

These actions do not create a vanilla muncher room. They spawn the vanilla muncher actor directly into the current room near the player.

That distinction matters:

- fast iteration was the goal for v1
- room injection and full room replacement are much higher risk
- plain `Instantiate(prefab)` was not reliable enough for ETG-owned setup

## Player-Facing Scope

Current behavior:

- works from the `Room` submenu in the command panel
- spawns into the current room instead of building the original shop room
- supports both `Gunber Muncher` and `Evil Muncher`
- retries automatically if the request is made while the game is still in `LoadingDungeon`

Not included in this v1:

- spawning the original muncher shop room layout
- recreating vanilla room injection tables
- guaranteeing every edge-case interaction that depends on original room flow

## Owning Code

Primary files:

- `src/RandomLoadout/Commands/InGameCommandController.CommandPage.cs`
- `src/RandomLoadout/Commands/RoomDebugCommandService.cs`
- `defaults/config/ETG-Gameplay-Dashboard.localization.en.json5`
- `defaults/config/ETG-Gameplay-Dashboard.localization.zh-CN.json5`

Key entry points in `RoomDebugCommandService`:

- `SpawnGunberMuncher(...)`
- `SpawnEvilMuncher(...)`
- `SpawnGunberMuncherNow(...)`
- `SpawnEvilMuncherNow(...)`
- `ResolveMuncherBehaviourFromOriginalRoomAsset(...)`
- `ResolveMuncherPrefabFromAssetBundle(...)`

## Implementation Summary

The stable path is:

1. Check whether the scene is still `LoadingDungeon`.
2. If loading is still in progress, queue a coroutine retry.
3. Resolve the original room asset that contains the placed muncher object.
4. Extract the room-owned `DungeonPlaceableBehaviour` from that room asset.
5. Spawn through `DungeonPlaceableBehaviour.InstantiateObject(...)`.
6. Log a post-spawn snapshot so runtime failures can be triaged from `BepInEx\LogOutput.log`.

The important implementation detail is step 4 and step 5.

The original placed object already carries ETG-side setup that a bare prefab spawn does not reliably reproduce. The working solution therefore prefers the original room's placed non-enemy object instead of treating the muncher as a normal standalone prefab-only spawn.

## Why Plain Prefab Spawn Was Not Enough

Early attempts resolved the prefab and called `Object.Instantiate(...)` near the player. That route was not stable enough for this feature.

Observed problems during investigation:

- green success message but no visible muncher in-room
- prefab resolution succeeded but runtime state was incomplete
- scene timing mattered during floor transitions
- Evil Muncher failed completely when resolved from the wrong asset bundle

The current implementation still keeps prefab resolution as a diagnostic and fallback path, but the preferred route is:

`PrototypeDungeonRoom -> placed object -> DungeonPlaceableBehaviour -> InstantiateObject`

That better matches how the game expects the object to enter the room.

## Asset Chain We Had To Trace

These values were verified from extracted assets and decompiled data, not guessed.

### Gunber Muncher

- room asset path:
  `Assets/data/rooms/shop rooms/SubShop_Muncher_01.asset`
- room asset bundle:
  `shared_auto_002`
- prefab asset path:
  `Assets/data/prefabs/npcs/NPC_GunberMuncher.prefab`
- prefab asset bundle:
  `shared_auto_002`

### Evil Muncher

- room asset path:
  `Assets/data/rooms/shop rooms/SubShop_EvilMuncher_01.asset`
- room asset bundle:
  `shared_auto_001`
- prefab asset path:
  `Assets/data/prefabs/npcs/NPC_GunberMuncher_Evil.prefab`
- prefab asset bundle:
  `shared_auto_001`

The bundle split is the most important pitfall from this feature.

The Evil variant does not live beside `Gunber Muncher` in `shared_auto_002`. If code assumes both variants come from the same bundle, Evil resolution fails with `PrototypeRoomResolved=False` and `PrefabResolved=False`.

## Decompiled / Extracted Clues Worth Keeping

These notes are worth preserving because they save future reverse-engineering time:

- `SubShop_Muncher_01` and `SubShop_EvilMuncher_01` are the room assets that contain the placed muncher non-enemy object.
- `NPC_GunberMuncher` and `NPC_GunberMuncher_Evil` are valid prefab-level names, but prefab-only spawning is not the preferred route.
- `NPC_GunberMuncher_Evil` still uses `GunberMuncherController`; Evil behavior is not implemented as a completely separate controller type.
- the evil variant behavior appears to be tied to runtime state such as `RequiredNumberOfGuns > 2` and evil-specific reward fields, not a separate controller class.
- the first practical implementation did not need to recreate vanilla room injection tables.

## Tools Used During Research

The current documented investigation path used:

- `ilspycmd`
- `AssetRipper`

They serve different purposes:

- `ilspycmd` is good for decompiling runtime code and confirming controller behavior
- `AssetRipper` is good for verifying asset names, bundle ownership, prefab paths, and room asset paths

For this feature, `AssetRipper` was the decisive tool for finding the real bundle and asset path chain.

## Scene Timing And Deferred Spawn

Calling the action during a floor transition can happen while the current scene still reports `LoadingDungeon`.

Because of that, the service now:

- checks the current normalized scene name
- queues one pending retry per muncher type
- polls until a real gameplay scene and current room are available
- then performs the actual spawn

This avoided a class of false failures where the request itself was valid, but the room was not ready yet.

## Logging Expectations

All muncher diagnostics write through `[RandomLoadout][Command]`.

The detailed trace is now optional and disabled by default.

Feature-specific logging guide:

- [Muncher Spawn Logging](../../operations/logging-muncher-spawn.md)

## Why This Is A Non-Standard API Surface

This repository is not using a stable public helper like `SpawnMuncherInCurrentRoom(...)` because ETG does not expose one in a clean reusable form for this exact use case.

The feature is effectively built by combining:

- vanilla asset-bundle lookup
- traced room asset ownership
- extracted placed-object data
- ETG runtime object instantiation behavior
- custom scene-readiness retry logic

That is why this page intentionally keeps the reverse-engineered details.

Without those details, future contributors are very likely to repeat the same dead ends:

- loading the wrong bundle for Evil
- assuming prefab instantiate is enough
- trying to recreate the full vanilla room flow before validating a direct spawn

## Extension Guidance

If you need to extend this feature:

- prefer tracing the original room asset first
- verify bundle ownership before coding
- keep the room-owned `DungeonPlaceableBehaviour` route when possible
- add logs before changing spawn semantics
- treat full room injection as a separate feature, not a small follow-up edit

If you later add more special NPC or room objects, follow the same research order:

1. find the room asset
2. find the prefab asset
3. confirm the bundle for each
4. decompile the controlling runtime component
5. only then decide whether prefab spawn or room-placed-object spawn is the safer route

## Read Next

- [Commands](../commands.md)
- [Code Index](../code-index.md)
- [Logging](../../operations/logging.md)
- [Runtime Hotspots](../../architecture/runtime-hotspots.md)
