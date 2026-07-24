# `tools/translate` 脚本说明

这份文档不是操作手册。

它的目的只有一个：

- 让后续接手的 agent 能快速理解这个目录里的 Python 脚本分别负责什么
- 理解这些脚本为什么被创建
- 理解这套流程为什么强调“最小操作规程 + 自动检查”，而不是继续向 Markdown 堆细则

## 目录设计目标

这个目录服务的是 `pickup gameplay` 中文翻译流程。

当前要服务的最终 runtime 目标是：

- `defaults/catalog/EtgGameplayDashboard.pickup-gameplay.json`
- `defaults/catalog/EtgGameplayDashboard.pickup-info-terms.json`

也就是说，这里的脚本不应该再把旧 bilingual work 文件视为最终数据模型，而应该把它视为过渡性工作格式或中间源格式。

它解决的不是“怎么翻译一句话”这种单点问题，而是下面这些更容易反复出错的工程问题：

- 批量翻译时，哪些字段允许改，哪些字段绝对不能动
- 游戏内已有中文物品名，如何统一替换，不让 agent 自造译名
- 英文引号、专有名词、源码残留这类问题，如何稳定暴露并修复
- 翻译完成后，如何快速验收一个 `pickupId` 区间是不是已经达到当前规则要求

所以这里的脚本本质上是在做三件事：

1. 导出可翻译批次
2. 自动扫描常见翻译违规
3. 把合格结果安全映射回 schema v2 runtime 目标

## 为什么不是只写在 Markdown 里

之前已经验证过，只靠 `README.md` / `MUST.md` / `GLOSSARY.md` 这类文本规则，效果不稳定。

原因很简单：

- agent 可能读了，但连续翻译时会漏掉细节规则
- 文档可以说明“应该怎样”，但不能保证“结果真的合格”
- 同一个模型、同一推理强度，如果没有自动检查，输出一致性仍然会漂移

所以这里现在采用的思路是：

- `README.md` / `MUST.md` 只保留最小操作规程、入口和边界
- 大部分一致性要求交给 Python 脚本检查、规范化和报告
- 问题尽量通过 report 暴露，而不是依赖人工回忆规则

当前高层入口命令压成了两个：

- `main.py translate`
- `main.py postcheck`

其他脚本仍然保留，作为这两个高层命令调用的底层能力。

## 各脚本职责

### `gameplay_translation_workflow.py`

这是共用基础模块。

它本身不直接面向用户执行，主要提供：

- 默认路径
- JSON 读写
- 批次字段定义
- `pickupId` 索引
- 翻译状态标准化
- 游戏内物品名替换的共用逻辑
- 旧工作格式到 runtime 目标字段之间的共用映射基础

为什么要有这个文件：

- 避免每个脚本各自复制一套路径和字段规则
- 避免多个脚本对“哪些字段可翻译”“如何识别条目完成状态”出现分歧

如果这里的共用逻辑改了，通常会同时影响导出、规范化、回写几个阶段。

### `localize_game_synergy_names.py`

作用：

- 读取 `tools/data/reference/etg-synergies.json` 中按 `synergies[NameKey]` 索引的游戏资源协同名称
- 规范化 `chineseSynergyHighlights` 和 `chineseUsageNotes` 中以协同名开头的条目
- 同时扫描英文单词加冒号的标题（例如 `Bug:`、`Altered:`、`Removed:`），检查中文是否保留为“中文标题（English Title）：正文”
- 将游戏内正式中文协同名替换到中文协同文本标题中
- 协同标题保留英文，统一输出为 `中文标题（English Title）`
- `#SOULAIR` 的特殊名 `\\o/` 原样保留，并从普通缺失中文名统计中单独列出
- 对标题边界不明确或中英文条目数量不一致的内容只生成报告，不自动修改

默认先执行 dry-run；确认 `temp/game-synergy-name-localization.report.json` 后，再加 `--apply` 写回中文工作文件。

例如：

```powershell
python .\tools\translate\localize_game_synergy_names.py --dry-run
python .\tools\translate\localize_game_synergy_names.py --apply
python .\tools\data\build_pickup_gameplay_v2.py
```

`tools/data/reference/etg-synergies.json` 是游戏资源快照，脚本本身必须保存在 `tools/translate/`，不要把处理逻辑放入 `temp/`。

### `export_unresolved_game_synergy_names.py`

作用：

- 将未能安全自动替换的协同标题导出到 `temp/unresolved-game-synergy-names.json`
- 保留 `pickupId`、物品名称、协同 key、游戏中文名、英文源文本和当前中文文本
- 同时列出游戏资源本身没有中文名的协同，以及中英文条目数量不一致的记录

```powershell
python .\tools\translate\export_unresolved_game_synergy_names.py
```

### `export_pickup_gameplay_translation_batch.py`

作用：

- 从 `pickup-gameplay.zh-CN.work.json` 按 `pickupId` 区间导出一个临时翻译批次

它导出的不是原始最小数据，而是“翻译工作包”：

- 英文原文
- 当前中文
- 当前条目名
- 预本地化参考文 `localizedEnglish*`
- `sourceHash`

为什么要有它：

- 避免 agent 直接在 runtime v2 输出上编辑
- 让翻译修改在一个可局部审查、可局部重做的批次文件中完成
- 给后续扫描和回写提供稳定输入

### `scan_pickup_gameplay_quote_preservation.py`

作用：

- 检查英文原文里出现的 `"..."` 是否在中文里保留了对应英文片段

为什么要有它：

- 引号内容是最容易在翻译时被不小心删掉、改写掉的部分
- 这类错误很难靠肉眼大批量稳定发现
- 用脚本检查比继续在 Markdown 里强调“记得保留引号”更可靠

### `scan_pickup_gameplay_proper_noun_first_mentions.py`

作用：

- 扫描中文里残留的裸露外文专名
- 找出那些理论上应该写成 `中文（English）` 的位置

为什么要有它：

- 这是目前最常见的格式漂移来源之一
- 这类问题单靠阅读规则无法稳定执行
- 需要启发式扫描把问题批量暴露出来

这个脚本是“启发式工具”，不是绝对真理。

也就是说：

- 它的任务是尽量减少漏报
- 允许存在少量误报
- 后续通过白名单、标题忽略、括号模式优化逐步收紧

当前还专门排除了：

- 已经写成 `中文（English）` 的片段
- 协同标题前面的英文名

因为这两类在当前流程里属于允许状态，不应该继续打扰审查。

### `scan_pickup_gameplay_ui_control_terms.py`

作用：

- 扫描中文正文里残留的英文键位名、控制器术语和 UI 术语
- 例如 `Shift`、`D-pad`、`UI`、`HUD`

为什么要有它：

- 这类词不一定属于普通 proper noun 问题，更像专项本地化遗漏
- 单独报出来后，agent 更容易理解这是“键位/UI 术语残留”，而不是泛泛的英文漏翻
- 这类问题很适合给出明确改写建议，例如 `切换键（Shift）`、`十字键（D-pad）`

### `scan_pickup_gameplay_source_residue.py`

作用：

- 扫描英文源字段和中文翻译字段里的源码污染残留
- 例如 HTML 标签、wiki 模板括号、参数串、`label=` 这类抓取残片

为什么要有它：

- 这类问题不是翻译偏好问题，而是数据质量问题
- 单靠人工读文档，很难稳定区分“翻译写错了”还是“英文源本身就脏”
- 把它单独报出来之后，agent 可以更快判断是该清中文，还是该回头修英文源脚本

### `scan_pickup_gameplay_stat_value_labels.py`

作用：

- 扫描旧工作格式里对应 runtime `statSections[*].stats[*].parts[*].label` 的结构化英文标签
- 区分：
  - 已有安全映射、应该写进 `valueMappings` 的固定标签
  - 还没有定中文、需要先补词表的未知标签

为什么要有它：

- `pickup gameplay` 现在的正文翻译流程只覆盖 4 个正文中文字段
- 但游戏内面板还会直接显示 `stats[*].value`
- 按当前 schema v2 设计，`stats[*].value` 应保持英文源值不动，再通过 `pickup-info-terms.json` 的 `displayValues` 提供中文显示
- 像 `10 (Impact) 15 (Explosion) 3 (Bees)` 这类内容，适合由脚本给出映射覆盖建议，而不是直接改写 `value`

### `scan_pickup_gameplay_stat_value_source_drift.py`

作用：

- 直接对比 `zh-CN.work.json` 和 `en.json` 的 `stats[*].value`
- 检查这些结构字段是否偏离英文源值
- 发现差异时返回非零退出码，适合做硬检查

为什么要有它：

- `scan-stat-values` 负责映射覆盖建议，不直接判断 `value` 是否被改坏
- 这类问题需要一个一键可见、可接自动化的硬检查入口
- 如果发现漂移，可以直接配合 `restore-stat-values` 恢复

### `localize_pickup_gameplay_stat_value_labels.py`

作用：

- 按固定映射同步维护最终 runtime terms 里的 `displayValues`
- 不改 `stats[*].value` 本体

为什么要有它：

- 这类标签本质上是“固定词表问题”，不是自由翻译问题
- 只要映射稳定，就不应该反复让 agent 手改
- `stats[*].value` 的英文源值需要保持稳定，避免把结构字段翻坏
- 和扫描器配合后，可以形成：
  - `scan-stat-values` 看报告
  - 扩映射
  - `localize-stat-values` 回写最终 terms 映射

### `restore_pickup_gameplay_stat_values_from_english.py`

作用：

- 用 `defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.en.json` 恢复 `zh-CN.work.json` 里的 `stats[*].value`
- 只恢复结构化英文源值，不动最终 terms 映射目标

为什么要有它：

- 这类结构字段一旦被误本地化，不适合依赖 git 历史做粗粒度回退
- 直接按英文源文件定向恢复，更安全，也更符合当前数据设计

### `normalize_pickup_gameplay_item_names.py`

作用：

- 把翻译文本里能明确命中的游戏内英文物品名，替换成标准中文 `chineseDisplayName`

为什么要有它：

- 游戏里已有一套中文物品名
- 这部分不应该每次靠 agent 自己判断
- 统一替换可以显著减少译名漂移

它只处理“能确认命中的游戏内物品名”。

它不会负责：

- 外部作品名
- 现实人名
- 协同标题名
- agent 自己自由翻译出来的普通英文短语

### `apply_pickup_gameplay_translation_batch.py`

作用：

- 把批次里的中文翻译安全写回辅助工作格式，并为后续映射到 runtime v2 做准备

它会做的事情包括：

- 按 `pickupId` 匹配条目
- 校验 `sourceHash`
- 只更新允许回写的中文字段
- 标准化 `translationStatus`
- 更新时间和统计信息

为什么要有它：

- 防止 agent 直接手改 runtime v2 输出或辅助工作格式时误伤别的结构
- 防止基于旧英文源的批次覆盖新数据
- 保持回写动作可重复、可验证

### `check_pickup_gameplay_translation_batch.py`

作用：

- 做一轮区间级别的预检查
- 自动导出批次
- 自动运行现有扫描
- 汇总成一个 `check report`

这是目前最接近“验收入口”的脚本。

为什么要有它：

- 让流程从“翻完后人工想想哪里可能错了”变成“先跑验收，再按报告修”
- 让旧批次也能被重新验收，而不必重新人工通读全文
- 给后续 agent 一个统一入口，而不是让它们自己拼命令

当前 report 里已经会汇总：

- 缺翻字段
- 引号保留问题
- 专有名词首提问题
- `suggestedFix`
- 某些问题的 `suggestedRewriteTemplates`

这就是为什么后面推荐使用：

- `check -> 修 -> 再 check -> 回写`

而不是：

- 读长文档 -> 直接翻 -> 希望没出错

### `run_pickup_gameplay_translate_phase.py`

作用：

- 作为高层“翻译”命令入口
- 负责导出批次、初始检查
- 在需要时串起 `normalize -> check`

为什么要有它：

- 弱 agent 不需要记住多个底层命令的顺序
- 只要记住 `main.py translate ...` 就能进入正确流程

### `run_pickup_gameplay_postcheck_phase.py`

作用：

- 作为高层“翻译后”命令入口
- 负责自动化检查、`stats[*].value` 硬检查、命名检查
- 在人工检查通过后，可用 `--apply` 进入回写

为什么要有它：

- 把“自动化检查”和“人工检查后再回写”压成一个统一的后处理阶段
- 让高层流程真正只剩下两个入口命令

## 推荐理解顺序

如果你是后续接手的 agent，建议这样理解这些代码：

1. 先读 `MUST.md`
2. 再读 `README.md`
3. 然后看这份 `SCRIPTS.md`
4. 真正需要改逻辑时，再读：
   - `gameplay_translation_workflow.py`
   - 你要动的那一个脚本

这里的含义是：

- `MUST.md` / `README.md` 负责告诉你“高层只跑哪两个命令、按什么顺序跑、哪些边界不能碰”
- `SCRIPTS.md` 负责告诉你“这些脚本分别在兜什么问题”
- 具体格式要求和纠错建议，优先从脚本生成的 report 获取，而不是回头扩写 Markdown

## 一句话总结

这个目录里的 Python 脚本不是“翻译器”。

它们是这套翻译流程的护栏、检查器和回写器。

目标不是替 agent 做翻译决策，而是尽量把：

- 哪些内容能改
- 哪些格式必须统一
- 哪些问题必须暴露

变成可以重复执行、可以验证、可以收敛，并最终稳定落回 schema v2 runtime 输出的工程流程。
