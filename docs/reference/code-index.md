# Code Index

Use this page as the first code navigation map for a new agent. It answers:

- what code owns a feature area
- what docs to read before editing it
- what checks usually matter after the edit

Keep this page index-like. Put long explanations in the linked docs.

Project naming and the `EtgGameplayDashboard` to `EtgGameplayDashboard` migration are
defined in [Project Naming](./project-naming.md).

## First Stop

Before editing source:

1. Read [Start Here](../getting-started/start-here.md).
2. Read [Source Guide](../../src/AGENTS.md).
3. Use the task map below to find the owning files.

If a feature touches ETG runtime behavior and the API is not already used in this repository, verify the member in referenced assemblies or decompiled sources before coding.

## Repository Areas

| Area | Owns | Start here |
| --- | --- | --- |
| `src/EtgGameplayDashboard/` | BepInEx plugin, Unity/ETG integration, runtime services, IMGUI command panel | [Source Guide](../../src/AGENTS.md) |
| `src/EtgGameplayDashboard.Core/` | Pure parsing, config models, selection, warnings, seed behavior | [System Overview](../architecture/system-overview.md) |
| `tests/EtgGameplayDashboard.Core.Tests/` | Core parser, rule, and selection checks | [Testing Matrix](./testing-matrix.md) |
| `defaults/config/` and `defaults/presets/` | Shipped config, localization, catalog baselines, and built-in preset files | [Config Format](./config-format.md) |
| `tools/` | Build, deploy, release, generated docs, developer utilities | [Development Setup](../getting-started/development-setup.md) |
| `docs/` | Project knowledge, workflows, decisions, references | [Docs README](../README.md) |

## Runtime Entry Points

Start with these when tracing how the mod enters the game:

| File | Look here for |
| --- | --- |
| `src/EtgGameplayDashboard/Plugin.cs` | BepInEx plugin shell and root ownership |
| `src/EtgGameplayDashboard/Plugin.Bootstrap.cs` | plugin startup orchestration and API bootstrap |
| `src/EtgGameplayDashboard/Plugin.Bootstrap.Setup.cs` | service construction and game-manager startup wiring |
| `src/EtgGameplayDashboard/PluginConfigurationFacade.cs` | configuration binding, normalization, and persistence callbacks |
| `src/EtgGameplayDashboard/Plugin.Lifecycle.cs` | plugin lifecycle callbacks and teardown coordination |
| `src/EtgGameplayDashboard/Plugin.RunLifecycle.cs` | run observation, automatic start-item grants, runtime fallback updates, and deferred floor teleport state |
| `src/EtgGameplayDashboard/Plugin.CatalogExport.cs` | runtime pickup catalog export |
| `src/EtgGameplayDashboard/Plugin.State.cs` | plugin-level mutable state |
| `src/EtgGameplayDashboard/Runtime/RunLifecycleTracker.cs` | run-start detection rules |
| `src/EtgGameplayDashboard/Runtime/RunSceneWatcher.cs` | scene readiness observation |

Read next:

- [Runtime Hotspots](../architecture/runtime-hotspots.md)
- [Logging](../operations/logging.md)

## Command Panel And UI

All in-game command-panel work starts in `src/EtgGameplayDashboard/Commands/`.

| File | Look here for |
| --- | --- |
| `InGameCommandController.cs` | controller shell and page dispatch |
| `InGameCommandController.Navigation.cs` | shared controller navigation routing across command-panel pages |
| `InGameCommandController.State.cs` | UI state, selected page/category, controller-focus state, dimensions, colors, cached data |
| `InGameCommandController.Styles.cs` | IMGUI styles |
| `InGameCommandController.CommandPage.cs` | main command page layout, top-level controls, and first-stage controller navigation routing |
| `InGameCommandController.CommandActions.cs` | general command button actions, including `Reveal Map` and its teleporter-promotion path |
| `InGameCommandController.Teleport.cs` | teleport picker UI, floor resolution, and `load_level` runtime handoff |
| `InGameCommandController.CharacterPage.cs` | character tab layout |
| `InGameCommandController.CharacterActions.cs` | character-related actions |
| `InGameCommandController.BossRush.cs` | Boss Rush controls |
| `InGameCommandController.Currency.cs` | money, keys, blanks, armor, health controls |
| `InGameCommandController.CursorColor.cs` | combat cursor color page, enable/disable button, color selection, and controller navigation |
| `InGameCommandController.Room.cs` | room tools such as chest spawning |
| `InGameCommandController.Room.BossSelection.cs` | Boss-room option enumeration, selection state, and controller focus for Boss selection |
| `InGameCommandController.PlayerStats.cs` | player stat panel |
| `InGameCommandController.PickupBrowser.cs` | item-browser page rendering, filter controls, item cards, and add/select actions |
| `InGameCommandController.PickupBrowser.Navigation.cs` | pickup-browser focus movement, filter navigation, and controller selection actions |
| `InGameCommandController.LoadoutEditor.cs` | Start Items and preset editor page rendering and actions |
| `InGameCommandController.LoadoutEditor.Navigation.cs` | loadout-editor focus movement and controller selection actions |
| `InGameCommandController.InputDiagnostics.cs` | command-panel keyboard/controller input diagnostics |
| `InGameCommandController.MapDiagnostics.cs` | command-panel map and teleporter diagnostic sampling |
| `InGameCommandController.PickupIcons.cs` | shared runtime pickup and GameUI atlas icon resolution for command pages and loadout views |
| `InGameCommandController.About.cs` | About / Credits page |
| `InGameCommandController.Settings.cs` | settings page layout, keyboard key config, and first-stage controller navigation routing |
| `Ui/DashboardTheme.cs` | centralized theme palettes and command-panel, category-button, disabled-state, pickup-info, and scroll-bar colors |
| `Ui/DashboardThemeCatalog.cs` | stable theme IDs and localized theme display names |

Supporting services:

| File | Look here for |
| --- | --- |
| `GrantCommandService*.cs` | command execution, pickup resolution, user-facing result messages |
| `Commands/PickupBrowserQueryService.cs` | pickup catalog transformation, alias indexing, sorting, search, and browser filters |
| `Commands/PickupBrowserEntry.cs` | browser-facing pickup row model and searchable display metadata |
| `Commands/ControllerFocusNavigator.cs` | shared directional focus movement across command-panel pages |
| `Commands/MapFeatureRuntimeCoordinator.cs` | floor-scoped map reveal/direct-teleport activation and automatic-reveal lifecycle |
| `Commands/LoadoutEditorDataCoordinator.cs` | synchronized loadout editor read models after preset/rule edits |
| `Commands/LoadoutEditorState.cs` | mutable Loadout editor workflow state, edit fields, selected rule, and scroll positions |
| `Commands/CommandPanelLifecycleCoordinator.cs` | command-panel player-input cleanup and deferred GUI-focus release |
| `CombatCursorColorCatalog.cs` | combat cursor color IDs, target HEX values, Unity colors, and normalization |
| `Runtime/CommandPanelCursorRenderHooks.cs` | ETG cursor suppression, panel-layer redraw, and custom cursor color material rendering |
| `Runtime/RoomEnemyReplayHooks.cs` | Harmony entry points for room entry, reinforcement capture, and replay-wave insertion |
| `Runtime/RoomEnemyReplayService.cs` | room-replay coordination, snapshot lifecycle, wave sequencing, Boss reward re-arming, and runtime hook-facing state |
| `Runtime/RoomEnemyReplayService.Diagnostics.cs` | room map/teleporter diagnostics, replay verification, and deferred Boss sprite/intro diagnostics |
| `Runtime/RoomRewindCleanupService.cs` | room-scoped projectile, corpse, VFX, debris, pickup, and Boss reward-pedestal cleanup |
| `Runtime/BossRoomDecorationRestorer.cs` | Boss-room decoration capture, prototype/template resolution, and destructible restoration |
| `Runtime/RoomPlayerStateRestorer.cs` | player health, stats, inventory, gun, passive-item, and active-item snapshot restoration |
| `Runtime/RoomReplayStateModels.cs` | replay snapshot, enemy-entry, and Boss-room decoration state models |
| `Runtime/RoomEnemyWaveSpawner.cs` | recorded enemy-wave instantiation and replayed Boss visibility restoration |
| `Runtime/RoomReplayWaveDiagnostics.cs` | replay-wave comparison, counting, and diagnostic formatting |
| `Runtime/PrivateFieldAccessor.cs` | shared ETG private-field reads and writes used by state restoration |
| `PlayerDebugCommandService.cs` | player debug operations |
| `PlayerRuntimeOverrideServiceBase.cs` | shared skeleton for player runtime property override services |
| `PlayerHealthOverrideService.cs` | runtime max-health override tracking and rollback restoration |
| `RoomDebugCommandService.cs` | room-level debug operations such as spawning chests, Gunber Muncher (常规吃枪怪) / Evil Muncher (邪恶吃枪怪), map reveal, and teleporter-point promotion |
| `RoomDebugCommandService.RoomReplay.cs` | room enemy rewind/respawn commands, room validation, player rewind toggles, and replay-specific result diagnostics |
| `RoomDebugCommandService.Muncher.cs` | Gunber Muncher / Evil Muncher spawn queues, prefab and room-asset resolution, placement, registration, and diagnostics |
| `RoomDebugCommandService.Npc.cs` | Persistent Breach NPC unlocks using the vanilla foyer-visibility flags |
| `RoomDebugCommandService.BossSelection.cs` | Boss-room prototype enumeration, Boss-name resolution, Boss selection, and Boss-selection caching/diagnostics |
| `RoomDebugCommandService.Map.cs` | floor map reveal, minimap teleporter activation, direct-teleport room promotion, and map diagnostics |
| `RoomDebugCommandService.Helpers.cs` | shared room/object descriptions, scene and prototype resolution, result localization, and debug logging helpers |
| `RapidFireToggleService.cs` | rapid fire toggle |
| `PlayerRuntimeStatOverrideService.cs` | persistent player Damage, Movement, Coolness, and Curse runtime overrides |
| `ProjectileModifierService.cs` | persistent projectile size, speed, reload-speed, and accuracy overrides |
| `AutoReloadToggleService.cs` | auto reload toggle |
| `AmmoModeToggleService.cs` | ammo mode toggle and locked-ammo behavior |
| `InvincibilityToggleService.cs` | invincibility toggle |
| `FoyerCharacterSwitchService*.cs` | foyer character switching and unlock helpers |
| `src/EtgGameplayDashboard/Runtime/EtgFloorSceneResolver.cs` | floor token to ETG scene-name mapping, including special-floor exceptions such as Rat Den |

Read next:

- [Commands](./commands.md)
- [Dashboard UI Theme](./ui-theme.md)
- [UI Icon Reuse](./ui-icon-reuse.md)
- [Items 页面性能优化](../operations/performance-items.md)
- [Muncher Spawn](./runtime-internals/muncher-spawn.md)
- [Map Reveal And Teleporter Promotion](./map-teleport.md)
- [Localization And Language Switching](./localization.md)
- [Runtime Property Overrides](../architecture/runtime-property-overrides.md)
- [Boss Room Rewind](../architecture/boss-room-rewind.md)
- [Testing Matrix](./testing-matrix.md)

## Start Items, Config, And Presets

Use this route for changes to start-item rules, preset selection, add/remove, duplicate prevention, or config reload.

| File | Look here for |
| --- | --- |
| `src/EtgGameplayDashboard/Commands/InGameCommandController.LoadoutEditor.cs` | Start Items and preset UI |
| `src/EtgGameplayDashboard/Commands/LoadoutRuleEditorService*.cs` | editable rule entries, cache refresh, add/remove, preset operations |
| `src/EtgGameplayDashboard/Commands/LoadoutRuleEditorEntry.cs` | UI-facing rule row model |
| `src/EtgGameplayDashboard/Commands/LoadoutPresetEditorEntry.cs` | UI-facing preset row model |
| `src/EtgGameplayDashboard/Configuration/JsonLoadoutRuleFileProvider*.cs` | load, parse, save, convert, and preset persistence |
| `src/EtgGameplayDashboard/Configuration/LoadoutRuleFileModel.cs` | file model for rules/presets |
| `src/EtgGameplayDashboard/Configuration/DefaultLoadoutRuleDefinitionFactory.cs` | default rule fallback |
| `defaults/config/EtgGameplayDashboard.rules.json5` | shipped Start Items config anchor |
| `defaults/presets/*.json` | shipped built-in preset files |

Pure core types:

| File | Look here for |
| --- | --- |
| `src/EtgGameplayDashboard.Core/Configuration/LoadoutConfig.cs` | normalized loadout config |
| `src/EtgGameplayDashboard.Core/Configuration/LoadoutRuleConfig.cs` | normalized rule config |
| `src/EtgGameplayDashboard.Core/Selection/LoadoutSelectionService.cs` | selection and duplicate behavior |

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
| `src/EtgGameplayDashboard/Etg/EtgPickupResolver*.cs` | live pickup lookup, catalog lookup, aliases, category details |
| `src/EtgGameplayDashboard/Etg/EtgPickupGranter.cs` | actual grant behavior against the player |
| `src/EtgGameplayDashboard/Etg/EtgOwnedPickupReader.cs` | current player inventory reading |
| `src/EtgGameplayDashboard/Etg/EtgPickupCatalogExporter.cs` | exporting pickup metadata |
| `src/EtgGameplayDashboard/Runtime/NearbyPickupTipService.cs` | nearby dropped-pickup detection for gameplay overlay lookups |
| `src/EtgGameplayDashboard/Etg/EtgPickupResolver.Catalog.cs` | Breach NPC shop display resolution through live foyer stock slots, plus compatibility handling for other meta-shop controllers |
| `src/EtgGameplayDashboard/Commands/InGameCommandController.PickupBrowser.cs` | browser filtering and item card display |
| `src/EtgGameplayDashboard/Configuration/JsonPickupAliasFileProvider.cs` | alias file loading |
| `src/EtgGameplayDashboard/Configuration/PickupAliasRegistry.cs` | alias lookup |
| `defaults/catalog/EtgGameplayDashboard.pickups.json` | shipped pickup catalog |
| `defaults/catalog/EtgGameplayDashboard.pickups.by-category.json` | shipped grouped pickup catalog |
| `EtgGameplayDashboard.pickup-names.game-language.json` in game config | compact exported pickup-name snapshot aligned to the current ETG runtime language |
| `defaults/catalog/EtgGameplayDashboard.pickup-gameplay.json` | shipped nearby-pickup gameplay runtime catalog (schema v2) |
| `defaults/catalog/EtgGameplayDashboard.pickup-info-terms.json` | shipped nearby-pickup section/stat/display-value terms (schema v2) |
| `defaults/catalog/EtgGameplayDashboard.boss-names.json` | extracted Boss room names and English/Simplified Chinese display text keyed by vanilla room prototype |
| `defaults/config/EtgGameplayDashboard.aliases.json5` | shipped aliases |

Read next:

- [UI Icon Reuse](./ui-icon-reuse.md)
- [Localization And Language Switching](./localization.md)
- [Pickups](./pickups.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)

## Runtime Toggles And Gameplay Features

Controller aim-lock implementation details are documented in [Controller Aim Lock](./controller-aim-lock.md).

Use this route for toggles that change live player, gun, or run behavior.

| Feature | Start files |
| --- | --- |
| Rapid fire | `src/EtgGameplayDashboard/Commands/RapidFireToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Auto reload | `src/EtgGameplayDashboard/Commands/AutoReloadToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Ammo mode | `src/EtgGameplayDashboard/Commands/AmmoModeToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Invincibility | `src/EtgGameplayDashboard/Commands/InvincibilityToggleService.cs`, `InGameCommandController.CommandActions.cs` |
| Runtime property overrides | `src/EtgGameplayDashboard/Commands/PlayerRuntimeOverrideServiceBase.cs`, `src/EtgGameplayDashboard/Commands/PlayerHealthOverrideService.cs`, `src/EtgGameplayDashboard/Plugin.RunLifecycle.cs` |
| Player stats panel | `src/EtgGameplayDashboard/Commands/InGameCommandController.PlayerStats.cs` |
| Ammonomicon / game UI actions | `src/EtgGameplayDashboard/Commands/InGameCommandController.CommandActions.cs` |

Read next:

- [Runtime Hotspots](../architecture/runtime-hotspots.md)
- [Runtime Property Overrides](../architecture/runtime-property-overrides.md)
- [Logging](../operations/logging.md)

## Boss Rush And Character Flow

| Feature | Start files |
| --- | --- |
| Boss Rush UI | `src/EtgGameplayDashboard/Commands/InGameCommandController.BossRush.cs` |
| Boss Rush runtime | `src/EtgGameplayDashboard/Runtime/BossRushService*.cs`, `BossRushHooks.cs`, `BossRushState.cs` |
| Character switching | `src/EtgGameplayDashboard/Commands/FoyerCharacterSwitchService*.cs`, `InGameCommandController.CharacterPage.cs` |

Read next:

- [Runtime Hotspots](../architecture/runtime-hotspots.md)
- [Character Switch Strategy](../decisions/character-switch-strategy.md)
- [Smoke Checklist](../operations/smoke-checklist.md)

## Localization And User-Facing Text

| File | Look here for |
| --- | --- |
| `src/EtgGameplayDashboard/Localization/GuiText.cs` | language setting, lookup, fallback |
| `src/EtgGameplayDashboard/Etg/EtgPickupResolver*.cs` | runtime-localized pickup names and English pickup-name fallback |
| `src/EtgGameplayDashboard/Commands/InGameCommandController.cs` | language-change detection and page refresh |
| `defaults/config/EtgGameplayDashboard.localization.en.json5` | English UI strings |
| `defaults/config/EtgGameplayDashboard.localization.zh-CN.json5` | Simplified Chinese UI strings |
| `src/EtgGameplayDashboard/Commands/InGameCommandController.CommandPage.cs` | language button location |

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
| `tests/EtgGameplayDashboard.Core.Tests/GrantCommandParserTests.cs` | command parsing |
| `tests/EtgGameplayDashboard.Core.Tests/LoadoutSelectionServiceTests.cs` | selection behavior |
| `tests/EtgGameplayDashboard.Core.Tests/RuleFileProviderTests.cs` | rules and presets persistence behavior |
| `tests/EtgGameplayDashboard.Core.Tests/AliasRegistryTests.cs` | alias lookup |

Read next:

- [Testing Matrix](./testing-matrix.md)
