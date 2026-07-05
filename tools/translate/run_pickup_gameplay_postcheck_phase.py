from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from gameplay_translation_workflow import REPO_ROOT, load_json


SCRIPT_DIR = Path(__file__).resolve().parent
CHECK_SCRIPT_PATH = SCRIPT_DIR / "check_pickup_gameplay_translation_batch.py"
STAT_SOURCE_DRIFT_SCRIPT_PATH = SCRIPT_DIR / "scan_pickup_gameplay_stat_value_source_drift.py"
APPLY_SCRIPT_PATH = SCRIPT_DIR / "apply_pickup_gameplay_translation_batch.py"
NAMING_SCRIPT_PATH = REPO_ROOT / "tools" / "devtools" / "check_naming.py"
BUILD_SCRIPT_PATH = REPO_ROOT / "tools" / "build" / "build.py"
DEFAULT_BATCH_DIR = REPO_ROOT / "temp" / "pickup-gameplay-translation-batches"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="High-level post-translation workflow: automated checks first, then optional apply/build after manual review."
    )
    parser.add_argument("--input", default="", help="Optional existing batch JSON path.")
    parser.add_argument("--start-id", type=int, help="Inclusive pickupId lower bound when --input is omitted.")
    parser.add_argument("--end-id", type=int, help="Inclusive pickupId upper bound when --input is omitted.")
    parser.add_argument("--include-complete", action="store_true", help="Include already complete entries when exporting a batch.")
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Apply the batch back to the zh-CN work file after automated checks are clean. Use this after manual review.",
    )
    parser.add_argument(
        "--with-build",
        action="store_true",
        help="Run the Debug build after apply and naming check.",
    )
    parser.add_argument(
        "--summary",
        action="store_true",
        help="Print only the consolidated issue summary instead of all scanner output.",
    )
    return parser.parse_args()


def run_script(script_path: Path, arguments: list[str], *, echo_stdout: bool = True) -> subprocess.CompletedProcess[str]:
    completed = subprocess.run(
        [sys.executable, str(script_path)] + arguments,
        text=True,
        encoding="utf-8",
        check=True,
        capture_output=True,
    )
    if echo_stdout and completed.stdout:
        print(completed.stdout.strip())
    return completed


def build_next_step_message(blocking_issues: dict[str, int]) -> str:
    issue_names = ", ".join(blocking_issues.keys())
    if any(name in blocking_issues for name in ("Missing", "Unexpected Chinese", "Quotes", "Proper nouns", "UI/controls", "English residue", "Chinese residue")):
        return (
            "Next step: fix {0}, then re-run "
            "python .\\tools\\translate\\new\\main.py translate --input <batch-json> --normalize"
        ).format(issue_names)
    if issue_names:
        return "Next step: review and fix {0}, then re-run postcheck.".format(issue_names)
    return "Next step: review manually, then re-run with --apply if it still looks good."


def default_batch_path(start_id: int, end_id: int) -> Path:
    return DEFAULT_BATCH_DIR / "pickup-gameplay.zh-CN.{0:04d}-{1:04d}.check.json".format(start_id, end_id)


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


def main() -> int:
    args = parse_args()

    if args.input.strip():
        batch_path = Path(args.input)
        run_script(CHECK_SCRIPT_PATH, ["--input", str(batch_path)], echo_stdout=not args.summary)
        start_id, end_id = infer_range_from_batch(batch_path)
    else:
        if args.start_id is None or args.end_id is None:
            raise ValueError("--start-id and --end-id are required when --input is omitted.")
        start_id, end_id = args.start_id, args.end_id
        batch_path = default_batch_path(start_id, end_id)
        check_args = ["--start-id", str(start_id), "--end-id", str(end_id)]
        if args.include_complete:
            check_args.append("--include-complete")
        run_script(CHECK_SCRIPT_PATH, check_args, echo_stdout=not args.summary)

    report_path = default_report_path(start_id, end_id)
    report = load_json(report_path)

    run_script(STAT_SOURCE_DRIFT_SCRIPT_PATH, [], echo_stdout=not args.summary)
    run_script(NAMING_SCRIPT_PATH, ["--verbose"], echo_stdout=not args.summary)

    blocking_counts = {
        "Missing": int(report.get("missingTranslationIssueCount", 0)),
        "Unexpected Chinese": int(report.get("unexpectedChineseContentIssueCount", 0)),
        "Quotes": int(report.get("quotePreservationIssueCount", 0)),
        "Proper nouns": int(report.get("properNounFirstMentionIssueCount", 0)),
        "UI/controls": int(report.get("uiControlTermIssueCount", 0)),
        "English residue": int(report.get("englishSourceResidueIssueCount", 0)),
        "Chinese residue": int(report.get("chineseTextResidueIssueCount", 0)),
    }
    blocking_issues = {name: count for name, count in blocking_counts.items() if count > 0}

    if blocking_issues:
        print("Post-translation automated checks found blocking issues:")
        for name, count in blocking_issues.items():
            print("  - {0}: {1}".format(name, count))
        print(build_next_step_message(blocking_issues))
        return 1

    if args.summary:
        print("Post-translation automated checks passed.")
        print(
            "Summary: Missing=0, Unexpected Chinese=0, Quotes=0, Proper nouns=0, UI/controls=0, English residue=0, Chinese residue=0."
        )
        if not args.apply:
            print("Next step: manual review, then re-run with --apply if it still looks good.")
            return 0
        print("Next step: apply the batch now.")

    if not args.apply:
        print("Post-translation automated checks passed.")
        print("Next step: do manual review. If it still looks good, re-run with --apply to write back the batch.")
        return 0

    run_script(APPLY_SCRIPT_PATH, ["--input", str(batch_path)], echo_stdout=not args.summary)
    run_script(NAMING_SCRIPT_PATH, ["--verbose"], echo_stdout=not args.summary)
    if args.with_build:
        run_script(BUILD_SCRIPT_PATH, ["--configuration", "Debug"])
    print("Post-translation apply phase complete.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
