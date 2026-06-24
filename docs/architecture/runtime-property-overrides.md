# Runtime Property Overrides

Use this page when you need to preserve intentional player runtime mutations that ETG may rebuild or discard during other gameplay events such as weapon swaps.

Read this before adding any new player-stat override that needs to survive ETG-owned refresh logic.

## What This Owns

The runtime property override pattern is the small service skeleton used for:

- tracking the latest intended runtime value for a player-owned property
- subscribing to ETG events that commonly rebuild that property
- restoring the tracked value immediately when the game rolls it back
- keeping a polling fallback for cases that do not surface a reliable event

This is for runtime-only state that is intentionally mutated after the player already exists in-scene.

Examples:

- debug-added max health that should survive weapon swaps
- future runtime armor-cap overrides
- future temporary stat containers that ETG may reconstruct

Do not use this pattern for:

- config-backed defaults that should be applied only once at run start
- pure core selection logic
- values that already have a stable ETG-supported persistence path

## Current Files

| File | Owns |
| --- | --- |
| `src/RandomLoadout/Commands/PlayerRuntimeOverrideServiceBase.cs` | generic player-level tracking lifecycle: store per-player override state, attach/update/reset flow, and shared helper utilities |
| `src/RandomLoadout/Commands/PlayerHealthOverrideService.cs` | concrete health override implementation, including ETG health/gun event hooks and rollback detection/restoration |
| `src/RandomLoadout/Commands/PlayerDebugCommandService.cs` | the current caller that registers health overrides after `+1 Max HP` succeeds |
| `src/RandomLoadout/Plugin.Bootstrap.cs` | service construction and dependency wiring |
| `src/RandomLoadout/Plugin.RunLifecycle.cs` | per-frame fallback update call |

## How The Skeleton Works

The base service provides a standard player-scoped lifecycle:

1. A caller mutates a runtime property and then calls `TrackOverride(player)`.
2. The concrete service captures the intended post-mutation value into a player-owned state object.
3. The concrete service subscribes to ETG callbacks that often rebuild the same property.
4. When a callback or polling fallback sees the property rolled back, the concrete service restores the tracked value.

The design intentionally splits responsibilities:

- base class:
  player dictionary, `TrackOverride`, `Update`, `Clear`, `Reset`, and shared gun-label formatting
- concrete class:
  ETG members, state shape, rollback detection rules, event handlers, and restore operation

## Health Implementation

`PlayerHealthOverrideService` currently protects runtime max-health overrides.

It watches:

- `PlayerController.GunChanged`
- `HealthHaver.OnHealthChanged`
- the plugin `Update()` polling fallback

It restores when:

- the live `HealthHaver.GetMaxHealth()` falls below the tracked intended maximum

It restores by:

- calling `HealthHaver.SetHealthMaximum(...)`
- calling `HealthHaver.ForceSetCurrentHealth(...)`

## Extension Rules

When adding another runtime property override:

1. Create a new concrete service that derives from `PlayerRuntimeOverrideServiceBase<TState>`.
2. Keep the state object limited to the minimum runtime values and ETG event handlers that property needs.
3. Subscribe only to ETG callbacks that are already verified in this repository or in referenced assemblies.
4. Restore only when the value clearly rolled back, not on every event.
5. Keep polling as a fallback only when event coverage is incomplete.

Prefer one concrete override service per property area unless two properties always share:

- the same ETG state owner
- the same rollback detector
- the same restore API

## Verification

After editing a runtime property override:

- build Debug
- reproduce the rollback trigger in game
- check the BepInEx log for restore warnings/info when rollback happens
- verify the value stays stable without visible flicker

Read next:

- [Runtime Hotspots](./runtime-hotspots.md)
- [System Overview](./system-overview.md)
- [Code Index](../reference/code-index.md)
