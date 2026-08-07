## 格式化自动修复
dotnet format .\EtgGameplayDashboard.sln whitespace --no-restore


## 代码风格

## 检查而不修改

dotnet format .\EtgGameplayDashboard.sln style --verify-no-changes --no-restore

### 自动修复

dotnet format .\EtgGameplayDashboard.sln style --no-restore

## Roslyn 静态分析

自动修复
dotnet format .\EtgGameplayDashboard.sln analyzers `
  --severity info `
  --no-restore

检查 analyzer 诊断而不修改源码：

dotnet format .\EtgGameplayDashboard.sln analyzers --severity info --verify-no-changes --no-restore

## json输出

dotnet format .\EtgGameplayDashboard.sln analyzers `
  --severity info `
  --verify-no-changes `
  --no-restore `
  --report .\artifacts\analyzers

按代码文件拆分 analyzer 任务：

python .\tools\devtools\split_analyzer_report.py `
  --report .\artifacts\analyzers\format-report.json `
  --output .\artifacts\analyzer-tasks

输出结构：

`index.json` 提供文件索引；`files/<扁平化相对代码路径>.task/diagnostics.json` 保存该文件的
诊断 ID、行列号、项目 ID 和原始描述。相同文件在多个项目中出现时，工具会合并重复诊断。

项目使用 SDK-style `.csproj`，但仍然目标为 `.NET Framework 3.5`。Analyzer 通过根目录
`Directory.Build.props` 中的 `Microsoft.CodeAnalysis.NetAnalyzers` 引入，运行在构建机上，
不会成为游戏运行时依赖。
