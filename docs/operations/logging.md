# Logging

Use this page when you need to inspect `BepInEx` output, choose the right debug log switch, or jump to a feature-specific logging guide.

If your change touches ETG runtime behavior, log review is not optional.

## Must Read First

Before triaging runtime issues, read:

1. [Start Here](../getting-started/start-here.md)
2. [Runtime Hotspots](../architecture/runtime-hotspots.md)
3. [Testing Matrix](../reference/testing-matrix.md)

## 30-Second Commands

Read recent Boss Rush and error lines from the default ETG install log:

```powershell
python .\tools\logs\read_log.py --preset bossrush --preset error --tail 200 --dedupe-consecutive --ignore-case
```

Read startup and run-state lines from a specific log:

```powershell
python .\tools\logs\read_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --preset init --preset run
```

Extract only EtgGameplayDashboard-owned lines:

```powershell
python .\tools\logs\extract_etg_gameplay_dashboard_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log"
```

## Message Prefixes

EtgGameplayDashboard-written log lines use structured prefixes such as:

- `[EtgGameplayDashboard][Init]`
- `[EtgGameplayDashboard][Run]`
- `[EtgGameplayDashboard][BossRush]`
- `[EtgGameplayDashboard][Grant]`
- `[EtgGameplayDashboard][Command]`
- `[EtgGameplayDashboard][Performance]`

These prefixes separate plugin logs from Unity, ETG, BepInEx, and other mods.

## Feature Log Switches

High-volume diagnostics are now split by feature and disabled by default.

Config file location:

- `BepInEx\config\ETG-Gameplay-Dashboard.cfg`

Available optional switches:

```ini
[Debug]
EnableMapTeleportVerboseLogs = false
EnableMuncherVerboseLogs = false
EnableRoomEnemyReplayVerboseLogs = false
EnableBossIntroSkipVerboseLogs = false
EnableFloorTeleportVerboseLogs = false
EnableBossRushVerboseLogs = false
EnableCommandPanelHealthVerboseLogs = false
EnableCommandPanelCursorVerboseLogs = true
EnableCommandPanelGameplayInputVerboseLogs = false
EnableCommandPanelControllerGameplayInputVerboseLogs = false
EnableCommandPanelShortcutVerboseLogs = true
EnableCommandPanelCursorRenderVerboseLogs = false
EnableCommandPanelCursorRenderProbe = false
EnableControllerAimVerboseLogs = false
EnableActiveItemGrantVerboseLogs = false
EnableNearbyPickupVerboseLogs = false
EnableStartupWindowFocusVerboseLogs = false
EnablePerformanceVerboseLogs = false
EnableCharacterSwitchVerboseLogs = true
EnableDamageDiagnosticsVerboseLogs = false
```

Use them only while actively reproducing an issue. Leave them off for normal play.

Feature guides:

- [Map Reveal Logging](./logging-map-teleport.md)
- [Muncher Spawn Logging](./logging-muncher-spawn.md)
- [Room Enemy Replay Logging](./logging-room-enemy-replay.md)
- [Boss Audio Diagnostics](./logging-boss-audio.md)
- [Boss Intro Skip Logging](./logging-boss-intro-skip.md)
- [Floor Teleport Logging](./logging-floor-teleport.md)
- [Boss Rush Logging](./logging-boss-rush.md)
- [Command-Panel Health Logging](./logging-command-panel-health.md)
- [Command-Panel Cursor Logging](./logging-command-panel-cursor.md)
- [Command-Panel Gameplay Input Logging](./logging-command-panel-gameplay-input.md)
- [Command-Panel Controller Gameplay Input Logging](./logging-command-panel-controller-gameplay-input.md)
- [Command-Panel Shortcut Logging](./logging-command-panel-shortcut.md)
- [Command-Panel Cursor Render Logging](./logging-command-panel-cursor-render.md)
- [Controller Aim / Cursor Logging](./logging-controller-aim.md)
- [Active-Item Grant Logging](./logging-active-item-grant.md)
- [Nearby Pickup Logging](./logging-nearby-pickup.md)
- [Startup Window Focus](./startup-window-focus.md)
- [Performance Logging](./logging-performance.md)
- [Items 页面性能优化](./performance-items.md)
- [Loadout 长列表性能优化](./performance-loadout-lists.md)
- [Character Switch Logging](./logging-character-switch.md)
- [Damage Diagnostics Logging](./logging-damage-diagnostics.md)

## Startup Self-Check

Startup emits a Boss Rush self-check summary under `[EtgGameplayDashboard][Init]`.

Healthy startup typically includes:

- `Boss Rush service initialized. Startup self-check is running.`
- `Boss Rush hook ready: ...`
- `Boss Rush startup self-check complete. Applied hooks=..., Skipped hooks=0.`

Treat any of these as actionable:

- `Boss Rush hook skipped: ...`
- `Boss Rush hook failed: ...`
- `Boss Rush startup self-check warning: ...`

If a hook signature no longer matches the game assembly, the plugin now logs and skips that hook instead of hard-failing the whole plugin.

## What To Check After Runtime Changes

After any hook, scene, Boss Rush, character-select-hub, reward, pause-flow, or custom room-object change, check for:

- hook install failures
- null references during scene transition
- unexpected startup warnings
- Boss Rush state progression logs
- return-to-character-select logs
- feature-specific warnings from `[EtgGameplayDashboard][Command]`
- command-panel health rollback diagnostics when heart or armor HUD animations replay unexpectedly
- command-panel cursor diagnostics when controller-to-mouse handoff makes the in-game cursor disappear or flicker
- command-panel gameplay input diagnostics when WASD movement stops working while the command panel is open
- command-panel cursor render diagnostics when the ETG cursor appears below the Control Panel
- active-item grant diagnostics when active items drop near the player instead of entering the active-item bar
- nearby-pickup diagnostics when the overlay does not appear for dropped loot or shop merchandise
  this now also includes schema-v2 gameplay/terms input-file path and existence diagnostics when nearby-pickup verbose logs are enabled
- startup window-focus diagnostics when Steam launch enters audio playback but does not fully foreground the game window
- performance diagnostics when entering a run causes frame drops, long frames, or scene-transition stutter

Use [Testing Matrix](../reference/testing-matrix.md) to decide the rest of the validation set.

## Reader Tools

Use the newer log reader when you want filtering, tailing, presets, or consecutive-line dedupe.

Custom regex filtering example:

```powershell
python .\tools\logs\read_log.py --pattern "Boss Rush|HandleExitToMainMenu|NullReference"
```

Write extracted plugin lines to a file:

```powershell
python .\tools\logs\extract_etg_gameplay_dashboard_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" -o ".\etg-gameplay-dashboard.log"
```

Include older unprefixed plugin lines:

```powershell
python .\tools\logs\extract_etg_gameplay_dashboard_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --include-unprefixed-plugin-lines
```

## Read Next

- Startup window focus diagnostics:
  [./startup-window-focus.md](./startup-window-focus.md)
- Map reveal diagnostics:
  [./logging-map-teleport.md](./logging-map-teleport.md)
- Muncher diagnostics:
  [./logging-muncher-spawn.md](./logging-muncher-spawn.md)
- Boss intro skip diagnostics:
  [./logging-boss-intro-skip.md](./logging-boss-intro-skip.md)
- Floor teleport diagnostics:
  [./logging-floor-teleport.md](./logging-floor-teleport.md)
- Boss Rush diagnostics:
  [./logging-boss-rush.md](./logging-boss-rush.md)
- Command-panel health diagnostics:
  [./logging-command-panel-health.md](./logging-command-panel-health.md)
- Command-panel cursor diagnostics:
  [./logging-command-panel-cursor.md](./logging-command-panel-cursor.md)
- Command-panel gameplay input diagnostics:
  [./logging-command-panel-gameplay-input.md](./logging-command-panel-gameplay-input.md)
- Command-panel cursor render diagnostics:
  [./logging-command-panel-cursor-render.md](./logging-command-panel-cursor-render.md)
- Active-item grant diagnostics:
  [./logging-active-item-grant.md](./logging-active-item-grant.md)
- Nearby pickup diagnostics:
  [./logging-nearby-pickup.md](./logging-nearby-pickup.md)
- Performance diagnostics:
  [./logging-performance.md](./logging-performance.md)
- Startup window-focus diagnostics:
  [./startup-window-focus.md](./startup-window-focus.md)
- Smoke checklist:
  [./smoke-checklist.md](./smoke-checklist.md)
- Runtime risk areas:
  [../architecture/runtime-hotspots.md](../architecture/runtime-hotspots.md)
- Tool entrypoints:
  [../../tools/README.md](../../tools/README.md)
