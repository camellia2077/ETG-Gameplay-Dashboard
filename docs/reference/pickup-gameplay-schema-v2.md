# Pickup Gameplay Schema v2

Use this page as the field contract for the nearby-pickup runtime gameplay data.

This schema is optimized for runtime reads first.
Translation tooling may use helper work files or intermediate batches, but the shipped runtime format should follow this
page.

## Files

Schema v3 uses two runtime JSON files under `defaults/catalog/`:

- `EtgGameplayDashboard.pickup-gameplay.json`
- `EtgGameplayDashboard.pickup-info-terms.json`

Responsibilities:

- `pickup-gameplay.json`
  - pickup facts
  - bilingual pickup names
  - bilingual nearby-pickup prose
  - ordered stat sections
- `pickup-info-terms.json`
  - section labels
  - stat labels
  - display-value translations for raw runtime values

## Design Goals

The schema is intentionally shaped around these rules:

- one pickup record per `pickupId`
- no English base file plus zh-CN overlay merge at runtime
- raw gameplay facts stay language-neutral where possible
- user-facing labels and display-value translations stay outside the main pickup facts file
- `pickupId` is the stable lookup key

## `pickup-gameplay.json`

Top-level structure:

```json
{
  "schemaVersion": 3,
  "generatedUtc": "2026-07-03 04:26:47",
  "sourceLanguageFiles": {
    "en": "defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.en.json",
    "zh-CN": "defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json"
  },
  "pickupCount": 668,
  "languages": ["en", "zh-CN"],
  "pickups": {
    "0": {
      "id": 0,
      "category": "gun",
      "names": {
        "en": "Magic Lamp",
        "zh-CN": "阿拉丁神灯"
      },
      "wikiKey": "Magic_Lamp",
      "quality": "B",
      "type": "Semiautomatic",
      "statSections": [],
      "text": {
        "summary": {
          "en": "...",
          "zh-CN": "..."
        },
        "effects": {
          "en": ["..."],
          "zh-CN": ["..."]
        },
        "synergies": {
          "en": ["..."],
          "zh-CN": ["..."]
        },
        "notes": {
          "en": ["..."],
          "zh-CN": ["..."]
        }
      }
    }
  }
}
```

### Top-Level Fields

- `schemaVersion`
  - required integer
  - current value is `3`
- `generatedUtc`
  - required UTC timestamp string
  - metadata only
- `sourceLanguageFiles`
  - optional provenance object
  - metadata only
  - current generated values use repository-relative legacy source paths
- `pickupCount`
  - required integer count of `pickups`
- `languages`
  - required language-code array
  - current shipped values are `en` and `zh-CN`
- `pickups`
  - required object
  - keys are stringified pickup ids such as `"0"` or `"541"`

### Pickup Record Fields

Each `pickups["<pickupId>"]` object supports:

- `id`
  - required integer
  - should match the enclosing object key
- `category`
  - required lowercase string enum
  - current values:
    - `gun`
    - `passive`
    - `active`
- `names`
  - required localized name object
  - current keys:
    - `en`
    - `zh-CN`
- `wikiKey`
  - optional string
  - stable wiki/reference key, not player-facing UI text
- `quality`
  - optional raw quality value
  - keep raw source values such as `S`, `A`, `B`, `Special`
  - do not replace with localized display text in this file
- `type`
  - optional raw type value
  - keep raw source values such as `Passive`, `Active`, `Semiautomatic`, `Beam`
  - do not replace with localized display text in this file
- `statSections`
  - ordered array of stat-section objects
- `text`
  - localized nearby-pickup prose object

### `names`

`names` is the bilingual pickup-title block.

Rules:

- `names.en` is the canonical English runtime title
- `names.zh-CN` is the Simplified Chinese runtime title
- if a language value is missing, runtime may fall back to the other available language or live ETG display name

### `statSections`

Each entry in `statSections` supports:

- `key`
  - section identity key such as `core`, `ammo`, `handling`, `impact`, `timing`
- `stats`
  - ordered array of stat entries

Each stat entry supports:

- `key`
  - stable stat identity key such as `class`, `dps`, `damage`, `fire_rate`
- `parts`
  - required ordered array of structured stat-value parts
  - each part has a raw `value` and may have a raw `label`
  - keep part content in source-language / raw form; translate it through `pickup-info-terms.json`

Example:

```json
{
  "key": "damage",
  "parts": [
    { "value": "22", "label": "Large" },
    { "value": "8", "label": "Medium" },
    { "value": "3", "label": "Small" }
  ]
}
```

Rules:

- `statSections[*].key` is structural
- `stats[*].key` is structural
- `stats[*].parts[*].value` and `stats[*].parts[*].label` are content
- do not localize `stats[*].key`

### `text`

`text` owns the prose shown in the nearby-pickup panel.

Supported blocks:

- `summary`
  - localized object with `en` and `zh-CN`
- `effects`
  - localized array object
- `synergies`
  - localized array object
- `notes`
  - localized array object

Rules:

- `summary.*` is a single string per language
- `effects.*`, `synergies.*`, and `notes.*` are arrays of strings
- arrays are preferred over semicolon-packed strings so runtime and tooling can reason about entries more clearly

## `pickup-info-terms.json`

Top-level structure:

```json
{
  "schemaVersion": 2,
  "generatedUtc": "2026-07-05 01:51:31",
  "languages": ["en", "zh-CN"],
  "sections": {},
  "stats": {},
  "displayValues": {}
}
```

### Top-Level Fields

- `schemaVersion`
  - required integer
  - current value is `2`
- `generatedUtc`
  - required UTC timestamp string
- `languages`
  - required language-code array
- `sections`
  - required localized lookup table for section labels
- `stats`
  - required localized lookup table for stat labels
- `displayValues`
  - required localized lookup table for raw runtime values

### `sections`

`sections` translates section identity keys used by the nearby-pickup UI.

Current expected keys:

- `quality`
- `type`
- `summary`
- `effects`
- `synergies`
- `notes`

Each value is a localized object:

```json
"quality": {
  "en": "Quality:",
  "zh-CN": "品质："
}
```

### Section Labels And Values Stay Separate

The nearby-pickup UI treats section labels and their values as separate data and visual roles:

- `pickup-info-terms.json.sections.*` contains only localized section-label text such as `Quality:` or `Summary:`
- quality, type, and stat values come from `pickup-gameplay.json` and may be translated through `pickup-info-terms.json.displayValues`
- summary, effects, synergies, and notes content comes from `pickup-gameplay.json.text.*`
- a section key selects the localized label and the matching `DashboardTheme.PickupInfo*Label` color only
- section values, stat rows, and descriptive text always use the neutral `DashboardTheme.PickupInfoBody` color selected from the theme's `Primary` background

Do not concatenate label and value into one styled field or let a section-label color propagate to its value. This separation is a runtime rendering contract over the structured `parts` data. See [Dashboard UI Theme Rules](./ui-theme-rules.md) for the visual rules.

### `stats`

`stats` translates stat identity keys used inside `statSections[*].stats[*].key`.

Example:

```json
"fire_rate": {
  "en": "Fire",
  "zh-CN": "射速"
}
```

Rules:

- this table localizes stable stat keys
- it does not localize raw values such as `Semiautomatic` or `SILLY`

### `displayValues`

`displayValues` translates raw runtime strings shown as values.

Example:

```json
"Semiautomatic": {
  "en": "Semiautomatic",
  "zh-CN": "半自动"
}
```

Use this for:

- quality values such as `A`, `B`, `Special`
- type values such as `Passive`, `Beam`
- gun-class values such as `SHOTGUN`, `SILLY`
- stat values that should render with a localized label

Rules:

- keys are raw source values
- values are localized display objects
- do not move these translated values into `pickup-gameplay.json`

## Stable Keys vs Translated Content

Treat these as stable structural keys:

- `pickups`
- `id`
- `category`
- `names`
- `wikiKey`
- `quality`
- `type`
- `statSections`
- `text`
- `summary`
- `effects`
- `synergies`
- `notes`
- `sections`
- `stats`
- `displayValues`
- language codes such as `en`, `zh-CN`
- section keys such as `core`, `ammo`, `handling`
- stat keys such as `class`, `dps`, `fire_rate`

Treat these as translatable content:

- `names.<language>`
- `text.summary.<language>`
- `text.effects.<language>[]`
- `text.synergies.<language>[]`
- `text.notes.<language>[]`
- `pickup-info-terms.json` localized label values
- `pickup-info-terms.json` localized display-value values

Treat these as raw source values that should not be directly translated in `pickup-gameplay.json`:

- `quality`
- `type`
- `statSections[*].stats[*].parts[*].value`
- `statSections[*].stats[*].parts[*].label`

## Runtime Expectations

Current runtime behavior assumes:

- pickup lookup is by integer `pickupId`
- UI label lookup uses `pickup-info-terms.json`
- raw display-value lookup uses `pickup-info-terms.json`
- each stat is rendered from its structured `parts` array; the old `stats[*].value` field is not supported
- English fallback remains available inside the same runtime schema

Relevant code:

- [JsonPickupGameplayProvider.cs](/C:/code/ETG-Gameplay-Dashboard/src/EtgGameplayDashboard/Configuration/JsonPickupGameplayProvider.cs)
- [PickupGameplayEntry.cs](/C:/code/ETG-Gameplay-Dashboard/src/EtgGameplayDashboard/Configuration/PickupGameplayEntry.cs)
- [PickupInfoTermsRegistry.cs](/C:/code/ETG-Gameplay-Dashboard/src/EtgGameplayDashboard/Configuration/PickupInfoTermsRegistry.cs)
- [Plugin.PickupWikiTips.cs](/C:/code/ETG-Gameplay-Dashboard/src/EtgGameplayDashboard/Plugin.PickupWikiTips.cs)

## Guidance For Translation Tooling

Translation workflows do not need to edit the runtime files directly, but any work-file or batch format should map
cleanly back to these runtime targets:

- runtime title target:
  - `pickups["<id>"].names.zh-CN`
- runtime summary target:
  - `pickups["<id>"].text.summary.zh-CN`
- runtime effects target:
  - `pickups["<id>"].text.effects.zh-CN`
- runtime synergies target:
  - `pickups["<id>"].text.synergies.zh-CN`
- runtime notes target:
  - `pickups["<id>"].text.notes.zh-CN`
- runtime terms target:
  - `pickup-info-terms.json`

If translation tooling introduces a helper work format, treat this page as the source of truth for final runtime field
mapping.
