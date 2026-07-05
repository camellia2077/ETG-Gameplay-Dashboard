from __future__ import annotations

import argparse
from pathlib import Path

from translation_workflow import build_source_hash, load_json, summarize_entries, validate_status


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.zh-CN.work.json"


REQUIRED_ENTRY_KEYS = (
    "pickupId",
    "category",
    "wikiKey",
    "internalName",
    "englishDisplayName",
    "chineseDisplayName",
    "englishNotes",
    "chineseNotes",
    "translationStatus",
    "sourceHash",
    "updatedUtc",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate the Simplified Chinese pickup wiki tip work file."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to RandomLoadout.pickup-wiki-tips.zh-CN.work.json.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)
    payload = load_json(work_file_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))

    seen_pickup_ids: set[int] = set()
    for index, entry in enumerate(entries):
        if not isinstance(entry, dict):
            raise ValueError("Entry #{0} was not an object.".format(index))

        missing_keys = [key for key in REQUIRED_ENTRY_KEYS if key not in entry]
        if missing_keys:
            raise ValueError("Entry #{0} was missing keys: {1}".format(index, ", ".join(missing_keys)))

        pickup_id = entry.get("pickupId")
        if not isinstance(pickup_id, int) or pickup_id <= 0:
            raise ValueError("Entry #{0} has invalid pickupId: {1}".format(index, pickup_id))
        if pickup_id in seen_pickup_ids:
            raise ValueError("Duplicate pickupId found in work file: {0}".format(pickup_id))
        seen_pickup_ids.add(pickup_id)

        translation_status = str(entry.get("translationStatus", "")).strip().lower()
        if not validate_status(translation_status):
            raise ValueError("Entry pickupId {0} has invalid translationStatus: {1}".format(pickup_id, translation_status))

        expected_hash = build_source_hash(
            str(entry.get("englishDisplayName", "")).strip(),
            str(entry.get("englishNotes", "")).strip(),
            str(entry.get("wikiKey", "")).strip(),
        )
        if str(entry.get("sourceHash", "")).strip() != expected_hash:
            raise ValueError("Entry pickupId {0} has mismatched sourceHash.".format(pickup_id))

        chinese_display_name = str(entry.get("chineseDisplayName", "")).strip()
        chinese_notes = str(entry.get("chineseNotes", "")).strip()
        if translation_status in {"approved", "reviewed"} and (not chinese_display_name or not chinese_notes):
            raise ValueError("Entry pickupId {0} is marked {1} but is missing Chinese fields.".format(pickup_id, translation_status))

    summary = summarize_entries(entries)
    print(
        "Validated {0} entries. Translated: {1}. Approved: {2}. Stale: {3}. Pending: {4}.".format(
            summary["entryCount"],
            summary["translatedCount"],
            summary["approvedCount"],
            summary["staleCount"],
            summary["pendingCount"],
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
