# ETG-Gameplay-Dashboard v0.3.7

[English description below / 英文说明见下]

## 版本摘要 (Highlights)
* 本次更新优化了靠近物品图鉴的数据集与读取逻辑，提升游戏启动时物品信息配置文件的加载速度，并支持动态显示您当前实际绑定的键盘快捷键。

## 新增功能 (Added)
* **[数据读取与加载优化]**：重构了 `靠近物品信息显示(nearby pickup info)` 的数据加载架构为 Schema V2，将原本分散庞大的中英文工作文件进行合并与重构，并引入高效的对象级 JSON 解析。在优化结构的同时，文件总行数精简了 25,146 行（降幅约 29.73%），通过合并相同字段剔除了 20,287 个重复的属性数据标签（降幅约 40.83%），极大降低了启动加载时间与运行时的内存占用。
* **[快捷键提示动态化]**：控制面板顶部的开启与关闭提示信息现已支持动态刷新，会根据您在配置文件中绑定的实际按键实时显示当前的 `键盘(keyboard)` 快捷键（如 `F7`、`F8` 等），而非显示表意模糊的静态提示文本。
* **[界面文本与排版规范化]**：
  - 将 `设置(settings)` 页面中 `手柄说明(Controller Help)`、`键盘说明(Keyboard Help)`、`高级工具(Advanced Tools)` 选项右侧的动作按钮由原先误导性的 `Change / 切换` 修改为符合实际行为的 `View Details / 查看详情`（仅对只展示信息而不切换选项的按钮进行调整，以符合其实际行为）。
  - 将英文 Pickups 页面标题由 `Pickup Browser` 变更为 `Items`，以解决此前与 General 分类下 `Pickups` 词汇重复的问题（中文标题“物品”保持未变）。

## 修复问题 (Fixed)
* **[修复图鉴数字键加载异常]**：修复了由于引入新解析器而导致 `pickups` 字典对象中纯数字键（如 `"0"`、`"541"`）解析失效的回归问题，完全恢复了 `靠近物品信息显示(nearby pickup info)` 的正常加载与显示。
* **[修复设置页文本裁剪]**：修复了设置页面底部声明文字由于显示区域高度不足和未自动换行而被截断的问题，现已启用自动换行并将行高合理增至 72 像素。

## 游戏内按键与操作 (Controls)
* `F7` 或手柄 `R3`：打开/关闭辅助控制面板。

## 安装指南 (Installation)
1. 关闭《挺进地牢》游戏。
2. 下载本 Release 下方的发布包（Standalone 独立版 `ETG-Gameplay-Dashboard-v0.3.7-standalone.zip` 包含全部依赖，推荐；或 Mod管理器版 `ETG-Gameplay-Dashboard-v0.3.7-mod-manager.zip`）。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能。

---

# ETG-Gameplay-Dashboard v0.3.7

## Highlights
* Reorganized the database and parsing logic for the nearby pickup tips to speed up the loading of item configuration files during game startup, while adding support for dynamic custom keyboard shortcut display.

## Added
* **[Data Loading & Performance Optimization]**: Reorganized the `nearby pickup info` catalog schema to V2. The database has been separated, merged, and processed via a structured JSON library. This optimization reduces the dataset by 25,146 lines (~29.73%) and eliminates 20,287 duplicate data tags (~40.83% reduction in metadata labels), drastically improving startup catalog loading speed and reducing memory usage.
* **[Dynamic Shortcut Display]**: The control panel's toggle instructions now dynamically display your actual configured `keyboard` key (e.g. `F7`, `F8`) rather than displaying a static, vague text description.
* **[UI Polish & Accessibility]**:
  - Renamed action buttons for `Controller Help`, `Keyboard Help`, and `Advanced Tools` on the `settings` page from `Change` to `View Details` to better reflect their actual behavior, while keeping `Change` for true toggle settings.
  - Renamed the English page title of the `pickups` tab from `Pickup Browser` to `Items` to eliminate naming conflict with the `Pickups` subcategory under the General tab (Chinese title remains "物品").

## Fixed
* **[Wiki-tip Numeric Key Regression]**: Fixed a regression caused by the parser refactoring where numeric object keys (such as `"0"` and `"541"`) failed to parse, restoring the `nearby pickup info` display.
* **[Settings Page Text Clipping]**: Fixed a layout issue where the disclaimer label at the bottom of the `settings` page was clipped due to insufficient height and lack of word wrap. The area has been adjusted to 72 pixels in height with word wrapping enabled.

## In-Game Controls
* `F7` or controller `R3`: Toggle the Gameplay Dashboard panel.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package (Standalone version `ETG-Gameplay-Dashboard-v0.3.7-standalone.zip` containing all dependencies, recommended; or Mod Manager version `ETG-Gameplay-Dashboard-v0.3.7-mod-manager.zip` below).
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
