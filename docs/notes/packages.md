python .\tools\release\build_release_package.py

默认会同时产出两种包：

- `standalone`：独立安装包，包含 `BepInExPack_EtG` 运行时，适合直接解压到游戏目录
- `mod-manager`：Mod 启动器包，只包含本 Mod 的 DLL、配置、文本资源和预设；要求用户已安装 `BepInExPack_EtG` 和 `Mod the Gungeon API`

## 两种Release的zip

python .\tools\release\build_release_package.py --configuration Release

## 独立安装包：

python .\tools\release\build_release_package.py --package standalone

## Mod 启动器包：

python .\tools\release\build_release_package.py --package mod-manager

覆盖包名里的版本号：

python .\tools\release\build_release_package.py --version 0.2.3

## delete

你可以使用本项目提供的 Python 清理脚本来删除安装在游戏目录中的 Mod 主程序和 BepInEx 注入框架（默认会完整清理 BepInEx 以及所有释放的配置文件和依赖）：

```powershell
python .\tools\clean_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon"
```

如果你只希望**清理本插件及依赖 DLL**，而**保留** BepInEx 框架本身和配置文件，可以加上 `--plugins-only` 参数：

```powershell
python .\tools\clean_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --plugins-only
```

如果你想从 `r2modman` 的指定 profile 目录里删除当前项目安装进去的 DLL 和配置文件，可以直接执行：

```powershell
python .\tools\clean_mod.py "" --plugins-only
```
