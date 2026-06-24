from __future__ import annotations

import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from release_package_compliance import (
    ensure_no_game_owned_dlls,
    ensure_required_package_files_for_type,
    stage_license_files,
    write_install_readme,
    write_third_party_notices,
)
from release_package_upstream import (
    ensure_cached_upstream_archive,
    extract_upstream_content,
    sha256_for_file,
)
from tool_common import get_default_sync_specs, get_plugin_output_path, read_repo_version, run_process, sync_generated_version_files, get_runtime_dependency_specs, get_local_dependency_path


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
