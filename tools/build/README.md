# Build Tools

This folder contains local development build and test entrypoints.

Files:

- `build.py`: restores and builds the `EtgGameplayDashboard` SDK-style project with `dotnet build` (targeting .NET Framework 3.5)
- `test.py`: builds and runs `EtgGameplayDashboard.Core.Tests`

Typical usage:

- `python .\tools\build\build.py --configuration Debug`
- `python .\tools\build\build.py --configuration Release`
- `python .\tools\build\test.py --configuration Debug`

Code quality checks:

- `dotnet format .\EtgGameplayDashboard.sln whitespace --verify-no-changes --no-restore`
- `dotnet format .\EtgGameplayDashboard.sln style --verify-no-changes --no-restore`
- `dotnet format .\EtgGameplayDashboard.sln analyzers --severity info --verify-no-changes --no-restore`
