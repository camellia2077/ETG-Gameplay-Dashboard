# Map Reveal And Teleporter Promotion

Use this page when you need to understand how the `Reveal Map` button works, why it does not behave exactly like natural ETG room discovery, and which runtime APIs currently drive the combined map-reveal and teleporter-promotion feature.

## User-Facing Behavior

The command panel currently exposes one related action on the `General` page:

- `Reveal Map`
  Reveals the current floor on the minimap and also promotes already-registered teleporter rooms toward a usable teleport state on the current floor.

The current implementation targets the practical gameplay result, not the exact vanilla visual state. It does not fully recreate every visual or state transition that vanilla ETG performs when the player naturally discovers a teleporter room.

## Code Ownership

Start here:

- `src/RandomLoadout/Commands/InGameCommandController.CommandPage.cs`
  Button placement, enabled-state color, and controller focus entries.
- `src/RandomLoadout/Commands/InGameCommandController.CommandActions.cs`
  Button execution and status messaging.
- `src/RandomLoadout/Commands/RoomDebugCommandService.cs`
  Runtime map reveal, teleporter promotion, and command logging.
- `src/RandomLoadout/Commands/InGameCommandController.cs`
  Runtime sampling logs and room-transition observation while teleporter promotion is active.

## Implementation Summary

### Reveal Map

`Reveal Map` currently does this in `RoomDebugCommandService.RevealCurrentFloorMap(...)`:

1. Validates `GameManager`, `Dungeon`, room list, and `Minimap.Instance`.
2. Sets `player.EverHadMap = true`.
3. Iterates `GameManager.Instance.Dungeon.data.rooms`.
4. Sets `room.RevealedOnMap = true` for rooms that were still hidden.
5. Calls `Minimap.RevealAllRooms(true)`.
6. Calls `Minimap.RevealMinimapRoom(player.CurrentRoom, true, true, true)` for the active room.

This is enough to push more of the floor into the minimap state, but it is not a full substitute for vanilla room-entry flow.

### Teleporter Promotion

`Reveal Map` also promotes rooms that are already part of the minimap teleporter system.

For rooms that are already present in `Minimap.RoomToTeleportMap`, the service currently tries to push them closer to a usable teleport state by applying:

- `room.hasEverBeenVisited = true`
- `room.forceTeleportersActive = true`
- `minimap.RevealMinimapRoom(room, true, true, false)`
- `gameManager.StartCoroutine(room.DeferredMarkVisibleRoomsActive(player))`

The service also preserves the older fallback call path for rooms that are already considered teleportable by ETG:

- `room.AddProceduralTeleporterToRoom()`

In practice, the successful path discovered during debugging was not "make every room teleportable", but "promote rooms that ETG already registered as teleporter-bearing rooms".

## Why This Exists

During runtime debugging we observed:

- natural teleporter rooms can already appear in `Minimap.RoomToTeleportMap` before they become usable
- entering such a room can change `CanTeleportFromRoom()` and `CanTeleportToRoom()` from `false` to `true` after a short delay
- simply setting `RevealedOnMap` is not enough to reproduce the vanilla teleporter-ready state
- `AddProceduralTeleporterToRoom()` alone was not sufficient for rooms that ETG still considered not teleportable

That led to the current promotion-based implementation.

## Important Limits

- The feature currently targets rooms that ETG already treats as teleporter-capable.
- It does not guarantee that every room on the floor becomes a valid teleport destination.
- It does not fully mimic the exact minimap visuals or discovery state that vanilla ETG applies after the player physically enters a room.
- Runtime state can still differ from the natural room-entry path even when teleport becomes usable.

## Logging And Verification

The feature keeps its detailed runtime diagnostics, but they are disabled by default to avoid flooding normal play logs.

Feature-specific logging guide:

- [Map Reveal Logging](../operations/logging-map-teleport.md)

## Related ETG APIs

The current implementation relies on public members that are already available through the referenced ETG assemblies:

- `Minimap.Instance`
- `Minimap.RevealAllRooms(bool revealSecretRooms)`
- `Minimap.RevealMinimapRoom(...)`
- `Minimap.RoomToTeleportMap`
- `RoomHandler.RevealedOnMap`
- `RoomHandler.TeleportersActive`
- `RoomHandler.CanTeleportFromRoom()`
- `RoomHandler.CanTeleportToRoom()`
- `RoomHandler.AddProceduralTeleporterToRoom()`
- `RoomHandler.DeferredMarkVisibleRoomsActive(PlayerController p)`
- `RoomHandler.hasEverBeenVisited`
- `RoomHandler.forceTeleportersActive`
- `PlayerController.EverHadMap`

## Change Guidance

If you change this feature:

1. Keep the runtime logs until the replacement behavior has been verified in-game.
2. Prefer extending ETG's own teleporter state path over replacing the teleport interaction with a fully custom map-click warp.
3. Re-test both:
   - using `Reveal Map` immediately from the entrance room
   - entering a known teleporter room after `Reveal Map`
4. Re-check `BepInEx` logs after each runtime change.
