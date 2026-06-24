---
description: 游戏辅助面板（Gameplay-Dashboard）GitHub Release 双语描述格式指南
---

# GitHub Release Style Guide

本指南规定了 `ETG-Gameplay-Dashboard` 在 GitHub 发布版本（Release）时，双语描述内容的设计原则、格式规范与模块结构。

由于本项目是一个面向玩家的游戏辅助/优化面板，其发布说明在定位和内容上必须完全面向最终用户。

---

## 核心设计原则

1. **玩家视角优先 (Player-Centric)**
   - 避免提及纯代码重构、类名修改、内部接口优化、开发者测试脚本等玩家不可感知的技术细节。
   - 仅仅介绍新增了什么功能、修复了什么问题、对游玩流程有什么改善。

2. **绝对禁止使用 Emoji (No Emojis)**
   - 全文（包括标题、正文、列表项、安装步骤）严禁使用任何 Emoji 表情符号。
   - 保持发布说明的排版干净、专业与规范。

3. **包含操作与按键指南 (Controls & Keybinds)**
   - 必须指明如何呼出面板（如 `F7` 键）。
   - 任何涉及按键触发、菜单导航或快捷开启的功能，必须明确写出操作方式。

4. **开箱即用的安装引导 (Plug-and-Play Install Guide)**
   - 提供简单明了的安装与清理步骤，告知如何解压到游戏根目录。
   - 说明发布包内已集成的依赖项（如已捆绑 `BepInEx` 及 `ModTheGungeonAPI` 依赖）。

5. **严谨的双语隔离 (Bilingual Separation)**
   - 采用**中文在上，英文在下**的垂直排列结构，中间使用 `---` 分割线隔离。
   - 双语的排版格式、加粗重点及代码块应完全对称。

---

## 描述模板 (Markdown Template)

GitHub Release 发布说明必须严格按照以下模板结构进行填充（严禁任何 Emoji）：

```markdown
# ETG-Gameplay-Dashboard v[X.Y.Z]

[English description below / 英文说明见下]

## 版本摘要 (Highlights)
* 用一两句话概括本次更新的主要玩点或重大优化。

## 新增功能 (Added)
* **[新功能名称]**：描述该功能对玩法的改变以及它在游戏中的表现。
* **[操控/界面优化]**：描述新增的按键配置、界面缩放或操作便捷性改进。

## 修复问题 (Fixed)
* **[修复内容]**：描述修复了什么游戏异常、崩溃或属性回滚等问题，以及修复后的表现。

## 游戏内按键与操作 (Controls)
* `F7`：打开/关闭辅助控制面板。
* `[其他按键]`：触发特定功能或进行快捷切换。

## 安装指南 (Installation)
1. 关闭《挺进地牢》游戏。
2. 下载本 Release 下方的 `ETG-Gameplay-Dashboard-v[X.Y.Z]-ETG.zip`。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能。

---

# ETG-Gameplay-Dashboard v[X.Y.Z]

## Highlights
* Summarize the major gameplay optimizations of this release in 1-2 sentences.

## Added
* **[Feature Name]**: Describe what it changes in terms of gameplay and how it behaves.
* **[Control/UI Optimizations]**: Describe new key bindings, UI scaling, or accessibility improvements.

## Fixed
* **[Fixed Issue]**: Describe what bug, crash, or stat rollback was fixed, and how it behaves now.

## In-Game Controls
* `F7`: Toggle the Gameplay Dashboard panel.
* `[Other Key]`: Trigger specific actions or toggle states.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package `ETG-Gameplay-Dashboard-v[X.Y.Z]-ETG.zip` below.
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
```

---

## 写作规范细节

- **词汇对齐**：
  - 初始装备/初始物品统一译为 `Start Items` 或 `custom starting loadouts`
  - 预设译为 `Presets`
  - 冷却方式译为 `cooldown types` / `cooldown categories`
  - 属性面板译为 `Character Stats`
- **格式一致**：
  - 按键名称（如 `F7`）、参数项（如 `--clean-config`）、文件路径统一用反引号包围。
  - 强调的功能短语使用 **加粗**。
