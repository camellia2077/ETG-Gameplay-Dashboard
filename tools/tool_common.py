from __future__ import annotations

import argparse
import subprocess
import sys
from typing import Callable
from pathlib import Path


PROJECT_NAME = "RandomLoadout"
CONFIGURATION_CHOICES = ("Debug", "Release")
MSBUILD_PATH = Path(r"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")
DEFAULT_CONFIG_DIRECTORY = Path("defaults") / "config"
DEFAULT_CATALOG_DIRECTORY = Path("defaults") / "catalog"
DEFAULT_PRESET_DIRECTORY = Path("defaults") / "presets"
DEFAULT_CONFIG_RELATIVE_PATHS = (
    Path("randomgun.randomloadout.cfg"),
    Path("ETG-Gameplay-Dashboard.aliases.json5"),
    Path("ETG-Gameplay-Dashboard.localization.en.json5"),
    Path("ETG-Gameplay-Dashboard.localization.zh-CN.json5"),
    Path("ETG-Gameplay-Dashboard.rules.json5"),
    Path("presets") / "preset.default.json",
    Path("presets") / "preset.casey_synergies.json",
)
DEFAULT_CATALOG_FILE_NAMES = (
    "RandomLoadout.pickups.json",
    "RandomLoadout.pickups.by-category.json",
    "RandomLoadout.pickup-gameplay.json",
    "RandomLoadout.pickup-info-terms.json",
    "RandomLoadout.rules.full-pool.json5",
)
REQUIRED_BUILD_DLLS = (
    "0Harmony.dll",
    "Assembly-CSharp.dll",
    "BepInEx.dll",
    "ModTheGungeonAPI.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
)

VERSION_FILE_NAME = "VERSION"
PLUGIN_VERSION_FILE = Path("src") / "RandomLoadout" / "Properties" / "Version.g.cs"
CORE_VERSION_FILE = Path("src") / "RandomLoadout.Core" / "Properties" / "Version.g.cs"
LICENSE_HEADER = (
    "// Copyright (C) 2026 camellia2077\n"
    "// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later."
)


def get_repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def get_version_file_path(repo_root: Path) -> Path:
    return repo_root / VERSION_FILE_NAME


def read_repo_version(repo_root: Path) -> str:
    version_path = get_version_file_path(repo_root)
    if not version_path.is_file():
        raise FileNotFoundError("Version file not found: {0}".format(version_path))

    version = version_path.read_text(encoding="utf-8").strip()
    if not version:
        raise ValueError("Version file was empty: {0}".format(version_path))

    return version


def _build_plugin_version_source(version: str) -> str:
    return """{header}

using System.Reflection;

[assembly: AssemblyVersion("{version}")]
[assembly: AssemblyFileVersion("{version}")]
[assembly: AssemblyInformationalVersion("{version}")]

namespace RandomLoadout
{{
    internal static class BuildVersionInfo
    {{
        public const string Version = "{version}";
    }}
}}
""".format(header=LICENSE_HEADER, version=version)


def _build_core_version_source(version: str) -> str:
    return """{header}

using System.Reflection;

[assembly: AssemblyVersion("{version}")]
[assembly: AssemblyFileVersion("{version}")]
[assembly: AssemblyInformationalVersion("{version}")]
""".format(header=LICENSE_HEADER, version=version)


def _write_if_changed(path: Path, content: str) -> None:
    normalized_content = content.replace("\r\n", "\n")
    if path.is_file():
        existing = path.read_text(encoding="utf-8").replace("\r\n", "\n")
        if existing == normalized_content:
            return

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(normalized_content, encoding="utf-8", newline="\n")


def sync_generated_version_files(repo_root: Path) -> str:
    version = read_repo_version(repo_root)
    _write_if_changed(repo_root / PLUGIN_VERSION_FILE, _build_plugin_version_source(version))
    _write_if_changed(repo_root / CORE_VERSION_FILE, _build_core_version_source(version))
    return version


def add_configuration_argument(parser: argparse.ArgumentParser, help_text: str) -> None:
    parser.add_argument(
        "-c",
        "--configuration",
        default="Debug",
        choices=CONFIGURATION_CHOICES,
        help=help_text,
    )


def resolve_msbuild() -> Path:
    if not MSBUILD_PATH.is_file():
        raise FileNotFoundError("MSBuild not found at '{0}'.".format(MSBUILD_PATH))
    return MSBUILD_PATH


def ensure_required_build_dlls(repo_root: Path) -> None:
    lib_path = repo_root / "lib"
    missing = [dll for dll in REQUIRED_BUILD_DLLS if not (lib_path / dll).is_file()]
    if missing:
        raise FileNotFoundError(
            "Missing required DLLs in '{0}': {1}".format(lib_path, ", ".join(missing))
        )


def get_plugin_project_path(repo_root: Path) -> Path:
    return repo_root / "src" / PROJECT_NAME / "{0}.csproj".format(PROJECT_NAME)


def get_test_project_path(repo_root: Path) -> Path:
    return repo_root / "tests" / "RandomLoadout.Core.Tests" / "RandomLoadout.Core.Tests.csproj"


def get_plugin_output_path(repo_root: Path, configuration: str) -> Path:
    return (
        repo_root / "src" / PROJECT_NAME / "bin" / configuration / "{0}.dll".format(PROJECT_NAME)
    )


def get_local_dependency_path(repo_root: Path, file_name: str) -> Path:
    return repo_root / "lib" / file_name


def get_runtime_dependency_specs() -> list[tuple[str, Path]]:
    return [
        ("0Harmony.dll", Path("BepInEx") / "plugins"),
        ("ModTheGungeonAPI.dll", Path("BepInEx") / "plugins" / "MtGAPI"),
        ("Ionic.Zip.dll", Path("BepInEx") / "plugins" / "MtGAPI"),
        ("Newtonsoft.Json.dll", Path("BepInEx") / "plugins" / "MtGAPI"),
        ("System.Xml.dll", Path("BepInEx") / "plugins" / "MtGAPI"),
        ("System.Xml.Linq.dll", Path("BepInEx") / "plugins" / "MtGAPI"),
        ("UnityEngine.CoreModule.MTGAPIPatcher.mm.dll", Path("monomod")),
    ]


def get_default_config_dir(repo_root: Path) -> Path:
    return repo_root / DEFAULT_CONFIG_DIRECTORY


def get_default_config_paths(repo_root: Path) -> list[Path]:
    config_dir = get_default_config_dir(repo_root)
    preset_dir = repo_root / DEFAULT_PRESET_DIRECTORY
    resolved_paths: list[Path] = []
    for relative_path in DEFAULT_CONFIG_RELATIVE_PATHS:
        root_dir = preset_dir if relative_path.parts and relative_path.parts[0] == "presets" else config_dir
        trimmed_relative_path = Path(*relative_path.parts[1:]) if relative_path.parts and relative_path.parts[0] == "presets" else relative_path
        resolved_paths.append(root_dir / trimmed_relative_path)
    return resolved_paths


def get_default_config_sync_specs(repo_root: Path) -> list[tuple[Path, Path]]:
    config_dir = get_default_config_dir(repo_root)
    preset_dir = repo_root / DEFAULT_PRESET_DIRECTORY
    specs: list[tuple[Path, Path]] = []
    for relative_path in DEFAULT_CONFIG_RELATIVE_PATHS:
        if relative_path.parts and relative_path.parts[0] == "presets":
            source_path = preset_dir / Path(*relative_path.parts[1:])
            target_relative_path = relative_path
        else:
            source_path = config_dir / relative_path
            target_relative_path = relative_path

        specs.append((source_path, target_relative_path))

    return specs


def get_default_catalog_dir(repo_root: Path) -> Path:
    return repo_root / DEFAULT_CATALOG_DIRECTORY


def get_default_catalog_paths(repo_root: Path) -> list[Path]:
    catalog_dir = get_default_catalog_dir(repo_root)
    return [catalog_dir / file_name for file_name in DEFAULT_CATALOG_FILE_NAMES]


def get_default_sync_paths(repo_root: Path) -> list[Path]:
    return get_default_config_paths(repo_root) + get_default_catalog_paths(repo_root)


def get_default_sync_specs(repo_root: Path) -> list[tuple[Path, Path]]:
    specs = get_default_config_sync_specs(repo_root)
    for catalog_path in get_default_catalog_paths(repo_root):
        specs.append((catalog_path, Path(catalog_path.name)))

    return specs


def get_test_output_path(repo_root: Path, configuration: str) -> Path:
    return (
        repo_root
        / "tests"
        / "RandomLoadout.Core.Tests"
        / "bin"
        / configuration
        / "RandomLoadout.Core.Tests.exe"
    )


def run_process(command: list[str], cwd: Path) -> int:
    completed = subprocess.run(command, cwd=str(cwd))
    return completed.returncode


def fail(message: str) -> int:
    print(message, file=sys.stderr)
    return 1


def require_existing_directory(path: Path, label: str) -> Path:
    if not path.is_dir():
        raise FileNotFoundError("{0} does not exist: {1}".format(label, path))
    return path


def run_cli(main_func: Callable[[], int]) -> int:
    try:
        return main_func()
    except FileNotFoundError as error:
        return fail(str(error))
    except OSError as error:
        return fail("OS error: {0}".format(error))
    except KeyboardInterrupt:
        return fail("Operation cancelled.")
