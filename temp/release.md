# ETG-Gameplay-Dashboard v0.4.1

[English description below / 英文说明见下]

## 版本摘要 (Highlights)
* 本版本带来了显著的控制面板开启与界面加载性能提升，大幅降低了按 `F7` 呼出面板时的等待延迟，并在 Boss Rush 模式(Boss Rush)激活期间暂时禁用房间回溯(Rewind)。此外，彻底解耦并移除了对 ModTheGungeonAPI (`etgmodding.etg.mtgapi`) 的运行与代码依赖。

## 性能与体验优化 (Optimizations)
* **面板开启响应提速**：优化了控制面板(control panel)标题文字的渲染机制，将按 `F7` 首次呼出控制面板的响应延迟降低了约 77%（从约 694 ms 缩短至约 158 ms），使面板呼出更加流畅快速。
* **初始物品界面滑动优化**：重构了初始物品(Start Items)预设界面的图标加载机制，改为分帧队列增量加载图标，消除了打开与滑动预设卡片列表时的掉帧与卡顿。

## 机制与行为调整 (Changes)
* **Boss Rush 房间回溯拦截**：在 Boss Rush 模式(Boss Rush)激活期间暂时禁用房间回溯(Rewind)。通过拦截面板中的 Rewind 按钮及 `C` 快捷键(hotkey)触发并给出提示，避免在 Boss Rush 下使用回溯导致无法进入下一层且无法使用 Esc 菜单(Esc menu)的问题。
* **移除 ModTheGungeonAPI 强依赖**：移除了插件主程序对 ModTheGungeonAPI (`etgmodding.etg.mtgapi`) 的 `BepInDependency` 显式依赖声明，并将内部协程调度与弹药锁定逻辑替换为原生 Unity 组件（移除 `ETGMod` 与 `GunBehaviour` 依赖）。

## 游戏内按键与操作 (Controls)
* **键盘控制**：
  * 按 `F7`（默认，可在 `设置(settings)` 中修改）：打开/关闭 `控制面板(control panel)`。
  * 如果不想使用鼠标完成选择、切换等操作，也可以使用键盘，详见 `设置(settings)` 中的 `键盘说明(keyboard help)`。
* **手柄控制**：
  * 按手柄 `LB+R3` 组合键（默认，可在 `设置(settings)` 中修改或关闭手柄呼出开关）：打开/关闭 `控制面板(control panel)`。
  * 详细手柄操作与菜单导航详见 `设置(settings)` 中的 `手柄说明(controller help)`。

## 安装指南 (Installation)
1. 关闭《挺进地牢》(Enter the Gungeon) 游戏。
2. 下载本 Release 下方的 `ETG-Gameplay-Dashboard-v0.4.1-ETG.zip`。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能！

---

# ETG-Gameplay-Dashboard v0.4.1

## Highlights
* This release introduces significant performance optimizations for opening the panel and browsing loadout presets, while temporarily disabling room Rewind during Boss Rush mode and fully decoupling the runtime from ModTheGungeonAPI.

## Optimizations
* **Faster Panel Opening**: Optimized the panel title rendering mechanism to reduce the initial `F7` panel open latency by ~77% (from ~694 ms down to ~158 ms), making the dashboard feel much more responsive.
* **Smoother Loadout Preset Browsing**: Refactored icon loading for `Start Items` presets to incrementally generate preview icons across frames, eliminating stutter and frame drops when opening or scrolling through preset cards.

## Changes & Protections
* **Boss Rush Rewind Interception**: Temporarily disabled room Rewind during Boss Rush mode. Intercepts panel Rewind button presses and the `C` hotkey during Boss Rush to prevent issues where players cannot proceed to the next floor or use the Esc menu.
* **Removed ModTheGungeonAPI Dependency**: Removed the explicit `BepInDependency` attribute for ModTheGungeonAPI (`etgmodding.etg.mtgapi`), and replaced internal coroutines and ammo locking handlers with native Unity MonoBehaviours (removing `ETGMod` and `GunBehaviour` dependencies).

## In-Game Controls
* **Keyboard**:
  * Press `F7` (default, configurable under Settings): Toggle the Gameplay Dashboard panel.
  * If you prefer not to use a mouse for selection and navigation, you can use the keyboard instead (see Keyboard Help in Settings for details).
* **Controller**:
  * Press `LB+R3` (default combination, configurable or can be disabled in Settings): Toggle the Gameplay Dashboard panel.
  * For detailed button mappings, see Controller Help in Settings.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package `ETG-Gameplay-Dashboard-v0.4.1-ETG.zip` below.
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
