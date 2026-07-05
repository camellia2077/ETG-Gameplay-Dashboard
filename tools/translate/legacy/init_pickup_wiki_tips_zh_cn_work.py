from __future__ import annotations

import argparse
from pathlib import Path

from translation_workflow import (
    build_name_map,
    build_work_entry,
    load_existing_entries,
    load_json,
    summarize_entries,
    utc_now_text,
    write_json,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ENGLISH_TIPS_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.en.json"
DEFAULT_OUTPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.zh-CN.work.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Initialize or refresh the Simplified Chinese pickup wiki tip work file."
    )
    parser.add_argument(
        "--english-tips",
        default=str(DEFAULT_ENGLISH_TIPS_PATH),
        help="Path to RandomLoadout.pickup-wiki-tips.en.json.",
    )
    parser.add_argument(
        "--game-language-names",
        default="",
        help="Optional path to RandomLoadout.pickup-names.game-language.json exported from a Chinese ETG runtime.",
    )
    parser.add_argument(
        "--output",
        default=str(DEFAULT_OUTPUT_PATH),
        help="Output path for the zh-CN work file.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    english_tips_path = Path(args.english_tips)
    names_path = Path(args.game_language_names) if args.game_language_names else None
    output_path = Path(args.output)

    english_payload = load_json(english_tips_path)
    english_tips = english_payload.get("tips", [])
    if not isinstance(english_tips, list):
        raise ValueError("English tips file did not contain a 'tips' array: {0}".format(english_tips_path))

    names_by_pickup_id, game_language_code = build_name_map(names_path)
    existing_by_pickup_id = load_existing_entries(output_path)
    entries: list[dict] = []
    for english_tip in english_tips:
        if not isinstance(english_tip, dict):
            continue

        pickup_id = english_tip.get("pickupId")
        if not isinstance(pickup_id, int) or pickup_id <= 0:
            continue

        entries.append(build_work_entry(english_tip, existing_by_pickup_id.get(pickup_id), names_by_pickup_id.get(pickup_id)))

    summary = summarize_entries(entries)
    payload = {
        "generatedUtc": utc_now_text(),
        "sourceEnglishTipsFile": str(english_tips_path),
        "sourceNamesFile": str(names_path) if names_path else "",
        "gameLanguageCode": game_language_code,
        "entryCount": summary["entryCount"],
        "translatedCount": summary["translatedCount"],
        "staleCount": summary["staleCount"],
        "approvedCount": summary["approvedCount"],
        "pendingCount": summary["pendingCount"],
        "entries": entries,
    }
    write_json(output_path, payload)

    print(
        "Initialized zh-CN work file with {0} entries. Translated: {1}. Approved: {2}. Stale: {3}. Output: {4}".format(
            summary["entryCount"],
            summary["translatedCount"],
            summary["approvedCount"],
            summary["staleCount"],
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
