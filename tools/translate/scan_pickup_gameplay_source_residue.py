from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from gameplay_translation_workflow import REPO_ROOT, TRANSLATABLE_FIELD_PAIRS, load_json


DEFAULT_INPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-gameplay.zh-CN.work.json"
RESIDUE_PATTERNS = {
    "htmlTag": re.compile(r"<[^>\r\n]+>"),
    "wikiTemplateBrace": re.compile(r"\{\{|\}\}"),
    "wikiLinkBracket": re.compile(r"\[\[|\]\]"),
    "pipeAssignment": re.compile(r"\|[A-Za-z_][A-Za-z0-9_ -]*="),
    "rawStatAssignment": re.compile(
        r"(?<![A-Za-z])(?:label|class|dps|damage|firerate|fire_rate|shotspeed|shot_speed|range|force|spread|sold|reload|ammo|magazine|quality|type)\s*=",
        re.IGNORECASE,
    ),
    "htmlEntity": re.compile(r"&(?:nbsp|quot|amp|lt|gt|#\d+);", re.IGNORECASE),
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan gameplay fields for likely source-residue pollution such as HTML, wiki template syntax, or raw stat parameter fragments."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to a gameplay translation batch JSON or the zh-CN gameplay work JSON.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/source-residue-report.<input-name>.json.",
    )
    return parser.parse_args()


def default_output_path(input_path: Path) -> Path:
    safe_name = input_path.stem.replace(" ", "_")
    return REPO_ROOT / "temp" / "{0}.source-residue-report.json".format(safe_name)


def collect_matches(text: str) -> list[dict]:
    matches: list[dict] = []
    for kind, pattern in RESIDUE_PATTERNS.items():
        for match in pattern.finditer(text):
            fragment = match.group(0).strip()
            if not fragment:
                continue
            matches.append(
                {
                    "kind": kind,
                    "fragment": fragment,
                }
            )
    deduped: list[dict] = []
    seen: set[tuple[str, str]] = set()
    for match in matches:
        key = (match["kind"], match["fragment"])
        if key in seen:
            continue
        seen.add(key)
        deduped.append(match)
    return deduped


def build_issue(entry: dict, text: str, field: str, source_field: str) -> dict:
    return {
        "pickupId": entry.get("pickupId"),
        "englishDisplayName": entry.get("englishDisplayName", ""),
        "field": field,
        "sourceField": source_field,
        "matches": collect_matches(text),
        "text": text,
    }


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    output_path = Path(args.output) if args.output.strip() else default_output_path(input_path)

    payload = load_json(input_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Input file did not contain an 'entries' array: {0}".format(input_path))

    english_source_issues: list[dict] = []
    chinese_text_issues: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue

        for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
            english_text = str(entry.get(english_key, "")).strip()
            chinese_text = str(entry.get(chinese_key, "")).strip()

            english_matches = collect_matches(english_text)
            if english_matches:
                issue = build_issue(entry, english_text, english_key, english_key)
                issue["matches"] = english_matches
                english_source_issues.append(issue)

            chinese_matches = collect_matches(chinese_text)
            if chinese_matches:
                issue = build_issue(entry, chinese_text, chinese_key, english_key)
                issue["matches"] = chinese_matches
                chinese_text_issues.append(issue)

    report = {
        "inputFile": input_path.as_posix(),
        "englishSourceIssueCount": len(english_source_issues),
        "chineseTextIssueCount": len(chinese_text_issues),
        "englishSourceIssues": english_source_issues,
        "chineseTextIssues": chinese_text_issues,
        "notes": [
            "This scanner looks for likely source-pollution residue such as HTML tags, wiki template braces, raw parameter assignments, or HTML entities.",
            "English-source issues often indicate upstream scrape cleanup is needed. Chinese-text issues usually mean raw residue leaked into the translation output.",
        ],
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(
        "Scanned {0}. Found {1} English-source residue issue(s) and {2} Chinese-text residue issue(s). Report: {3}".format(
            input_path,
            len(english_source_issues),
            len(chinese_text_issues),
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
