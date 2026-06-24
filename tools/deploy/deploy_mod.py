from __future__ import annotations

import argparse
import hashlib
import errno
import re
import shutil
import sys
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import (
    add_configuration_argument,
    fail,
    get_default_sync_paths,
    get_default_sync_specs,
    get_local_dependency_path,
    get_plugin_output_path,
    get_repo_root,
    get_runtime_dependency_specs,
    require_existing_directory,
    run_process,
    run_cli,
)


def sha256_for_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as file_obj:
        for chunk in iter(lambda: file_obj.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def safe_copy_file(source_path: Path, target_path: Path) -> None:
    # Copy file contents first. This is the part we strictly require.
    shutil.copyfile(source_path, target_path)

    # Then best-effort metadata copy. Some filesystems reject timestamp metadata
    # updates and raise OSError(EINVAL), which should not block deployment.
    try:
        shutil.copystat(source_path, target_path)
    except OSError as error:
        if error.errno != errno.EINVAL:
            raise

        print(
            "Warning: copied file but skipped metadata sync due to filesystem limitation: {0}".format(
                target_path
            )
        )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Copy the built mod DLL into an Enter the Gungeon BepInEx plugins folder."
    )
    parser.add_argument(
        "game_path",
        help="Path to the Enter the Gungeon installation directory.",
    )
    add_configuration_argument(parser, "Build configuration to deploy. Defaults to Debug.")
    parser.add_argument(
        "--overwrite-config",
        action="store_true",
        help="Overwrite existing files in BepInEx\\config with the repository defaults and catalog snapshots.",
    )
    parser.add_argument(
        "--skip-build",
        action="store_true",
        help="Skip the pre-deploy build step. By default deploy builds the selected configuration first.",
    )
    return parser.parse_args()


def copy_default_files(repo_root: Path, config_dir: Path, overwrite: bool) -> int:
    default_paths = get_default_sync_paths(repo_root)
    default_specs = get_default_sync_specs(repo_root)
    for default_path in default_paths:
        if not default_path.is_file():
            return fail("Repository default file not found: {0}".format(default_path))

    copied_count = 0
    skipped_count = 0
    for default_path, target_relative_path in default_specs:
        target_path = config_dir / target_relative_path
        target_path.parent.mkdir(parents=True, exist_ok=True)
        target_exists = target_path.exists()
        if target_exists and not overwrite:
            if target_path.name in LOCALIZATION_FILE_NAMES:
                updated, added_count = merge_missing_localization_keys(default_path, target_path)
                if updated:
                    print("Merged {0} missing localization key(s): {1}".format(added_count, target_path))
                    copied_count += 1
                else:
                    print("Kept existing default target: {0}".format(target_path))
                    skipped_count += 1
                continue

            print("Kept existing default target: {0}".format(target_path))
            skipped_count += 1
            continue

        safe_copy_file(default_path, target_path)
        action = "Overwrote" if target_exists else "Copied"
        print("{0} repository default: {1} -> {2}".format(action, default_path, target_path))
        copied_count += 1

    print("Default file sync complete: {0} copied, {1} kept.".format(copied_count, skipped_count))
    return 0


def copy_runtime_dependency(repo_root: Path, game_path: Path, file_name: str, relative_target_dir: Path) -> int:
    source_path = get_local_dependency_path(repo_root, file_name)
    if not source_path.is_file():
        return fail(
            "Runtime dependency not found: {0}\nExpected to copy this dependency during deploy.".format(
                source_path
            )
        )

    target_dir = game_path / relative_target_dir
    target_dir.mkdir(parents=True, exist_ok=True)
    target_path = target_dir / file_name
    safe_copy_file(source_path, target_path)

    source_hash = sha256_for_file(source_path)
    target_hash = sha256_for_file(target_path)
    if source_hash != target_hash:
        return fail(
            "Dependency deploy verification failed.\nSource: {0} ({1})\nTarget: {2} ({3})".format(
                source_path, source_hash, target_path, target_hash
            )
        )

    print(f"Copied runtime dependency {source_path} -> {target_path}")
    print(f"Verified dependency SHA-256: {target_hash}")
    return 0


def copy_runtime_dependencies(repo_root: Path, game_path: Path) -> int:
    for file_name, relative_target_dir in get_runtime_dependency_specs():
        dependency_exit_code = copy_runtime_dependency(
            repo_root,
            game_path,
            file_name,
            relative_target_dir,
        )
        if dependency_exit_code != 0:
            return dependency_exit_code

    return 0


METADATA_PATH = Path("release") / "release_package_metadata.json"
CACHE_DIRECTORY = Path(".cache") / "release"
LOCALIZATION_FILE_NAMES = {
    "ETG-Gameplay-Dashboard.localization.en.json5",
    "ETG-Gameplay-Dashboard.localization.zh-CN.json5",
}
JSON5_STRING_ENTRY_PATTERN = re.compile(
    r"(?:\"(?P<dqk>(?:\\.|[^\"])*)\"|'(?P<sqk>(?:\\.|[^'])*)'|(?P<bare>[A-Za-z0-9_.-]+))\s*:\s*(?:\"(?P<dqv>(?:\\.|[^\"])*)\"|'(?P<sqv>(?:\\.|[^'])*)')",
    re.S,
)


def main() -> int:
    args = parse_args()

    repo_root = get_repo_root()

    if not args.skip_build:
        build_script = repo_root / "tools" / "build.py"
        build_exit_code = run_process(
            [sys.executable, str(build_script), "--configuration", args.configuration],
            repo_root,
        )
        if build_exit_code != 0:
            return fail("Build failed with exit code {0}. Deployment aborted.".format(build_exit_code))

    source_dll = get_plugin_output_path(repo_root, args.configuration)

    if not source_dll.is_file():
        return fail(
            "Build output not found: {0}\nRun the build first, for example: python .\\tools\\build.py --configuration {1}".format(
                source_dll,
                args.configuration,
            )
        )

    game_path = require_existing_directory(Path(args.game_path).expanduser(), "Game path")

    # If BepInEx framework files are missing, restore BepInExPack_EtG
    bepinex_dll = game_path / "BepInEx" / "core" / "BepInEx.dll"
    winhttp_dll = game_path / "winhttp.dll"
    if not bepinex_dll.is_file() or not winhttp_dll.is_file():
        print("BepInEx framework missing in game directory. Restoring BepInExPack_EtG...")
        try:
            from release.release_package_upstream import (
                load_metadata,
                ensure_cached_upstream_archive,
                extract_upstream_content,
            )
            metadata = load_metadata(repo_root / "tools" / METADATA_PATH)
            cache_dir = repo_root / CACHE_DIRECTORY
            upstream_archive_path = ensure_cached_upstream_archive(cache_dir, metadata)
            extract_upstream_content(upstream_archive_path, metadata, game_path)
            print("Successfully restored BepInExPack_EtG framework.")
        except Exception as error:
            return fail("Failed to restore BepInEx framework: {0}".format(error))

    plugins_dir = game_path / "BepInEx" / "plugins"
    config_dir = game_path / "BepInEx" / "config"
    plugins_dir.mkdir(parents=True, exist_ok=True)
    config_dir.mkdir(parents=True, exist_ok=True)

    target_dll = plugins_dir / source_dll.name
    safe_copy_file(source_dll, target_dll)

    source_hash = sha256_for_file(source_dll)
    target_hash = sha256_for_file(target_dll)
    if source_hash != target_hash:
        return fail(
            "Deploy verification failed.\nSource: {0} ({1})\nTarget: {2} ({3})".format(
                source_dll, source_hash, target_dll, target_hash
            )
        )

    print(f"Copied {source_dll} -> {target_dll}")
    print(f"Verified SHA-256: {target_hash}")

    dependency_exit_code = copy_runtime_dependencies(repo_root, game_path)
    if dependency_exit_code != 0:
        return dependency_exit_code

    return copy_default_files(repo_root, config_dir, args.overwrite_config)


def merge_missing_localization_keys(default_path: Path, target_path: Path) -> tuple[bool, int]:
    default_text = default_path.read_text(encoding="utf-8")
    target_text = target_path.read_text(encoding="utf-8")
    default_table = parse_json5_flat_object(default_text)
    target_table = parse_json5_flat_object(target_text)

    missing_keys = [key for key in default_table.keys() if key not in target_table]
    if not missing_keys:
        return False, 0

    newline = "\r\n" if "\r\n" in target_text else "\n"
    closing_index = target_text.rfind("}")
    if closing_index < 0:
        raise ValueError("Localization file does not contain a closing object brace: {0}".format(target_path))

    before = target_text[:closing_index].rstrip()
    after = target_text[closing_index:]
    additions = [
        '  "{0}": "{1}"'.format(key, escape_json5_string(default_table[key]))
        for key in missing_keys
    ]

    needs_separator = bool(target_table) and not before.endswith(",") and not before.endswith("{")
    if needs_separator:
        before += ","

    if not before.endswith("{"):
        before += newline

    merged_text = before + (newline.join(additions)) + newline + after.lstrip()
    target_path.write_text(merged_text, encoding="utf-8", newline=newline)
    return True, len(missing_keys)


def parse_json5_flat_object(raw_text: str) -> dict[str, str]:
    values: dict[str, str] = {}
    for match in JSON5_STRING_ENTRY_PATTERN.finditer(raw_text):
        key = get_group_value(match, "dqk", "sqk", "bare")
        value = unescape_json5_string(get_group_value(match, "dqv", "sqv"))
        if key:
            values[key] = value
    return values


def get_group_value(match: re.Match[str], *group_names: str) -> str:
    for group_name in group_names:
        value = match.group(group_name)
        if value:
            return value
    return ""


def unescape_json5_string(value: str) -> str:
    return (
        value.replace("\\\"", "\"")
        .replace("\\'", "'")
        .replace("\\\\", "\\")
        .replace("\\n", "\n")
        .replace("\\r", "\r")
        .replace("\\t", "\t")
    )


def escape_json5_string(value: str) -> str:
    return (
        value.replace("\\", "\\\\")
        .replace("\"", "\\\"")
        .replace("\n", "\\n")
        .replace("\r", "\\r")
        .replace("\t", "\\t")
    )


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
