# Translation Workflow

This folder owns the legacy command-line workflow for gradually translating pickup wiki tips into Simplified Chinese.

Run the translation workflow commands in `pwsh` (PowerShell 7.6.0), not Windows PowerShell.

## Goal

Keep the English runtime tip catalog immutable while allowing batch-by-batch Chinese translation work.

Use the scripts here to:

1. initialize or refresh the Chinese work file
2. export a small batch of untranslated entries
3. apply translated results back into the work file
4. postprocess translated Chinese notes so embedded English item names align with the Chinese names already present in the work file
5. sync `chineseDisplayName` from the latest game-language name export when needed
6. optionally sync `chineseDisplayName` from `itemtips-cn.tip` when the game export still falls back to English
7. validate that the work file structure was not broken

## Files

- `legacy/init_pickup_wiki_tips_zh_cn_work.py`
- `legacy/export_pickup_wiki_tip_translation_batch.py`
- `legacy/apply_pickup_wiki_tip_translation_batch.py`
- `legacy/postprocess_pickup_wiki_tip_translation_work.py`
- `legacy/sync_pickup_wiki_tip_chinese_display_names.py`
- `legacy/sync_pickup_wiki_tip_chinese_display_names_from_itemtips_cn.py`
- `legacy/scan_bare_english_terms_in_pickup_wiki_tips.py`
- `legacy/validate_pickup_wiki_tip_translation_work.py`
- `legacy/translation_workflow.py`

## Source Of Truth

- English baseline:
  `defaults/catalog/EtgGameplayDashboard.pickup-wiki-tips.en.json`
- Chinese work file:
  `defaults/catalog/EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json`

Never overwrite English fields during translation work.

Treat the English fields as the immutable source layer for runtime and tooling. Keep `englishDisplayName` and
`englishNotes` intact so the mod can later choose between Chinese-only, English-only, or mixed bilingual display modes
at runtime.

## Chinese Name Source Priority

When deciding `chineseDisplayName`, use these sources in priority order:

1. the latest game-language name export
2. `temp/etg-itemtips-cn/itemtips-cn.tip` from the cloned `etg-itemtips-cn` repository when the game export is missing,
   still English, or clearly falls back to a placeholder

If the game export already provides a confirmed Chinese name, keep that value.

If the game export is empty, English-only, or a placeholder such as `STRING_NOT_FOUND`, it is valid to import the
Chinese name from `itemtips-cn.tip`.

If both sources provide a usable Chinese name but disagree, do not silently invent a third wording. Keep the currently
chosen project value unless the project owner explicitly asks for a correction.

Do not fill `chineseDisplayName` from ad-hoc prose translation alone. Only write a new display name when it comes from
one of the approved name sources above or from an explicit owner-approved correction.

When a confirmed name source uses digits or Latin model markers in the localized name, keep that source form exactly.
Do not proactively standardize it into a different style.

Examples:

- if the confirmed source says `鲁布-亚达因型2号`, do not rewrite it to `鲁布-亚达因型二号`
- if the confirmed source says `M9` or `C4`, keep `M9` or `C4`

Apply the same rule inside `chineseNotes` when the item itself is mentioned by name. The first mention should follow
the confirmed `chineseDisplayName` form instead of introducing a second variant spelling.

## Editable Fields

Only edit these fields during translation:

- `chineseDisplayName`
- `chineseNotes`
- `translationStatus`
- `updatedUtc`

The JSON should remain bilingual and program-friendly:

- keep the English source fields intact for lookup, search, and runtime display toggles
- use `chineseDisplayName` and `chineseNotes` only as the localization layer
- do not delete English terms from the data model just because the Chinese translation is complete

Section labels inside translated notes must stay normalized for runtime parsing:

- `Effects:` -> `效果：`
- `Notes:` -> `备注：`
- `Trivia:` -> `趣味冷知识：`

Do not invent alternate Chinese labels for these three markers. Keep them consistent across all entries.

Keep `chineseNotes` as content-source text, not UI-formatted text:

- do not manually insert blank lines before `效果：`, `备注：`, or `趣味冷知识：`
- do not translate with presentation-driven `\n` / `\n\n` line breaks just to control in-game layout
- store the section labels in plain prose order and let the runtime formatter insert visual line breaks

In short: JSON owns wording, the program owns display formatting.

When translating notes, preserve any existing `chineseDisplayName`. If a Chinese item name is already present, do not
rewrite it unless the project owner explicitly asks for a name correction.

If `chineseDisplayName` is already non-empty, do not translate or rewrite it during note translation.

Inside `chineseNotes`, confirmed names should not be left as English-only just to preserve searchability. When a term
has a confirmed or stable Chinese rendering, write it as `中文名（English Name）` with full-width parentheses.

Use this wording pattern inside `chineseNotes`:

- first mention: `中文名（English Name）`
- later mentions in the same entry: usually `中文名`
- if there is no confirmed Chinese rendering: keep the English term unchanged

At the current first-pass translation stage, do not force-translate these term groups unless the project already has a
confirmed Chinese rendering for them:

- enemy names
- gun-class / item-class labels such as `SHOTGUN`, `FIRE`, or similar category tags
- common synergy / combo names

For these groups, preserving the English source term in first-pass translation is preferred over inventing a temporary
Chinese wording that may later need to be mass-corrected.

This bilingual-parentheses rule applies to the current item name in prose, synergy names, game names, character names,
enemy names, and external reference names when a stable Chinese rendering is available.

Do not append the English name inside `chineseDisplayName` itself. The standalone display-name field should stay as the
confirmed in-game Chinese name only. The `中文名（English Name）` expansion rule is for prose inside `chineseNotes`.

If an item does not yet have a confirmed Chinese name, keep the item name in English inside `chineseNotes` rather than
inventing or machine-translating a new Chinese name.

If `chineseDisplayName` is empty, do not translate the item name.

If an item's confirmed display name is still English, keep that English item name unchanged and do not translate it.

If `chineseDisplayName` and `englishDisplayName` are identical and both are English, keep that item name in English
during translation and do not translate it into Chinese.

Some official game-language names in the Chinese game client are still English words or model names such as `M9`.
If a game-verified or source-verified localized display name uses that form, keep it exactly as-is. This is normal and
should not be treated as a missing translation.

Do not edit these fields manually:

- `pickupId`
- `category`
- `wikiKey`
- `internalName`
- `englishDisplayName`
- `englishNotes`
- `sourceHash`

## Completion Rule

An entry counts as complete when both are non-empty:

- `chineseDisplayName`
- `chineseNotes`

Draft entries with seeded English notes are still considered in-progress translation work.

Once an entry already has both a confirmed `chineseDisplayName` and a translated `chineseNotes`, do not translate it
again unless the project owner explicitly asks for a revision.

Batch export skips complete entries by default unless explicitly requested with `--include-complete`.

## Typical Flow

1. Refresh the work file:

```powershell
python .\tools\translate\legacy\init_pickup_wiki_tips_zh_cn_work.py --game-language-names "<path-to-EtgGameplayDashboard.pickup-names.game-language.json>"
```

2. Export a translation batch:

```powershell
python .\tools\translate\legacy\export_pickup_wiki_tip_translation_batch.py --status pending --status draft --limit 20 --output .\temp\pickup-batch-001.json
```

You can also target a hand-picked set of items by repeating `--pickup-id`:

```powershell
python .\tools\translate\legacy\export_pickup_wiki_tip_translation_batch.py --pickup-id 1 --pickup-id 2 --pickup-id 3 --pickup-id 4 --pickup-id 5 --output .\temp\pickup-batch-001.json
```

If you need to revise entries that are already complete, add `--include-complete`:

```powershell
python .\tools\translate\legacy\export_pickup_wiki_tip_translation_batch.py --pickup-id 1 --pickup-id 2 --include-complete --output .\temp\pickup-batch-review.json
```

3. Apply a translated batch:

```powershell
python .\tools\translate\legacy\apply_pickup_wiki_tip_translation_batch.py --input .\temp\pickup-batch-001.translated.json
```

4. Validate the work file:

```powershell
python .\tools\translate\legacy\validate_pickup_wiki_tip_translation_work.py
```

Optional postprocess step after a translated batch:

```powershell
python .\tools\translate\legacy\postprocess_pickup_wiki_tip_translation_work.py --pickup-id 623
```

This postprocess step builds a replacement table from the current `zh-CN.work.json` entries. Any entry that already has a
`chineseDisplayName` can contribute aliases such as `englishDisplayName`, `wikiKey`, and `internalName`, which are then
replaced inside translated `chineseNotes`.

Optional scan step before a revision batch:

```powershell
python .\tools\translate\legacy\scan_bare_english_terms_in_pickup_wiki_tips.py --min-pickup-id 1 --max-pickup-id 60 --output .\temp\pickup-bare-english-1-60.json
```

This scan step only reports candidate bare English terms inside `chineseNotes`. It does not modify the work file.
Use it to narrow down which entries may still need `中文名（English Name）` cleanup before a manual revision pass.

Optional display-name sync step when you have a fresh game export:

```powershell
python .\tools\translate\legacy\sync_pickup_wiki_tip_chinese_display_names.py --game-language-names "<path-to-EtgGameplayDashboard.pickup-names.game-language.json>"
```

This sync is intentionally strict for `zh-CN` work:

- only values that look like actual Chinese names are written to `chineseDisplayName`
- English fallback values are treated as empty
- `STRING_NOT_FOUND` style placeholders are treated as empty

Optional fallback sync from the cloned `etg-itemtips-cn` repository:

```powershell
python .\tools\translate\legacy\sync_pickup_wiki_tip_chinese_display_names_from_itemtips_cn.py --itemtips-tip ".\temp\etg-itemtips-cn\itemtips-cn.tip"
```

This fallback source is useful when the current game export still writes English names. It only imports names that are
actually Chinese and keeps entries like `AK-47` empty.

## Agent Rules

If an agent is used to translate a batch:

- only give it the exported batch JSON, not the full work file
- tell it to preserve `pickupId` and `sourceHash`
- tell it to edit only Chinese fields
- tell it to keep item names in English when no confirmed `chineseDisplayName` exists
- reject its output if validation fails
