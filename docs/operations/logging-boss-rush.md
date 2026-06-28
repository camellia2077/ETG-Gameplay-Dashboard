# Boss Rush Logging

Use this page when you need the detailed runtime diagnostics for Boss Rush floor loading, readiness checks, and boss-room handoff.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableBossRushVerboseLogs = true
```

Default:

- `false`

## What It Adds

When enabled, the plugin emits detailed Boss Rush flow logs for:

- scene observation while Boss Rush is active
- first-floor character bootstrap
- floor wait and readiness checks
- boss-room scan and staging-room handoff
- natural vanilla floor transition acceptance
- return-to-character-select flow observation

Typical useful lines include:

- `Level load notification received`
- `Observed active Boss Rush scene via ...`
- `Preparing player state and boss-room teleport ...`
- `Boss Rush floor wait frame ...`
- `Boss Rush floor ready check ...`
- `Teleported to boss staging room ...`
- `Accepted vanilla floor transition into ...`
- `Observed character select hub via ...`

## What Still Logs When Disabled

When `EnableBossRushVerboseLogs = false`, the trace-style Boss Rush info lines stay silent.

Failure-oriented warnings still remain visible, such as:

- missing character prefab / bootstrap failure
- failed boss-room detection
- forced reset because state and scene no longer match
- intercepted game over warnings
- other explicit Boss Rush warning conditions

User-facing Boss Rush command results also still remain visible.

## Typical Workflow

1. Turn on `EnableBossRushVerboseLogs`.
2. Start Boss Rush from the Breach.
3. Read `BepInEx\LogOutput.log`.
4. Check:
   - whether the selected scene was observed correctly
   - whether the player and dungeon were considered ready
   - whether a boss room and staging room were found
   - whether the next floor transition was accepted after reward claim

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Command reference:
  [../reference/commands.md](../reference/commands.md)
