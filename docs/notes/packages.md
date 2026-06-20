python .\tools\release\build_release_package.py

如果你想显式指定 Release，可以用：

python .\tools\release\build_release_package.py --configuration Release

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
