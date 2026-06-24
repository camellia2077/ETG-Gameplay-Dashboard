# Localization And Language Switching

Use this page when a task touches command-panel language, pickup names, preset display text, or any user-facing
English/Chinese switch behavior.

## Why This Needs Its Own Guide

This project has two different localization paths:

1. Command-panel UI text
2. Live ETG pickup names and preset item labels

Most language bugs come from changing one path while assuming the other path updates automatically.

## Supported Language Modes

The command panel supports three language preference values:

- `auto`
- `en`
- `zh-CN`

`auto` is the repository default. In this mode, the command panel follows the game's current language as detected from
`GameManager` or its options object.

Owning code:

- `src/RandomLoadout/Plugin.Bootstrap.cs`
- `src/RandomLoadout/Localization/GuiText.cs`

## UI Text Pipeline

UI text is owned by `GuiText`.

Startup flow:

1. `Plugin.Awake()` calls `GuiText.Initialize(Paths.ConfigPath)`.
2. `GuiText` loads:
   - `ETG-Gameplay-Dashboard.localization.en.json5`
   - `ETG-Gameplay-Dashboard.localization.zh-CN.json5`
3. The plugin binds `[UI] Language` from `randomgun.randomloadout.cfg`.
4. The value is normalized to `auto`, `en`, or `zh-CN`.
5. `GuiText.SetLanguageOverride(...)` stores the preference.

Resolution rules:

- if override is `en` or `zh-CN`, `GuiText.CurrentLanguageCode` uses that override
- if override is `auto`, `GuiText` reflects ETG runtime language members from `GameManager` and its options object
- if detection fails, English is the fallback
- when Chinese text is missing, English text is the fallback

Important implementation detail:

- `GuiText` localizes flat key/value UI strings only
- it does not own ETG pickup names

## Language Toggle And Refresh Flow

The command-page language button changes the preference, but the important part is the follow-up refresh.

Manual toggle flow:

1. `InGameCommandController.CommandPage.cs`
2. `ExecuteToggleLanguage(...)`
3. `_languageSetter(nextLanguage)`
4. `_lastGuiLanguageCode = string.Empty`
5. `HandleLanguageChanged()`

Refresh work done by `HandleLanguageChanged()`:

- reset pickup browser state
- reset character-page cache
- refresh loadout preset entries
- refresh loadout editor entries
- refresh random-pool editor entries

There is also an automatic refresh path for `auto` mode:

- `InGameCommandController.OnGUI(...)` compares `_lastGuiLanguageCode` with `GuiText.CurrentLanguageCode`
- when ETG language changes outside the mod, the controller still calls `HandleLanguageChanged()`

Owning code:

- `src/RandomLoadout/Commands/InGameCommandController.cs`
- `src/RandomLoadout/Commands/InGameCommandController.CommandPage.cs`

If a localization fix changes what a page renders, make sure that page is covered by this refresh path.

## Pickup Names Are Not Plain UI Strings

Pickup names do not come from `GuiText` JSON files.

They come from live ETG runtime pickup data and are normalized by `EtgPickupResolver`.

Key rule:

- `EtgPickupCatalogEntry.DisplayName` is the localized display name for the current UI language
- `EtgPickupCatalogEntry.EnglishDisplayName` is a separate English-oriented fallback used when the command panel is in
  English

Owning code:

- `src/RandomLoadout/Etg/EtgPickupCatalogEntry.cs`
- `src/RandomLoadout/Etg/EtgPickupResolver.Catalog.cs`
- `src/RandomLoadout/Etg/EtgPickupResolver.Helpers.cs`

## What Depends On Game Resources

Not all non-control-panel text and images are owned by this mod.

In practice, the project splits into two buckets:

1. Mod-owned UI resources
2. ETG-owned runtime resources

Mod-owned UI resources:

- command-panel titles, buttons, hints, and status text
- settings labels
- built-in preset title localization keys such as `display_name_key`

These are localized by this project through:

- `GuiText`
- `ETG-Gameplay-Dashboard.localization.en.json5`
- `ETG-Gameplay-Dashboard.localization.zh-CN.json5`

ETG-owned runtime resources:

- pickup display names
- pickup journal-derived labels
- pickup icons reused by the Pickup Browser
- other live game-facing labels that come from ETG objects instead of mod JSON

These are localized by reading live ETG data and ETG string tables, not by adding entries to the mod's localization
JSON files.

## Does That Make New Languages Easy

Usually easier, but not automatic.

If a piece of text or art already exists in ETG for a target language, the mod can often reuse it with relatively
little extra content work.

That is why:

- pickup icons are effectively language-agnostic
- pickup names can often follow ETG language support
- many game-native labels do not require the mod to ship a separate translated copy

But there are important limits:

1. The mod still has to select the correct language path in code.
2. The relevant page must refresh cached data after a language change.
3. Some mod surfaces are not game resources at all, so they still need explicit mod translations.
4. ETG support for a language only helps if the target field actually comes from ETG runtime data.

Practical rule:

- if the content is game-native and ETG already supports that language, localization is usually low-friction
- if the content is mod-authored, you still need to add keys, translations, and fallback behavior in this repository
- if the content is game-native but the mod caches or transforms it, you still need to verify that the transformation
  preserves the intended language

For this repository, the most common mistake is assuming that pickup names will switch language just because the command
panel UI switched. That only works when the pickup display path also uses the correct ETG-backed label and refreshes its
cached rows.

## How English Pickup Names Are Resolved

`EtgPickupResolver` uses a safer two-path strategy.

Current-language label:

- `GetPickupLabel(...)`
- reads ETG display/journal labels
- resolves them through `ResolveLocalizedLabelForCurrentUiLanguage(...)`

English label:

- `GetEnglishPickupLabel(...)`
- first tries the pickup's `itemName`
- then tries the backup ETG string tables directly
- finally falls back to raw names

Important constraint:

- do not switch the global `StringTableManager` language just to render English pickup names
- this previously caused unstable behavior and crashes
- the current implementation reads backup string tables directly instead

The backup-table path uses:

- `m_backupCoreTable`
- `m_backupItemsTable`

and calls `GetExactString(0)` on the entry object via reflection.

## Where English Pickup Names Must Be Applied

If the command panel is in English, any pickup-facing UI should prefer `EnglishDisplayName` and only fall back when it
is missing.

Confirmed call sites:

- `InGameCommandController.Styles.cs`
  `PickupBrowserEntry.ResolveDisplayName(...)`
- `LoadoutRuleEditorService.Entries.cs`
  `GetPickupDisplayName(...)`
- `LoadoutRuleEditorService.cs`
  success and duplicate-result messages for Start Items edits

This matters for:

- Pickup Browser row labels
- Pickup Browser search text
- Start Items preset rule rows
- Random pool item rows
- add/remove/duplicate status messages

## Preset Names vs Preset Item Names

These are different problems and different code paths.

Preset title localization:

- built-in presets should use `display_name_key`
- the key resolves through `GuiText`
- this is documented in `config-format.md`

Preset item-name localization:

- preset JSON usually stores `id`, `alias`, or `name`
- the visible item label is resolved later from the live pickup catalog
- if English UI still shows Chinese item names, the bug is usually in `EtgPickupResolver`, pickup browser row-model
  code, or `LoadoutRuleEditorService`

## Troubleshooting Checklist

When a language bug is reported, check these in order:

1. Is the issue about UI strings or pickup names?
2. Is `[UI] Language` set to `auto`, `en`, or `zh-CN` as expected?
3. If `auto`, is `GuiText.CurrentLanguageCode` matching the actual ETG runtime language?
4. If the page shows pickups, does it use `EnglishDisplayName` in English mode?
5. Does the page refresh after a language change, or is it using stale cached rows?
6. If the issue is a built-in preset title, is `display_name_key` present and defined in both localization files?
7. If runtime behavior changed, review the BepInEx log after reproducing

## Files To Open First

- `src/RandomLoadout/Localization/GuiText.cs`
- `src/RandomLoadout/Plugin.Bootstrap.cs`
- `src/RandomLoadout/Commands/InGameCommandController.cs`
- `src/RandomLoadout/Commands/InGameCommandController.CommandPage.cs`
- `src/RandomLoadout/Commands/InGameCommandController.Styles.cs`
- `src/RandomLoadout/Commands/LoadoutRuleEditorService.cs`
- `src/RandomLoadout/Commands/LoadoutRuleEditorService.Entries.cs`
- `src/RandomLoadout/Etg/EtgPickupResolver.Helpers.cs`
- `src/RandomLoadout/Etg/EtgPickupResolver.Catalog.cs`

## Related Docs

- [Code Index](./code-index.md)
- [Commands](./commands.md)
- [Config Format (JSON5)](./config-format.md)
- [Pickups](./pickups.md)
- [Logging](../operations/logging.md)
