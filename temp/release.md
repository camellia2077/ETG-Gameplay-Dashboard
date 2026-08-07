**v0.4.3 控制面板关闭按键、拾取物快捷键与地图范围优化 / Close Button, Pickup Shortcuts & Map Scope Improvements**

[English description below / 英文说明见下]

## 版本摘要 (Highlights)

本版本为控制面板(Control Panel)新增关闭按键，新增 Reveal Map 的楼层全开范围选择，并为拾取物(Pickups)提供可自定义的键盘快捷键。

## 新增功能 (Added)

* **控制面板关闭按钮**：主页面和子页面均可使用关闭按钮(`×`)退出控制面板(Control Panel)，并可在设置(settings)的显示(Display)分类中控制是否显示。
* **Reveal Map 楼层全开范围设置**：在设置(settings)的游戏(Game)分类中选择仅当前楼层全开(`Current Floor`)，或进入任意楼层都自动全开(`Every Floor`)。
* **拾取物键盘快捷键**：
  * 在 General → 拾取物(Pickups)中配置血量上限(Max HP)、护甲(Armor)、空响弹(Blank)、钥匙(Key)、老鼠钥匙(Rat Key)、弹壳(Casings)和霸权币(Hegemony Credit)的快捷键。
  * 在 Player → Characters → 拾取物(Pickups)中编辑适用于角色的快捷键，并与 General 页面共享配置。
  * 编辑快捷键时直接切换当前页面内容，顶部按钮显示“退出编辑”(Exit Editing)，退出后恢复原有操作内容。
  * 关闭控制面板(Control Panel)后，按下快捷键即可执行对应拾取物操作。
  * 仅支持键盘按键，不支持手柄绑定；控制面板占用的按键不可绑定。
  * 支持左右 Shift、Ctrl、Alt 以及常用标点按键的本地化显示。

## 修复问题 (Fixed)

* **修复楼层切换异常**：修复启用 `Every Floor` 后通过楼梯或电梯进入下一层时，电梯动画未完成或玩家被卡在入口位置的问题。

## 游戏内按键与操作 (Controls)

* **键盘控制**：
  * 按 `F7`（默认，可在设置(settings)中修改）：打开或关闭控制面板(Control Panel)。
  * 在 General → 拾取物(Pickups)或 Player → Characters → 拾取物(Pickups)中点击“设置快捷键”，再按下要绑定的键盘按键；点击“退出编辑”(Exit Editing)返回普通操作内容。
  * 如果不想使用鼠标完成选择、切换等操作，也可以使用键盘，详见设置(settings)中的键盘说明(Keyboard Help)。
* **手柄控制**：
  * 按手柄 `LB+R3` 组合键（默认，可在设置(settings)中修改或关闭手柄呼出开关）：打开或关闭控制面板(Control Panel)。
  * 详细手柄操作与菜单导航详见设置(settings)中的手柄说明(Controller Help)。

## 安装指南 (Installation)

1. 关闭《挺进地牢》(Enter the Gungeon)游戏。
2. 下载本 Release 下方的 `ETG-Gameplay-Dashboard-v0.4.3-ETG.zip`。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 发布包已集成所需的 `BepInEx` 和 `ModTheGungeonAPI` 依赖文件。
5. 启动游戏即可使用新版功能。

---

# ETG-Gameplay-Dashboard v0.4.3

## Highlights

This release improves Gameplay Dashboard closing and navigation, adds floor-range control for Reveal Map, and introduces configurable keyboard shortcuts for pickups.

## Added

* **Gameplay Dashboard close button**: Use the `×` close button on the main page and sub-pages, with visibility controlled from Display settings.
* **Reveal Map floor scope**: Choose `Current Floor` to reveal the map only on the current floor, or `Every Floor` to reveal the map automatically on every floor under Game settings.
* **Pickup keyboard shortcuts**:
  * Configure shortcuts for Max HP, Armor, Blank, Key, Rat Key, Casings, and Hegemony Credit under General → Pickups.
  * Edit applicable character pickup shortcuts under Player → Characters → Pickups using the same shared bindings as General.
  * Shortcut editing changes the current page content in place; the top button becomes `Exit Editing` and restores the normal pickup actions.
  * Close the Gameplay Dashboard panel, then press a configured key to execute the corresponding pickup action.
  * Only keyboard keys are supported for bindings. Controller bindings are not supported, and keys reserved by the panel cannot be assigned.
  * Left/right Shift, Ctrl, Alt, and common punctuation keys have localized display names.

## Fixed

* **Fixed floor transition issues**: Fixed elevator animation and player positioning problems when entering the next floor through stairs or elevators with `Every Floor` enabled.

## In-Game Controls

* **Keyboard**:
  * Press `F7` (default, configurable under Settings): Toggle the Gameplay Dashboard panel.
  * In General → Pickups or Player → Characters → Pickups, click `Set Shortcuts`, press the keyboard key to assign, and click `Exit Editing` to restore the normal pickup actions.
  * If you prefer not to use a mouse for selection and navigation, see Keyboard Help in Settings.
* **Controller**:
  * Press `LB+R3` (default combination, configurable or can be disabled in Settings): Toggle the Gameplay Dashboard panel.
  * For detailed button mappings, see Controller Help in Settings.

## Installation Guide

1. Close `Enter the Gungeon`.
2. Download `ETG-Gameplay-Dashboard-v0.4.3-ETG.zip` from this release.
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. The release package includes the required `BepInEx` and `ModTheGungeonAPI` dependency files.
5. Launch the game and enjoy the new features.
