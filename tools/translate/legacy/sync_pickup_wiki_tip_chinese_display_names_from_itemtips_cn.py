from __future__ import annotations

import argparse
import json
from pathlib import Path

from translation_workflow import (
    contains_chinese_characters,
    load_json,
    normalize_lookup_key,
    strip_internal_suffixes,
    write_json,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json"
DEFAULT_ITEMTIPS_TIP_PATH = REPO_ROOT / "temp" / "etg-itemtips-cn" / "itemtips-cn.tip"

# Confirmed game-facing override. The itemtips repo only exposes the shared Megahand entry name.
MANUAL_CHINESE_DISPLAY_NAME_OVERRIDES = {
    "Air Shooter": "空中射手",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Synchronize chineseDisplayName values in the zh-CN work file from etg-itemtips-cn tip data."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json.",
    )
    parser.add_argument(
        "--itemtips-tip",
        default=str(DEFAULT_ITEMTIPS_TIP_PATH),
        help="Path to itemtips-cn.tip from the etg-itemtips-cn repository.",
    )
    return parser.parse_args()


def load_itemtips_name_map(path: Path) -> dict[str, str]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    items = payload.get("items", {})
    results: dict[str, str] = {}
    if not isinstance(items, dict):
        return results

    for item_key, item in items.items():
        if not isinstance(item, dict):
            continue

        normalized_key = normalize_lookup_key(str(item_key))
        chinese_name = str(item.get("name", "")).strip()
        if not normalized_key or not chinese_name or not contains_chinese_characters(chinese_name):
            continue

        results[normalized_key] = chinese_name
    return results


def build_lookup_candidates(entry: dict) -> list[str]:
    english_display_name = str(entry.get("englishDisplayName", "")).strip()
    internal_name = str(entry.get("internalName", "")).strip()
    wiki_key = str(entry.get("wikiKey", "")).strip()

    candidates: list[str] = []
    seen: set[str] = set()

    def add_candidate(value: str) -> None:
        normalized = normalize_lookup_key(value)
        if not normalized or normalized in seen:
            return
        seen.add(normalized)
        candidates.append(normalized)

    add_candidate(english_display_name)
    for variant in strip_internal_suffixes(internal_name):
        add_candidate(variant)

    normalized_wiki_key = normalize_lookup_key(wiki_key)
    normalized_display_name = normalize_lookup_key(english_display_name)
    if normalized_wiki_key and normalized_wiki_key == normalized_display_name:
        add_candidate(wiki_key)

    return candidates


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)
    itemtips_tip_path = Path(args.itemtips_tip)

    payload = load_json(work_file_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))

    itemtips_name_map = load_itemtips_name_map(itemtips_tip_path)

    updated_count = 0
    localized_count = 0
    cleared_count = 0
    unchanged_count = 0

    for entry in entries:
        if not isinstance(entry, dict):
            continue

        english_display_name = str(entry.get("englishDisplayName", "")).strip()
        current_display_name = str(entry.get("chineseDisplayName", "")).strip()

        next_display_name = MANUAL_CHINESE_DISPLAY_NAME_OVERRIDES.get(english_display_name, "").strip()
        if not next_display_name:
            for candidate in build_lookup_candidates(entry):
                candidate_name = itemtips_name_map.get(candidate, "").strip()
                if candidate_name:
                    next_display_name = candidate_name
                    break

        if current_display_name == next_display_name:
            unchanged_count += 1
        else:
            entry["chineseDisplayName"] = next_display_name
            updated_count += 1
            if next_display_name:
                localized_count += 1
            elif current_display_name:
                cleared_count += 1

        translation_status = str(entry.get("translationStatus", "")).strip().lower()
        chinese_notes = str(entry.get("chineseNotes", "")).strip()
        if not next_display_name:
            if translation_status in {"approved", "reviewed"}:
                entry["translationStatus"] = "draft" if chinese_notes else "pending"
        elif translation_status == "pending" and chinese_notes:
            entry["translationStatus"] = "draft"

    payload["sourceNamesFile"] = str(itemtips_tip_path)
    payload["gameLanguageCode"] = "zh-CN"
    write_json(work_file_path, payload)

    print(
        "Synchronized chineseDisplayName from itemtips-cn for {0} entries. Localized: {1}. Cleared: {2}. Unchanged: {3}.".format(
            updated_count,
            localized_count,
            cleared_count,
            unchanged_count,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
