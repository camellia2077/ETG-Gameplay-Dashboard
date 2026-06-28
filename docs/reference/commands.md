# Commands

Use this page when you need the current in-game command panel behavior, supported actions, and developer notes for changing that surface.

## Must Read First

Before changing command UI, pickup grant behavior, Boss Rush entry flow, or character-select-hub UI actions, read:

1. [Start Here](../getting-started/start-here.md)
2. [Terminology And Naming](./terminology.md)
3. [Runtime Hotspots](../architecture/runtime-hotspots.md)
4. [Testing Matrix](./testing-matrix.md)

## Player-Facing Behavior

### Command Panel

- Press the configured command-panel key to open or close the command panel. The default is `F7`; change `[UI] CommandPanelKey` in `randomgun.randomloadout.cfg` to another Unity `KeyCode` name such as `F8`, `Insert`, or `BackQuote`.
- The panel also supports opening from a 360 controller by pressing `R3`.
- The panel is positioned near the bottom center of the screen to avoid the top-left HUD.
- The UI uses a darker ETG-friendly color scheme with clearer text sizing.
- The command page can show a separate side stats panel with current player vitals and combat stats from `HealthHaver` and `PlayerStats`.
- Opening the panel does not automatically focus the command input field. Click the input field before typing a command.

### Current Gamepad Support

- Opening the command panel is supported from 360 controller `R3` short press.
- The `Settings` page no longer exposes a gamepad-open binding selector.
- Basic gamepad navigation currently exists only on the `Command` and `Settings` pages.
- On those two pages, the current intended control scheme is:
  - d-pad style horizontal/vertical input moves focus
  - `A` confirms the focused button
  - `B` goes back or closes the current page
  - `LB` switches categories on the main `Command` page
- Text-entry interactions are still keyboard/mouse-first. Opening the panel from a gamepad does not make the command input field or pickup-browser search field practical to use from a controller yet.
- Subpages that rely on larger lists or more complex layouts, such as pickup browsing and editor-style pages, do not yet have full gamepad navigation support.

### Supported Typed Commands

- `<name>`
- `<alias>`
- `<id>`
- `gun <name>`
- `gun <alias>`
- `gun <id>`
- `passive <name>`
- `passive <alias>`
- `passive <id>`
- `active <name>`
- `active <alias>`
- `active <id>`
- `item <name>`
- `item <alias>`
- `item <id>`

Lookup behavior:

- resolution order is `id -> alias -> internalName -> displayName`
- all lookup inputs are case-insensitive
- if the value starts with a number, the leading number is treated as `pickupId`
- if no known target prefix is present, the input is treated as `item` / `any`

Recommended input style:

- prefer internal names such as `platinumbullets`
- use aliases when you want stable shorthand
- use display names only as a compatibility fallback

### Main Buttons

- `Grant`
  Executes the typed command.
- `Random`
  Grants one random supported pickup without requiring text input.
- `Boss Rush`
  Opens the Boss Rush page.
- `Pickups`
  Opens the in-game pickup browser with search, category filters, and runtime sprite icons.
- `Start Items`
  Opens the start-items editor for the current rules file.
- `Rapid OFF` / `Rapid ON`
  Toggles hold-to-rapid-fire mode for the current gun.
- `Reload OFF` / `Reload Fast` / `Reload Anim`
  Cycles automatic reload between off, instant reload, and vanilla animated reload when the current gun's clip is empty and ammo is available.
- `Stats OFF` / `Stats ON`
  Toggles the side player-stats panel.
- `God OFF` / `God ON`
  Toggles invincibility. When enabled, the player is kept non-vulnerable and protected from touch damage, pits, and status effects; disabling restores the values captured when the toggle was enabled.
- `Lang Auto` / `Lang EN` / `Lang CN`
  Cycles the command panel language preference through `auto`, `en`, and `zh-CN`, then persists it to `randomgun.randomloadout.cfg` under `[UI] Language`.
- `Settings`
  Opens panel preferences. The current settings page includes keyboard key selection, UI size, language, and experimental-mode controls.
- `Currency`
  Opens the resource-actions submenu.
- `Room`
  Opens the room-tools submenu.
- `Teleport`
  Opens a separate left-side floor picker. Each row loads one configured floor through the same vanilla level-load route as the console `load_level` flow.
- `Reveal Map`
  Runs the current-floor minimap reveal pass and promotes teleporter-capable rooms toward a usable teleport state. This is not the same thing as reproducing the exact vanilla minimap-discovery visuals for every room.

### Pickup Browser

- search matches `alias`, `internalName`, `displayName`, and `pickupId`
- in English UI, visible pickup names should prefer the catalog's `EnglishDisplayName`; search still includes both
  localized and English pickup names
- category filters support `All`, `Gun`, `Passive`, and `Active`
- quality filters are ordered `S`, `A`, `B`, `C`, `D`, with `Special`, `Excluded`, and all-quality options available
- when the `Gun` category is selected, gun-class filters support pistol, full-auto, shotgun, rifle, beam, charge, explosive, elemental, and special buckets
- when the `Passive` category is selected, passive subcategory filters support bullet-related items
- when the `Active` category is selected, cooldown filters support uses, damage, time, and room buckets
- clicking a result row or its `Grant` button grants the selected pickup directly
- clicking `Add` writes the selected pickup as a `specific` ID rule in the current start-items rules file
- duplicate `specific` ID rules are blocked by the in-game editor
- icons are reused from the game's live pickup sprites

### Start Items Editor

The Start Items editor opens on a preset preview list:

- clicking a preset row or `Open` selects that preset and opens its detail page
- clicking `Select` only selects that preset; it does not open the detail page
- preset rows show total, specific, and random rule counts
- `Add Item` / `添加物品`
  Available on the preset detail page. Opens the pickup browser in Start Items add mode. In this mode, clicking a row or `Add` writes the selected pickup to the active preset instead of granting it to the current run.
- `Reload Config` / `重新读取配置`
  Invalidates the cached resolved start-items config and manually re-reads the current rules file from disk on the next automatic grant.
- `New` / `新建`
  Creates an empty preset with an auto-generated unique name such as `preset` / `preset-2` in English or `预设` / `预设-2` in Chinese, selects it, and saves it as a separate preset file under `BepInEx\config\presets\`.
- `Copy` / `复制`
  Duplicates the active preset's rules into a new uniquely named preset, selects it, and saves it as a separate preset file under `BepInEx\config\presets\`.
- `Delete` / `删除`
  Deletes the active preset, then selects the next available preset. Deleting the only preset is blocked.
- `Rename` / `改名`
  Available on the preset preview page. Renames the active preset, updates `[StartItems] ActivePreset`, and blocks empty or duplicate names.
- in Chinese UI, the default `default` preset is renamed to `预设`; new presets then start from `预设-2` when `预设` already exists
- `Remove`
  Deletes the selected rule from the current active preset by its current row index.
- `Add` and `Remove` edit only the current active preset file under `BepInEx\config\presets\`
- rows include resolved pickup metadata such as quality, gun class, and active cooldown where available
- preset titles and preset item names use different localization paths: built-in preset titles come from
  `display_name_key`, while item rows are resolved from the live pickup catalog
- `Add` and `Remove` automatically invalidate the cached config and refresh the in-game editor list; the manual reload button is kept for edits made outside the game
- changes affect the next automatic start-of-run grant; they do not immediately grant or remove items in the current run

### Currency Menu

In the `Currency` submenu:

- `+1 Key`
  Adds one key to the current player.
- `+1 Rat Key`
  Adds one rat key to the current player.
- `+50 Casings`
  Adds 50 casings to the current player.
- `+10 Hegemony`
  Adds 10 meta currency via `TrackedStats.META_CURRENCY`.

### Room Menu

In the `Room` submenu:

- `Enemies -> Refresh Room Enemies`
  Replays the current room's predefined enemy setup after the room has been cleared. This v1 behavior is backed by the room reset path and does not guarantee the exact same formation, positions, or reinforcement timing as the first room entry.
- `Spawn Gunber Muncher`
  Spawns the vanilla Gunber Muncher (常规吃枪怪) actor directly into the current room. This is a custom runtime spawn path, not a full recreation of the original muncher room.
- `Spawn Evil Muncher`
  Spawns the vanilla Evil Muncher (邪恶吃枪怪) actor directly into the current room. This follows the same non-standard runtime integration as Gunber Muncher, but resolves from a different vanilla asset bundle.
- chest tier buttons
  Selects the chest tier for the next spawn. Current v1 options are `Brown`, `Blue`, `Green`, `Red`, `Black`, `Synergy`, and `Rainbow`.
- `Spawn Chest`
  Spawns one unlocked chest of the selected tier in the current room near the player.

### Teleport Picker

The left-side `Teleport` picker loads these floors:

- `load_level keep`: Keep / Chamber 1
- `load_level oubliette`: Oubliette / Chamber 1.5 / Sewer
- `load_level proper`: Proper / Chamber 2
- `load_level abbey`: Abbey / Chamber 2.5 / Old King
- `load_level mine`: Mines / Chamber 3
- `load_level ratden`: Rat Den / Chamber 3.5
- `load_level hollow`: Hollow / Chamber 4
- `load_level R&G_Dept`: R&G Dept / Chamber 4.5
- `load_level forge`: Forge / Chamber 5
- `load_level heli`: Bullet Hell / Chamber 6

The picker refuses normal teleports while Boss Rush is active so the Boss Rush state machine does not conflict with manual floor loads.

Implementation notes:

- Gamepad shortcut detection, preset mapping, and basic controller navigation state currently live in `src/RandomLoadout/Commands/InGameCommandController.cs` and `src/RandomLoadout/Commands/InGameCommandController.State.cs`.
- Main command-page controller focus and button routing currently live in `src/RandomLoadout/Commands/InGameCommandController.CommandPage.cs`.
- Settings-page controller focus and preset controls currently live in `src/RandomLoadout/Commands/InGameCommandController.Settings.cs`.
- Teleport buttons live in `src/RandomLoadout/Commands/InGameCommandController.Teleport.cs`.
- Floor-token-to-scene mapping lives in `src/RandomLoadout/Runtime/EtgFloorSceneResolver.cs`.
- The flow intentionally follows the same high-level route as upstream `load_level` behavior:
  `Foyer.Instance.OnDepartedFoyer()` first when leaving the Breach, then `GameManager.Instance.LoadCustomLevel(sceneName)`.
- Do not assume every floor uses a simple `tt_` scene prefix. The Rat Den is the important exception in the current picker:
  `load_level ratden` must resolve to `ss_resourcefulrat`, not `tt_resourcefulrat`.
- The Rat Den mapping was verified against upstream `Load-Level` / `ETGModConsole` behavior. Using the wrong scene name can appear to "work" while actually loading the wrong floor or failing mid-transition.
- When changing teleport mappings, always verify the actual destination in-game and re-check the BepInEx log after testing.

### Reveal Map

The `General` page now also exposes `Reveal Map`.

High-level behavior:

- `Reveal Map`
  Pushes the current floor through the minimap reveal path and also promotes already-registered teleporter rooms toward a usable teleport state.

Important caveat:

- `Reveal Map` reflects the gameplay result more than the exact minimap presentation. A room can become teleport-usable without fully matching the natural "player walked into this room" minimap state.

Implementation reference:

- [Map Reveal And Teleporter Promotion](./map-teleport.md)

### Boss Rush Page

On the `Boss Rush` page:

- `Start Boss Rush`
  Starts an independent boss-rush run from the character-select hub.
- `Return to Character Select`
  Aborts an active Boss Rush and returns to character select.

Current Boss Rush v1 behavior:

- starts only in the character-select hub
- uses the fixed floor order `Keep -> Proper -> Mines -> Hollow -> Forge -> Hell`
- loads each vanilla floor, then routes the player toward the boss encounter
- waits for a boss reward claim before loading the next floor
- returns to character select on death or after clearing Hell

### Character Page Modes

In the `Characters` page:

- `Mode: Unlock`
  Tries to unlock the clicked hidden character in save data.
  `Robot` is excluded from unlock mode in this panel.
- `Mode: Switch Only`
  Performs immediate character switching without writing unlock flags.

### Configurable Start Loadout

The automatic start-of-run loadout uses:

- `randomgun.randomloadout.cfg`
  simple on/off switches
- `ETG-Gameplay-Dashboard.rules.json5`
  Start Items config anchor and fallback entry point
- `ETG-Gameplay-Dashboard.aliases.json5`
  shared alias definitions for rules and commands

Current minimum behavior:

- `EnableRandomLoadout` controls whether automatic start-of-run grants happen
- rules support:
  - `random`
  - `specific`
- the in-game loadout editor writes preset edits back into the active file under `BepInEx\config\presets\`
- `specific` rules support:
  - `name`
  - `alias`
  - `id`
- `random` rules support:
  - `pool`
  - `poolAliases`
  - `poolIds`

Resolution priority:

- `specific`: `id -> alias -> internalName -> displayName`
- `random`: `poolIds -> poolAliases -> pool`

## Developer Notes

### Implementation References

- [ModTheGungeonAPI Reference](./modthegungeonapi.md)
- [Muncher Spawn](./runtime-internals/muncher-spawn.md)
- [Localization And Language Switching](./localization.md)
- [Pickup Grant Strategy](../decisions/pickup-grant-strategy.md)
- [Character Switch Strategy](../decisions/character-switch-strategy.md)

### Runtime Notes

- command execution logs are tagged with `[RandomLoadout][Command]`
- the command panel is intentionally a compact debug and experimentation surface, not a full console
- Boss Rush entry and return actions touch ETG runtime hotspots and should be treated as manual-verify areas
- invincibility uses ETG runtime damage flags (`HealthHaver.IsVulnerable`, `HealthHaver.PreventAllDamage`, and player immunity flags), so verify it in combat before release
- character-select-hub actions must not assume scene token meaning equals gameplay-state meaning

### After Editing This Surface

At minimum:

- run the checks from [Testing Matrix](./testing-matrix.md)
- if runtime behavior changed, run [Smoke Checklist](../operations/smoke-checklist.md)
- review [Logging](../operations/logging.md) after testing

## Read Next

- Runtime terminology:
  [./terminology.md](./terminology.md)
- Runtime risk areas:
  [../architecture/runtime-hotspots.md](../architecture/runtime-hotspots.md)
- Testing expectations:
  [./testing-matrix.md](./testing-matrix.md)
