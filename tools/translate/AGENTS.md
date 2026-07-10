# Translation Workflows

`tools/translate/` now uses this layout:

- current pickup-gameplay workflow:
  - [ENTRYPOINT.md](/C:/code/ETG-Gameplay-Dashboard/tools/translate/ENTRYPOINT.md)
  - [MUST.md](/C:/code/ETG-Gameplay-Dashboard/tools/translate/MUST.md)
  - [README.md](/C:/code/ETG-Gameplay-Dashboard/tools/translate/README.md)
  - [GLOSSARY.md](/C:/code/ETG-Gameplay-Dashboard/tools/translate/GLOSSARY.md)
  - [SCRIPTS.md](/C:/code/ETG-Gameplay-Dashboard/tools/translate/SCRIPTS.md)
  - [main.py](/C:/code/ETG-Gameplay-Dashboard/tools/translate/main.py)

- legacy wiki-tip workflow:
  - `tools/translate/legacy/`
  - legacy docs and helper scripts stay there for historical translation/reference work

If the task is about the current nearby-pickup translation flow, use the root `tools/translate/` gameplay files, not
`legacy/`.

## Runtime Schema Target

The nearby-pickup gameplay translation flow no longer treats the legacy bilingual gameplay files as the final runtime
contract.

Before changing translation scripts, work files, batch formats, or review rules for nearby-pickup gameplay text, read:

- [Pickup Gameplay Schema v2](/C:/code/ETG-Gameplay-Dashboard/docs/reference/pickup-gameplay-schema-v2.md)

Use that page as the final runtime field contract.

Practical rule:

- translation tooling may still use helper work files or intermediate batch shapes
- but those shapes should be judged by how cleanly they map into:
  - `defaults/catalog/RandomLoadout.pickup-gameplay.json`
  - `defaults/catalog/RandomLoadout.pickup-info-terms.json`

When the translation workflow documentation and the runtime schema differ, prefer the runtime schema doc as the target
state and update the workflow docs/scripts to match.
