# ETG-Gameplay-Dashboard v0.3.8

[English description below / 英文说明见下]

## 版本摘要 (Highlights)
* 带来更直观的开局物品界面布局，支持图标分类预览、单条规则独立启用/禁用、随机选择预设，并提供更安全的手柄快捷键方案。

## 新增功能 (Added)
* **[开局物品预设列表图标预览]**：旨在允许玩家在点开并进入某个 `预设(preset)` 详情之前，除命名外能直观通过图标得知该预设的物品内容。在 `开局物品(start items)` 预设列表的卡片中，按 `枪械(gun)`、`主动(active)`、`被动(passive)` 分类并分行展示图标，且每个 `随机池(random pool)` 单独占用一行（行标签简写为 `随机(random)`）；提供一键开关来开启或关闭此预览。
* **[开局物品预设随机选择]**：此前玩家若想在每局游戏开局时体验不同的 `预设(presets)`，需要手动进行切换；现新增 `随机(random)` 功能，在开启该功能后或在每次开局发放完当前 `预设(preset)` 的瞬间，会自动抽取下一局将要发放的 `预设(preset)`，使 `控制面板(control panel)` 的激活显示与 UI 状态同步跟随下一次将要发放的配置；开启该功能后，手动选择 `预设(presets)` 的按键会被禁用；该 `随机(random)` 功能在关闭再重新开启后，会自动切换 to 与此前激活状态不同的其他 `预设(preset)`，且底层逻辑类似于将所有预设像扑克牌洗牌后依次发牌，在整副牌发完前绝不重复。
* **[开局物品规则启用与禁用]**：支持在 `控制面板(control panel)` 的 `开局物品(start items)` 编辑界面（即打开某个 `预设(presets)` 后），对单个特定添加物品或 `随机池(random pool)` 规则进行独立的 `开启/关闭(Enable/Disable)` 切换。该功能默认处于 `开启(Enable)` 状态，旨在允许玩家临时禁用 `预设(presets)` 中不想在开局发放的某些特定物品或随机池规则，且无需直接删除这些配置，以便日后重新启用。
* **[手柄快捷键配置与防误触优化]**：在 `设置(settings)` 页面中引入手柄快捷键配置项 `CommandPanelControllerShortcut`，提供 `LB+R3`（默认）、`LB+X`、`LB+Y` 以及 `R3`（长按 0.5 秒打开/短按关闭）四种弹出方式。此项修改旨在解决此前单按 `R3` 即刻弹出 `控制面板(control panel)` 在游玩中极易误触的问题（默认选项现已改为 `LB+R3`，且独立 `R3` 模式已调整为长按判定）；同时在 `手柄说明(Controller Help)` 页面中动态显示当前的绑定按键与新增组合键的操作指引。
* **[启用/禁用手柄呼出按键开关]**：在 `设置(settings)` 页面中新增“启用手柄呼出按键”开关（配置文件引入配置项 `DisableCommandPanelControllerShortcut`），默认开启该功能；若玩家关闭此开关，将完全禁用手柄快捷键弹出或关闭 `控制面板(control panel)` 的响应（键盘快捷键不受影响）；同时更新了 `设置(settings)` 页面的键盘方向键与 D-pad 焦点顺序，在新增设置项后仍保持从上到下线性移动。
* **[新增默认预设与界面加宽]**：配合 `开局物品(start items)` 面板拓宽且改为双列展示（一行两个卡片）的排版优化，解决了此前因展示空间不足仅限 2 个默认 `预设(presets)` 的限制，新增了 `defaults/presets/preset.shadow_warrior.json` (暗影战士) 与 `defaults/presets/preset.hard_light.json` (硬光)，使默认 `预设(presets)` 总数达到 4 个。由于默认及自定义 `预设(presets)` 增多后原先的显示空间有限，我们将 `开局物品(start items)` 状态下的 `控制面板(control panel)` 宽度由 612 像素拓宽至 900 像素以增加 UI 尺寸，并改为双列展示卡片且重构了行高计算。

## 修复问题 (Fixed)
* **[修复切换枪械时心形图标闪烁与护甲动画误触发问题]**：优化了最大生命值保护机制，并在恢复生命值状态时采用静默更新，避免了切换枪械时生命值心形图标闪烁、以及由于 HUD 刷新重建导致的护甲受损/恢复动画异常播出的问题。
* **[修复调试增加最大生命值时 HUD 动画重复播出问题]**：解决在调试命令中增加心限时，同时修改最大值与恢复血量产生重复的血量改变事件并导致 HUD 重复播放增加心心动画的回归问题。

## 游戏内按键与操作 (Controls)
* **键盘控制**：
  * 按 `F7`（默认，可在 `设置(settings)` 中修改）：打开/关闭 `控制面板(control panel)`。
  * 如果不想使用鼠标完成选择、切换等操作，也可以使用键盘，详见 `设置(settings)` 中的 `键盘说明(keyboard help)`。
* **手柄控制**：
  * 按手柄 `LB+R3` 组合键（默认，可在 `设置(settings)` 中修改或关闭手柄呼出开关）：打开/关闭 `控制面板(control panel)`。
  * 详细手柄操作与菜单导航详见 `设置(settings)` 中的 `手柄说明(controller help)`。

## 安装指南 (Installation)
1. 关闭《挺进地牢》(Enter the Gungeon) 游戏。
2. 下载本 Release 下方的发布包（Standalone 独立版 `ETG-Gameplay-Dashboard-v0.3.8-standalone.zip` 包含全部依赖，推荐；或 Mod管理器版 `ETG-Gameplay-Dashboard-v0.3.8-mod-manager.zip`）。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能！

---

# ETG-Gameplay-Dashboard v0.3.8

## Highlights
* Brings a more intuitive and elegant start items UI layout, supporting icon categorization preview, individual rule toggles, random preset selection, and safer controller shortcut configurations.

## Added
* **[Start Items Preset List Icon Preview]**: Designed to allow players to visually review a preset's contents via icons without clicking to open details. Icons are categorised and displayed on separate rows for guns, actives, passives on preset cards, and each random pool occupies its own row (labeled as "random" for space efficiency). A toggle is provided on the page to enable/disable the preview.
* **[Random Preset Selection]**: Added a random mode to draw presets automatically. Previously players had to switch presets manually for every run; now it automatically draws the next preset immediately after granting the current loadout, keeping the active preset and UI selection state synchronized with the next grant. Manual preset selection is disabled when enabled, and toggling it off and on again guarantees selecting a different preset (shuffled like a deck of playing cards and drawn sequentially without repeating).
* **[Individual Rule Enable/Disable Toggles]**: Allows players to toggle individual item or random pool rules on or off inside the preset editor. The rules are enabled by default. This is designed to let players temporarily disable specific rules in a preset without deleting them so they can be reused later.
* **[Controller Shortcut Configurations]**: Introduced `CommandPanelControllerShortcut` under settings, offering `LB+R3` (default), `LB+X`, `LB+Y`, and `R3` (0.5s hold to open / short press to close) options. This prevents accidental panel triggers on simple R3 presses during active gameplay. Controller Help now shows dynamic layout guides.
* **[Enable/Disable Controller Shortcuts Switch]**: Added an "Enable Controller Shortcut" toggle in settings (bound to `DisableCommandPanelControllerShortcut` in config, defaults to enabled). Disabling it blocks all controller inputs from opening/closing the panel while keeping keyboard input active. D-pad navigation focus is adjusted to maintain linear vertical movement.
* **[New Default Presets and Widened UI]**: Expanded default presets from 2 to 4 by adding `preset.shadow_warrior.json` (Shadow Warrior) and `preset.hard_light.json` (Hard Light). Due to limited display space for more presets, the panel width in loadout editor is widened from 612px to 900px to accommodate a 2-column card layout with reworked row height calculations.

## Fixed
* **[Fixed HUD Heart Flashing and Armor Animation on Gun Changes]**: Optimized the maximum health protection logic and utilized silent status updates when restoring health variables, resolving heart icon flashing and unexpected armor damage/restoration animations during gun swaps.
* **[Fixed Duplicate Heart Gain HUD Animation in Debug]**: Fixed a regression where adding hearts in debug triggered duplicate events and repeated the heart-gain HUD animation.

## In-Game Controls
* **Keyboard**:
  * Press `F7` (default, configurable under Settings): Toggle the Gameplay Dashboard panel.
  * If you prefer not to use a mouse for selection and navigation, you can use the keyboard instead (see Keyboard Help in Settings for details).
* **Controller**:
  * Press `LB+R3` (default combination, configurable or can be disabled in Settings): Toggle the Gameplay Dashboard panel.
  * For detailed button mappings, see Controller Help in Settings.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package (Standalone version `ETG-Gameplay-Dashboard-v0.3.8-standalone.zip` containing all dependencies, recommended; or Mod Manager version `ETG-Gameplay-Dashboard-v0.3.8-mod-manager.zip` below).
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
