# Active-Item Grant Logging

Use this page when an active item should enter the player's active-item bar, but instead drops near the player or fails to expand the available active-item slots as expected.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableActiveItemGrantVerboseLogs = true
```

Default:

- `false`

## What It Adds

When enabled, the plugin emits detailed active-item grant diagnostics for:

- temporary active-slot capacity expansion requests
- rollback detection when ETG rebuilds active-item capacity
- rollback restoration attempts
- `LootEngine.TryGivePrefabToPlayer(...)` rejection details
- pre-grant and post-expansion active-item capacity snapshots

Typical useful lines include:

- `Detected active-item capacity rollback`
- `Restored tracked active-item capacity override`
- `Expanded active-item capacity before granting via LootEngine.TryGivePrefabToPlayer`
- `LootEngine.TryGivePrefabToPlayer returned false`

## What Still Logs When Disabled

When `EnableActiveItemGrantVerboseLogs = false`, the high-frequency active-item capacity rollback and grant-diagnostic lines stay silent.

Normal grant success and failure messages still remain visible, such as:

- `Granted Active: ...`
- `Failed to grant Active pickup ID ...`
- normal command-panel grant results

## Typical Workflow

1. Turn on `EnableActiveItemGrantVerboseLogs`.
2. Reproduce the active-item grant issue.
3. Read `BepInEx\LogOutput.log`.
4. Check:
   - whether the desired active-item capacity increased before grant
   - whether ETG rolled the capacity back after the expansion
   - whether `LootEngine.TryGivePrefabToPlayer(...)` still returned `false`
   - whether ETG spewed the item to the floor after rejecting the direct grant

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Pickup grant strategy:
  [../decisions/pickup-grant-strategy.md](../decisions/pickup-grant-strategy.md)
