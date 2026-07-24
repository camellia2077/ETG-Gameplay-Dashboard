步骤 1：切换至桌面模式
在 Steam Deck 上按下电源键，选择“切换至桌面模式” (Switch to Desktop)。

步骤 2：强制启用 Proton 兼容层（切换至 Windows 版本的游戏）
1. 在桌面模式下打开 Steam 客户端。
2. 找到《Enter the Gungeon》（挺进地牢），右键选择“属性” (Properties)。
3. 在左侧菜单中选择“兼容性” (Compatibility)。
4. 勾选“强制使用特定的 Steam Play 兼容性工具” (Force the use of a specific Steam Play compatibility tool)。
5. 在下拉列表中选择最新版本的 Proton (例如 Proton 9.0 或 Proton Experimental)。
6. Steam 随后会自动下载更新，将游戏从 Linux 原生版切换为 Windows 版。

步骤 3：解压独立版压缩包
将独立版 (standalone) 压缩包内的所有文件和文件夹，解压并直接复制到游戏的根目录（即包含 EtG.exe 的文件夹）中。

步骤 4：设置启动选项以覆盖 DLL
1. 在 Steam 客户端中，右键《Enter the Gungeon》，选择“属性” (Properties)。
2. 在左侧菜单中选择“通用” (General)。
3. 找到“启动选项” (Launch Options) 文本框，在其中输入以下内容：
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   （注意：如果上述设置无效，可尝试改为：WINEDLLOVERRIDES="winhttp.dll=n,b" %command%）

步骤 5：启动游戏
关闭属性窗口，在 Steam 中直接启动游戏即可。现在 BepInEx 将会正常加载并注入 Mod，您就可以在游戏中正常呼出控制面板了。
