# Command-Panel Health Logging

Use this page when the control panel appears to replay the HUD "heart gained" or "armor gained" animation after moving focus, even though the visible values do not change.

This guide is specifically for the command-panel `Player -> Pickups` and `General -> Pickups` health and armor flows.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableCommandPanelHealthVerboseLogs = true
```

Default:

- `false`

## What It Adds

When enabled, the plugin emits detailed command-panel health diagnostics for:

- `+1 Max HP` and `+1 Armor` command execution snapshots
- command-panel input override apply / clear lifecycle
- tracked health-change callbacks
- max-health rollback detection
- rollback restoration
- focus-move snapshots that include current health, max health, armor, and blanks
- per-frame state-change snapshots that correlate health/armor changes with current-gun instance changes, panel page, and focus

Typical useful lines include:

- `Executing add max health command`
- `Finished add max health command`
- `Executing add armor command`
- `Finished add armor command`
- `Applied command panel input override`
- `Cleared command panel input override`
- `Observed tracked health changed callback`
- `Detected unexpected max-health rollback`
- `Restored tracked health override`
- `Preserved tracked max health during gun change`
- `Command page controller navigation moved focus`
- `Currency page controller navigation moved focus`
- `Observed player health state change`

## What Still Logs When Disabled

When `EnableCommandPanelHealthVerboseLogs = false`, the high-volume health-diagnostic trace lines stay silent.

Normal command success and failure messages still remain visible, such as:

- `Granted +1 max health`
- `Granted +1 armor`
- other normal control-panel command results

## Why This Logging Exists

ETG can rebuild parts of the player runtime state while switching guns.

If command-panel navigation input leaks into gameplay input, a left/right or D-pad press can move panel focus and also switch the current gun. That gun switch can temporarily rebuild the player's max-health state from the gun-side runtime context and roll it back to the vanilla baseline. Our health override then restores the intended value, which makes the HUD replay the "heart gained" or "armor gained" animation even though the final values settle back to the same numbers.

The command panel now applies a temporary input override while it is open to stop menu navigation from also reaching gameplay gun-switch input. This logging exists to confirm that flow when debugging.

## Typical Workflow

1. Turn on `EnableCommandPanelHealthVerboseLogs`.
2. Open the control panel.
3. Add max health or armor once or more.
4. Move selection around the command panel without intentionally changing health or armor.
5. Read `BepInEx\LogOutput.log`.
6. Check:
   - whether `Applied command panel input override` appeared when the panel opened
   - whether focus movement produced only navigation logs or also a health callback
   - whether a gun change happened near the same time as the replayed animation
   - whether `Detected unexpected max-health rollback` appeared
   - whether the rollback was immediately followed by `Restored tracked health override`
   - whether `Observed player health state change` shows `GunChanged=true` together with a health, max-health, or armor change

## Interpreting Common Patterns

- If focus movement logs appear and no rollback logs follow:
  the panel input isolation is probably working, and the repeated HUD animation is coming from another source.

- If focus movement is followed by health-changed callbacks with a lower `MaxValue`, then `Detected unexpected max-health rollback`, then `Restored tracked health override`:
  menu navigation is still causing a gameplay-side state rebuild, usually through leaked gun-switch input.

- If `Applied command panel input override` never appears while the panel is open:
  the panel-side input isolation did not arm correctly, so investigate command-panel open / close lifecycle first.

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Command reference:
  [../reference/commands.md](../reference/commands.md)
