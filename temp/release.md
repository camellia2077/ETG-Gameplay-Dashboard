# ETG-Gameplay-Dashboard v0.4.7

[English description below / 英文说明见下]

## 版本摘要 (Highlights)

本版本扩展了玩家战斗与属性控制，新增飞行(Flight)、跳过充能(Skip Charge)和 Projectile 调整功能，并统一了拾取物(pickups)、货币和房间回溯(Room Rewind)的键盘快捷键配置。同时改进了房间回溯说明页面和控制面板快捷键设置体验。

## 新增功能 (Added)

* **玩家战斗控制**：在 `Player → Combat` 中新增飞行(Flight)和跳过充能(Skip Charge)。启用跳过充能后，蓄力枪械会直接以满充能状态射击。
* **玩家属性编辑**：在 `Player → Character → Stats` 中编辑伤害、移动速度、酷气值(Coolness)、诅咒值(Curse)和华丽值(Magnificence)，并可使用说明按钮查看属性含义。
* **Projectile 控制**：在 `Player → Character → Projectiles` 中调整子弹大小、子弹速度、换弹速度和扩散(Spread)。支持子弹大小 `1` 至 `30`、子弹速度 `1` 至 `999`、换弹速度 `0.25` 至 `4.0`、扩散 `0` 至 `999`。
* **统一键盘快捷键**：拾取物、货币和房间回溯现在使用统一的快捷键设置界面。可设置、清除快捷键，并在捕获按键时显示提示；控制面板保留的按键不能被分配。
* **房间回溯说明**：为玩家回溯(Player Rewind)、房间残留清理(Room Residual Cleanup)和回溯模式新增信息页面及 Back/返回操作。

## 修复问题 (Fixed)

* **信息页面关闭**：修复信息页面关闭按钮无法正常通过鼠标操作的问题，统一使用次级页面 Back/返回逻辑。
* **控制面板快捷键**：修复设置控制面板自定义快捷键后，按键仍可能再次触发控制面板开关的问题。
* **子弹尺寸显示**：修复调整子弹尺寸后，Projectile 精灵显示深度可能被 ETG 重置，导致子弹显示层级异常的问题；现在会恢复正确的精灵深度。

## 游戏内按键与操作 (Controls)

* **键盘控制**：
  * 按 `F7`（默认，可在 `设置(settings)` 中修改）：打开或关闭 `控制面板(control panel)`。
  * 在 `设置(settings)` 中可以配置控制面板快捷键，并使用 Cycle Preset/切换预设、Set Shortcut/设置快捷键和 Reset Default/恢复默认值。
  * 在 `拾取物(pickups)` 页面或 `Player → Character → Pickups` 中选择 Set Shortcuts/设置快捷键，按下要绑定的键，再选择清除按钮即可移除绑定。
  * 在 `Room → Rewind` 中选择 Set Shortcut/设置快捷键，为房间回溯配置快捷键。拾取物快捷键和房间回溯快捷键不能使用同一个按键。
  * 如果不想使用鼠标完成选择、切换等操作，可以使用键盘，详见 `设置(settings)` 中的 Keyboard Help/键盘说明。
* **手柄控制**：
  * 按手柄 `LB+R3` 组合键（默认，可在 `设置(settings)` 中修改或关闭手柄呼出开关）：打开或关闭 `控制面板(control panel)`。
  * 详细手柄操作与菜单导航详见 `设置(settings)` 中的 Controller Help/手柄说明。

## 安装指南 (Installation)

1. 关闭《挺进地牢》(Enter the Gungeon) 游戏。
2. 下载本 Release 下方的 `ETG-Gameplay-Dashboard-v0.4.7-ETG.zip`。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 发布包已集成所需的 `BepInEx` 和 `ModTheGungeonAPI` 依赖文件。
5. 启动游戏即可使用新版功能。

---

# ETG-Gameplay-Dashboard v0.4.7

## Highlights

This release expands player combat and stat controls with Flight, Skip Charge, and projectile adjustments. It also unifies keyboard shortcut configuration for pickups, currency actions, and Room Rewind while improving Room Rewind information pages and command-panel shortcut setup.

## Added

* **Player combat controls**: `Player → Combat` now includes Flight and Skip Charge. When Skip Charge is enabled, charged guns fire immediately at full charge.
* **Player stat editing**: `Player → Character → Stats` now supports editing damage, movement speed, Coolness, Curse, and Magnificence, with information buttons explaining each stat.
* **Projectile controls**: `Player → Character → Projectiles` now supports projectile size, projectile speed, reload speed, and spread. The supported ranges are size `1` to `30`, speed `1` to `999`, reload speed `0.25` to `4.0`, and spread `0` to `999`.
* **Unified keyboard shortcuts**: Pickup, currency, and Room Rewind shortcuts now use one configuration flow. Shortcuts can be assigned or cleared, with an on-screen prompt during key capture; keys reserved by the command panel cannot be assigned.
* **Room Rewind information**: Added information pages and Back navigation for Player Rewind, Room Residual Cleanup, and the selected rewind mode.

## Fixed

* **Information page closing**: Fixed information pages not closing correctly through mouse interaction by using the same secondary-page Back flow everywhere.
* **Command-panel shortcut handling**: Fixed a custom command-panel key being able to trigger the panel toggle again after it was configured.
* **Projectile size rendering**: Fixed an issue where ETG could reset the Projectile sprite depth after changing projectile size, causing incorrect display layering. The correct sprite depth is now restored.

## In-Game Controls

* **Keyboard**:
  * Press `F7` (default, configurable under `Settings`): Toggle the `Gameplay Dashboard` panel.
  * Configure the command-panel key under `Settings` with Cycle Preset, Set Shortcut, and Reset Default.
  * In the `Pickups` page or `Player → Character → Pickups`, select Set Shortcuts, press a key to assign it, or use the clear button to remove the binding.
  * In `Room → Rewind`, select Set Shortcut to configure the Room Rewind key. Pickup shortcuts and the Room Rewind shortcut cannot share a key.
  * If you prefer not to use a mouse for selection and navigation, use the keyboard instead (see Keyboard Help in `Settings`).
* **Controller**:
  * Press `LB+R3` (default combination, configurable or can be disabled in `Settings`): Toggle the `Gameplay Dashboard` panel.
  * For detailed button mappings, see Controller Help in `Settings`.

## Installation Guide

1. Close `Enter the Gungeon`.
2. Download the release package `ETG-Gameplay-Dashboard-v0.4.7-ETG.zip` below.
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. The release package includes the required `BepInEx` and `ModTheGungeonAPI` dependencies.
5. Launch the game and enjoy.
