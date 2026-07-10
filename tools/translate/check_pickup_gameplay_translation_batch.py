from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

from gameplay_translation_workflow import REPO_ROOT, TRANSLATABLE_FIELD_PAIRS, build_default_batch_output_path, load_json, write_json


DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "RandomLoadout.pickup-gameplay.zh-CN.work.json"
DEFAULT_BATCH_DIR = REPO_ROOT / "temp" / "pickup-gameplay-translation-batches"
EXPORT_SCRIPT_PATH = REPO_ROOT / "tools" / "translate" / "new" / "export_pickup_gameplay_translation_batch.py"
QUOTE_SCAN_SCRIPT_PATH = REPO_ROOT / "tools" / "translate" / "new" / "scan_pickup_gameplay_quote_preservation.py"
PROPER_NOUN_SCAN_SCRIPT_PATH = REPO_ROOT / "tools" / "translate" / "new" / "scan_pickup_gameplay_proper_noun_first_mentions.py"
UI_CONTROL_SCAN_SCRIPT_PATH = REPO_ROOT / "tools" / "translate" / "new" / "scan_pickup_gameplay_ui_control_terms.py"
SOURCE_RESIDUE_SCAN_SCRIPT_PATH = REPO_ROOT / "tools" / "translate" / "new" / "scan_pickup_gameplay_source_residue.py"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Preview-check a pickup gameplay translation range by exporting a batch and running validation scans."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json.",
    )
    parser.add_argument(
        "--input",
        default="",
        help="Optional existing batch JSON path. If omitted, the script exports a temporary batch from --start-id/--end-id.",
    )
    parser.add_argument(
        "--start-id",
        type=int,
        help="Inclusive pickupId lower bound. Required when --input is omitted.",
    )
    parser.add_argument(
        "--end-id",
        type=int,
        help="Inclusive pickupId upper bound. Required when --input is omitted.",
    )
    parser.add_argument(
        "--include-complete",
        action="store_true",
        help="Include already complete entries when exporting a temporary batch.",
    )
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
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/pickup-gameplay-translation-batches/pickup-gameplay.zh-CN.<start>-<end>.check-report.json.",
    )
    return parser.parse_args()


def default_batch_path(start_id: int, end_id: int) -> Path:
    return build_default_batch_output_path(start_id, end_id, checked=True)


def default_count_batch_path(count: int, only_missing: bool) -> Path:
    return build_default_batch_output_path(count=count, only_missing=only_missing, checked=True)


def default_report_path(start_id: int, end_id: int) -> Path:
    return DEFAULT_BATCH_DIR / "pickup-gameplay.zh-CN.{0:04d}-{1:04d}.check-report.json".format(start_id, end_id)


def run_python_script(script_path: Path, arguments: list[str]) -> subprocess.CompletedProcess[str]:
    command = [sys.executable, str(script_path)] + arguments
    return subprocess.run(command, check=True, capture_output=True, text=True, encoding="utf-8")


def infer_range_from_batch(batch_payload: dict) -> tuple[int, int]:
    pickup_id_range = batch_payload.get("pickupIdRange", {})
    if isinstance(pickup_id_range, dict):
        start_id = int(pickup_id_range.get("start", -1))
        end_id = int(pickup_id_range.get("end", -1))
        if start_id >= 0 and end_id >= 0:
            return start_id, end_id

    entries = batch_payload.get("entries", [])
    pickup_ids = [entry.get("pickupId") for entry in entries if isinstance(entry, dict) and isinstance(entry.get("pickupId"), int)]
    if not pickup_ids:
        return -1, -1
    return min(pickup_ids), max(pickup_ids)


def collect_missing_translation_issues(entries: list[dict]) -> list[dict]:
    issues: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue

        missing_fields: list[str] = []
        for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
            if str(entry.get(english_key, "")).strip() and not str(entry.get(chinese_key, "")).strip():
                missing_fields.append(chinese_key)

        if not missing_fields:
            continue

        issues.append(
            {
                "pickupId": entry.get("pickupId"),
                "englishDisplayName": entry.get("englishDisplayName", ""),
                "missingChineseFields": missing_fields,
                "suggestedFix": "Fill the missing Chinese fields from the corresponding English source fields. Keep the Chinese field blank if the English field is blank.",
            }
        )
    return issues


def collect_unexpected_chinese_content_issues(entries: list[dict]) -> list[dict]:
    issues: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue

        unexpected_fields: list[str] = []
        for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
            if not str(entry.get(english_key, "")).strip() and str(entry.get(chinese_key, "")).strip():
                unexpected_fields.append(chinese_key)

        if not unexpected_fields:
            continue

        issues.append(
            {
                "pickupId": entry.get("pickupId"),
                "englishDisplayName": entry.get("englishDisplayName", ""),
                "unexpectedChineseFields": unexpected_fields,
                "suggestedFix": "Clear these Chinese fields because the corresponding English source fields are blank.",
            }
        )
    return issues


def summarize_statuses(entries: list[dict]) -> dict[str, int]:
    counts: dict[str, int] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        status = str(entry.get("translationStatus", "")).strip().lower() or "unknown"
        counts[status] = counts.get(status, 0) + 1
    return counts


def add_quote_issue_suggestions(issues: list[dict]) -> list[dict]:
    updated_issues: list[dict] = []
    for issue in issues:
        updated_issue = dict(issue)
        missing_quotes = updated_issue.get("missingQuotedEnglishFragments", [])
        if isinstance(missing_quotes, list) and missing_quotes:
            updated_issue["suggestedFix"] = "Preserve the quoted English fragment(s) in the Chinese text: {0}.".format(
                ", ".join('"{0}"'.format(fragment) for fragment in missing_quotes)
            )
        else:
            updated_issue["suggestedFix"] = "Preserve the quoted English fragment from the source text in the Chinese text."
        updated_issues.append(updated_issue)
    return updated_issues


def add_proper_noun_issue_suggestions(issues: list[dict]) -> list[dict]:
    updated_issues: list[dict] = []
    for issue in issues:
        updated_issue = dict(issue)
        fragments = updated_issue.get("suspiciousRawLatinFragments", [])
        if isinstance(fragments, list) and fragments:
            rewrite_templates = ["中文待定（{0}）".format(fragment) for fragment in fragments]
            updated_issue["suggestedFix"] = (
                "For the first mention, either replace these with the canonical in-game Chinese term if one exists, "
                "or rewrite them using templates like: {0}."
            ).format("; ".join(rewrite_templates))
            updated_issue["suggestedRewriteTemplates"] = rewrite_templates
        else:
            updated_issue["suggestedFix"] = (
                "For the first mention, replace the raw foreign proper noun with the canonical in-game Chinese term "
                "if one exists, or rewrite it as 中文待定（English）."
            )
        updated_issues.append(updated_issue)
    return updated_issues


def add_source_residue_issue_suggestions(issues: list[dict], is_english_source: bool) -> list[dict]:
    updated_issues: list[dict] = []
    for issue in issues:
        updated_issue = dict(issue)
        matches = updated_issue.get("matches", [])
        fragments = []
        if isinstance(matches, list):
            fragments = [str(match.get("fragment", "")).strip() for match in matches if isinstance(match, dict)]
            fragments = [fragment for fragment in fragments if fragment]

        if is_english_source:
            if fragments:
                updated_issue["suggestedFix"] = (
                    "The English source appears contaminated by raw residue fragments: {0}. "
                    "Clean or reinterpret the source before translating this field literally."
                ).format(", ".join(fragments))
            else:
                updated_issue["suggestedFix"] = (
                    "The English source appears contaminated by raw markup or parameter residue. Review the source manually before translating."
                )
        else:
            if fragments:
                updated_issue["suggestedFix"] = (
                    "Remove raw residue from the Chinese text and rewrite it as natural prose. Suspicious fragments: {0}."
                ).format(", ".join(fragments))
            else:
                updated_issue["suggestedFix"] = "Remove raw markup or parameter residue from the Chinese text and rewrite it as natural prose."
        updated_issues.append(updated_issue)
    return updated_issues


def add_ui_control_issue_suggestions(issues: list[dict]) -> list[dict]:
    updated_issues: list[dict] = []
    for issue in issues:
        updated_issue = dict(issue)
        matches = updated_issue.get("matches", [])
        rewrites = []
        if isinstance(matches, list):
            rewrites = [
                str(match.get("suggestedRewrite", "")).strip()
                for match in matches
                if isinstance(match, dict) and str(match.get("suggestedRewrite", "")).strip()
            ]
        if rewrites:
            updated_issue["suggestedFix"] = (
                "Replace raw English control/UI terms with natural Chinese wording, or wrap them as 中文（English） when needed. "
                "Suggested rewrites: {0}."
            ).format("; ".join(rewrites))
            updated_issue["suggestedRewriteTemplates"] = rewrites
        else:
            updated_issue["suggestedFix"] = (
                "Replace raw English control/UI terms with natural Chinese wording, or wrap them as 中文（English） when needed."
            )
        updated_issues.append(updated_issue)
    return updated_issues


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)

    if args.input.strip():
        batch_path = Path(args.input)
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
        export_args = [
            "--work-file",
            str(work_file_path),
            "--output",
            str(batch_path),
        ]
        if args.start_id is not None and args.end_id is not None:
            export_args.extend(["--start-id", str(args.start_id), "--end-id", str(args.end_id)])
        if args.count is not None:
            export_args.extend(["--count", str(args.count)])
        if args.only_missing:
            export_args.append("--only-missing")
        if args.include_complete:
            export_args.append("--include-complete")
        export_result = run_python_script(EXPORT_SCRIPT_PATH, export_args)
        print(export_result.stdout.strip())

    batch_payload = load_json(batch_path)
    entries = batch_payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Batch file did not contain an 'entries' array: {0}".format(batch_path))

    start_id, end_id = infer_range_from_batch(batch_payload)
    report_path = Path(args.output) if args.output.strip() else default_report_path(start_id, end_id)

    quote_result = run_python_script(QUOTE_SCAN_SCRIPT_PATH, ["--input", str(batch_path)])
    proper_noun_result = run_python_script(PROPER_NOUN_SCAN_SCRIPT_PATH, ["--input", str(batch_path)])
    ui_control_result = run_python_script(UI_CONTROL_SCAN_SCRIPT_PATH, ["--input", str(batch_path)])
    source_residue_result = run_python_script(SOURCE_RESIDUE_SCAN_SCRIPT_PATH, ["--input", str(batch_path)])
    print(quote_result.stdout.strip())
    print(proper_noun_result.stdout.strip())
    print(ui_control_result.stdout.strip())
    print(source_residue_result.stdout.strip())

    quote_report_path = REPO_ROOT / "temp" / "{0}.quote-preservation-report.json".format(batch_path.stem.replace(" ", "_"))
    proper_noun_report_path = REPO_ROOT / "temp" / "{0}.proper-noun-first-mention-report.json".format(
        batch_path.stem.replace(" ", "_")
    )
    ui_control_report_path = REPO_ROOT / "temp" / "{0}.ui-control-term-report.json".format(batch_path.stem.replace(" ", "_"))
    source_residue_report_path = REPO_ROOT / "temp" / "{0}.source-residue-report.json".format(batch_path.stem.replace(" ", "_"))
    quote_report = load_json(quote_report_path)
    proper_noun_report = load_json(proper_noun_report_path)
    ui_control_report = load_json(ui_control_report_path)
    source_residue_report = load_json(source_residue_report_path)

    missing_translation_issues = collect_missing_translation_issues(entries)
    unexpected_chinese_content_issues = collect_unexpected_chinese_content_issues(entries)
    quote_issues = add_quote_issue_suggestions(quote_report.get("issues", []))
    proper_noun_issues = add_proper_noun_issue_suggestions(proper_noun_report.get("issues", []))
    ui_control_issues = add_ui_control_issue_suggestions(ui_control_report.get("issues", []))
    english_source_residue_issues = add_source_residue_issue_suggestions(
        source_residue_report.get("englishSourceIssues", []),
        is_english_source=True,
    )
    chinese_text_residue_issues = add_source_residue_issue_suggestions(
        source_residue_report.get("chineseTextIssues", []),
        is_english_source=False,
    )

    report = {
        "workflow": "pickup-gameplay-translation-check",
        "batchFile": batch_path.as_posix(),
        "pickupIdRange": {
            "start": start_id,
            "end": end_id,
        },
        "entryCount": len(entries),
        "statusCounts": summarize_statuses(entries),
        "missingTranslationIssueCount": len(missing_translation_issues),
        "unexpectedChineseContentIssueCount": len(unexpected_chinese_content_issues),
        "quotePreservationIssueCount": int(quote_report.get("issueCount", 0)),
        "properNounFirstMentionIssueCount": int(proper_noun_report.get("issueCount", 0)),
        "uiControlTermIssueCount": int(ui_control_report.get("issueCount", 0)),
        "englishSourceResidueIssueCount": int(source_residue_report.get("englishSourceIssueCount", 0)),
        "chineseTextResidueIssueCount": int(source_residue_report.get("chineseTextIssueCount", 0)),
        "missingTranslationIssues": missing_translation_issues,
        "unexpectedChineseContentIssues": unexpected_chinese_content_issues,
        "quotePreservationIssues": quote_issues,
        "properNounFirstMentionIssues": proper_noun_issues,
        "uiControlTermIssues": ui_control_issues,
        "englishSourceResidueIssues": english_source_residue_issues,
        "chineseTextResidueIssues": chinese_text_residue_issues,
    }
    write_json(report_path, report)
    print(
        "Wrote translation check report to {0}. Missing: {1}, Unexpected Chinese: {2}, Quotes: {3}, Proper nouns: {4}, UI/controls: {5}, English residue: {6}, Chinese residue: {7}.".format(
            report_path,
            report["missingTranslationIssueCount"],
            report["unexpectedChineseContentIssueCount"],
            report["quotePreservationIssueCount"],
            report["properNounFirstMentionIssueCount"],
            report["uiControlTermIssueCount"],
            report["englishSourceResidueIssueCount"],
            report["chineseTextResidueIssueCount"],
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
