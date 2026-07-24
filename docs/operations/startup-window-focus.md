# Startup Window Focus

Use this page when ETG launches with audio running but the player still has to click the taskbar icon or when Windows keeps the taskbar visible after startup.

## Config Switch

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableStartupWindowFocusVerboseLogs = true
```

Default is `false`.

When `EnableStartupWindowFocusVerboseLogs = false`, the startup window-focus fix still runs, but the detailed window-enumeration, foreground-monitor, and Win32 call-trace lines stay silent.

## Status

This is intended as a long-term in-mod fix, not a throwaway workaround.

Why it is reasonable to keep:

- it lives entirely inside the BepInEx startup flow, so release packages do not need Python or an external helper
- it only runs during startup and only touches Win32 foreground APIs for the current ETG process
- it is based on repeated real-machine log captures, not on guessed timing
- it keeps the diagnostic logs that made the issue debuggable, so future regressions can be triaged from `BepInEx\LogOutput.log`

What still makes it environment-sensitive:

- Windows foreground rules are OS-managed and can vary by shell state, overlays, Steam timing, and user focus policy
- ETG exposes more than one visible top-level window during startup, so the correct handoff depends on observed runtime behavior

In practice, this means the fix is durable, and the diagnostic logs stay available behind an opt-in switch because startup focus is an integration problem, not a pure in-process logic problem.

## Final Fix Shape

The startup focus logic is implemented in:

- `src/EtgGameplayDashboard/Runtime/GameWindowFocusService.cs`
- `src/EtgGameplayDashboard/Plugin.Bootstrap.cs`
- `src/EtgGameplayDashboard/Plugin.RunLifecycle.cs`

The final behavior is:

1. Wait for `GameManager` startup and subscribe to normal run-lifecycle observation.
2. Do not focus immediately during plugin `Awake`.
3. Wait until the first playable foyer load is observed.
4. Delay again for a short settle window.
5. Enumerate visible top-level windows that belong to the current ETG process.
6. Prefer the `BepInEx ...` console window first.
7. If a separate `Enter the Gungeon` game window is also visible, do a follow-up foreground handoff to that game window.

## Why This Works

Real-world logs showed three important things:

1. Focusing too early was unreliable.
   The game had already started playing audio, but the foreground-capable ETG windows were not yet in the right state.

2. Focusing only the game window was incomplete on the affected machine.
   The game could come forward, but the Windows taskbar could remain visible.

3. The external helper that worked in practice matched the `BepInEx` console window first.
   Reproducing that handoff path inside the mod fixed automatic entry without depending on an external tool.

Because of that evidence, the code now mirrors the proven sequence instead of using a simpler but less reliable `SetForegroundWindow` call against the game window alone.

## Key Logs

Look for these lines in `BepInEx\LogOutput.log`:

- `Scheduling startup window focus attempt after ...`
- `Startup window focus helper discovered current-process windows: ...`
- `Startup window focus helper matched window: ...`
- `Startup window focus helper follow-up game window is available: ...`
- `Startup window focus attempt is beginning. Stage=primary ...`
- `Startup window focus attempt is beginning. Stage=game_window_follow_up ...`
- `Foreground window after focus attempt. Stage=...`

These lines tell you:

- whether startup timing was late enough
- which ETG-owned windows were actually visible
- whether the code foregrounded the console window, the game window, or both
- whether Windows accepted the foreground request

## If It Regresses

Reproduce from a real Steam launch and then inspect `BepInEx\LogOutput.log`.

1. Turn on `EnableStartupWindowFocusVerboseLogs`.
2. Reproduce from a real Steam launch.
3. Read `BepInEx\LogOutput.log`.

Check in order:

1. Did the foyer scene get observed?
2. Did the 4-second delayed startup focus attempt get scheduled?
3. Which current-process windows were discovered?
4. Did `primary` target `BepInEx` or the game window?
5. Did `game_window_follow_up` run?
6. After each attempt, which window was actually foreground?

If those answers change on a future machine or Windows build, update the selection or timing logic using the same log-first workflow rather than replacing the service with a blind delay tweak.
