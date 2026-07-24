# Project Naming

## Canonical name

The repository name remains `ETG-Gameplay-Dashboard`.

The canonical internal project name is:

- Product/display name: `ETG Gameplay Dashboard`
- Code identifier: `EtgGameplayDashboard`
- File and generated-artifact prefix: `EtgGameplayDashboard`
- Log prefix: `[EtgGameplayDashboard]`

`EtgGameplayDashboard` is the name for the complete project, including the
visual item browser, filters, nearby pickup information, teleport and grant
tools, health/armor/blank controls, start-item presets, run-start grants,
character switching, keyboard aim assist, and controller aim lock.

## Legacy name

`RandomLoadout` and the misspelled `RandonLoadout` are historical names. The
repository is being migrated without backward compatibility, so they must not
be used for new code, files, logs, generated data, or documentation.

The phrase `random loadout` may remain only when it describes the actual
randomized-loadout feature rather than the project identity.

## Naming rules

Use these forms for new and migrated project-owned identifiers:

| Context | Canonical form |
| --- | --- |
| User-facing product name | `ETG Gameplay Dashboard` |
| C# namespaces, assemblies, and type prefixes | `EtgGameplayDashboard` |
| JSON, debug, log, and release artifact prefix | `EtgGameplayDashboard` |
| Log category prefix | `[EtgGameplayDashboard]` |
| Repository name | `ETG-Gameplay-Dashboard` |

Examples:

```text
EtgGameplayDashboard.dll
EtgGameplayDashboard.pickup-gameplay.json
EtgGameplayDashboard.debug.log
EtgGameplayDashboard.Core
```

During migration, every old-name reference must be classified before editing:

1. Project identity references must be renamed.
2. File paths and generated-artifact names must be renamed.
3. Feature names such as a random-loadout preset may remain descriptive when
   they refer to that specific feature.
4. Historical release notes may retain the old name when documenting what an
   older version actually used; current instructions and examples must use the
   canonical name.
