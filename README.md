# ETG-Gameplay-Dashboard

中文 | [English](README_en.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-0078d4.svg)](#)

`ETG-Gameplay-Dashboard` 是一个基于 `BepInEx` 的《挺进地牢》（Enter the Gungeon）游戏体验优化项目。

本项目目前主要专注于简化开局流程并提供更具视觉化的体验：

- 图形化的物品类别浏览与筛选
- 图形化的自定义开局初始装备物品发放
- 辅助控制面板工具，用于优化每一局游戏过程中的即时游玩体验

项目的核心设计思想是改善整体的游玩工作流，而非仅仅作为一个传统的外挂/修改器。即便如此，它也在合适的地方集成了常用的修改/辅助功能，例如运行时指令、调试辅助以及可配置的游戏参数调整。

## 安装与使用

1. 从 [Releases](https://github.com/camellia2077/ETG-Gameplay-Dashboard/releases) 页面下载最新版本的发布压缩包（如 `ETG-Gameplay-Dashboard-vX.Y.Z-ETG.zip`）并解压。
2. 找到 Windows 上《挺进地牢》（Enter the Gungeon）的游戏安装目录。
   - 通常位于：`steam\steamapps\common\Enter the Gungeon`。
   - **快速查找方法**：在 Steam 库中，右键点击左侧列表的《挺进地牢》游戏，选择 **管理** -> **浏览本地文件** 即可直接打开游戏目录。
3. 将解压出来的文件夹**内部的所有文件与子文件夹**（包括 `BepInEx`、`winhttp.dll` 等）复制并覆盖粘贴到 `Enter the Gungeon` 游戏安装目录下。
   > [!IMPORTANT]
   > 注意：
   > **复制解压后文件夹内部的所有子文件与文件夹！**
   > **复制解压后文件夹内部的所有子文件与文件夹！**
   > **复制解压后文件夹内部的所有子文件与文件夹！**
   > （不要复制解压后的单个文件夹）。
4. 打开游戏，在游戏中按下 `F7` 键即可开启或关闭操作面板。

## 开发

有关开发说明、架构设计预期以及 Agent 交接指南，请阅读：

- [src/AGENTS.md](./src/AGENTS.md)

## 致谢 / 开源依赖

分发在面向玩家的发布包中：

- [`BepInEx`](https://github.com/BepInEx/BepInEx)
- [`HarmonyX`](https://github.com/BepInEx/HarmonyX)
- [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI) (以及相关的运行期第三方 DLL 依赖)
- 其他通过 `BepInExPack_EtG` 捆绑分发的组件

实现参考与社区灵感来源：

- [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
- [`Nevernamed22/OnceMoreIntoTheBreach`](https://github.com/Nevernamed22/OnceMoreIntoTheBreach)

许可证与署名说明：

- 仓库级署名和依赖声明：
  [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md)
- 发布包合规性细节：
  [docs/operations/release-package.md](./docs/operations/release-package.md)
