# Floor Teleport Logging

Use this page when you need the detailed runtime diagnostics for the control-panel `Teleport` flow, especially the foyer bootstrap and deferred floor-load handoff.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableFloorTeleportVerboseLogs = true
```

Default:

- `false`

## What It Adds

When enabled, the plugin emits detailed teleport diagnostics for:

- floor token resolution
- foyer departure before floor load
- deferred teleport staging through the first run floor
- bootstrap floor arming and re-arming
- deferred readiness reset and ready-frame counting
- final deferred teleport execution
- direct `LoadCustomLevel(...)` issue points

Typical useful lines include:

- `Teleport requested`
- `Teleport resolve`
- `Teleport deferred through first run floor`
- `Deferred teleport staged from foyer`
- `Deferred teleport armed after entering bootstrap floor`
- `Deferred teleport ready check ...`
- `Executing deferred teleport`
- `LoadCustomLevel issued`

## What Still Logs When Disabled

When `EnableFloorTeleportVerboseLogs = false`, the trace-style teleport info lines stay silent.

Failure-oriented warnings still remain visible, such as:

- missing floor definition
- foyer bootstrap failure
- teleport exception during load
- other explicit teleport-unavailable warnings

Normal command success/failure messages also still remain visible.

## Typical Workflow

1. Turn on `EnableFloorTeleportVerboseLogs`.
2. Open the control panel and trigger a `Teleport` action.
3. If starting from the Breach, verify the bootstrap step into the first run floor.
4. Read `BepInEx\LogOutput.log`.
5. Check:
   - whether the token resolved to the expected load scene
   - whether the teleport was forced into deferred mode from foyer
   - whether readiness frames were accumulating or resetting
   - whether the final `LoadCustomLevel(...)` call was actually reached

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Command reference:
  [../reference/commands.md](../reference/commands.md)
