---
trigger: always_on
---

# Repository Guide

This file contains rules that apply anywhere in this repository.

## Shell And Encoding

- Use `pwsh` (PowerShell 7.6.0) as the default shell entry for commands.
- Keep edited text files UTF-8 compatible.
- Prefer ASCII in code and docs unless the file already intentionally uses localized text.
- Use quoted Windows paths when they contain spaces.

## Project Shape

- `src/EtgGameplayDashboard/` is the BepInEx, Unity, ETG, and in-game UI integration layer.
- `src/EtgGameplayDashboard.Core/` is the pure logic layer for parsing, config models, selection, seeds, warnings, and results.
- `defaults/config/` contains repository defaults. Do not overwrite a user's live game config unless explicitly asked.
- `tools/` contains build, deploy, release, and developer tooling.
- `docs/` contains behavior notes, setup, operations, architecture, and decisions.

For first-time orientation, use `docs/reference/code-index.md` as the code navigation map. It points task types to the files and docs that usually matter.

## External API Rule

This project is built on top of Enter the Gungeon, BepInEx, ModTheGungeonAPI, Unity, and referenced assemblies.

Before implementing behavior against game/runtime APIs:

- Check `docs/reference/code-index.md` to identify the owning code area.
- Search the existing project wrappers and services first.
- Prefer documented local abstractions over direct game API calls.
- Check repository docs and prior decisions for the same subsystem.
- If an API is already referenced in code, follow the existing usage pattern.
- If a new game member or behavior is uncertain, verify it from referenced assemblies or decompiled sources before coding.
- If verification requires decompilation and the user asked not to decompile, stop and report that requirement instead of guessing.

## Configuration Safety

- Do not use deployment options that overwrite live user config unless the user explicitly asks for it.
- In particular, avoid `--overwrite-config` for normal deploys.
- Preserve user rules, aliases, language settings, and active preset choices in the game `BepInEx\config` directory.
- When localization defaults change, copy only localization files if the user needs the in-game text updated.
- Feature-specific verbose debug logs must default to `false` in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`.
- When you add or change a verbose log switch, document both:
  - how to enable or disable it in `docs/operations/logging.md`
  - what it captures in the matching feature logging guide under `docs/operations/`

## Common Commands

- Naming check: `python .\tools\devtools\check_naming.py --verbose`
- Debug build: `python .\tools\build\build.py --configuration Debug`
- Release build: `python .\tools\build\build.py --configuration Release`
- Deploy debug build: `python .\tools\deploy\deploy_mod.py "<Enter the Gungeon path>"`

## Build Reporting

- Do not report the compiled or deployed DLL SHA-256 in every response by default.
- Report a DLL SHA-256 only when the user explicitly asks for it or when it is needed to diagnose deployment integrity.

## Change Discipline

- Keep changes scoped to the requested behavior.
- Prefer small service/controller additions over broad rewrites.
- Do not revert unrelated dirty work.
- If a change touches runtime hooks, scene flow, game state, item granting, config loading, or deployment, update or consult the matching docs.
- For user-facing UI or behavior changes, verify with at least a Debug build.
- For ETG runtime changes, inspect the BepInEx log when behavior cannot be validated by build output alone.
- When debugging runtime issues, prefer turning on only the relevant feature-specific verbose log switch and leave unrelated verbose switches off.
