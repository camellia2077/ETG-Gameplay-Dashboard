# Config Format (JSON5)

`RandomLoadout` configuration uses `json5` files:

- `ETG-Gameplay-Dashboard.rules.json5`
- `ETG-Gameplay-Dashboard.aliases.json5`
- `ETG-Gameplay-Dashboard.localization.en.json5`
- `ETG-Gameplay-Dashboard.localization.zh-CN.json5`
- `RandomLoadout.rules.full-pool.json5`

Start Items presets are stored as one file per preset under `presets/`, for example:

- `presets/preset.default.json`
- `presets/preset.casey_synergies.json`
- `presets/preset.border-collie.json`

The mod also maintains `RandomLoadout.selection-state.json5` automatically. This file stores shuffled random-pool
orders and the next index for each active preset id so random start-item pools cycle through a shuffled order across
runs. It is runtime state, not a user-authored rules file.

## Catalog And Web-Derived Data

Repository-shipped catalog snapshots live under `defaults/catalog/`.

Use this directory for runtime-ready, normalized data files that the mod may deploy and read directly, for example:

- `RandomLoadout.pickups.json`
- `RandomLoadout.pickups.by-category.json`
- `RandomLoadout.pickup-gameplay.json`
- `RandomLoadout.pickup-info-terms.json`

For web-derived content such as wiki descriptions, store only the runtime-ready JSON snapshot here. Do not store raw
HTML pages or full webpage dumps in `defaults/catalog/`.

Preferred pattern:

- `tools/data/`: generator scripts and optional scrape/cache helpers
- `defaults/catalog/`: generated, compact JSON that is safe for runtime use and repository distribution

For gameplay-focused nearby-pickup info, runtime data now uses schema v2:

- `RandomLoadout.pickup-gameplay.json` stores the runtime pickup facts
- `RandomLoadout.pickup-info-terms.json` stores UI-facing labels and display-value translations

Keep the gameplay file compact and structured around:

- top-level `schemaVersion`
- top-level `pickups` object keyed by stringified `pickupId`
- per-pickup `names.en` / `names.zh-CN`
- per-pickup facts such as `wikiKey`, `quality`, and `type`
- ordered `statSections`
- localized `text.summary`, `text.effects`, `text.synergies`, and `text.notes`

Keep the terms file structured around:

- `sections`
- `stats`
- `displayValues`

Extra scrape-only artifacts belong outside `defaults/catalog/`.

The old bilingual sources may still exist for translation or migration workflows, but they are no longer the active
runtime nearby-pickup format.

Legacy files such as `RandomLoadout.pickup-wiki-tips.en.json` and
`RandomLoadout.pickup-wiki-tips.zh-CN.work.json` may still exist in the repository for migration/reference purposes,
but they are no longer part of the active runtime nearby-pickup display path.

## Supported JSON5 Features

The loader supports these JSON5 conveniences:

- Line comments: `// ...`
- Block comments: `/* ... */`
- Trailing commas in arrays and objects
- Single-quoted strings: `'value'`

## Recommended Style

To keep files readable and stable, use this style:

- Keep property names in double quotes
- Keep string values in double quotes
- Use comments for intent and grouping
- Keep pickup ids as integers, not strings
- Keep one rule object per block, and one alias entry per line

## Start Items Preset Structure

Each Start Items preset lives in its own `.json` file under `presets/`. Each preset has two layers:

- Internal identity: `id`
- Display text: `display_name_key` for built-in localized presets, or `name` for user-authored presets

This separation matters:

- `id` is what the game stores in `randomgun.randomloadout.cfg` as the active preset
- `display_name_key` lets one shipped preset file show different names in Chinese and English
- `name` is the plain display text for custom presets such as `边牧` or `Boss Rush Test`

### Preset Fields

Each preset object supports:

- `id`: required stable preset id used internally
- `display_name_key`: optional localization key for built-in presets
- `name`: optional plain display name for custom presets
- `rules`: required array of Start Items rules, which may be empty
- `pickups`: optional array of preset pickup objects such as `{ "type": "key", "count": 1 }`

For `pickups`:

- Duplicate pickup `type` entries are merged automatically when loaded and saved
- `count` is stored by grant units, not always the raw in-game amount
- `casings` uses fixed bundles where `count: 1` means one grant of `50` casings

Display priority is:

1. `display_name_key`
2. `name`
3. `id`

### Built-In Preset Example

Use `display_name_key` when one preset should show different names by UI language:

```json5
{
  "id": "casey_synergies",
  "display_name_key": "preset.casey_synergies",
  "rules": [
    {
      "enabled": true,
      "mode": "specific",
      "category": "gun",
      "id": 541,
    },
    {
      "enabled": true,
      "mode": "random",
      "category": "gun",
      "count": 1,
      "poolIds": [118, 457, 143, 26],
    },
  ],
}
```

With matching localization entries:

```json5
// ETG-Gameplay-Dashboard.localization.zh-CN.json5
"preset.casey_synergies": "卡西协同"

// ETG-Gameplay-Dashboard.localization.en.json5
"preset.casey_synergies": "Casey Synergies"
```

### Custom Preset Example

Use `name` when the preset should always display exactly what the author typed:

```json5
{
  "id": "border-collie",
  "name": "边牧",
  "rules": [
    {
      "enabled": true,
      "mode": "specific",
      "category": "active",
      "id": 108,
    },
  ],
  "pickups": [
    { "type": "key", "count": 1 },
    { "type": "armor", "count": 2 },
    { "type": "casings", "count": 1 },
  ],
}
```

### Full Preset File Example

```json5
{
  "id": "border-collie",
  "name": "边牧",
  "rules": [
    {
      "enabled": true,
      "mode": "specific",
      "category": "active",
      "id": 108,
    },
  ],
  "pickups": [
    { "type": "key", "count": 1 },
    { "type": "casings", "count": 2 },
  ],
}
```

The active preset is stored in `randomgun.randomloadout.cfg`:

```ini
[StartItems]
ActivePreset = casey_synergies
```

## Start Items Rule Fields

Each object inside `rules` supports:

- `enabled`: optional bool, defaults to `true`
- `mode`: required, `specific` or `random`
- `category`: required for `specific`; optional readability field for `random`
- `count`: optional for `random`, defaults to `1`
- `id`: optional specific pickup id
- `alias`: optional specific pickup alias
- `name`: optional specific pickup internal/display name
- `poolIds`: optional random-pool pickup ids
- `poolAliases`: optional random-pool pickup aliases
- `pool`: optional random-pool pickup names

### Specific Rule Example

```json5
{
  "enabled": true,
  "mode": "specific",
  "category": "gun",
  "id": 541,
}
```

### Random Rule Example

```json5
{
  "enabled": true,
  "mode": "random",
  "category": "passive",
  "count": 2,
  "poolIds": [427, 114, 118],
  "poolAliases": [],
  "pool": [],
}
```

For `mode: "random"` rules, `poolIds`, `poolAliases`, and `pool` are resolved across all supported pickup categories.
This allows one random pool to mix guns, passives, and actives. The rule-level `category` is optional for random
rules; when present, it is retained for readability and warning context, but it does not restrict random pool entries.
Random pools are shuffled once and then consumed by index across runs. When the pool contents change, the stored order is
discarded and a new shuffled order is created.

## Distributed Default Presets

On a fresh deploy, built-in preset content is read from these shipped files:

- `presets/preset.default.json`
- `presets/preset.casey_synergies.json`

`ETG-Gameplay-Dashboard.rules.json5` is now only the lightweight Start Items config anchor that ships beside the
directory-based preset files. The actual default preset content comes from those preset JSON files. Built-in preset
names come from localization keys in the preset JSON, not from a hard-coded bilingual `name` field.

## Command Panel Config

The command panel language and keyboard toggle key are stored in `randomgun.randomloadout.cfg`:

```ini
[UI]
Language = auto
CommandPanelKey = F7
CommandPanelControllerShortcut = LB+R3
ThemePreset = theme1
DisableCommandPanelControllerShortcut = false
```

Supported language values:

- `auto`
- `en`
- `zh-CN`

`auto` is the repository default and follows the game's current language. UI strings come from the localization JSON
files, while pickup names still come from the runtime pickup catalog. See
[Localization And Language Switching](./localization.md) for the full implementation notes and troubleshooting flow.

Use a Unity `KeyCode` name such as `F7`, `F8`, `Insert`, or `BackQuote`. Invalid values fall back to `F7`.

`UI.CommandPanelControllerShortcut` controls the controller shortcut used to open the panel. Supported values are `LB+R3`,
`LB+X`, `LB+Y`, and `R3`; the default is `LB+R3`. `LB+R3`, `LB+X`, and `LB+Y` trigger when the second button is pressed
while LB is held. `R3` opens after holding R3 for 0.5 seconds and closes immediately when R3 is pressed.

`UI.DisableCommandPanelControllerShortcut` disables the controller shortcut when set to `true`, while leaving the keyboard
command-panel key available. The default is `false`.

`UI.ThemePreset` stores only a stable theme ID, such as `theme1`, `theme2`, `theme3`, `theme4`, or `theme5`. The current IDs are `theme1` for the default
theme, `theme2` for the Mars Relic theme, `theme3` for the Cyberpunk theme, `theme4` for the Snowfield theme, and `theme5` for the Hazard theme. Theme names and color values are defined by the plugin and are not part of the
configuration identity, so they can be changed without requiring config migration.

`UI.EnableCommandPanelCursorAbovePanel` draws the ETG mouse cursor after the Control Panel so it remains visible above
the panel. Controller cursor behavior is left to ETG, while Control Panel navigation remains controller-driven. It is
disabled by default while this behavior is being validated.

The combat cursor color uses two settings under `[Combat]`: `CursorColorEnabled` and `CursorColorPreset`. Custom coloring is
disabled by default; selecting a color in the Combat cursor page enables it. The page also has an explicit enable/disable button. `OFF` disables only custom coloring;
the original ETG mouse cursor continues to be drawn above the Control Panel independently of the Combat Cursor color state.

`CursorColorPreset` stores only a stable opaque preset ID: `preset_01` through `preset_08`. The current target HEX values are
`preset_01=#00E5FF`, `preset_02=#39FF14`, `preset_03=#FFF000`, `preset_04=#FF1493`, `preset_05=#FF0000`,
`preset_06=#FF8C00`, `preset_07=#9900FF`, and `preset_08=#0066FF`. Display names and HEX values are not config identity;
they can change without invalidating a preset. To preserve an old visual color while introducing a new one, add a new preset
ID instead of changing the existing preset's meaning. Selecting a color writes the preset ID and enables the feature;
turning the page off writes `CursorColorEnabled=false`. Turning it back on selects `preset_01` if no enabled preset is selected.

## Example: Aliases

```json5
{
  "aliases": [
    { "alias": "casey_bat", "id": 541 },
    { "alias": "eyepatch", "id": 118 },
  ],
}
```

## Notes

- Missing or invalid `ETG-Gameplay-Dashboard.rules.json5` falls back to `RandomLoadout.rules.full-pool.json5`, then to built-in defaults.
- `UI.Language` accepts `auto`, `en`, or `zh-CN`.
- `UI.CommandPanelKey` chooses the key that opens and closes the in-game command panel.
- The in-game command panel opens from the configured 360 controller shortcut (`LB+R3` by default). The standalone R3 mode
  requires a 0.5-second hold to open and closes immediately on the next R3 press.
- `StartItems.ActivePreset` stores the active preset id, not the localized display text.
- Missing or invalid `ETG-Gameplay-Dashboard.aliases.json5` falls back to built-in default aliases.
