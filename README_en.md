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
4. Launch the game and press `F7` or controller `R3` to open or close the control panel.

## Development

For development notes, architecture expectations, and agent handoff guidance, read:

- [src/AGENTS.md](./src/AGENTS.md)

## Data Sources And Attribution

Some pickup gameplay information used by this project is derived from the `Enter the Gungeon` community wiki on `wiki.gg`:

- <https://enterthegungeon.wiki.gg/>

As indicated in the `wiki.gg` page footer, relevant page content is generally published under the `Creative Commons Attribution-ShareAlike 4.0 License` unless otherwise noted. This project fetches, cleans, restructures, and reformats that data for in-game reference use.

Some Chinese item-name text used during development was also partially referenced from, and in some cases directly adopted from, the published text results of:

- <https://github.com/Lynx3x/etg-itemtips-cn>

That repository is published under the `MIT License`. This project only references and incorporates a subset of its Chinese text results and does not fully reuse or redistribute that repository as-is.

This is an unofficial community project and is not affiliated with `wiki.gg`, `Dodge Roll`, or `Devolver Digital`. Original content licensing and site terms should be interpreted according to the source pages and the relevant upstream project or site notices.

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
