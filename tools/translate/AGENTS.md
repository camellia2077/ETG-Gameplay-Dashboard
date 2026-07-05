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
