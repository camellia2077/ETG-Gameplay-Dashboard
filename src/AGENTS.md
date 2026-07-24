# Source Guide

This file applies to code under `src/`.

Use it as the source-editing contract for ETG runtime, command UI, config, and core logic changes.

For a task-to-file navigation map, read `../docs/reference/code-index.md` before opening source files. This source guide defines editing rules; the code index tells a new agent where to look first.

## Layer Boundaries

### `src/EtgGameplayDashboard/`

Runtime integration layer:

- BepInEx startup and teardown
- Unity and ETG runtime access
- Harmony hooks
- scene and run lifecycle
- pickup resolution and granting
- config file providers
- alias loading
- logging
- in-game command UI

### `src/EtgGameplayDashboard.Core/`

Pure logic layer:

- command parsing models
- loadout rule/config models
- selection logic
- deterministic seed behavior
- structured warnings and results

Keep reusable, testable decision logic in `EtgGameplayDashboard.Core` when it does not need Unity, ETG, BepInEx, or file-system access.

## ETG API Usage Order

When adding or changing gameplay behavior, use this order:

1. Reuse existing services, providers, resolvers, readers, or granters in this project.
2. Search existing ETG/Unity/BepInEx usage in nearby code.
3. Read the relevant local docs or decision notes.
4. Use public members from referenced assemblies when they are already known or verifiable.
5. Use reflection only when the codebase already has a compatible pattern and the member is stable enough for the task.
6. If behavior depends on unknown private members or unverified runtime details, require decompilation before implementing.

Do not invent ETG field names or method behavior from memory.

## File Responsibility

For detailed task routing, use `../docs/reference/code-index.md`.

- `Plugin*.cs`: plugin lifecycle, config binding, runtime polling, run grant flow, and bootstrap wiring.
- `InGameCommandController*.cs`: in-game IMGUI pages, command actions, UI state, styles, and browser/editor views.
- `EtgPickupResolver*.cs`: lookup and catalog data from ETG pickup objects.
- `EtgPickupGranter.cs`: granting selected pickups to the player.
- `EtgOwnedPickupReader.cs`: reading currently owned player pickups.
- `JsonLoadoutRuleFileProvider*.cs`: loading, parsing, saving, converting, and preset access for rules files.
- `GrantCommandService*.cs`: command execution and resolution.
- `FoyerCharacterSwitchService*.cs`: character switching and unlocking.
- `BossRushService*.cs`: Boss Rush flow and scene/player handling.

When a type already uses partial sibling files, add behavior to the matching responsibility file instead of growing the root file.

## UI And Config Rules

- Keep command panel UI behavior in `InGameCommandController.*`.
- Keep UI state in `InGameCommandController.State.cs`.
- Keep visual style constants in the state/style files rather than scattering colors or dimensions.
- Do not read or write rule/config files directly from UI drawing code.
- Route editable Start Items behavior through `LoadoutRuleEditorService`.
- Route file persistence through provider classes.
- After Add/Remove/Preset actions, refresh cached service/UI state consistently.

## Runtime Safety

- Do not assume a scene token is the same thing as gameplay readiness.
- Prefer vanilla ETG flow over custom scene or player-state cuts.
- Before editing a Harmony hook, verify the target signature and parameter names.
- Be careful around `PlayerController`, `Gun`, `PickupObject`, `GameManager`, and scene lifecycle code.
- New runtime toggles should be reset in plugin teardown if they hold state or modify player/gun state.
- Avoid persistent mutation of ETG object definitions when a per-frame or per-player runtime service is sufficient.

## Read Docs By Task

Read only the relevant docs for the subsystem being changed:

- Code navigation by task: `../docs/reference/code-index.md`
- Startup and orientation: `../docs/getting-started/start-here.md`
- Runtime terms: `../docs/reference/terminology.md`
- Scene, hooks, or lifecycle: `../docs/architecture/runtime-hotspots.md`
- Architecture boundaries: `../docs/architecture/system-overview.md`
- Commands or command panel behavior: `../docs/reference/commands.md`
- Pickup lookup or item granting: `../docs/reference/modthegungeonapi.md`, `../docs/decisions/pickup-grant-strategy.md`, `../docs/reference/pickups.md`
- Character switching: `../docs/decisions/character-switch-strategy.md`, `../docs/reference/modthegungeonapi.md`
- Config, aliases, and Start Items rules: `../docs/reference/config-format.md`
- Build, deploy, and logs: `../docs/getting-started/development-setup.md`, `../docs/operations/deploy.md`, `../docs/operations/logging.md`
- Prior decisions and history: `../docs/decisions/`, `../docs/history/`

For feature-specific runtime diagnostics:

- keep verbose debug log switches defaulted to `false`
- add or update the matching guide in `../docs/operations/` that explains what the switch captures
- update `../docs/operations/logging.md` so agents can find the switch and the feature guide from the logging index

## Verification

- Run `python .\tools\devtools\check_naming.py --verbose` after code changes.
- Run `python .\tools\build\build.py --configuration Debug` for runtime or UI changes.
- Run Release build when packaging, release docs, or release tooling changes.
- Read the BepInEx log after runtime, hook, scene-transition, item-grant, or deployment-sensitive changes.
- Update docs when behavior, config format, deployment flow, generated files, or terminology changes.
