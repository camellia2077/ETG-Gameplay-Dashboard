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

Extract only RandomLoadout-owned lines:

```powershell
python .\tools\logs\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log"
```

## Message Prefixes

RandomLoadout-written log lines use structured prefixes such as:

- `[RandomLoadout][Init]`
- `[RandomLoadout][Run]`
- `[RandomLoadout][BossRush]`
- `[RandomLoadout][Grant]`
- `[RandomLoadout][Command]`

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
EnableFloorTeleportVerboseLogs = false
EnableBossRushVerboseLogs = false
EnableCommandPanelHealthVerboseLogs = false
EnableCommandPanelCursorVerboseLogs = false
EnableActiveItemGrantVerboseLogs = false
```

Use them only while actively reproducing an issue. Leave them off for normal play.

Feature guides:

- [Map Reveal Logging](./logging-map-teleport.md)
- [Muncher Spawn Logging](./logging-muncher-spawn.md)
- [Floor Teleport Logging](./logging-floor-teleport.md)
- [Boss Rush Logging](./logging-boss-rush.md)
- [Command-Panel Health Logging](./logging-command-panel-health.md)
- [Command-Panel Cursor Logging](./logging-command-panel-cursor.md)
- [Active-Item Grant Logging](./logging-active-item-grant.md)

## Startup Self-Check

Startup emits a Boss Rush self-check summary under `[RandomLoadout][Init]`.

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
- feature-specific warnings from `[RandomLoadout][Command]`
- command-panel health rollback diagnostics when heart or armor HUD animations replay unexpectedly
- command-panel cursor diagnostics when controller-to-mouse handoff makes the in-game cursor disappear or flicker
- active-item grant diagnostics when active items drop near the player instead of entering the active-item bar

Use [Testing Matrix](../reference/testing-matrix.md) to decide the rest of the validation set.

## Reader Tools

Use the newer log reader when you want filtering, tailing, presets, or consecutive-line dedupe.

Custom regex filtering example:

```powershell
python .\tools\logs\read_log.py --pattern "Boss Rush|HandleExitToMainMenu|NullReference"
```

Write extracted plugin lines to a file:

```powershell
python .\tools\logs\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" -o ".\randomloadout.log"
```

Include older unprefixed plugin lines:

```powershell
python .\tools\logs\extract_randomloadout_log.py "C:\Game\steam\steamapps\common\Enter the Gungeon\BepInEx\LogOutput.log" --include-unprefixed-plugin-lines
```

## Read Next

- Map reveal diagnostics:
  [./logging-map-teleport.md](./logging-map-teleport.md)
- Muncher diagnostics:
  [./logging-muncher-spawn.md](./logging-muncher-spawn.md)
- Floor teleport diagnostics:
  [./logging-floor-teleport.md](./logging-floor-teleport.md)
- Boss Rush diagnostics:
  [./logging-boss-rush.md](./logging-boss-rush.md)
- Command-panel health diagnostics:
  [./logging-command-panel-health.md](./logging-command-panel-health.md)
- Command-panel cursor diagnostics:
  [./logging-command-panel-cursor.md](./logging-command-panel-cursor.md)
- Active-item grant diagnostics:
  [./logging-active-item-grant.md](./logging-active-item-grant.md)
- Smoke checklist:
  [./smoke-checklist.md](./smoke-checklist.md)
- Runtime risk areas:
  [../architecture/runtime-hotspots.md](../architecture/runtime-hotspots.md)
- Tool entrypoints:
  [../../tools/README.md](../../tools/README.md)
