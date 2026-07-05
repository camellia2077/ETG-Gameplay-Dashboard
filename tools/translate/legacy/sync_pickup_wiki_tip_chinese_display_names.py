from __future__ import annotations

import argparse
from pathlib import Path

from translation_workflow import build_name_map, load_json, normalize_chinese_display_name, write_json


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.zh-CN.work.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Synchronize chineseDisplayName values in the zh-CN work file from a game-language pickup name export."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to RandomLoadout.pickup-wiki-tips.zh-CN.work.json.",
    )
    parser.add_argument(
        "--game-language-names",
        required=True,
        help="Path to RandomLoadout.pickup-names.game-language.json exported from the game runtime.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)
    names_file_path = Path(args.game_language_names)

    payload = load_json(work_file_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))

    names_by_pickup_id, game_language_code = build_name_map(names_file_path)

    updated_count = 0
    cleared_count = 0
    localized_count = 0
    unchanged_count = 0

    for entry in entries:
        if not isinstance(entry, dict):
            continue

        pickup_id = entry.get("pickupId")
        if not isinstance(pickup_id, int) or pickup_id <= 0:
            continue

        name_entry = names_by_pickup_id.get(pickup_id) or {}
        next_display_name = normalize_chinese_display_name(
            str(name_entry.get("gameDisplayName", "")).strip(),
            str(entry.get("englishDisplayName", "")).strip(),
        )
        current_display_name = str(entry.get("chineseDisplayName", "")).strip()
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

    payload["sourceNamesFile"] = str(names_file_path)
    payload["gameLanguageCode"] = game_language_code
    write_json(work_file_path, payload)

    print(
        "Synchronized chineseDisplayName for {0} entries. Localized: {1}. Cleared: {2}. Unchanged: {3}.".format(
            updated_count,
            localized_count,
            cleared_count,
            unchanged_count,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
