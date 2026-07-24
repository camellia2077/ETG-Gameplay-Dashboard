# Character Switch Logging

Use this guide to diagnose a Breach character switch that succeeds once but cannot switch the selected P1 or P2 player again.

## Switch

The diagnostic switch is enabled by default for the current P2 reproduction:

```ini
[Debug]
EnableCharacterSwitchVerboseLogs = true
```

Set it to `false` after the issue is resolved to reduce BepInEx log noise.

## What It Captures

Each `[EtgGameplayDashboard][Command] Character switch diagnostic` entry records the requested target and character, then the P1/P2 player references before clearing, after clearing, after replacement registration, and after finalization. Every player snapshot includes its Unity instance ID, player index, character identity, and active state. The P2 flow deactivates its old player before deferred destruction so `GameManager.RefreshAllPlayers()` cannot retain a stale destroyed P2 reference.

## Reproduction

1. Open `General -> Characters` in the Breach.
2. Set `Target: P2`, switch P2 to any character, then attempt a second P2 switch.
3. Send the contiguous `Character switch diagnostic` lines surrounding both attempts from `BepInEx\LogOutput.log`.
