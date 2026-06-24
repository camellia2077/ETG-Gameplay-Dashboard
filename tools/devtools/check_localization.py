from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve()
SCRIPT_DIRECTORY = SCRIPT_PATH.parent
REPOSITORY_ROOT = SCRIPT_DIRECTORY.parent.parent
DEFAULT_ENGLISH_PATH = REPOSITORY_ROOT / "defaults" / "config" / "ETG-Gameplay-Dashboard.localization.en.json5"
DEFAULT_SIMPLIFIED_CHINESE_PATH = REPOSITORY_ROOT / "defaults" / "config" / "ETG-Gameplay-Dashboard.localization.zh-CN.json5"
SOURCE_GLOBS = ("src/**/*.cs",)
LOCALIZATION_KEY_PREFIXES = ("gui.", "label.", "result.", "parse.")
LOCALIZATION_STRING_PATTERN = re.compile(r"\"(?P<key>(?:gui|label|result|parse)\.[^\"]+)\"")
JSON5_STRING_ENTRY_PATTERN = re.compile(
    r"(?:\"(?P<dqk>(?:\\.|[^\"])*)\"|'(?P<sqk>(?:\\.|[^'])*)'|(?P<bare>[A-Za-z0-9_.-]+))\s*:\s*(?:\"(?P<dqv>(?:\\.|[^\"])*)\"|'(?P<sqv>(?:\\.|[^'])*)')",
    re.S,
)


@dataclass(frozen=True)
class LocalizationFinding:
    kind: str
    path: Path
    detail: str


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate localization keys used by source files against repository localization resources."
    )
    parser.add_argument(
        "--repo-root",
        default=str(REPOSITORY_ROOT),
        help="Repository root used to resolve source and localization paths.",
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Print scanned file counts and key counts when validation succeeds.",
    )
    return parser.parse_args()


def resolve_target_files(repo_root: Path) -> list[Path]:
    files: set[Path] = set()
    for pattern in SOURCE_GLOBS:
        for path in repo_root.glob(pattern):
            if path.is_file():
                files.add(path.resolve())
    return sorted(files)


def load_localization_table(path: Path) -> dict[str, str]:
    content = path.read_text(encoding="utf-8")
    values: dict[str, str] = {}
    for match in JSON5_STRING_ENTRY_PATTERN.finditer(content):
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


def collect_source_keys(paths: list[Path], repo_root: Path) -> tuple[set[str], list[LocalizationFinding]]:
    keys: set[str] = set()
    findings: list[LocalizationFinding] = []
    for path in paths:
        try:
            content = path.read_text(encoding="utf-8", errors="ignore")
        except OSError as error:
            findings.append(
                LocalizationFinding(
                    "read-error",
                    path,
                    str(error),
                )
            )
            continue

        for match in LOCALIZATION_STRING_PATTERN.finditer(content):
            key = match.group("key")
            if key.startswith(LOCALIZATION_KEY_PREFIXES) and not key.endswith("."):
                keys.add(key)

    return keys, findings


def build_missing_key_findings(
    source_keys: set[str],
    english_table: dict[str, str],
    simplified_chinese_table: dict[str, str],
    english_path: Path,
    simplified_chinese_path: Path,
) -> list[LocalizationFinding]:
    findings: list[LocalizationFinding] = []
    for key in sorted(source_keys):
        if key not in english_table:
            findings.append(LocalizationFinding("missing-en", english_path, key))
        if key not in simplified_chinese_table:
            findings.append(LocalizationFinding("missing-zh", simplified_chinese_path, key))
    return findings


def build_table_mismatch_findings(
    english_table: dict[str, str],
    simplified_chinese_table: dict[str, str],
    english_path: Path,
    simplified_chinese_path: Path,
) -> list[LocalizationFinding]:
    findings: list[LocalizationFinding] = []
    english_keys = set(english_table.keys())
    simplified_chinese_keys = set(simplified_chinese_table.keys())

    for key in sorted(english_keys - simplified_chinese_keys):
        findings.append(LocalizationFinding("zh-missing-key", simplified_chinese_path, key))

    for key in sorted(simplified_chinese_keys - english_keys):
        findings.append(LocalizationFinding("en-missing-key", english_path, key))

    return findings


def print_findings(findings: list[LocalizationFinding], repo_root: Path) -> None:
    for finding in findings:
        relative_path = finding.path.relative_to(repo_root).as_posix()
        print(f"{relative_path}: {finding.kind}: {finding.detail}")


def main() -> int:
    args = parse_arguments()
    repo_root = Path(args.repo_root).resolve()
    english_path = (repo_root / DEFAULT_ENGLISH_PATH.relative_to(REPOSITORY_ROOT)).resolve()
    simplified_chinese_path = (repo_root / DEFAULT_SIMPLIFIED_CHINESE_PATH.relative_to(REPOSITORY_ROOT)).resolve()

    missing_files = [path for path in (english_path, simplified_chinese_path) if not path.is_file()]
    if missing_files:
        for path in missing_files:
            print(f"Localization file not found: {path}", file=sys.stderr)
        return 1

    english_table = load_localization_table(english_path)
    simplified_chinese_table = load_localization_table(simplified_chinese_path)
    source_paths = resolve_target_files(repo_root)
    source_keys, findings = collect_source_keys(source_paths, repo_root)
    findings.extend(
        build_missing_key_findings(
            source_keys,
            english_table,
            simplified_chinese_table,
            english_path,
            simplified_chinese_path,
        )
    )
    findings.extend(
        build_table_mismatch_findings(
            english_table,
            simplified_chinese_table,
            english_path,
            simplified_chinese_path,
        )
    )

    if findings:
        print_findings(findings, repo_root)
        print(
            "Localization check failed: {0} issue(s), {1} source key(s), {2} file(s).".format(
                len(findings),
                len(source_keys),
                len(source_paths),
            )
        )
        return 1

    if args.verbose:
        print(
            "Localization check passed: {0} source key(s), {1} English key(s), {2} Simplified Chinese key(s), {3} file(s).".format(
                len(source_keys),
                len(english_table),
                len(simplified_chinese_table),
                len(source_paths),
            )
        )

    return 0


if __name__ == "__main__":
    sys.exit(main())
