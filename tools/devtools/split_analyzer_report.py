from __future__ import annotations

import argparse
import json
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Split a dotnet format analyzer report into per-file task folders."
    )
    parser.add_argument("--report", required=True, help="Path to dotnet format JSON report.")
    parser.add_argument(
        "--output",
        required=True,
        help="Directory where the index and per-file task folders will be written.",
    )
    parser.add_argument(
        "--repo-root",
        default=str(Path(__file__).resolve().parents[2]),
        help="Repository root used to create stable relative paths.",
    )
    return parser.parse_args()


def load_report(report_path: Path) -> list[dict[str, Any]]:
    with report_path.open("r", encoding="utf-8-sig") as handle:
        report = json.load(handle)

    if not isinstance(report, list):
        raise ValueError("The analyzer report root must be a JSON array.")

    return [entry for entry in report if isinstance(entry, dict)]


def normalized_path(path_value: str) -> str:
    return str(Path(path_value).resolve()).casefold()


def relative_path(file_path: Path, repo_root: Path) -> Path:
    try:
        return file_path.resolve().relative_to(repo_root.resolve())
    except ValueError:
        return Path("external") / file_path.name


def diagnostic_key(project_id: str, change: dict[str, Any]) -> tuple[Any, ...]:
    return (
        project_id,
        change.get("LineNumber"),
        change.get("CharNumber"),
        change.get("DiagnosticId"),
        change.get("FormatDescription"),
    )


def build_file_tasks(
    entries: list[dict[str, Any]], repo_root: Path
) -> dict[str, dict[str, Any]]:
    tasks: dict[str, dict[str, Any]] = {}
    seen: defaultdict[str, set[tuple[Any, ...]]] = defaultdict(set)

    for entry in entries:
        file_path_value = entry.get("FilePath")
        if not isinstance(file_path_value, str) or not file_path_value:
            continue

        file_path = Path(file_path_value)
        path_key = normalized_path(file_path_value)
        task = tasks.setdefault(
            path_key,
            {
                "fileName": entry.get("FileName") or file_path.name,
                "filePath": str(file_path.resolve()),
                "relativePath": relative_path(file_path, repo_root).as_posix(),
                "projects": [],
                "diagnostics": [],
            },
        )

        document_id = entry.get("DocumentId") or {}
        project_id = ((document_id.get("ProjectId") or {}).get("Id"))
        document_id_value = document_id.get("Id")
        project = {
            "projectId": project_id,
            "documentId": document_id_value,
        }
        if project not in task["projects"]:
            task["projects"].append(project)

        changes = entry.get("FileChanges") or []
        if not isinstance(changes, list):
            continue
        for change in changes:
            if not isinstance(change, dict):
                continue
            key = diagnostic_key(project_id or "", change)
            if key in seen[path_key]:
                continue
            seen[path_key].add(key)
            task["diagnostics"].append(
                {
                    "projectId": project_id,
                    "documentId": document_id_value,
                    "lineNumber": change.get("LineNumber"),
                    "charNumber": change.get("CharNumber"),
                    "diagnosticId": change.get("DiagnosticId"),
                    "formatDescription": change.get("FormatDescription"),
                }
            )

    return tasks


def write_json(path: Path, value: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(value, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def flattened_task_folder_name(relative_path: str) -> str:
    return relative_path.replace("/", "__").replace("\\", "__") + ".task"


def write_tasks(tasks: dict[str, dict[str, Any]], output_dir: Path) -> dict[str, Any]:
    output_dir.mkdir(parents=True, exist_ok=True)

    index_files: list[dict[str, Any]] = []
    for task in sorted(tasks.values(), key=lambda value: value["relativePath"].casefold()):
        task_folder = output_dir / "files" / flattened_task_folder_name(task["relativePath"])
        task_file = task_folder / "diagnostics.json"
        write_json(task_file, task)
        index_files.append(
            {
                "fileName": task["fileName"],
                "relativePath": task["relativePath"],
                "taskFile": task_file.relative_to(output_dir).as_posix(),
                "diagnosticCount": len(task["diagnostics"]),
            }
        )

    total_diagnostics = sum(item["diagnosticCount"] for item in index_files)
    index = {
        "schemaVersion": 1,
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "fileCount": len(index_files),
        "diagnosticCount": total_diagnostics,
        "files": index_files,
    }
    write_json(output_dir / "index.json", index)
    return index


def main() -> int:
    args = parse_args()
    report_path = Path(args.report).resolve()
    output_dir = Path(args.output).resolve()
    repo_root = Path(args.repo_root).resolve()
    if not report_path.is_file():
        raise FileNotFoundError("Analyzer report not found: {0}".format(report_path))

    entries = load_report(report_path)
    tasks = build_file_tasks(entries, repo_root)
    index = write_tasks(tasks, output_dir)
    print(
        "Split {0} diagnostics across {1} source files into {2}.".format(
            index["diagnosticCount"], index["fileCount"], output_dir
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
