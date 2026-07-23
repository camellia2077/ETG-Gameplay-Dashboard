# Performance Logging

Use this guide when ETG becomes noticeably choppy only while `ETG-Gameplay-Dashboard` is loaded and you need evidence for which runtime step is causing the slowdown.

## Switch

Config file:

- `BepInEx\config\ETG-Gameplay-Dashboard.cfg`

Enable this switch only while reproducing the issue:

```ini
[Debug]
EnablePerformanceVerboseLogs = true
```

Leave it `false` for normal play.

## What It Captures

When enabled, the plugin writes `[RandomLoadout][Performance]` lines for:

- gameplay-only sampling in dungeon scenes, not the Breach / character-select hub
- an extra focus window covering the first 10 seconds after entering a gameplay scene
- rolling FPS summaries every few seconds
- long-frame warnings when a frame crosses the built-in threshold
- slow `Plugin.Update()` sub-step timings tied to gameplay runtime services, such as nearby-pickup or runtime-toggle updates
- General -> Items pickup-browser open, catalog/alias refresh, and slow UI-draw timings
- Items icon rendering stages under the `PickupBrowserIcon:` prefix, including skipped input data, tk2d clone failures, bounds failures, render exceptions, and successful texture creation
- Items name resolution under the `PickupBrowserName:` prefix, including current/game language codes and the localized, English, game-language, internal, and final display-name values
- deferred floor-teleport staging and execution timing
- foyer character switch timing
- automatic start-loadout grant timing, including slow per-pickup grant calls
- control-panel cold-open timing, from the toggle request through the first `OnGUI` pass, including style setup, language/focus handling, page-data preparation, GUI-matrix setup, and first-frame completion
- Boss selection page entry, first option preparation/draw timing, and Boss/room-variant selection operation timing

Control-panel open traces use the `OpenTrace:` marker. Each trace has a `TraceId`, cumulative `TotalMs`, and (where applicable) `StageMs` for the preceding stage. The trace is emitted only for the first GUI pass after opening, so it does not add per-frame log noise.

Each log line also includes recent runtime context such as scene name, whether a `PrimaryPlayer` exists, whether a deferred teleport is pending, and the last major lifecycle event the plugin observed.

## Recommended Repro

1. Turn on `EnablePerformanceVerboseLogs`.
2. Start the game through the normal launcher path that reproduces the stutter.
3. Enter the Breach, switch characters if that is part of the repro, then start a run.
4. Stop after the first obvious hitch or after the first room fully loads.
5. Read `BepInEx\LogOutput.log` and search for `[RandomLoadout][Performance]`.

## What To Look For

- `Long frame captured` shows a visible hitch and the last lifecycle event before it.
- `Slow Update step` points to a specific gameplay-time per-frame subsystem that exceeded the threshold.
- `Operation timing` shows one-shot work such as character switching, deferred teleport, or automatic loadout grant.
- `PickupBrowser:` lines separate catalog refresh cost from slow per-frame UI drawing; repeated `Slow pickup browser draw` lines indicate that rendering the item list is the expensive phase.
- `PickupBrowserIcon:` lines show where an icon disappears: `Icon render begin`, `tk2dSprite.AddComponent`, `bounds`, `exception`, or `success`.
- `PickupBrowserName:` lines show whether the language code or one of the catalog name fields caused an unexpected English label.
- `BossPage:` lines show the time from selecting the Boss page to its first completed draw, option preparation cost, and the duration/result of selecting a Boss or room variant.
- `FPS summary` shows whether the slowdown is a single spike or a sustained low-FPS window.
- `FocusWindow=true` means the log happened inside the first 10 seconds after entering the gameplay scene, which is the highest-priority window for your current repro.

If the slowest lines consistently point at character switching, compare the timestamps against the recent switch-only fix path. If they point at loadout grant or nearby-pickup scanning instead, the stutter is likely elsewhere.
