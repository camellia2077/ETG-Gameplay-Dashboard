# Pickup Gameplay 翻译流程

这份文档只保留最小操作规程。
翻译细则、格式校验和大部分纠错提示，优先交给 Python 脚本处理。

开始前先读：

- [MUST.md](C:/code/ETG-Gameplay-Dashboard/tools/translate/MUST.md)
- [GLOSSARY.md](C:/code/ETG-Gameplay-Dashboard/tools/translate/GLOSSARY.md)

统一入口：

```powershell
python .\tools\translate\main.py --help
```

高层命令只有两个：

```powershell
python .\tools\translate\main.py translate ...
python .\tools\translate\main.py postcheck ...
```

## 处理范围

只处理这 4 个字段：

- `chineseGameplaySummary`
- `chineseEffectHighlights`
- `chineseSynergyHighlights`
- `chineseUsageNotes`

不要直接改：

- `chineseDisplayName`
- 顶层术语表
- 英文源字段
- `sourceHash`

## 关键文件

- 主工作文件：
  - `defaults/catalog/RandomLoadout.pickup-gameplay.zh-CN.work.json`
- 临时批次目录：
  - `temp/pickup-gameplay-translation-batches/`

## 翻译

1. 先导出批次并做初检，想快看就加 `--summary`：

```powershell
python .\tools\translate\main.py translate --start-id <start> --end-id <end> --include-complete --summary
python .\tools\translate\main.py translate --count 5 --only-missing --summary
```

2. 只修改生成的 `.check.json` 批次文件。
3. 不要直接改主工作文件。
4. `localizedEnglish*` 只作为参考，不回写主工作文件。
5. 翻译过程中，反复检查并替换：

```powershell
python .\tools\translate\main.py translate --input .\temp\pickup-gameplay-translation-batches\<batch>.check.json --normalize --summary
```

6. 根据 `check-report.json` 和各扫描报告修复问题。
   常见问题和建议会直接在报告里给出，不要靠手记规则。

## 翻译后

1. 先跑自动化检查，想快看就加 `--summary`：

```powershell
python .\tools\translate\main.py postcheck --input .\temp\pickup-gameplay-translation-batches\<batch>.check.json --summary
```

2. 摘要里先看这几项全是 `0`：

- `Missing`
- `Unexpected Chinese`
- `Quotes`
- `Proper nouns`
- `UI/controls`
- `English residue`
- `Chinese residue`

3. `scan-stat-source-drift` 应为 `0` 问题。
4. 再做 agent 获取文本检查。
5. 都过了再回写：

```powershell
python .\tools\translate\main.py postcheck --input .\temp\pickup-gameplay-translation-batches\<batch>.check.json --apply
```

6. 完成后，`defaults/catalog/RandomLoadout.pickup-gameplay.zh-CN.work.json` 才算这一批已完成。

## 代码优先

- 缺翻、引号、专有名词、源码残留，先看 `check` 生成的报告。
- 键位名、控制器名、UI/HUD 这类英文残留，先看 `check` 里的 `UI/controls` 项。
- 结构化 `stat value` 标签问题，先跑 `scan-stat-values` / `localize-stat-values`。
- 如果 `stats[*].value` 被误改成中文，用 `restore-stat-values` 按 `en.json` 恢复英文源值。
- 文档只负责说明入口、顺序和边界；细则尽量沉淀进脚本，而不是继续堆在 Markdown 里。

## 额外说明

- `pickupId` 区间里缺号是正常现象，不代表漏抓。
- 协同标题当前允许保留英文。
