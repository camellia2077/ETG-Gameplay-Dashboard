# Docs

This directory holds detailed documentation for `EtgGameplayDashboard`.

The current internal project name is `EtgGameplayDashboard`; see [Project Naming](reference/project-naming.md)
for the naming and migration rules. Existing historical references to
`EtgGameplayDashboard` are retained only until the corresponding migration batch is
completed.
The repository root `README.md` stays high-level; operational and design details live here.

## Start Here

If you are new to the project:

1. [Start Here](./getting-started/start-here.md)
2. [Code Index](./reference/code-index.md)
3. [Terminology And Naming](./reference/terminology.md)
4. [Source Guide](../src/AGENTS.md)

## Read By Goal

I am new to the project:

- [Start Here](./getting-started/start-here.md)
- [Code Index](./reference/code-index.md)
- [System Overview](./architecture/system-overview.md)
- [Testing Matrix](./reference/testing-matrix.md)

I am changing ETG runtime behavior:

- [Terminology And Naming](./reference/terminology.md)
- [Runtime Hotspots](./architecture/runtime-hotspots.md)
- [Source Guide](../src/AGENTS.md)
- [Logging](./operations/logging.md)

I am changing Boss Rush:

- [Runtime Hotspots](./architecture/runtime-hotspots.md)
- [ModTheGungeonAPI Reference](./reference/modthegungeonapi.md)
- [Commands](./reference/commands.md)
- [Smoke Checklist](./operations/smoke-checklist.md)

I am changing command UI or pickup grant behavior:

- [Commands](./reference/commands.md)
- [Muncher Spawn](./reference/runtime-internals/muncher-spawn.md)
- [Map Reveal And Teleporter Promotion](./reference/map-teleport.md)
- [Localization And Language Switching](./reference/localization.md)
- [Pickup Grant Strategy](./decisions/pickup-grant-strategy.md)
- [Pickups](./reference/pickups.md)

I am changing Start Items presets or JSON config:

- [Code Index](./reference/code-index.md)
- [Config Format (JSON5)](./reference/config-format.md)
- [Localization And Language Switching](./reference/localization.md)
- [Source Guide](../src/AGENTS.md)

I am changing language switching or localization:

- [Localization And Language Switching](./reference/localization.md)
- [Pickup Gameplay Schema v2](./reference/pickup-gameplay-schema-v2.md)
- [Commands](./reference/commands.md)
- [Config Format (JSON5)](./reference/config-format.md)
- [Pickups](./reference/pickups.md)
- [Code Index](./reference/code-index.md)

I am changing build, deploy, or tools:

- [Development Setup](./getting-started/development-setup.md)
- [Deploy](./operations/deploy.md)
- [Tools README](../tools/README.md)

## Structure

- `getting-started/`
  Day-1 and development entry docs
- `reference/`
  Stable command and generated reference material
- `reference/runtime-internals/`
  Reverse-engineered runtime facts and non-standard ETG integration notes that depend on asset tracing, decompilation, or deep log-driven verification
- `architecture/`
  Responsibility map and implementation research
- `operations/`
  Deploy, logs, and day-to-day workflow notes
- `decisions/`
  Decision records and tradeoff explanations
- `history/`
  Version history snapshots

## Quick Links

- [Start Here](./getting-started/start-here.md)
- [Development Setup](./getting-started/development-setup.md)
- [System Overview](./architecture/system-overview.md)
- [Runtime Hotspots](./architecture/runtime-hotspots.md)
- [Research Entry](./architecture/research-entry.md)
- [Project Scope](./architecture/research/project-scope.md)
- [Implementation Guidance](./architecture/research/implementation-guidance.md)
- [Deploy](./operations/deploy.md)
- [Release Package](./operations/release-package.md)
- [Logging](./operations/logging.md)
- [Map Reveal Logging](./operations/logging-map-teleport.md)
- [Muncher Spawn Logging](./operations/logging-muncher-spawn.md)
- [Floor Teleport Logging](./operations/logging-floor-teleport.md)
- [Boss Rush Logging](./operations/logging-boss-rush.md)
- [Command-Panel Health Logging](./operations/logging-command-panel-health.md)
- [Command-Panel Cursor Logging](./operations/logging-command-panel-cursor.md)
- [Command-Panel Gameplay Input Logging](./operations/logging-command-panel-gameplay-input.md)
- [Command-Panel Controller Gameplay Input Logging](./operations/logging-command-panel-controller-gameplay-input.md)
- [Smoke Checklist](./operations/smoke-checklist.md)
- [Command Notes](./notes/cmd.md)
- [Code Index](./reference/code-index.md)
- [Commands](./reference/commands.md)
- [UI Icon Reuse](./reference/ui-icon-reuse.md)
- [Items 页面性能优化](./operations/performance-items.md)
- [Loadout 长列表性能优化](./operations/performance-loadout-lists.md)
- [Muncher Spawn](./reference/runtime-internals/muncher-spawn.md)
- [Map Reveal And Teleporter Promotion](./reference/map-teleport.md)
- [Localization And Language Switching](./reference/localization.md)
- [Pickup Gameplay Schema v2](./reference/pickup-gameplay-schema-v2.md)
- [Terminology And Naming](./reference/terminology.md)
- [Testing Matrix](./reference/testing-matrix.md)
- [Config Format (JSON5)](./reference/config-format.md)
- [Pickups](./reference/pickups.md)
- [ModTheGungeonAPI Reference](./reference/modthegungeonapi.md)
- [Character Switch Strategy](./decisions/character-switch-strategy.md)
- [Pickup Grant Strategy](./decisions/pickup-grant-strategy.md)
- [History](./history/)
- [lib dependency notes](../lib/README.md)
