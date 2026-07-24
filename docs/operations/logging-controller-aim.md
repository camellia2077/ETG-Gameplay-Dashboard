# Controller Aim / Cursor Logging

Use this diagnostic to capture why the player's view follows the mouse cursor or the controller's right stick.
It samples `PlayerController.DetermineAimPointInWorld()` and `CameraController.GetCoreOffset()` every 10 frames and
records the raw world-space aim point, the distance from the player to that point, the controller aim vector, whether
the player aim point was overridden, and whether camera aim look was suppressed.

The switch is disabled by default:

```ini
[Debug]
EnableControllerAimVerboseLogs = false
```

## Reproduction

1. Close Enter the Gungeon.
2. Open `C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\config\ETG-Gameplay-Dashboard.cfg`.
3. Under `[Debug]`, set `EnableControllerAimVerboseLogs = true`.
4. Start the game, enter a normal dungeon room, and do not open the control panel.
5. With keyboard and mouse, place the mouse near the player, then far from the player, and move it around the player.
6. Restart or switch to a controller, then repeat with the right stick at small and large angles.
7. Close the game and restore `EnableControllerAimVerboseLogs = false`.

Extract the relevant lines with:

```powershell
python .\tools\logs\read_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --pattern "\[EtgGameplayDashboard\]\[Aim\]" --tail 300
```

You can also save them to a file:

```powershell
python .\tools\logs\read_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --pattern "\[EtgGameplayDashboard\]\[Aim\]" --tail 300 -o .\controller-aim.log
```

Please send `controller-aim.log`. The important fields are `DeviceMode`, `RawAimDistance`, `AimVector`,
`RawAimPoint`, `UnadjustedAimPoint`, `AimPointOverrideApplied`, `LockActive`, `PreventAimLookBefore`, and
`PreventAimLookAfter`. Do not enable unrelated verbose switches.
