# Command-Panel Shortcut Logging

Use this diagnostic when the Control Panel does not open from its keyboard or controller shortcut, especially in
two-player mode.

## Enable

The diagnostic switch is temporarily enabled for the current reproduction:

```ini
[Debug]
EnableCommandPanelShortcutVerboseLogs = true
```

Set it to `false` after collecting the log.

## What It Captures

Each `[EtgGameplayDashboard][Command] [Input] Command panel shortcut` entry records the configured keyboard key, keyboard
held/down state, controller shortcut detection, controller shortcut enabled state, panel visibility, game type, and
P1/P2 object readiness and input-override state. Accepted keyboard/controller toggle events are also logged separately.

## Reproduction

1. Enter a two-player run and leave the Control Panel closed.
2. Press the configured keyboard shortcut once.
3. Close the panel if it opens, then try the configured controller shortcut once.
4. Send the contiguous `Command panel shortcut` lines from `BepInEx\LogOutput.log`.

Extract them with:

```powershell
python .\tools\logs\read_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --pattern "Command panel shortcut" --tail 300
```
