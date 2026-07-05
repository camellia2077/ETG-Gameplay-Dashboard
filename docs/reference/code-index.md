# Code Index

Use this page as the first code navigation map for a new agent. It answers:

- what code owns a feature area
- what docs to read before editing it
- what checks usually matter after the edit

Keep this page index-like. Put long explanations in the linked docs.

## First Stop

Before editing source:

1. Read [Start Here](../getting-started/start-here.md).
2. Read [Source Guide](../../src/AGENTS.md).
3. Use the task map below to find the owning files.

If a feature touches ETG runtime behavior and the API is not already used in this repository, verify the member in referenced assemblies or decompiled sources before coding.

## Repository Areas

| Area | Owns | Start here |
| --- | --- | --- |
| `src/RandomLoadout/` | BepInEx plugin, Unity/ETG integration, runtime services, IMGUI command panel | [Source Guide](../../src/AGENTS.md) |
| `src/RandomLoadout.Core/` | Pure parsing, config models, selection, warnings, seed behavior | [System Overview](../architecture/system-overview.md) |
| `tests/RandomLoadout.Core.Tests/` | Core parser, rule, and selection checks | [Testing Matrix](./testing-matrix.md) |
| `defaults/config/` and `defaults/presets/` | Shipped config, localization, catalog baselines, and built-in preset files | [Config Format](./config-format.md) |
| `tools/` | Build, deploy, release, generated docs, developer utilities | [Development Setup](../getting-started/development-setup.md) |
| `docs/` | Project knowledge, workflows, decisions, references | [Docs README](../README.md) |

## Runtime Entry Points

Start with these when tracing how the mod enters the game:

| File | Look here for |
| --- | --- |
| `src/RandomLoadout/Plugin.cs` | BepInEx plugin shell and root ownership |
| `src/RandomLoadout/Plugin.Bootstrap.cs` | service construction, API bootstrap, game-manager startup wiring |
| `src/RandomLoadout/Plugin.RunLifecycle.cs` | new-run observation and automatic start-item grant flow |
| `src/RandomLoadout/Plugin.CatalogExport.cs` | runtime pickup catalog export |
| `src/RandomLoadout/Plugin.State.cs` | plugin-level mutable state |
| `src/RandomLoadout/Runtime/RunLifecycleTracker.cs` | run-start detection rules |
| `src/RandomLoadout/Runtime/RunSceneWatcher.cs` | scene readiness observation |

Read next:

- [Runtime Hotspots](../architecture/runtime-hotspots.md)
- [Logging](../operations/logging.md)

## Command Panel And UI

All in-game command-panel work starts in `src/RandomLoadout/Commands/`.

| File | Look here for |
| --- | --- |
| `InGameCommandController.cs` | controller shell and page dispatch |
| `InGameCommandController.State.cs` | UI state, selected page/category, controller-focus state, dimensions, colors, cached data |
| `InGameCommandController.Styles.cs` | IMGUI styles |
| `InGameCommandController.CommandPage.cs` | main command page layout, top-level controls, and first-stage controller navigation routing |
| `InGameCommandController.CommandActions.cs` | general command button actions, including `Reveal Map` and its teleporter-promotion path |
| `InGameCommandController.Teleport.cs` | teleport picker UI, floor resolution, and `load_level` runtime handoff |
| `InGameCommandController.CharacterPage.cs` | character tab layout |
| `InGameCommandController.CharacterActions.cs` | character-related actions |
| `InGameCommandController.BossRush.cs` | Boss Rush controls |
| `InGameCommandController.Currency.cs` | money, keys, blanks, armor, health controls |
| `InGameCommandController.Room.cs` | room tools such as chest spawning |
| `InGameCommandController.PlayerStats.cs` | player stat panel |
| `InGameCommandController.PickupBrowser.cs` | item browser, filters, item cards, add/select modes |
| `InGameCommandController.LoadoutEditor.cs` | Start Items and preset editor UI |
| `InGameCommandController.About.cs` | About / Credits page |
| `InGameCommandController.Settings.cs` | settings page layout, keyboard key config, and first-stage controller navigation routing |

Supporting services:

| File | Look here for |
| --- | --- |
| `GrantCommandService*.cs` | command execution, pickup resolution, user-facing result messages |
| `PlayerDebugCommandService.cs` | player debug operations |
| `PlayerRuntimeOverrideServiceBase.cs` | shared skeleton for player runtime property override services |
| `PlayerHealthOverrideService.cs` | runtime max-health override tracking and rollback restoration |
| `RoomDebugCommandService.cs` | room-level debug operations such as spawning chests, Gunber Muncher (常规吃枪怪) / Evil Muncher (邪恶吃枪怪), map reveal, and teleporter-point promotion |
| `RapidFireToggleService.cs` | rapid fire toggle |
| `AutoReloadToggleService.cs` | auto reload toggle |
| `AmmoModeToggleService.cs` | ammo mode toggle and locked-ammo behavior |
| `InvincibilityToggleService.cs` | invincibility toggle |
| `FoyerCharacterSwitchService*.cs` | foyer character switching and unlock helpers |
| `src/RandomLoadout/Runtime/EtgFloorSceneResolver.cs` | floor token to ETG scene-name mapping, including special-floor exceptions such as Rat Den |

Read next:

- [Commands](./commands.md)
- [UI Icon Reuse](./ui-icon-reuse.md)
- [Muncher Spawn](./runtime-internals/muncher-spawn.md)
- [Map Reveal And Teleporter Promotion](./map-teleport.md)
- [Localization And Language Switching](./localization.md)
- [Runtime Property Overrides](../architecture/runtime-property-overrides.md)
- [Testing Matrix](./testing-matrix.md)

## Start Items, Config, And Presets

Use this route for changes to start-item rules, preset selection, add/remove, duplicate prevention, or config reload.

| File | Look here for |
| --- | --- |
| `src/RandomLoadout/Commands/InGameCommandController.LoadoutEditor.cs` | Start Items and preset UI |
| `src/RandomLoadout/Commands/LoadoutRuleEditorService*.cs` | editable rule entries, cache refresh, add/remove, preset operations |
| `src/RandomLoadout/Commands/LoadoutRuleEditorEntry.cs` | UI-facing rule row model |
| `src/RandomLoadout/Commands/LoadoutPresetEditorEntry.cs` | UI-facing preset row model |
| `src/RandomLoadout/Configuration/JsonLoadoutRuleFileProvider*.cs` | load, parse, save, convert, and preset persistence |
| `src/RandomLoadout/Configuration/LoadoutRuleFileModel.cs` | file model for rules/presets |
| `src/RandomLoadout/Configuration/DefaultLoadoutRuleDefinitionFactory.cs` | default rule fallback |
| `defaults/config/ETG-Gameplay-Dashboard.rules.json5` | shipped Start Items config anchor |
| `defaults/presets/*.json` | shipped built-in preset files |

Pure core types:

| File | Look here for |
| --- | --- |
| `src/RandomLoadout.Core/Configuration/LoadoutConfig.cs` | normalized loadout config |
| `src/RandomLoadout.Core/Configuration/LoadoutRuleConfig.cs` | normalized rule config |
| `src/RandomLoadout.Core/Selection/LoadoutSelectionService.cs` | selection and duplicate behavior |

Read next:

- [Config Format](./config-format.md)
- [UI Icon Reuse](./ui-icon-reuse.md)
- [Localization And Language Switching](./localization.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)
- [Pickups](./pickups.md)

Config notes:

- Built-in shipped presets now use `id` plus `display_name_key`
- User-authored presets use `id` plus optional plain `name`
- See [Config Format](./config-format.md) for the exact Start Items JSON shape

## Pickup Lookup, Browser, And Granting

Use this route for item names, aliases, pickup cards, quality filters, categories, grant behavior, or owned-item reading.

| File | Look here for |
| --- | --- |
| `src/RandomLoadout/Etg/EtgPickupResolver*.cs` | live pickup lookup, catalog lookup, aliases, category details |
| `src/RandomLoadout/Etg/EtgPickupGranter.cs` | actual grant behavior against the player |
| `src/RandomLoadout/Etg/EtgOwnedPickupReader.cs` | current player inventory reading |
| `src/RandomLoadout/Etg/EtgPickupCatalogExporter.cs` | exporting pickup metadata |
| `src/RandomLoadout/Runtime/NearbyPickupTipService.cs` | nearby dropped-pickup detection for gameplay overlay lookups |
| `src/RandomLoadout/Commands/InGameCommandController.PickupBrowser.cs` | browser filtering and item card display |
| `src/RandomLoadout/Configuration/JsonPickupAliasFileProvider.cs` | alias file loading |
| `src/RandomLoadout/Configuration/PickupAliasRegistry.cs` | alias lookup |
| `defaults/catalog/RandomLoadout.pickups.json` | shipped pickup catalog |
| `defaults/catalog/RandomLoadout.pickups.by-category.json` | shipped grouped pickup catalog |
| `RandomLoadout.pickup-names.game-language.json` in game config | compact exported pickup-name snapshot aligned to the current ETG runtime language |
| `defaults/catalog/RandomLoadout.pickup-gameplay.en.json` | shipped nearby-pickup gameplay catalog |
| `defaults/catalog/RandomLoadout.pickup-gameplay.zh-CN.work.json` | Simplified Chinese gameplay localization overlay/work file |
| `defaults/config/ETG-Gameplay-Dashboard.aliases.json5` | shipped aliases |

Read next:

- [ModTheGungeonAPI Reference](./modthegungeonapi.md)
- [UI Icon Reuse](./ui-icon-reuse.md)
- [Localization And Language Switching](./localization.md)
- [Pickups](./pickups.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)

## Runtime Toggles And Gameplay Features

Use this route for toggles that change live player, gun, or run behavior.

| Feature | Start files |
| --- | --- |
| Rapid fire | `src/RandomLoadout/Commands/RapidFireToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Auto reload | `src/RandomLoadout/Commands/AutoReloadToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Ammo mode | `src/RandomLoadout/Commands/AmmoModeToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Invincibility | `src/RandomLoadout/Commands/InvincibilityToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Runtime property overrides | `src/RandomLoadout/Commands/PlayerRuntimeOverrideServiceBase.cs`, `src/RandomLoadout/Commands/PlayerHealthOverrideService.cs`, `src/RandomLoadout/Plugin.RunLifecycle.cs` |
| Player stats panel | `src/RandomLoadout/Commands/InGameCommandController.PlayerStats.cs` |
| Ammonomicon / game UI actions | `src/RandomLoadout/Commands/InGameCommandController.CommandActions.cs` |

Read next:

- [Runtime Hotspots](../architecture/runtime-hotspots.md)
- [Runtime Property Overrides](../architecture/runtime-property-overrides.md)
- [Logging](../operations/logging.md)

## Boss Rush And Character Flow

| Feature | Start files |
| --- | --- |
| Boss Rush UI | `src/RandomLoadout/Commands/InGameCommandController.BossRush.cs` |
| Boss Rush runtime | `src/RandomLoadout/Runtime/BossRushService*.cs`, `BossRushHooks.cs`, `BossRushState.cs` |
| Character switching | `src/RandomLoadout/Commands/FoyerCharacterSwitchService*.cs`, `InGameCommandController.CharacterPage.cs` |

Read next:

- [Runtime Hotspots](../architecture/runtime-hotspots.md)
- [Character Switch Strategy](../decisions/character-switch-strategy.md)
- [Smoke Checklist](../operations/smoke-checklist.md)

## Localization And User-Facing Text

| File | Look here for |
| --- | --- |
| `src/RandomLoadout/Localization/GuiText.cs` | language setting, lookup, fallback |
| `src/RandomLoadout/Etg/EtgPickupResolver*.cs` | runtime-localized pickup names and English pickup-name fallback |
| `src/RandomLoadout/Commands/InGameCommandController.cs` | language-change detection and page refresh |
| `defaults/config/ETG-Gameplay-Dashboard.localization.en.json5` | English UI strings |
| `defaults/config/ETG-Gameplay-Dashboard.localization.zh-CN.json5` | Simplified Chinese UI strings |
| `src/RandomLoadout/Commands/InGameCommandController.CommandPage.cs` | language button location |

Read next:

- [Localization And Language Switching](./localization.md)
- [Commands](./commands.md)
- [Config Format](./config-format.md)

After editing localization defaults, copy only localization files into the live game config when needed. Do not overwrite all live config.

## Build, Deploy, Release, And Logs

| Goal | Start here |
| --- | --- |
| Build or test locally | `tools/build/`, [Development Setup](../getting-started/development-setup.md) |
| Deploy into ETG | `tools/deploy/`, [Deploy](../operations/deploy.md) |
| Package release | `tools/release/`, [Release Package](../operations/release-package.md) |
| Read runtime logs | [Logging](../operations/logging.md) |
| Command notes used by the owner | `docs/notes/cmd.md` |

Common checks:

- `python .\tools\devtools\check_naming.py --verbose`
- `python .\tools\build\build.py --configuration Debug`
- `python .\tools\build\build.py --configuration Release` for packaging/release changes

## Tests

| Test file | Covers |
| --- | --- |
| `tests/RandomLoadout.Core.Tests/GrantCommandParserTests.cs` | command parsing |
| `tests/RandomLoadout.Core.Tests/LoadoutSelectionServiceTests.cs` | selection behavior |
| `tests/RandomLoadout.Core.Tests/RuleFileProviderTests.cs` | rules and presets persistence behavior |
| `tests/RandomLoadout.Core.Tests/AliasRegistryTests.cs` | alias lookup |

Read next:

- [Testing Matrix](./testing-matrix.md)
