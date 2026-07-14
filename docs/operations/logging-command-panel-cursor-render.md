# Command-Panel Cursor Render Logging

Use this diagnostic when the ETG mouse cursor appears underneath the RandomLoadout Control Panel. Controller cursor
behavior is intentionally left to ETG because its R3 aiming marker is not a free-screen mouse cursor.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableCommandPanelCursorRenderVerboseLogs = true
```

The switch is disabled by default. It samples `Repaint` events every 30 frames and does not change cursor or
panel rendering behavior.

For the controlled layering probe, enable this additional switch:

```ini
EnableCommandPanelCursorRenderProbe = true
```

The probe keeps ETG's original cursor and draws a white copy at the exact same position after the Control Panel.
It is intentionally temporary and should be disabled after the test.

## What It Captures

The log records sampled ordering markers for:

- `GameCursorController.OnGUI.prefix`
- `GameCursorController.OnGUI.postfix`
- `Plugin.OnGUI.begin`
- `Plugin.OnGUI.after_command_panel`
- `Plugin.OnGUI.end`

Each line includes the frame number, IMGUI event type, `GUI.depth`, cursor visibility/lock state, screen size,
and mouse position. The Harmony prefix and postfix are diagnostic hooks only; they do not suppress or redraw the cursor.
When the probe is enabled, sampled `Cursor render probe drawn after Control Panel` lines confirm that the copy draw path ran.

## Reproduction

1. Enable the switch and restart the game.
2. Reproduce once with the Control Panel closed.
3. Open the Control Panel and move the mouse outside the panel.
4. Move the mouse over the panel and leave it there for several seconds.
5. Switch to at least one other Control Panel page, then close the panel.
6. Send the extracted `[RandomLoadout][CursorRender]` lines from `BepInEx\LogOutput.log`.

Useful extraction command:

```powershell
python .\tools\logs\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" -o ".\cursor-render.log"
```

The important detail is the order of lines with the same `Frame=` value. That tells us whether ETG's cursor
`OnGUI` pass occurs before or after the Control Panel's `OnGUI` pass.
