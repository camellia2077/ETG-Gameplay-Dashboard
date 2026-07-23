# Command-Panel Cursor Logging

Use this page when controller use around the command panel appears to break normal mouse behavior, especially when the in-game cursor disappears, flickers, or does not reliably return after switching back from a controller.

This guide is specifically for command-panel cursor and input-device handoff diagnostics.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableCommandPanelCursorVerboseLogs = true
```

This switch is temporarily enabled for the current two-player input-handoff reproduction. Set it to `false` after
collecting the log.

## What It Adds

When enabled, the plugin emits detailed cursor-handoff diagnostics for:

- Unity cursor visibility changes
- Unity cursor lock-mode changes
- active input-device switches between controller and mouse / keyboard
- P1/P2 player and input-object state, including active device and mouse capability
- actual cursor tint and whether the custom cursor color path is active
- mouse left-click and right-click attempts while the command panel is open

Typical useful lines include:

- `Observed cursor visibility state change`
- `Observed active input device change`
- `Observed mouse button press`

The diagnostic also emits an immediate `Unity cursor state changed` line whenever `Cursor.visible` or
`Cursor.lockState` changes. This is not sampled, so it can capture a brief cursor-hide transition that may be
missed by the regular render-order samples.

## What Still Logs When Disabled

When `EnableCommandPanelCursorVerboseLogs = false`, the cursor and input-device trace lines stay silent.

Normal command-panel success, failure, and other unrelated debug logs still behave as before.

## Typical Workflow

1. Turn on `EnableCommandPanelCursorVerboseLogs`.
2. Open the control panel with a controller.
3. Navigate with the controller until the problem state appears.
4. Switch back to the mouse and try to interact again.
5. Read `BepInEx\LogOutput.log`.
6. Check:
   - whether `DeviceName` changed from controller to mouse / keyboard as expected
   - whether `UnityOSCursorVisible` changed unexpectedly during or after panel interaction
   - whether `CursorLockMode` changed at the same time as the cursor disappearance
   - whether mouse-click attempts were still being observed while the cursor looked missing

## Interpreting Common Patterns

- If device-change logs appear but cursor-visibility logs do not:
  the handoff is reaching the input layer, but Unity cursor state may not be updating with it.

- If cursor-visibility logs repeatedly flip between `true` and `false` after controller input:
  some runtime path is continuously reclaiming cursor state after the panel hands control back.

- If mouse-click logs continue while the cursor looks missing:
  input is still reaching the panel, so the issue is probably visual cursor state rather than full mouse loss.

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Command reference:
  [../reference/commands.md](../reference/commands.md)
