from __future__ import annotations

import argparse
import sys
from pathlib import Path

TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

from tool_common import LICENSE_HEADER, run_cli

IGNORED_DIRECTORY_NAMES = {
    ".git",
    ".cache",
    "bin",
    "dist",
    "obj",
    "temp",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Verify required license headers on source files.")
    parser.add_argument(
        "--repo-root",
        default=str(Path(__file__).resolve().parents[2]),
        help="Repository root to scan. Defaults to the repository root.",
    )
    return parser.parse_args()


def is_ignored(path: Path, repo_root: Path) -> bool:
    relative_parts = path.relative_to(repo_root).parts
    return any(part in IGNORED_DIRECTORY_NAMES for part in relative_parts)


def scan_files(repo_root: Path) -> list[Path]:
    matches: list[Path] = []
    for path in repo_root.rglob("*.cs"):
        if path.is_file() and not is_ignored(path, repo_root):
            matches.append(path)
    return matches


def has_required_header(path: Path) -> bool:
    text = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")
    return text.startswith(LICENSE_HEADER + "\n\n")


def main() -> int:
    args = parse_args()
    repo_root = Path(args.repo_root).resolve()
    missing = [path for path in scan_files(repo_root) if not has_required_header(path)]
    if missing:
        print("Missing required license header in {0} file(s):".format(len(missing)), file=sys.stderr)
        for path in missing:
            print(str(path), file=sys.stderr)
        return 1

    print("License header check passed for {0} file(s).".format(len(scan_files(repo_root))))
    return 0


if __name__ == "__main__":
    raise SystemExit(run_cli(main))
