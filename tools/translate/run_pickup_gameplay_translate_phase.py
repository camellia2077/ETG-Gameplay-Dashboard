from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from gameplay_translation_workflow import REPO_ROOT, load_json


SCRIPT_DIR = Path(__file__).resolve().parent
CHECK_SCRIPT_PATH = SCRIPT_DIR / "check_pickup_gameplay_translation_batch.py"
NORMALIZE_SCRIPT_PATH = SCRIPT_DIR / "normalize_pickup_gameplay_item_names.py"
DEFAULT_BATCH_DIR = REPO_ROOT / "temp" / "pickup-gameplay-translation-batches"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="High-level translation-phase workflow: export/check a batch, then optionally normalize canonical pickup names and re-check."
    )
    parser.add_argument("--input", default="", help="Optional existing batch JSON path.")
    parser.add_argument("--start-id", type=int, help="Inclusive pickupId lower bound when --input is omitted.")
    parser.add_argument("--end-id", type=int, help="Inclusive pickupId upper bound when --input is omitted.")
    parser.add_argument("--include-complete", action="store_true", help="Include already complete entries when exporting a batch.")
    parser.add_argument(
        "--count",
        type=int,
        help="Limit the exported batch to the first N eligible entries after filtering.",
    )
    parser.add_argument(
        "--only-missing",
        action="store_true",
        help="Only export entries that still have untranslated Chinese fields.",
    )
    parser.add_argument(
        "--normalize",
        action="store_true",
        help="Run normalize on the batch and then re-run check. Useful after translation edits.",
    )
    parser.add_argument(
        "--summary",
        action="store_true",
        help="Print only the consolidated issue summary instead of all scanner output.",
    )
    return parser.parse_args()


def run_script(script_path: Path, arguments: list[str], *, echo_stdout: bool = True) -> None:
    completed = subprocess.run(
        [sys.executable, str(script_path)] + arguments,
        text=True,
        encoding="utf-8",
        capture_output=True,
        check=True,
    )
    if echo_stdout and completed.stdout:
        print(completed.stdout.strip())


def default_batch_path(start_id: int, end_id: int) -> Path:
    return DEFAULT_BATCH_DIR / "pickup-gameplay.zh-CN.{0:04d}-{1:04d}.check.json".format(start_id, end_id)


def default_count_batch_path(count: int, only_missing: bool) -> Path:
    mode = "missing" if only_missing else "all"
    return DEFAULT_BATCH_DIR / "pickup-gameplay.zh-CN.count-{0:04d}.{1}.check.json".format(count, mode)


def default_report_path(start_id: int, end_id: int) -> Path:
    return DEFAULT_BATCH_DIR / "pickup-gameplay.zh-CN.{0:04d}-{1:04d}.check-report.json".format(start_id, end_id)


def infer_range_from_batch(batch_path: Path) -> tuple[int, int]:
    payload = load_json(batch_path)
    pickup_id_range = payload.get("pickupIdRange", {})
    if isinstance(pickup_id_range, dict):
        start_id = int(pickup_id_range.get("start", -1))
        end_id = int(pickup_id_range.get("end", -1))
        if start_id >= 0 and end_id >= 0:
            return start_id, end_id

    entries = payload.get("entries", [])
    pickup_ids = [entry.get("pickupId") for entry in entries if isinstance(entry, dict) and isinstance(entry.get("pickupId"), int)]
    if not pickup_ids:
        raise ValueError("Could not infer pickupId range from batch: {0}".format(batch_path))
    return min(pickup_ids), max(pickup_ids)


def build_summary_message(report: dict[str, object], batch_path: Path) -> str:
    return (
        "Summary for {0}: Missing={1}, Unexpected Chinese={2}, Quotes={3}, Proper nouns={4}, UI/controls={5}, "
        "English residue={6}, Chinese residue={7}."
    ).format(
        batch_path,
        int(report.get("missingTranslationIssueCount", 0)),
        int(report.get("unexpectedChineseContentIssueCount", 0)),
        int(report.get("quotePreservationIssueCount", 0)),
        int(report.get("properNounFirstMentionIssueCount", 0)),
        int(report.get("uiControlTermIssueCount", 0)),
        int(report.get("englishSourceResidueIssueCount", 0)),
        int(report.get("chineseTextResidueIssueCount", 0)),
    )


def main() -> int:
    args = parse_args()

    if args.input.strip():
        batch_path = Path(args.input)
        run_script(CHECK_SCRIPT_PATH, ["--input", str(batch_path)], echo_stdout=not args.summary)
    else:
        if (args.start_id is None) != (args.end_id is None):
            raise ValueError("--start-id and --end-id must be provided together.")
        if args.count is None and (args.start_id is None or args.end_id is None):
            raise ValueError("--start-id and --end-id are required when --input is omitted unless --count is provided.")
        if args.count is not None and args.count <= 0:
            raise ValueError("--count must be greater than 0.")
        if args.start_id is not None and args.end_id is not None:
            batch_path = default_batch_path(args.start_id, args.end_id)
        else:
            batch_path = default_count_batch_path(args.count, args.only_missing)
        check_args = []
        if args.start_id is not None and args.end_id is not None:
            check_args.extend(["--start-id", str(args.start_id), "--end-id", str(args.end_id)])
        if args.include_complete:
            check_args.append("--include-complete")
        if args.count is not None:
            check_args.extend(["--count", str(args.count)])
        if args.only_missing:
            check_args.append("--only-missing")
        run_script(CHECK_SCRIPT_PATH, check_args, echo_stdout=not args.summary)

    if args.normalize:
        run_script(NORMALIZE_SCRIPT_PATH, ["--input", str(batch_path)], echo_stdout=not args.summary)
        run_script(CHECK_SCRIPT_PATH, ["--input", str(batch_path)], echo_stdout=not args.summary)

    start_id, end_id = infer_range_from_batch(batch_path)
    report_path = default_report_path(start_id, end_id)
    report = load_json(report_path)

    if args.summary:
        print("Translation phase complete. Batch file: {0}".format(batch_path))
        print(build_summary_message(report, batch_path))
        print("Next step: edit only the batch file, then re-run with --input <batch-json> --normalize.")
        return 0

    print("Translation phase complete. Batch file: {0}".format(batch_path))
    print("Next step: edit only the batch file, then re-run with --input <batch-json> --normalize.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
