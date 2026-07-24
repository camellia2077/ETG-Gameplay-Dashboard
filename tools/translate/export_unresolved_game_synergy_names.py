from __future__ import annotations

import argparse
import re
from pathlib import Path
from typing import Any

from gameplay_translation_workflow import load_json, write_json
from localize_game_synergy_names import (
    DEFAULT_REPORT_PATH,
    DEFAULT_SYNERGY_PATH,
    DEFAULT_WORK_PATH,
    SPECIAL_UNTRANSLATED_NAMES,
    parse_synergy_catalog,
    split_english_synergy_segments,
    split_segments,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT_PATH = REPO_ROOT / "temp" / "unresolved-game-synergy-names.json"
SINGLE_WORD_TITLE_RE = re.compile(r"^([A-Za-z][A-Za-z'’.-]*)\s*:\s*")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Export unresolved game synergy-name localization items with pickup IDs for manual review."
    )
    parser.add_argument("--input", default=str(DEFAULT_WORK_PATH), help="Path to the zh-CN gameplay work file.")
    parser.add_argument("--synergies", default=str(DEFAULT_SYNERGY_PATH), help="Path to the extracted synergy catalog.")
    parser.add_argument("--report", default=str(DEFAULT_REPORT_PATH), help="Path to the localization report.")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT_PATH), help="Path for the unresolved-items JSON.")
    return parser.parse_args()


def build_pickup_context(entry: dict[str, Any]) -> dict[str, Any]:
    return {
        "pickupId": int(entry.get("pickupId", -1)),
        "englishDisplayName": str(entry.get("englishDisplayName", "") or "").strip(),
        "chineseDisplayName": str(entry.get("chineseDisplayName", "") or "").strip(),
        "wikiKey": str(entry.get("wikiKey", "") or "").strip(),
    }


def find_missing_single_word_english_titles(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Find notes/synergy segments whose English header is not retained in Chinese."""
    findings: list[dict[str, Any]] = []
    field_pairs = (
        ("englishSynergyHighlights", "chineseSynergyHighlights"),
        ("englishUsageNotes", "chineseUsageNotes"),
    )
    for entry in entries:
        for english_field, chinese_field in field_pairs:
            english_segments = split_segments(entry.get(english_field))
            chinese_segments = split_segments(entry.get(chinese_field))
            for index, english_segment in enumerate(english_segments):
                match = SINGLE_WORD_TITLE_RE.match(english_segment)
                if not match or index >= len(chinese_segments):
                    continue
                english_title = match.group(1)
                if english_title.lower() in {"http", "https"}:
                    continue
                chinese_segment = chinese_segments[index]
                if re.search(r"（" + re.escape(english_title) + r"）", chinese_segment, re.IGNORECASE):
                    continue
                findings.append(
                    {
                        **build_pickup_context(entry),
                        "field": chinese_field,
                        "segmentIndex": index,
                        "englishTitle": english_title,
                        "englishText": english_segment,
                        "currentChineseText": chinese_segment,
                        "reason": "single-word-english-title-not-retained",
                    }
                )
    return findings


def main() -> int:
    args = parse_args()
    work_payload = load_json(Path(args.input).resolve())
    synergy_payload = load_json(Path(args.synergies).resolve())
    report_payload = load_json(Path(args.report).resolve())
    catalog = parse_synergy_catalog(synergy_payload)
    english_titles = [english for english, _ in catalog.values()]
    entries_by_id = {
        int(entry.get("pickupId", -1)): entry
        for entry in work_payload.get("entries", [])
        if isinstance(entry, dict)
    }
    entries = list(entries_by_id.values())
    missing_single_word_titles = find_missing_single_word_english_titles(entries)

    missing_catalog_names: list[dict[str, Any]] = []
    special_untranslated_names: list[dict[str, Any]] = []
    for key, (english, chinese) in catalog.items():
        if chinese:
            continue
        affected_pickups = []
        for entry in entries_by_id.values():
            if english in str(entry.get("englishSynergyHighlights", "")):
                affected_pickups.append(build_pickup_context(entry))
        item = {
            "key": key,
            "englishTitle": english,
            "gameChineseTitle": "",
            "affectedPickups": affected_pickups,
        }
        if key in SPECIAL_UNTRANSLATED_NAMES:
            item["specialTitle"] = SPECIAL_UNTRANSLATED_NAMES[key]
            item["reason"] = "intentional-special-untranslated-name"
            special_untranslated_names.append(item)
        else:
            item["reason"] = "game-resource-has-no-chinese-name"
            missing_catalog_names.append(item)

    ambiguous_items: list[dict[str, Any]] = []
    for item in report_payload.get("skippedAmbiguous", []):
        if not isinstance(item, dict):
            continue
        pickup_id = int(item.get("pickupId", -1))
        entry = entries_by_id.get(pickup_id, {})
        field = str(item.get("field", "") or "")
        expected_title = str(item.get("chineseTitle", "") or "").strip()
        expected_english = str(item.get("englishTitle", "") or "").strip()
        whole_field = str(entry.get(field, "") or "")
        if expected_title and expected_english:
            canonical_title = expected_title + "（" + expected_english + "）"
            if canonical_title in whole_field:
                continue
        output_item = {
            **build_pickup_context(entry),
            "field": field,
            "segmentIndex": int(item.get("segmentIndex", -1)),
            "key": str(item.get("key", "") or ""),
            "englishTitle": expected_english,
            "gameChineseTitle": expected_title,
            "currentChineseText": str(item.get("text", "") or ""),
            "reason": "ambiguous-title-boundary",
        }
        ambiguous_items.append(output_item)

    alignment_items: list[dict[str, Any]] = []
    for item in report_payload.get("alignmentIssues", []):
        if not isinstance(item, dict):
            continue
        pickup_id = int(item.get("pickupId", -1))
        entry = entries_by_id.get(pickup_id, {})
        alignment_items.append(
            {
                **build_pickup_context(entry),
                "englishCount": int(item.get("englishCount", 0)),
                "chineseCount": int(item.get("chineseCount", 0)),
                "englishSegments": split_english_synergy_segments(entry.get("englishSynergyHighlights"), english_titles),
                "currentChineseSegments": split_segments(entry.get("chineseSynergyHighlights")),
                "reason": "english-chinese-segment-count-mismatch",
            }
        )

    output = {
        "schemaVersion": 1,
        "purpose": "Manual review of unresolved game synergy-name localization.",
        "source": {
            "workFile": str(Path(args.input).resolve()),
            "synergyCatalog": str(Path(args.synergies).resolve()),
            "localizationReport": str(Path(args.report).resolve()),
        },
        "counts": {
            "catalogWithoutChineseName": len(missing_catalog_names),
            "ambiguousTitleItems": len(ambiguous_items),
            "alignmentIssueItems": len(alignment_items),
            "specialUntranslatedNames": len(special_untranslated_names),
            "missingSingleWordEnglishTitles": len(missing_single_word_titles),
        },
        "catalogWithoutChineseName": missing_catalog_names,
        "specialUntranslatedNames": special_untranslated_names,
        "ambiguousTitleItems": ambiguous_items,
        "alignmentIssueItems": alignment_items,
        "missingSingleWordEnglishTitles": missing_single_word_titles,
    }
    output_path = Path(args.output).resolve()
    write_json(output_path, output)
    print("Wrote {0} unresolved item(s) to {1}".format(len(ambiguous_items) + len(alignment_items), output_path))
    print("Catalog entries without game Chinese names: {0}".format(len(missing_catalog_names)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
