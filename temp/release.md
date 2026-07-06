# ETG-Gameplay-Dashboard v0.3.6

[English description below / 英文说明见下]

## 版本摘要 (Highlights)

* **开源许可证变更（由 MIT 变更为 GPL-3.0-only）**
  
  **起因背景：**
  最近刷到某挺进地牢修改器分享视频时，发现它们仅提供了几个极其基础的功能。通过玩家在评论区的反馈得知，这些平台居然公然对此类基础功能进行收费。更搞笑的是，这些修改器的汉化是纯机翻，例如将“无需装弹 (No Reload)”翻译成“无重载”，将按 Q 键触发的“空响弹 (Blanks)”机翻为“空白”。
  
  **变更目的：**
  虽然 GPL 协议本身不限制商业分发，但它强制要求任何分发者都必须向使用者公开衍生版本的完整源代码，且允许使用者自由分发。这一“必须开源”的合规限制，可以有效防止这类依赖闭源套利、加塞广告的收费修改器平台在未来整合或使用本项目代码（因为它们无法在不开源其自身客户端的前提下合法复用），从而提前保障本项目的永久免费与开源透明性。

## 技术改进/重构 (Changed/Refactor)
* **[靠近拾取物图鉴性能重构]**：将靠近拾取物显示图鉴的功能由原先的“定时轮询扫描”优化为“事件驱动响应”，彻底解决在物品密集区域或 Bello 的商店中游玩时产生的游戏帧率卡顿(hitch)问题。

## 修复问题 (Fixed)
* **[修复 ID 为 0 的图鉴信息匹配]**：修复 ID 为 `0` 的物品“阿拉丁神灯 (Magic Lamp)”在靠近时无法在屏幕上正常显示详细物品图鉴提示信息的问题。
* **[修复商店/底座钩子运行时异常]**：修复在部分系统环境下靠近商店物品或 Boss 奖励底座时，物品图鉴提示信息无法正常弹出的运行时兼容性问题。

## 游戏内按键与操作 (Controls)
* `F7` 或手柄 `R3`：打开/关闭辅助控制面板。

## 安装指南 (Installation)
1. 关闭《挺进地牢》游戏。
2. 下载本 Release 下方的发布包（Standalone 独立版 `ETG-Gameplay-Dashboard-v0.3.6-standalone.zip` 包含全部依赖，推荐；或 Mod管理器版 `ETG-Gameplay-Dashboard-v0.3.6-mod-manager.zip`）。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能。

---

# ETG-Gameplay-Dashboard v0.3.6

## Highlights

* **License Change (from MIT to GPL-3.0-only)**
  
  **Background:**
  Recently, while scrolling through some showcase videos for Enter the Gungeon trainers/mods, I noticed they only offered a few bare-bones features. According to player feedback in the comments, these platforms actually have the audacity to charge users for such basic functions. To make matters funnier, their localizations are purely terrible machine translations. For instance, the shooter mechanic "No Reload" was literally mistranslated as a programming or system term like "No Overload" and "Blanks"—the consumable pickups triggered by pressing Q to clear enemy bullets—was blindly translated as a literal "Blank space/Empty."
  
  **Purpose of the Change:**
  While the GPL license itself does not restrict commercial distribution, it strictly mandates that any distributor must release the full source code of their derivative works to the users, allowing them to redistribute it freely. This copyleft restriction will effectively prevent those paid, closed-source modding platforms—which rely on paywalls and ad-injecting—from integrating or utilizing this project's code in the future, as they cannot legally reuse it without open-sourcing their own clients. This preemptively safeguards the permanent freeness, openness, and transparency of this project.

## Changed/Refactor
* **[Nearby Pickup Tip Performance Refactor]**: Refactored the nearby pickup tip overlay to be fully event-driven instead of using room-wide polling, resolving micro-stuttering/frame hitching when standing near many dropped items or in Bello's shop.

## Fixed
* **[Magic Lamp Wiki-tip Overlay]**: Fixed an issue where the item "Magic Lamp" with a catalog ID of `0` failed to load and display its nearby wiki-tip overlay.
* **[Shop Item and Reward Pedestal Hooks]**: Fixed a compatibility bug where nearby tips failed to pop up for shop items or boss reward pedestals under certain system environments.

## In-Game Controls
* `F7` or controller `R3`: Toggle the Gameplay Dashboard panel.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package (Standalone version `ETG-Gameplay-Dashboard-v0.3.6-standalone.zip` containing all dependencies, recommended; or Mod Manager version `ETG-Gameplay-Dashboard-v0.3.6-mod-manager.zip` below).
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
