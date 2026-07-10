from __future__ import annotations

import argparse
from pathlib import Path

from gameplay_translation_workflow import DEFAULT_WORK_FILE_PATH, load_json, write_json
from scan_pickup_gameplay_stat_value_labels import EXACT_STAT_VALUE_PHRASE_MAPPINGS, KNOWN_STAT_VALUE_LABEL_MAPPINGS


DEFAULT_INPUT_PATH = DEFAULT_WORK_FILE_PATH


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Localize structured English stat-value labels in defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json and sync the same mappings into valueMappings."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json.",
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
    output_path = Path(args.output) if args.output.strip() else input_path

    payload = load_json(input_path)

    value_mappings = payload.get("valueMappings")
    if not isinstance(value_mappings, dict):
        raise ValueError("Input file did not contain a top-level 'valueMappings' object: {0}".format(input_path))

    for english_fragment, chinese_fragment in KNOWN_STAT_VALUE_LABEL_MAPPINGS.items():
        value_mappings[english_fragment] = chinese_fragment
    for english_fragment, chinese_fragment in EXACT_STAT_VALUE_PHRASE_MAPPINGS.items():
        value_mappings[english_fragment] = chinese_fragment

    write_json(output_path, payload)
    print(
        "Synced {1} known stat-value label mapping(s) into valueMappings in {0}. stats[*].value was left unchanged.".format(
            output_path,
            len(KNOWN_STAT_VALUE_LABEL_MAPPINGS) + len(EXACT_STAT_VALUE_PHRASE_MAPPINGS),
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
