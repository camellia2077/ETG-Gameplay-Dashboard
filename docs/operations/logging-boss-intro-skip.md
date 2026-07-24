# Boss Intro Skip Logging

Use this diagnostic when `Combat -> Skip Boss Intro` does not skip a Boss introduction as expected.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableBossIntroSkipVerboseLogs = true
```

Default: `false`.

Restart the game after changing the setting. The in-game `Skip Boss Intro` toggle controls the gameplay feature separately; it does not enable this diagnostic log.

## What It Captures

When both the cfg switch and the in-game feature are enabled, `[EtgGameplayDashboard][Command]` includes:

- matching a Boss to a `BossTriggerZone`;
- observing `GenericIntroDoer` entry through the room or trigger-zone path;
- writing the native `Tribool.Ready` skip request;
- a timeout reason when the native intro never becomes active, including component and pause-state values;
- a warning when a Boss trigger zone has no matching `GenericIntroDoer`.

When `EnableBossIntroSkipVerboseLogs = false`, enabling or using `Skip Boss Intro` does not emit these diagnostics. Hook-install failures remain visible at startup because they indicate that the feature cannot operate.

## Read Next

- [Logging overview](./logging.md)
- [Commands](../reference/commands.md)
