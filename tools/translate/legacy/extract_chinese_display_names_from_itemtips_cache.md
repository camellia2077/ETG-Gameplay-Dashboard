# extract_chinese_display_names_from_itemtips_cache

Use `pwsh` (PowerShell 7.6.0) to run this helper.

This script reads a small repository-relative input file of English item names, looks up matching HTML pages under
`temp/etg-itemtips-cn/cache/`, and extracts the Chinese item names from the page title blocks.

It is meant for targeted name recovery, not for full-catalog scraping.

## Input

Pass `--input` with a path relative to the repository root.

Supported formats:

- `.txt`: one English item name per line
- `.json`: string array such as `["Shades's Revolver", "Smiley's Revolver"]`
- `.json`: object with `englishNames`
- `.json`: object with `entries`, where each entry contains `englishDisplayName`

## Output

The script writes a compact JSON file containing:

- `englishDisplayName`
- `chineseDisplayName`
- `cacheFile`

If `--output` is omitted, the default output path is the input file path plus `.resolved.json`.

## Example

Create a small input file:

```json
[
  "Shades's Revolver",
  "Smiley's Revolver"
]
```

Run the helper in `pwsh`:

```powershell
python .\tools\translate\extract_chinese_display_names_from_itemtips_cache.py --input .\temp\name-batch-001.json
```

Write to a custom output path:

```powershell
python .\tools\translate\extract_chinese_display_names_from_itemtips_cache.py --input .\temp\name-batch-001.json --output .\temp\name-batch-001.resolved.json
```

## Notes

- The script only checks candidate HTML files derived from each English name; it does not scan the whole cache.
- Some official localized names in the Chinese client are still English aliases or model names such as `M9`. When the
  cache page exposes that as the localized display name, keep it as a valid result.
- If no confirmed Chinese name is found, `chineseDisplayName` is left empty.
- This helper is good for filling `chineseDisplayName` candidates before editing `zh-CN.work.json`.
