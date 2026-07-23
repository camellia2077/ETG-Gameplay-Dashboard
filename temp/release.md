# ETG-Gameplay-Dashboard v0.3.10

[English description below / 英文说明见下]

## 版本摘要 (Highlights)
* 本次更新引入了下一层会生成 BOSS 的选择与 BOSS 战斗房间布局的选择、房间倒放时的玩家状态/属性(`stats`)回溯(rewind)（包含血量、护甲 armor、空响弹 blanks、枪械/弹药 guns、主动充能 active 与被动 passive，可按 `C` 键快捷触发）、键鼠辅助自瞄与手柄视角固定、角色属性倍率调节（伤害与移动速度）以及双人合作(Co-op)模式全功能玩家目标控制。

## 新增功能 (Added)
* **BOSS 选择与 BOSS 战斗房间的选择**：在房间(`Room`)菜单中新增 BOSS 选择功能，支持选择下一层关卡将要生成的 BOSS 种类（在大厅选择时作用于第 1 层关卡，局内选择时作用于下一层关卡），地牢在生成下一层关卡时将按照该设定直接生成指定的 BOSS；当选定 BOSS 包含多种原版房间地形时，面板提供不同 BOSS 战斗房间布局原型的选择。
* **房间倒放时的玩家状态回溯(rewind)**：在已被清理的战斗房间或 BOSS 房间触发房间倒放(`Rewind Room`/`Respawn Enemies`，支持按 `C` 键快捷触发) 时，本次更新新增了玩家状态/属性(`stats`)快照回溯(rewind)功能，可同步将玩家的血量(health)、护甲(armor)、空响弹(blanks)、枪械(guns，包括子弹)、主动道具(active items，包含充能状态)及被动道具(passive items)恢复至刚进入房间时的状态。
* **键鼠辅助自瞄(Keyboard Aim Assist)**：在战斗(`Combat`)页面新增键鼠辅助自动瞄准，提供 `普通自动瞄准` (`Auto Aim`，基础角度 15°) 与 `超级自动瞄准` (`Super Auto Aim`，基础角度 25°) 两种模式，支持 `0.5x`~`2.0x` 倍率调节，具备子弹预判与隔墙射线过滤，并带有实时倾角提示。
* **手柄视角固定(Controller Aim Lock)**：在战斗(`Combat`)页面新增手柄视角固定功能。开启后，当推动手柄右摇杆(`R3`)转动瞄准方向时，游戏视角镜头将始终平稳居中固定在玩家角色身上，而不随着 `R3` 摇杆的转动产生画面偏转与晃动。
* **双人合作模式目标控制与角色切换**：在图鉴浏览(`Pickup Browser`)、`常规(General)` 分类下的 `角色(Characters)` 页面、物品(`Pickups`)及战斗辅助页面全面接入 `P1`/`P2`/`Both` 目标控制；其中 `角色(Characters)` 页面支持单独指定替换 P1 或 P2 的角色，并新增了双人合作模式专属角色 `邪教徒(The Cultist)` 的切换选项（即 P2 的默认角色）。
* **角色属性倍率与辅助持久化**：新增玩家伤害倍率 (`1x`, `2x`, `5x`, `10x`, `100x`) 与移动速度倍率 (`1.5x`, `2x`, `3x`) 动态调节；无限弹药、自动装弹与按住连发等开关状态将自动保存并在下一次启动游戏时自动恢复。

## 修复问题 (Fixed)
* **双人合作目标解析修复**：修复在未接入第二位玩家(P2)时误选 P2 进行物品发放或角色切换导致的响应异常与空引用。
* **大厅角色切换镜头偏移**：修复在大厅(Foyer)重新选择角色时可能出现的双重碰撞体生成与镜头跟随偏移问题。

## 游戏内按键与操作 (Controls)
* **键盘控制**：
  * 按 `F7`（默认，可在 `设置(settings)` 中修改）：打开/关闭 `控制面板(control panel)`。
  * 按 `C`（默认，可在 `设置(settings)` 中修改）：在战斗房间或 BOSS 房间清理后触发敌人倒放/重置。
  * 如果不想使用鼠标完成选择、切换等操作，也可以使用键盘，详见 `设置(settings)` 中的 `键盘说明(keyboard help)`。
* **手柄控制**：
  * 按手柄 `LB+R3` 组合键（默认，可在 `设置(settings)` 中修改或关闭手柄呼出开关）：打开/关闭 `控制面板(control panel)`。
  * 详细手柄操作与菜单导航详见 `设置(settings)` 中的 `手柄说明(controller help)`。

## 安装指南 (Installation)
1. 关闭《挺进地牢》(Enter the Gungeon) 游戏。
2. 下载本 Release 下方的 `ETG-Gameplay-Dashboard-v0.3.10-ETG.zip`。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能！

---

# ETG-Gameplay-Dashboard v0.3.10

## Highlights
* This release introduces selection of the Boss to generate on the next floor and Boss combat room layout choices, player state/stat restoration upon room rewind (health, armor, blanks, guns/ammo, active item charge, and passive items, triggered via `C` key), keyboard aim assist and controller aim lock, player stat multipliers (damage and speed), and full Co-op target controls.

## Added
* **Boss Selection & Boss Combat Room Choices**: Added a Boss subpage under the Room menu, allowing players to select the Boss species for the next generated floor (targets Floor 1 when selected in the Foyer, or the next floor during a run), which the dungeon will generate accordingly upon level loading. If the selected Boss supports multiple room layouts, a second row enables choosing specific Boss combat room prototype layouts.
* **Player State Restoration Upon Room Rewind**: When rewinding or respawning a cleared combat room or Boss room (triggered via `C` key or panel), this update adds player state and stat snapshot restoration upon rewind—seamlessly resetting player health, armor, blanks, guns (including ammo), active item charge/cooldown states, and passive items back to room-entry state.
* **Keyboard Aim Assist**: Added Keyboard Aim Assist under the Combat page (featuring `Auto Aim` 15° base angle and `Super Auto Aim` 25° base angle modes, `0.5x`–`2.0x` multipliers, lead prediction, wall raycast filtering, and UI angle prompts).
* **Controller Aim Lock**: Added Controller Aim Lock under the Combat page. When enabled, pushing the right stick (`R3`) to rotate aiming direction suppresses camera offset, keeping the view centered on the player without camera swaying or shaking.
* **Full Co-op Target Controls & Character Switching**: Integrated `P1`/`P2`/`Both` target controls across Pickup Browser, Characters (under General), Pickups, and Combat pages. The Characters page now supports selecting P1 or P2 to swap characters independently, and adds a dedicated option for Co-op's default character, `The Cultist`.
* **Player Stat Multipliers & Saved Toggles**: Added dynamic player damage multipliers (`1x`, `2x`, `5x`, `10x`, `100x`) and movement speed multipliers (`1.5x`, `2x`, `3x`). Infinite Ammo, Auto Reload, and Rapid Fire settings are now saved automatically and restored on the next launch.

## Fixed
* **Co-op Target Resolution**: Fixed null-reference and status anomalies when P2 target actions were triggered without a second player present.
* **Foyer Character Switch Camera Offset**: Fixed double-collider spawning and camera tracking offset when switching characters in the Foyer.

## In-Game Controls
* **Keyboard**:
  * Press `F7` (default, configurable under Settings): Toggle the Gameplay Dashboard panel.
  * Press `C` (default, configurable under Settings): Rewind or respawn enemies in a cleared combat/Boss room.
  * See Keyboard Help in Settings for non-mouse keyboard navigation.
* **Controller**:
  * Press `LB+R3` (default combination, configurable under Settings): Toggle the Gameplay Dashboard panel.
  * See Controller Help in Settings for button mappings and navigation.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package `ETG-Gameplay-Dashboard-v0.3.10-ETG.zip` below.
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
