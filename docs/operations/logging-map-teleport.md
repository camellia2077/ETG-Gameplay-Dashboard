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

When enabled, the plugin emits high-frequency `[RandomLoadout][Command]` lines for:

- map reveal request entry
- room-scan progress
- minimap teleporter registration state
- direct teleporter-promotion attempts
- room-transition observation while promotion is active
- runtime sampling after reveal

Typical useful lines include:

- `Reveal map button pressed`
- `Map reveal requested`
- `Map reveal room scan`
- `Map direct teleport room promotion applied`
- `Map reveal completed`
- `Map direct teleport activated`
- `Map direct teleport room transition`
- `Map direct teleport runtime sample`

## What Still Logs When Disabled

When `EnableMapTeleportVerboseLogs = false`, the high-frequency room-scan and runtime-sample lines stay silent.

Failure-oriented warnings still remain visible, such as:

- `Map reveal unavailable`
- `Map reveal failed`
- coroutine exceptions or ETG-side runtime warnings

## Typical Workflow

1. Enter a normal dungeon floor.
2. Press `Reveal Map`.
3. Check whether known teleporter rooms become usable without natural discovery.
4. If behavior differs from expectation, compare `CanTeleportFromRoom`, `CanTeleportToRoom`, `TeleportersActive`, and `Minimap.RoomToTeleportMap` registration in the log.

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Runtime behavior reference:
  [../reference/map-teleport.md](../reference/map-teleport.md)
