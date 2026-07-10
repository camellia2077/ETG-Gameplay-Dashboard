# Gameplay 最小操作规程

开始翻译前，先读完这份文件。

当前 nearby-pickup gameplay 翻译流程的最终 runtime 目标是：

- `defaults/catalog/RandomLoadout.pickup-gameplay.json`
- `defaults/catalog/RandomLoadout.pickup-info-terms.json`

旧 `zh-CN.work.json` 和 `.check.json` 批次文件只是辅助工作格式，不是最终交付格式。

高层命令只有两个：

```powershell
python .\tools\translate\main.py translate ...
python .\tools\translate\main.py postcheck ...
```

## 只改这 4 个字段

- `chineseGameplaySummary`
- `chineseEffectHighlights`
- `chineseSynergyHighlights`
- `chineseUsageNotes`

不要改：

- `chineseDisplayName`
- 任意英文源字段
- `sourceHash`
- 顶层术语表

## 最低硬要求

1. 只在 `.check.json` 批次里工作，不要先改 runtime v2 输出文件。
2. `localizedEnglish*` 只作为参考，不回写 runtime v2 输出文件。
3. 英文空字段对应的中文字段也保持空，不要补写。
4. 术语和固定译名优先参考 `GLOSSARY.md`。
5. 程序检查和 agent 获取文本检查都通过后，才能进入可映射回 runtime v2 的回写阶段。

## 翻译

1. 先跑：

```powershell
python .\tools\translate\main.py translate --start-id <start> --end-id <end> --include-complete
```

2. 修改生成的 `.check.json` 批次。
3. 不要先把辅助工作格式当成最终目标。
4. 翻译过程中反复执行：

```powershell
python .\tools\translate\main.py translate --input <batch-json> --normalize
```

## 翻译后

1. 先跑自动化检查：

```powershell
python .\tools\translate\main.py postcheck --input <batch-json>
```

2. 回写前，至少满足：

- `Missing: 0`
- `Unexpected Chinese: 0`
- `Quotes: 0`
- `Proper nouns: 0`
- `UI/controls: 0`
- `English residue: 0`
- `Chinese residue: 0`

3. `scan-stat-source-drift` 应为 `0` 问题。
4. 自动化检查通过后，仍然要做人工检查。
5. 人工检查通过后再回写：

```powershell
python .\tools\translate\main.py postcheck --input <batch-json> --apply
```

## 说明

- 具体格式问题优先按 `check` 生成的报告修，不要继续向这份文档里堆细则。
- 如果发现新的高频问题，优先补 Python 检查器或规范化逻辑，而不是先补 Markdown 规则。
- `pickupId` 区间里出现缺号是正常现象，不代表漏抓。
- 协同标题当前允许保留英文。
