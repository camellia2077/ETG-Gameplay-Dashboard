# Command-Panel Gameplay Input Logging

Use this diagnostic when gameplay keyboard movement, especially WASD movement, stops working while the RandomLoadout
Control Panel is open.

## Enable

Edit `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableCommandPanelGameplayInputVerboseLogs = true
```

The switch is disabled by default. It logs only when one of the tracked values changes, rather than logging every frame.

## Reproduce

1. Start with the panel closed and press or hold each of `W`, `A`, `S`, and `D` once.
2. Open the command panel without changing the active input device.
3. Press or hold each of `W`, `A`, `S`, and `D` again.
4. Close the panel and repeat one movement key to verify that gameplay input resumes.
5. Disable the switch after collecting the log.

The relevant lines are prefixed with `[RandomLoadout][Command] [Input]` and contain:

- `PanelVisible`: whether the command panel is open;
- `W`, `A`, `S`, `D`: the raw Unity key states observed by the plugin;
- `IsInputOverridden`: whether the player currently has an input override;
- `CurrentInputState`: the ETG player input state;
- `CurrentFocus`: the current Unity GUI keyboard-control ID.

## Interpretation

- If WASD changes to `true` while `PanelVisible=true`, Unity is still receiving the keys. Compare
  `IsInputOverridden` and `CurrentInputState` with the closed-panel state.
- If `W/A/S/D` never changes while the panel is open, GUI focus or keyboard event capture is preventing the plugin from
  seeing the keys.
- If WASD is observed and `IsInputOverridden=true` while keyboard/mouse is active, the command panel's input-device branch
  is not being recognized correctly. Keyboard/mouse mode should leave gameplay input available while the panel is open.
- In controller mode, the panel reads D-pad navigation directly and should keep `IsInputOverridden=false` so the left
  stick remains available for gameplay movement.
- When the panel closes, the next state transition should report `IsInputOverridden=false` for the active player.

Extract plugin lines with:

```powershell
python .\tools\logs\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" -o ".\gameplay-input.log"
```
