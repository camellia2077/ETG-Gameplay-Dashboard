from __future__ import annotations

import argparse
from pathlib import Path

from gameplay_translation_workflow import DEFAULT_WORK_FILE_PATH, REPO_ROOT, load_json, write_json


DEFAULT_INPUT_PATH = DEFAULT_WORK_FILE_PATH
DEFAULT_ENGLISH_SOURCE_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-gameplay.en.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Restore stats[*].value in the zh-CN gameplay work file from the English source file while leaving valueMappings intact."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to RandomLoadout.pickup-gameplay.zh-CN.work.json.",
    )
    parser.add_argument(
        "--english-source",
        default=str(DEFAULT_ENGLISH_SOURCE_PATH),
        help="Path to RandomLoadout.pickup-gameplay.en.json.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional output path. Defaults to overwriting the input file.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    english_source_path = Path(args.english_source)
    output_path = Path(args.output) if args.output.strip() else input_path

    zh_payload = load_json(input_path)
    en_payload = load_json(english_source_path)

    zh_entries = zh_payload.get("entries", [])
    en_entries = en_payload.get("entries", [])
    if not isinstance(zh_entries, list):
        raise ValueError("Input file did not contain an 'entries' array: {0}".format(input_path))
    if not isinstance(en_entries, list):
        raise ValueError("English source file did not contain an 'entries' array: {0}".format(english_source_path))

    english_by_pickup_id = {
        entry.get("pickupId"): entry
        for entry in en_entries
        if isinstance(entry, dict) and isinstance(entry.get("pickupId"), int)
    }

    changed_entries: set[int] = set()
    changed_stats = 0

    for zh_entry in zh_entries:
        if not isinstance(zh_entry, dict):
            continue
        pickup_id = zh_entry.get("pickupId")
        if not isinstance(pickup_id, int):
            continue
        en_entry = english_by_pickup_id.get(pickup_id)
        if not isinstance(en_entry, dict):
            continue

        zh_stat_groups = zh_entry.get("statGroups", [])
        en_stat_groups = en_entry.get("statGroups", [])
        if not isinstance(zh_stat_groups, list) or not isinstance(en_stat_groups, list):
            continue

        for zh_group, en_group in zip(zh_stat_groups, en_stat_groups):
            if not isinstance(zh_group, dict) or not isinstance(en_group, dict):
                continue
            zh_stats = zh_group.get("stats", [])
            en_stats = en_group.get("stats", [])
            if not isinstance(zh_stats, list) or not isinstance(en_stats, list):
                continue

            for zh_stat, en_stat in zip(zh_stats, en_stats):
                if not isinstance(zh_stat, dict) or not isinstance(en_stat, dict):
                    continue
                english_value = str(en_stat.get("value", ""))
                if not english_value:
                    continue
                if str(zh_stat.get("value", "")) == english_value:
                    continue
                zh_stat["value"] = english_value
                changed_stats += 1
                changed_entries.add(pickup_id)

    write_json(output_path, zh_payload)
    print(
        "Restored stats[*].value from English source in {0}. Changed entries: {1}. Changed stats: {2}.".format(
            output_path,
            len(changed_entries),
            changed_stats,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
