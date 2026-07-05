from __future__ import annotations

import argparse
from pathlib import Path

from translation_workflow import (
    index_entries_by_pickup_id,
    is_entry_complete,
    load_json,
    normalize_status,
    summarize_entries,
    utc_now_text,
    validate_status,
    write_json,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.zh-CN.work.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Apply translated pickup wiki tip batch results into the Simplified Chinese work file."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to RandomLoadout.pickup-wiki-tips.zh-CN.work.json.",
    )
    parser.add_argument(
        "--input",
        required=True,
        help="Path to the translated batch JSON.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)
    input_path = Path(args.input)

    work_payload = load_json(work_file_path)
    work_entries = work_payload.get("entries", [])
    batch_payload = load_json(input_path)
    batch_entries = batch_payload.get("entries", [])
    if not isinstance(work_entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))
    if not isinstance(batch_entries, list):
        raise ValueError("Batch file did not contain an 'entries' array: {0}".format(input_path))

    work_entries_by_pickup_id = index_entries_by_pickup_id(work_entries)
    changed_count = 0
    applied_pickup_ids: list[int] = []
    now_text = utc_now_text()

    for batch_entry in batch_entries:
        if not isinstance(batch_entry, dict):
            continue

        pickup_id = batch_entry.get("pickupId")
        if not isinstance(pickup_id, int) or pickup_id <= 0:
            continue

        work_entry = work_entries_by_pickup_id.get(pickup_id)
        if work_entry is None:
            raise ValueError("Batch entry pickupId {0} was not found in {1}".format(pickup_id, work_file_path))

        batch_source_hash = str(batch_entry.get("sourceHash", "")).strip()
        work_source_hash = str(work_entry.get("sourceHash", "")).strip()
        if batch_source_hash and work_source_hash and batch_source_hash != work_source_hash:
            raise ValueError("Batch entry pickupId {0} has stale sourceHash.".format(pickup_id))

        new_chinese_display_name = str(batch_entry.get("chineseDisplayName", "")).strip()
        new_chinese_notes = str(batch_entry.get("chineseNotes", "")).strip()
        requested_status = str(batch_entry.get("translationStatus", "")).strip().lower()

        changed = False
        if new_chinese_display_name and new_chinese_display_name != str(work_entry.get("chineseDisplayName", "")).strip():
            work_entry["chineseDisplayName"] = new_chinese_display_name
            changed = True

        if new_chinese_notes and new_chinese_notes != str(work_entry.get("chineseNotes", "")).strip():
            work_entry["chineseNotes"] = new_chinese_notes
            changed = True

        entry_complete = is_entry_complete(work_entry)
        source_changed = str(work_entry.get("translationStatus", "")).strip().lower() == "stale"
        normalized_status = normalize_status(requested_status if validate_status(requested_status) else requested_status, source_changed, entry_complete)
        if normalized_status != str(work_entry.get("translationStatus", "")).strip().lower():
            work_entry["translationStatus"] = normalized_status
            changed = True

        if changed:
            work_entry["updatedUtc"] = now_text
            changed_count += 1
            applied_pickup_ids.append(pickup_id)

    summary = summarize_entries(work_entries)
    work_payload["generatedUtc"] = utc_now_text()
    work_payload["entryCount"] = summary["entryCount"]
    work_payload["translatedCount"] = summary["translatedCount"]
    work_payload["staleCount"] = summary["staleCount"]
    work_payload["approvedCount"] = summary["approvedCount"]
    work_payload["pendingCount"] = summary["pendingCount"]
    write_json(work_file_path, work_payload)

    print(
        "Applied {0} translated entrie(s) to {1}. PickupIds: {2}".format(
            changed_count,
            work_file_path,
            ", ".join(str(pickup_id) for pickup_id in applied_pickup_ids) if applied_pickup_ids else "<none>",
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
