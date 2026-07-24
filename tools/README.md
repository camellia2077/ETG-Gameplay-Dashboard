# Tools

This directory keeps repository tooling grouped by purpose.

## Categories

- `build/`: build and test entrypoints
- `data/`: data-generation scripts and optional scrape/cache helpers for repository-shipped catalog snapshots
- `deploy/`: deploy-to-game tooling
- `logs/`: log extraction and filtering helpers
- `release/`: player-facing release packaging
- `docs/`: documentation generation
- `devtools/`: standalone development utilities with their own structure
- `translate/`: command-line workflows for legacy wiki-tip data and current pickup-gameplay translation/validation

## Data Script Conventions

Use `tools/data/` for scripts that generate repository-shipped data from external or derived sources, such as wiki tip
catalogs.

When a data script produces content that the mod reads at runtime:

- store the generator in `tools/data/`
- write the shipped output to `defaults/catalog/`
- keep the shipped file compact and normalized for runtime use
- do not commit raw HTML dumps to `defaults/catalog/`

For legacy web-derived pickup tip data, keep any exported snapshot compact and normalized rather than storing raw
webpage dumps.

Gameplay-focused nearby-pickup data follows the same pattern:

- `defaults/catalog/EtgGameplayDashboard.pickup-gameplay.json`
- `defaults/catalog/EtgGameplayDashboard.pickup-info-terms.json`

Legacy wiki-tip localization helpers still live in `tools/translate/`, but they are no longer part of the active
runtime nearby-pickup display path. Current examples:

- `init_pickup_wiki_tips_zh_cn_work.py`: initialize or refresh `EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json`
- `export_pickup_wiki_tip_translation_batch.py`: export a small untranslated batch
- `apply_pickup_wiki_tip_translation_batch.py`: merge translated batch results back into the work file
- `postprocess_pickup_wiki_tip_translation_work.py`: replace embedded English item names in Chinese notes using the aligned Chinese name table already present in the work file
- `sync_pickup_wiki_tip_chinese_display_names.py`: sync `chineseDisplayName` from a game-language name export, while skipping English fallback values
- `sync_pickup_wiki_tip_chinese_display_names_from_itemtips_cn.py`: sync `chineseDisplayName` from `itemtips-cn.tip` when the game export still falls back to English
- `validate_pickup_wiki_tip_translation_work.py`: validate work-file structure and source hashes

Gameplay localization workflow helpers also live in `tools/translate/`. Current example:

- `init_pickup_gameplay_zh_cn_work.py`: initialize or refresh `EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json` while preserving Chinese prose progress and top-level gameplay term mappings
  Default work-file path: `defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json`

Schema-v2 runtime conversion lives in `tools/data/`:

- `build_pickup_gameplay_v2.py`: merge the legacy English gameplay source and the zh-CN translation work file into the runtime `pickup-gameplay.json` and `pickup-info-terms.json` outputs
  Legacy source defaults: `defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.en.json` and `defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json`

Legacy wiki-tip localization data is intentionally bilingual:

- `defaults/catalog/EtgGameplayDashboard.pickup-wiki-tips.en.json` stays as the immutable English source layer
- `defaults/catalog/EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json` keeps both English fields and Chinese fields
- these files are kept as legacy/translation-reference data rather than the current runtime nearby-pickup source
- inside `chineseNotes`, confirmed names should normally be written as `中文名（English Name）` on first mention so the
  prose stays readable while preserving lookup-friendly English terms

`tools/data/` still owns generation of shipped catalog snapshots, for example:

- `generate_pickup_gameplay_info.py`: build the English gameplay-info catalog sample
  Default output path: `defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.en.json`
- `generate_pickup_wiki_tips.py`: build the legacy English wiki-tip snapshot

## 30-Second Commands

- Build debug:
  `python .\tools\build\build.py --configuration Debug`
- Run tests:
  `python .\tools\build\test.py --configuration Debug`
- Deploy release to game:
  `python .\tools\deploy\deploy_mod.py "<game path>" --configuration Release --overwrite-config`
- Read recent Boss Rush + error logs:
  `python .\tools\logs\read_log.py --preset bossrush --preset error --tail 200 --dedupe-consecutive --ignore-case`
- `python .\tools\devtools\check_naming.py --verbose`
  Run the repository-specific naming checker for `src/**/*.cs`.
## Read Next

- Build and environment details:
  [`../docs/getting-started/development-setup.md`](../docs/getting-started/development-setup.md)
- Deploy workflow:
  [`../docs/operations/deploy.md`](../docs/operations/deploy.md)
- Logging workflow:
  [`../docs/operations/logging.md`](../docs/operations/logging.md)
