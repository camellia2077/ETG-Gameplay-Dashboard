from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from translation_workflow import load_json


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json"

# Match ASCII-led terms or phrases that still appear as bare English inside translated prose.
ENGLISH_TERM_PATTERN = re.compile(
    r"(?<![A-Za-z0-9])"
    r"([A-Za-z][A-Za-z0-9&.'-]*"
    r"(?:\s+[A-Za-z0-9&.'-]+){0,5})"
    r"(?![A-Za-z0-9])"
)
PARENTHESIZED_ENGLISH_PATTERN = re.compile(r"（[^（）]*[A-Za-z][^（）]*）")
ALL_CAPS_SHORT_PATTERN = re.compile(r"^[A-Z0-9]{1,4}$")

STOP_TERMS = {
    "ETG",
    "XTG",
    "DPS",
    "AGL",
    "AWP",
    "RPG",
    "LRAD",
    "CQC",
    "Boss",
    "Apple Arcade",
    "Supply Drop",
    "Hyper Beam",
    "Klobbering Time",
    "Triple Sticky",
    "Single Action",
    "Shotgun Affinity",
    "Gun That Can Kill The Past",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan translated pickup wiki tips for remaining bare English terms that may deserve 中文名（English Name） normalization."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json.",
    )
    parser.add_argument("--min-pickup-id", type=int, default=1, help="Inclusive pickupId lower bound.")
    parser.add_argument("--max-pickup-id", type=int, default=60, help="Inclusive pickupId upper bound.")
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON output path for the scan report.",
    )
    return parser.parse_args()


def build_known_terms(entries: list[dict]) -> dict[str, str]:
    known_terms: dict[str, str] = {}

    def add_term(english: str, chinese: str) -> None:
        english = english.strip()
        chinese = chinese.strip()
        if not english or not chinese:
            return
        if ALL_CAPS_SHORT_PATTERN.match(english):
            return
        known_terms.setdefault(english, chinese)

    for entry in entries:
        if not isinstance(entry, dict):
            continue
        chinese_name = str(entry.get("chineseDisplayName", "")).strip()
        english_name = str(entry.get("englishDisplayName", "")).strip()
        wiki_key = str(entry.get("wikiKey", "")).strip().replace("_", " ")
        internal_name = str(entry.get("internalName", "")).strip().replace("_", " ")
        add_term(english_name, chinese_name)
        add_term(wiki_key, chinese_name)
        add_term(internal_name, chinese_name)

    return known_terms


def strip_parenthesized_english(text: str) -> str:
    return PARENTHESIZED_ENGLISH_PATTERN.sub("", text)


def iter_matches(text: str) -> list[tuple[str, int, int]]:
    cleaned = strip_parenthesized_english(text)
    matches: list[tuple[str, int, int]] = []
    for match in ENGLISH_TERM_PATTERN.finditer(cleaned):
        term = match.group(1).strip()
        if not term:
            continue
        matches.append((term, match.start(1), match.end(1)))
    return matches


def should_keep_term(term: str) -> bool:
    if term in STOP_TERMS:
        return False
    if ALL_CAPS_SHORT_PATTERN.match(term):
        return False
    if term.lower() in {"is", "are", "was", "the", "and", "or"}:
        return False
    return True


def find_context(text: str, start: int, end: int, radius: int = 20) -> str:
    left = max(0, start - radius)
    right = min(len(text), end + radius)
    return text[left:right].replace("\n", " ")


def scan_entries(entries: list[dict], min_pickup_id: int, max_pickup_id: int) -> list[dict]:
    known_terms = build_known_terms(entries)
    report: list[dict] = []

    for entry in entries:
        pickup_id = int(entry.get("pickupId", 0))
        if pickup_id < min_pickup_id or pickup_id > max_pickup_id:
            continue

        chinese_notes = str(entry.get("chineseNotes", "")).strip()
        if not chinese_notes:
            continue

        candidates = []
        seen_terms: set[str] = set()
        for term, start, end in iter_matches(chinese_notes):
            if not should_keep_term(term):
                continue
            if term in seen_terms:
                continue
            seen_terms.add(term)
            candidates.append(
                {
                    "term": term,
                    "suggestedChinese": known_terms.get(term, ""),
                    "context": find_context(chinese_notes, start, end),
                }
            )

        if candidates:
            report.append(
                {
                    "pickupId": pickup_id,
                    "wikiKey": str(entry.get("wikiKey", "")),
                    "englishDisplayName": str(entry.get("englishDisplayName", "")),
                    "chineseDisplayName": str(entry.get("chineseDisplayName", "")),
                    "candidateCount": len(candidates),
                    "candidates": candidates,
                }
            )

    return report


def main() -> int:
    args = parse_args()
    payload = load_json(Path(args.work_file))
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Work file did not contain an 'entries' array.")

    report = scan_entries(entries, args.min_pickup_id, args.max_pickup_id)
    output = {
        "workFile": str(Path(args.work_file)),
        "minPickupId": args.min_pickup_id,
        "maxPickupId": args.max_pickup_id,
        "entryCount": len(report),
        "entries": report,
    }

    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Wrote scan report to {output_path}")
    else:
        print(json.dumps(output, ensure_ascii=False, indent=2))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
