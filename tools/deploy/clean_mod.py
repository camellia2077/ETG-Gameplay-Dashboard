from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import (
    fail,
    get_runtime_dependency_specs,
    require_existing_directory,
    run_cli,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Clean/delete built mod files, BepInEx loader, and dependencies from Enter the Gungeon folder."
    )
    parser.add_argument(
        "game_path",
        help="Path to the Enter the Gungeon installation directory.",
    )
    parser.add_argument(
        "--plugins-only",
        action="store_true",
        help="Only delete RandomLoadout plugins and dependencies, keeping BepInEx framework and configurations.",
    )
    return parser.parse_args()


def delete_file_or_dir(path: Path) -> None:
    if path.is_file():
        path.unlink()
        print("Deleted file: {0}".format(path))
    elif path.is_dir():
        shutil.rmtree(str(path))
        print("Deleted directory: {0}".format(path))


def main() -> int:
    args = parse_args()
    try:
        game_path = require_existing_directory(Path(args.game_path).expanduser(), "Game path")
    except FileNotFoundError as error:
        return fail(str(error))

    if args.plugins_only:
        # Only clean up plugin DLL, MtGAPI dependencies, and monomod patcher DLL
        plugin_dll = game_path / "BepInEx" / "plugins" / "RandomLoadout.dll"
        delete_file_or_dir(plugin_dll)

        for file_name, relative_target_dir in get_runtime_dependency_specs():
            target_path = game_path / relative_target_dir / file_name
            delete_file_or_dir(target_path)

        # Clean up empty MtGAPI directory if it exists and is empty
        mtg_api_dir = game_path / "BepInEx" / "plugins" / "MtGAPI"
        if mtg_api_dir.is_dir() and not any(mtg_api_dir.iterdir()):
            mtg_api_dir.rmdir()
            print("Removed empty directory: {0}".format(mtg_api_dir))
    else:
        # Default: Clean up BepInEx completely and all related loader/dependency files
        clean_targets = [
            game_path / "BepInEx",
            game_path / "doorstop_libs",
            game_path / "licenses",
            game_path / "monomod",
            game_path / "doorstop_config.ini",
            game_path / "winhttp.dll",
            game_path / "version.dll",
            game_path / "start_game_bepinex.sh",
            game_path / "README-INSTALL.txt",
            game_path / "THIRD_PARTY_NOTICES.md",
        ]

        for target in clean_targets:
            delete_file_or_dir(target)

    print("Clean operation completed successfully.")
    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
