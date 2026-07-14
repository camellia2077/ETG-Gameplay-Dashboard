# Command-Panel Controller Gameplay Input Logging

Use this diagnostic when left-stick gameplay movement stops working while the RandomLoadout Control Panel is open.

## Enable

Edit `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableCommandPanelControllerGameplayInputVerboseLogs = true
```

The switch is disabled by default. It logs only when the panel, device, input state, or tracked axes change.

## Reproduce

1. Start with the panel closed and move the controller's left stick in several directions.
2. Open the command panel without changing the active input device.
3. Move the left stick again, then close the panel and repeat once more.
4. Disable the switch after collecting the log.

Relevant lines are prefixed with `[RandomLoadout][Command] [Input]` and contain `Device`, `DPad`, `LeftStick`,
`RightStick`, `IsInputOverridden`, and `CurrentInputState`.

## Interpretation

- If `LeftStick` changes while `PanelVisible=true`, the controller input reaches the plugin. Compare the panel-open
  `IsInputOverridden` and `CurrentInputState` values with the closed-panel values.
- If `LeftStick` stays at `0.00,0.00` while the physical stick is moving, the active device is not exposing the stick
  through the input path being sampled, or another device has become active.
- If the stick is non-zero while the panel is open but the player remains in `NoInput` with
  `IsInputOverridden=true`, the input is being captured by the panel override. The panel should now keep
  `IsInputOverridden=false` and preserve the normal gameplay input state while it reads D-pad navigation directly.

Extract plugin lines with:

```powershell
python .\tools\logs\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" -o ".\controller-gameplay-input.log"
```
