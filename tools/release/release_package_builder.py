from __future__ import annotations

import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from release_package_upstream import (
    download_text,
    ensure_cached_upstream_archive,
    extract_upstream_content,
    sha256_for_file,
)
from tool_common import get_default_sync_specs, get_plugin_output_path, read_repo_version, run_process, sync_generated_version_files, get_runtime_dependency_specs, get_local_dependency_path


FORBIDDEN_GAME_DLLS = {
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
}

REPOSITORY_NOTICE_FILE_NAME = "THIRD_PARTY_NOTICES.md"


def stage_license_files(repo_root: Path, metadata: dict | None, staging_root: Path) -> list[dict]:
    licenses_directory = staging_root / "licenses"
    licenses_directory.mkdir(parents=True, exist_ok=True)

    bundled_components = metadata.get("bundledComponents", []) if metadata is not None else []
    staged_components = []
    for component in bundled_components:
        license_text = download_text(component["licenseUrl"]).strip()
        if not license_text:
            raise OSError("License text for '{0}' was empty.".format(component["name"]))

        output_name = component["licenseOutputName"]
        output_path = licenses_directory / output_name
        output_path.write_text(license_text + "\n", encoding="utf-8")
        staged_components.append(component)

    randomloadout_license_source = repo_root / "LICENSE"
    if not randomloadout_license_source.is_file():
        raise FileNotFoundError("Repository license file not found: {0}".format(randomloadout_license_source))

    shutil.copyfile(
        str(randomloadout_license_source),
        str(licenses_directory / "RandomLoadout-LICENSE.txt"),
    )

    return staged_components


def write_install_readme(version_tag: str, package_type: str, staging_root: Path) -> None:
    if package_type == "mod-manager":
        install_text = """ETG-Gameplay-Dashboard {0} Mod Manager Install Guide

1. Close `Enter the Gungeon`.
2. Open this archive.
3. Make sure `BepInExPack_EtG` and `Mod the Gungeon API` are already installed in your active profile.
4. Extract all files into the target profile or game root so the `BepInEx` folder merges in place.
5. Allow overwrite if Windows asks.
6. Launch the game. `ETG-Gameplay-Dashboard` should now be installed.

Project:
- Homepage: https://github.com/camellia2077/ETG-Gameplay-Dashboard
- Source code and releases: https://github.com/camellia2077/ETG-Gameplay-Dashboard

Uninstall:
- Remove `BepInEx\\plugins\\RandomLoadout.dll`
- Remove `BepInEx\\config\\randomgun.randomloadout.cfg`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.localization.en.json5`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.localization.zh-CN.json5`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.aliases.json5`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.rules.json5`
- Remove `BepInEx\\config\\presets\\*.json`
- Optionally remove the generated `RandomLoadout.pickups*.json` and `RandomLoadout.rules.full-pool.json5` files from `BepInEx\\config\\`
""".format(version_tag)
    else:
        install_text = """ETG-Gameplay-Dashboard {0} Standalone Install Guide

1. Close `Enter the Gungeon`.
2. Open this archive.
3. Extract all files into the game root directory that contains `Enter the Gungeon.exe`.
4. Allow overwrite if Windows asks.
5. Launch the game. `BepInEx` and `ETG-Gameplay-Dashboard` should now be installed.

Project:
- Homepage: https://github.com/camellia2077/ETG-Gameplay-Dashboard
- Source code and releases: https://github.com/camellia2077/ETG-Gameplay-Dashboard

Uninstall:
- Remove `BepInEx\\plugins\\RandomLoadout.dll`
- Remove `BepInEx\\config\\randomgun.randomloadout.cfg`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.localization.en.json5`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.localization.zh-CN.json5`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.aliases.json5`
- Remove `BepInEx\\config\\ETG-Gameplay-Dashboard.rules.json5`
- Remove `BepInEx\\config\\presets\\*.json`
- Optionally remove the generated `RandomLoadout.pickups*.json` and `RandomLoadout.rules.full-pool.json5` files from `BepInEx\\config\\`
- If you installed `BepInEx` only for this mod, you may also remove the `BepInEx` folder and the loader files that came from `BepInExPack_EtG`
""".format(version_tag)

    (staging_root / "README-INSTALL.txt").write_text(install_text, encoding="utf-8")


def write_third_party_notices(
    version_tag: str,
    metadata: dict | None,
    staged_components: list[dict],
    repo_root: Path,
    staging_root: Path,
    package_type: str,
) -> None:
    repository_notice_path = repo_root / REPOSITORY_NOTICE_FILE_NAME
    if not repository_notice_path.is_file():
        raise FileNotFoundError("Repository third-party notice file not found: {0}".format(repository_notice_path))

    repository_notice_text = repository_notice_path.read_text(encoding="utf-8").strip()
    if not repository_notice_text:
        raise OSError("Repository third-party notice file was empty: {0}".format(repository_notice_path))

    appendix_lines = [
        "## Release Package Appendix",
        "",
        "### ETG-Gameplay-Dashboard",
        "",
        "- Project: `ETG-Gameplay-Dashboard`",
        "- Homepage: <https://github.com/camellia2077/ETG-Gameplay-Dashboard>",
        "- License: `GPL-3.0-only`",
        "- Bundled license file: `licenses/RandomLoadout-LICENSE.txt`",
        "",
    ]

    if metadata is not None and package_type == "standalone":
        upstream_package = metadata["upstreamPackage"]
        appendix_lines.extend(
            [
                "This release package contains `ETG-Gameplay-Dashboard {0}` together with an unmodified redistribution of `{1} {2}`.".format(
                    version_tag, upstream_package["name"], upstream_package["version"]
                ),
                "",
                "### Redistributed Upstream Package",
                "",
                "- Package: `{0}`".format(upstream_package["name"]),
                "- Version: `{0}`".format(upstream_package["version"]),
                "- Official package page: <{0}>".format(upstream_package["packagePageUrl"]),
                "- Official download URL: <{0}>".format(upstream_package["downloadUrl"]),
                "- Upstream project homepage: <{0}>".format(upstream_package["projectHomepageUrl"]),
                "- Package license: `{0}`".format(upstream_package["licenseId"]),
                "- Redistribution note: this release package redistributes the upstream package unmodified and preserves its separate license terms.",
                "",
                "### Bundled Open Source Components From The Upstream Package",
                "",
            ]
        )

        for component in staged_components:
            appendix_lines.extend(
                [
                    "- `{0}`".format(component["name"]),
                    "  - Homepage: <{0}>".format(component["homepageUrl"]),
                    "  - License: `{0}`".format(component["licenseId"]),
                    "  - Bundled license file: `licenses/{0}`".format(component["licenseOutputName"]),
                ]
            )
    else:
        appendix_lines.extend(
            [
                "This release package contains `ETG-Gameplay-Dashboard {0}` only.".format(version_tag),
                "",
                "### Required External Dependencies",
                "",
                "- `BepInExPack_EtG` must already be installed.",
                "- `Mod the Gungeon API` must already be installed.",
                "- This package does not redistribute those dependencies.",
            ]
        )

    appendix_lines.extend(
        [
            "",
            "### Distribution Notes",
            "",
            "- This package does not include `Enter the Gungeon` game files.",
            "- This package does not include development-only DLLs from the repository `lib/` folder such as `Assembly-CSharp.dll` or `UnityEngine*.dll`.",
            "- Users remain subject to the original licenses and notices of the bundled upstream components.",
            "",
        ]
    )

    package_notice_text = repository_notice_text + "\n\n" + "\n".join(appendix_lines) + "\n"
    (staging_root / "THIRD_PARTY_NOTICES.md").write_text(package_notice_text, encoding="utf-8")


def ensure_no_game_owned_dlls(staging_root: Path) -> None:
    for path in staging_root.rglob("*.dll"):
        name = path.name
        if name in FORBIDDEN_GAME_DLLS or (
            name.startswith("UnityEngine")
            and name.endswith(".dll")
            and not name.endswith(".MTGAPIPatcher.mm.dll")
        ):
            raise OSError("Forbidden game-owned DLL found in release package staging area: {0}".format(path))


def ensure_required_package_files_for_type(staging_root: Path, package_type: str) -> None:
    required_paths = (
        staging_root / "BepInEx" / "plugins" / "RandomLoadout.dll",
        staging_root / "BepInEx" / "config" / "randomgun.randomloadout.cfg",
        staging_root / "BepInEx" / "config" / "ETG-Gameplay-Dashboard.localization.en.json5",
        staging_root / "BepInEx" / "config" / "ETG-Gameplay-Dashboard.localization.zh-CN.json5",
        staging_root / "BepInEx" / "config" / "ETG-Gameplay-Dashboard.aliases.json5",
        staging_root / "BepInEx" / "config" / "ETG-Gameplay-Dashboard.rules.json5",
        staging_root / "BepInEx" / "config" / "presets" / "preset.default.json",
        staging_root / "BepInEx" / "config" / "presets" / "preset.casey_synergies.json",
        staging_root / "THIRD_PARTY_NOTICES.md",
        staging_root / "README-INSTALL.txt",
        staging_root / "licenses" / "RandomLoadout-LICENSE.txt",
    )

    extra_full_paths = (
        staging_root / "BepInEx" / "plugins" / "MtGAPI" / "ModTheGungeonAPI.dll",
        staging_root / "monomod" / "UnityEngine.CoreModule.MTGAPIPatcher.mm.dll",
        staging_root / "licenses" / "BepInEx-LICENSE.txt",
    )

    paths_to_check = required_paths + (extra_full_paths if package_type == "standalone" else tuple())

    for required_path in paths_to_check:
        if not required_path.is_file():
            raise FileNotFoundError("Required packaged file was missing: {0}".format(required_path))


DIST_DIRECTORY = Path("dist")
PACKAGE_TYPE_CHOICES = ("standalone", "mod-manager")
def detect_mod_version(repo_root: Path) -> str:
    return read_repo_version(repo_root)


def normalize_version_tag(version: str) -> str:
    normalized = version.strip()
    if not normalized:
        raise ValueError("Package version was empty.")
    return normalized if normalized.startswith("v") else "v" + normalized


def build_plugin_if_needed(repo_root: Path, configuration: str, skip_build: bool) -> None:
    if skip_build:
        return

    build_script = repo_root / "tools" / "build.py"
    exit_code = run_process(
        [sys.executable, str(build_script), "--configuration", configuration],
        repo_root,
    )
    if exit_code != 0:
        raise OSError("Build failed with exit code {0}. Release packaging aborted.".format(exit_code))


def overlay_randomloadout_files(repo_root: Path, configuration: str, staging_root: Path, include_runtime_dependencies: bool) -> None:
    plugin_path = get_plugin_output_path(repo_root, configuration)
    if not plugin_path.is_file():
        raise FileNotFoundError("Build output not found: {0}".format(plugin_path))

    plugin_target = staging_root / "BepInEx" / "plugins" / plugin_path.name
    plugin_target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(str(plugin_path), str(plugin_target))

    if include_runtime_dependencies:
        # Copy ModTheGungeonAPI runtime dependencies (from lib/)
        for file_name, relative_target_dir in get_runtime_dependency_specs():
            source_path = get_local_dependency_path(repo_root, file_name)
            if not source_path.is_file():
                raise FileNotFoundError(
                    "Runtime dependency not found: {0}\nExpected to copy this dependency during release packaging.".format(
                        source_path
                    )
                )
            target_dir = staging_root / relative_target_dir
            target_dir.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(str(source_path), str(target_dir / file_name))

    config_directory = staging_root / "BepInEx" / "config"
    config_directory.mkdir(parents=True, exist_ok=True)
    for default_path, target_relative_path in get_default_sync_specs(repo_root):
        if not default_path.is_file():
            raise FileNotFoundError("Repository default file not found: {0}".format(default_path))

        target_path = config_directory / target_relative_path
        target_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(str(default_path), str(target_path))


def create_release_zip(staging_root: Path, output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    if output_path.exists():
        output_path.unlink()

    with zipfile.ZipFile(str(output_path), "w", compression=zipfile.ZIP_DEFLATED) as zip_file:
        for file_path in sorted(staging_root.rglob("*")):
            if not file_path.is_file():
                continue

            archive_name = str(file_path.relative_to(staging_root)).replace("\\", "/")
            zip_file.write(str(file_path), archive_name)


def build_release_package(
    repo_root: Path,
    metadata: dict,
    configuration: str,
    version: str,
    skip_build: bool,
    cache_directory: Path,
    package_type: str,
) -> Path:
    if package_type not in PACKAGE_TYPE_CHOICES:
        raise ValueError("Unsupported package type: {0}".format(package_type))

    sync_generated_version_files(repo_root)
    build_plugin_if_needed(repo_root, configuration, skip_build)

    mod_version = version.strip() if version else detect_mod_version(repo_root)
    version_tag = normalize_version_tag(mod_version)
    include_runtime_dependencies = package_type == "standalone"
    upstream_archive_path = ensure_cached_upstream_archive(cache_directory, metadata) if include_runtime_dependencies else None

    with tempfile.TemporaryDirectory(prefix="randomloadout_release_") as temp_directory:
        staging_root = Path(temp_directory) / "staging"
        staging_root.mkdir(parents=True, exist_ok=True)

        if include_runtime_dependencies:
            extract_upstream_content(upstream_archive_path, metadata, staging_root)

        overlay_randomloadout_files(repo_root, configuration, staging_root, include_runtime_dependencies)
        staged_components = stage_license_files(repo_root, metadata if include_runtime_dependencies else None, staging_root)
        write_install_readme(version_tag, package_type, staging_root)
        write_third_party_notices(version_tag, metadata if include_runtime_dependencies else None, staged_components, repo_root, staging_root, package_type)
        ensure_no_game_owned_dlls(staging_root)
        ensure_required_package_files_for_type(staging_root, package_type)

        suffix = "standalone" if package_type == "standalone" else "mod-manager"
        output_path = repo_root / DIST_DIRECTORY / "ETG-Gameplay-Dashboard-{0}-{1}.zip".format(version_tag, suffix)
        create_release_zip(staging_root, output_path)

    return output_path
