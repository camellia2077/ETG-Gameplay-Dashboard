# v0.3.9 主题色系全面重构与准星颜色自定义 / Theme Color Overhaul & Mouse Crosshair Customization

[English description below / 英文说明见下]

## 版本摘要 (Highlights)
* 本次更新引入了控制面板(control panel)的主题色系全面重构与切换，以及自定义鼠标准星颜色，并将面板描边重构为三层描边样式，同时解决了此前打开面板时会锁定角色移动的问题。

## 新增功能 (Added)
* **主题色系全面重构与切换**：在控制面板(control panel)头部新增了主题切换按钮，内置 5 种不同风格的主题：默认(`Default`/`默认`，配色与三层描边样式均源于游戏内“枪弹之书”/Ammonomicon)、雪原(`Snowfield`/`雪原`)和赛博朋克(`Cyberpunk`/`赛博朋克`)；此外，火星圣物(`Mars Relic`/`火星圣物`，赞美万机之神！)与警戒(`Hazard`/`警戒`)的主题配色，均源于作者的另外一个开源项目 [FlipBits](https://github.com/camellia2077/FlipBits)（该软件主要用于生成各种电报音频、进行音频可视化及机械音变声）。除默认主题外，其余主题颜色与原版游戏无关。每种主题均配备量身定制的面板、边框 and 文字颜色，且文字颜色会根据主背景亮度自动切换为纯黑或纯白以保障可读性。
* **经典三层描边样式**：仿照游戏内“枪弹之书”(Ammonomicon)的经典书本页面三层边框样式，将原本简单的单层描边重构为包含外中内三色带的三重描边边框样式。默认(`Default`/`默认`)主题的颜色搭配同样参照了该书本设计，使控制面板(control panel)及附近物品提示信息面板(Nearby Pickup Tip Overlay)的边缘立体感和质感更完美地契合《挺进地牢》原作的像素视觉风格。
* **自定义鼠标准星颜色**：在常规(`General`)页面新增光标颜色(`Cursor Color`)设置页，可自定义修改游戏内鼠标瞄准准星（准星样式光标）的颜色。提供 8 种高饱和度颜色预设（包括青色、亮绿、金黄、粉色、红色、橙色、电光紫和电光蓝），并配备了调色板预览。
* **面板开启时移动优化**：优化了控制面板(control panel)开启时的游戏输入控制逻辑。现在打开控制面板时，键盘 WASD 移动和手柄(controller)左摇杆的角色移动将能够完全正常运作，不再会被强制锁定。

## 修复问题 (Fixed)
* **鼠标光标遮挡问题**：修复了在打开控制面板(control panel)时鼠标光标可能被面板遮挡（图层渲染在面板下方）的问题。现支持在面板开启时强制在最顶层重绘游戏原版鼠标光标（可通过配置文件中的 `EnableCommandPanelCursorAbovePanel` 选项进行开启或关闭）。
* **附近物品信息面板滚动遮挡**：重构了附近物品提示信息面板(Nearby Pickup Tip Overlay)的段落文本排版，解决了不同主题或界面比例下滚动视图的滚动条(scrollbar)遮挡与文本溢出排版异常。
* **预设随机重复切换**：改进了开局物品(start items)预设(preset)随机抽取机制，开启随机或再次启用时会自动排除当前显示的预设(preset)，避免了连续抽取到同一个预设时的无反馈现象。

## 游戏内按键与操作 (Controls)
* **键盘控制**：
  * 按 `F7`（默认，可在 `设置(settings)` 中修改）：打开/关闭 `控制面板(control panel)`。
  * 如果不想使用鼠标完成选择、切换等操作，也可以使用键盘，详见 `设置(settings)` 中的 `键盘说明(keyboard help)`。
* **手柄控制**：
  * 按手柄 `LB+R3` 组合键（默认，可在 `设置(settings)` 中修改或关闭手柄呼出开关）：打开/关闭 `控制面板(control panel)`。
  * 详细手柄操作与菜单导航详见 `设置(settings)` 中的 `手柄说明(controller help)`。

## 安装指南 (Installation)
1. 关闭《挺进地牢》(Enter the Gungeon) 游戏。
2. 下载本 Release 下方的 `ETG-Gameplay-Dashboard-v0.3.9-ETG.zip`。
3. 将压缩包内的所有内容直接解压到游戏安装根目录（即含有 `EtG.exe` 的目录），若提示同名文件请允许覆盖。
4. 启动游戏即可享受新版功能！

---

# v0.3.9 主题色系全面重构与准星颜色自定义 / Theme Color Overhaul & Mouse Crosshair Customization

## Highlights
* This release introduces theme color scheme overhaul and switching, custom mouse crosshair colors, and a rewrite of the panel borders to a three-layered outline style. It also resolves the movement lock issue while the panel is open.

## Added
* **Theme Color Overhaul & Switching**: Added a theme toggle button in the control panel header, supporting 5 built-in themes: Default (color palette and three-layered outline style are both inspired by the in-game Ammonomicon), Snowfield, and Cyberpunk; meanwhile, the Mars Relic (Praise the Omnissiah!) and Hazard themes feature built-in color schemes from the author's other open-source app [FlipBits](https://github.com/camellia2077/FlipBits) (a tool for generating telegraph audio, audio visualization, and robotic voice modulation). Other than the Default theme, the other theme colors have nothing to do with the original game. Each theme features dedicated panel, border, and text palettes, with text colors automatically adapting to black or white based on background brightness for optimal readability.
* **Classic Three-Layered Outline Style**: Inspired by the classic page border style of the in-game Ammonomicon, the panel borders have been rewritten to a three-layered outline style. The Default theme's color palette is also inspired by this book design, making both the control panel and the Nearby Pickup Tip Overlay match the native pixel art style of Enter the Gungeon.
* **Custom Crosshair/Reticle Colors**: Added a dedicated Cursor Color subpage under the General page, allowing players to customize the color of the in-game mouse aiming crosshair. Provides 8 high-saturation color presets (Cyan, Lime, Yellow, Pink, Red, Orange, Electric Violet, and Electric Blue) with preview swatches.
* **Movement Unlocking Under Panel**: Optimized the player input blocking logic when the control panel is active. Players can now move freely using keyboard WASD or the controller left stick while navigating the panel.

## Fixed
* **Mouse Cursor Layering Occlusion**: Fixed an issue where the mouse cursor could render underneath the control panel when moved over it. The game cursor is now drawn on top of the panel (can be configured via `EnableCommandPanelCursorAbovePanel` in the config file).
* **Nearby Pickup Overlay Layout**: Rebuilt the Nearby Pickup Tip Overlay layout using structured text blocks, resolving scrollbar occlusion and text overflow issues across different themes and interface scales.
* **Random Preset Repetition**: Improved the random start items selection deck so that re-enabling or cycling random presets excludes the currently displayed preset, preventing visual state stutter when the same preset is drawn consecutively.

## In-Game Controls
* **Keyboard**:
  * Press `F7` (default, configurable under Settings): Toggle the Gameplay Dashboard panel.
  * If you prefer not to use a mouse for selection and navigation, you can use the keyboard instead (see Keyboard Help in Settings for details).
* **Controller**:
  * Press `LB+R3` (default combination, configurable or can be disabled in Settings): Toggle the Gameplay Dashboard panel.
  * For detailed button mappings, see Controller Help in Settings.

## Installation Guide
1. Close `Enter the Gungeon`.
2. Download the release package `ETG-Gameplay-Dashboard-v0.3.9-ETG.zip` below.
3. Extract all archive contents directly into the game installation root directory (the folder containing `EtG.exe`), allowing file overwrite if prompted.
4. Launch the game and enjoy!
