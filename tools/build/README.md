# Build Tools

This folder contains local development build and test entrypoints.

Files:

- `build.py`: builds the `EtgGameplayDashboard` plugin with .NET Framework MSBuild
- `test.py`: builds and runs `EtgGameplayDashboard.Core.Tests`

Typical usage:

- `python .\tools\build.py --configuration Debug`
- `python .\tools\build.py --configuration Release`
- `python .\tools\test.py --configuration Debug`
