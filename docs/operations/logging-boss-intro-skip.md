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
- observing `GatlingGullIntroDoer.TriggerSequence`, which is a separate Boss intro implementation used by Gatling Gull;
- observing `GatlingGullIntroDoer` running-state and phase changes;
- writing the native `Tribool.Ready` skip request;
- a timeout reason when the native intro never becomes active, including component and pause-state values;
- a warning when a Boss trigger zone has no matching `GenericIntroDoer`.

When `EnableBossIntroSkipVerboseLogs = false`, enabling or using `Skip Boss Intro` does not emit these diagnostics. Hook-install failures remain visible at startup because they indicate that the feature cannot operate.

The vanilla Gatling Gull intro is not a `GenericIntroDoer`. It uses `GatlingGullIntroDoer`, subscribes to `RoomHandler.Entered`, and advances its own phase machine. For that Boss, inspect the `GatlingGullIntroDoer.TriggerSequence` and phase diagnostics instead of expecting `GenericIntroDoer` skip-request lines.

## Read Next

- [Logging overview](./logging.md)
- [Commands](../reference/commands.md)
