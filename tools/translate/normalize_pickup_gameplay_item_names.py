from __future__ import annotations

import argparse
from pathlib import Path

from gameplay_translation_workflow import (
    DEFAULT_WORK_FILE_PATH,
    TRANSLATABLE_FIELD_PAIRS,
    build_name_pairs,
    localize_known_pickup_names_in_text,
    load_json,
    write_json,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Replace confirmed in-game pickup English names in translated gameplay text with canonical Chinese display names."
    )
    parser.add_argument(
        "--input",
        required=True,
        help="Path to a gameplay translation batch JSON or the zh-CN gameplay work JSON.",
    )
    parser.add_argument(
        "--name-source",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json used as the English-to-Chinese pickup name source.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    name_source_path = Path(args.name_source)

    payload = load_json(input_path)
    entries = payload.get("entries", [])
    name_source_payload = load_json(name_source_path)
    name_source_entries = name_source_payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Input file did not contain an 'entries' array: {0}".format(input_path))
    if not isinstance(name_source_entries, list):
        raise ValueError("Name source file did not contain an 'entries' array: {0}".format(name_source_path))

    name_pairs = build_name_pairs(name_source_entries)
    changed_entries = 0
    changed_fields = 0
    chinese_field_keys = [chinese_key for _, chinese_key in TRANSLATABLE_FIELD_PAIRS]

    for entry in entries:
        if not isinstance(entry, dict):
            continue

        entry_changed = False
        current_english_name = str(entry.get("englishDisplayName", "")).strip()
        for chinese_key in chinese_field_keys:
            original_text = str(entry.get(chinese_key, ""))
            updated_text = localize_known_pickup_names_in_text(
                original_text,
                name_pairs,
                current_english_name=current_english_name,
                is_synergy_field=(chinese_key == "chineseSynergyHighlights"),
            )
            if updated_text != original_text:
                entry[chinese_key] = updated_text
                changed_fields += 1
                entry_changed = True

        if entry_changed:
            changed_entries += 1

    write_json(input_path, payload)
    print(
        "Normalized confirmed in-game pickup names in {0}. Changed entries: {1}. Changed fields: {2}.".format(
            input_path,
            changed_entries,
            changed_fields,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
