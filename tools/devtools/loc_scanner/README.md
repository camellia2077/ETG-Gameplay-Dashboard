# LOC Scanner

通用代码行数扫描工具，采用独立子项目目录组织。

本工具只采集代码规模事实，用于帮助 Agent 找到需要进一步检查的候选文件和目录。它不执行静态代码分析，也不直接判断职责边界，更不会建议文件应该保留、移动、拆分或合并。

## 重构辅助工作流

推荐按以下顺序使用扫描结果：

1. 运行 LOC 扫描，记录文件行数、目录文件数、扫描范围和阈值。
2. Agent 阅读命中文件及其相关调用方、被调用方、测试和架构文档。
3. Agent 用英文描述实际代码问题，例如职责混合、层间耦合或运行时适配与业务决策混合。
4. Agent 根据代码证据决定是否保留、移动、拆分或合并，并制定重构计划。
5. 完成一个功能切片后运行测试和构建，再重新扫描作为规模变化的辅助验证。

LOC 结果是候选信号，不是架构结论。大文件可能具有高内聚，多个小文件也可能共同形成高耦合的职责边界。不要为了通过行数阈值而机械拆分文件。

仓库的重构边界和目标架构见：[Refactoring Guidelines](../../../docs/architecture/refactoring-guidelines.md)。

## 报告契约

扫描报告只应包含以下类型的信息：

- 扫描语言、工作区、路径和阈值
- 文件行数或目录代码文件数量
- 扫描状态、错误和汇总数量

报告不应包含以下内容：

- 文件的重构动作
- 文件的职责归属结论
- 拆分或合并理由
- 重构优先级
- 未经代码阅读验证的架构判断

## 目录结构

1. `src/loc_scanner/`
   - 工具实现与 CLI 入口
2. `config/`
   - 默认配置（`scan_lines.toml`）
3. `scripts/`
   - Windows bat 快捷入口
4. `tests/`
   - 工具测试
5. `docs/`
   - 使用文档

## 快速使用

从仓库根目录执行：

```bash
python -m tools.devtools.loc_scanner --lang py --under
```

常用参数：

- `--lang`：`cs | kt | py | rs`
- `paths`：可选，待扫描目录；默认可覆盖配置中的 `default_paths`（若该语言 `path_mode = "toml_only"`，则忽略命令行 `paths`）
- `--workspace-root`：相对路径解析根目录，默认当前目录
- `--config`：配置文件路径，默认 `tools/devtools/loc_scanner/config/scan_lines.toml`
- `--log-file`：日志输出路径；不传时写入 `<workspace-root>/.loc_scanner_logs/scan_<lang>.json`
- `--over N` / `--under [N]` / `--dir-over-files [N]`

`--over` 必须显式传入阈值；不传值时请省略该参数，让扫描器使用配置中的 `default_over_threshold`。

Windows 阈值扫描入口：

- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-over`
- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat py-dir-over-files`
- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-over`
- `tools\devtools\loc_scanner\scripts\scan_thresholds.bat cs-dir-over-files`

这些 bat 入口使用固定阈值与固定模式；如果需要自定义阈值、层级或路径，请直接调用 `python -m tools.devtools.loc_scanner`。

更多示例见：[docs/usage.md](docs/usage.md)，配置字段说明见：[docs/toml_config.md](docs/toml_config.md)

