# ETG-Gameplay-Dashboard

[中文](README.md) | English

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-0078d4.svg)](#)

`ETG-Gameplay-Dashboard` is an `Enter the Gungeon` gameplay experience optimization project built on `BepInEx`.

The project currently focuses on making run setup easier and more visual:

- graphical item category browsing and selection
- graphical custom starting-loadout item grants
- supporting dashboard tools for improving the moment-to-moment ETG play experience

It is primarily designed to improve the gameplay workflow rather than only act as a traditional trainer. That said, it also includes common modifier-style features where they fit the project, such as runtime commands, debug helpers, and configurable gameplay adjustments.

## Installation & Usage

1. Download the latest release package (e.g., `ETG-Gameplay-Dashboard-vX.Y.Z-ETG.zip`) from the [Releases](https://github.com/camellia2077/ETG-Gameplay-Dashboard/releases) page and extract it.
2. Find the game installation folder for `Enter the Gungeon` on Windows.
   - It is typically located at: `steam\steamapps\common\Enter the Gungeon`.
   - **Quick Tip**: In your Steam Library, right-click `Enter the Gungeon` -> **Manage** -> **Browse local files** to open the directory directly.
3. Copy **all files and subfolders** inside the extracted folder (including `BepInEx`, `winhttp.dll`, etc.) and paste them into the `Enter the Gungeon` root directory, allowing file overrides if prompted.
   > [!IMPORTANT]
   > Note:
   > **Copy all subfiles and folders inside the extracted folder!**
   > **Copy all subfiles and folders inside the extracted folder!**
   > **Copy all subfiles and folders inside the extracted folder!**
   > (Do not copy the single extracted folder itself).
4. Launch the game and press `F7` to open or close the control panel.

## Development

For development notes, architecture expectations, and agent handoff guidance, read:

- [src/AGENTS.md](./src/AGENTS.md)

## Credits / Open Source Dependencies

Redistributed in the player-facing release package:

- [`BepInEx`](https://github.com/BepInEx/BepInEx)
- [`HarmonyX`](https://github.com/BepInEx/HarmonyX)
- [`SpecialAPI/ModTheGungeonAPI`](https://github.com/SpecialAPI/ModTheGungeonAPI) (along with related runtime third-party DLL dependencies)
- other components bundled through `BepInExPack_EtG`

Implementation references and community inspiration:

- [`SpecialAPI/SaveAPI`](https://github.com/SpecialAPI/SaveAPI)
- [`Nevernamed22/OnceMoreIntoTheBreach`](https://github.com/Nevernamed22/OnceMoreIntoTheBreach)

License and attribution notes:

- repository-level attribution and dependency notices:
  [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md)
- release-package compliance details:
  [docs/operations/release-package.md](./docs/operations/release-package.md)
