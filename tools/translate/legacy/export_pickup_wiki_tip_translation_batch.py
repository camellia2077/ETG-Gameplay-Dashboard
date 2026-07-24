from __future__ import annotations

import argparse
from pathlib import Path

from translation_workflow import is_entry_complete, load_json, write_json


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json"
DEFAULT_OUTPUT_PATH = REPO_ROOT / "temp" / "pickup-wiki-tip-translation-batch.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Export a translation batch from the Simplified Chinese pickup wiki tip work file."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json.",
    )
    parser.add_argument(
        "--output",
        default=str(DEFAULT_OUTPUT_PATH),
        help="Output JSON path for the translation batch.",
    )
    parser.add_argument(
        "--pickup-id",
        action="append",
        type=int,
        default=[],
        help="Optional pickupId to export. Can be repeated.",
    )
    parser.add_argument(
        "--status",
        action="append",
        default=[],
        help="Only export entries with these translation statuses. Can be repeated.",
    )
    parser.add_argument(
        "--limit",
        type=int,
        default=20,
        help="Maximum number of entries to export.",
    )
    parser.add_argument(
        "--include-complete",
        action="store_true",
        help="Include entries whose Chinese display name and Chinese notes are already both filled.",
    )
    return parser.parse_args()


def should_export_entry(entry: dict, requested_statuses: set[str], requested_pickup_ids: set[int], include_complete: bool) -> bool:
    if requested_pickup_ids and entry.get("pickupId") not in requested_pickup_ids:
        return False

    status = str(entry.get("translationStatus", "")).strip().lower()
    if requested_statuses and status not in requested_statuses:
        return False

    if not include_complete and is_entry_complete(entry):
        return False

    return True


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)
    output_path = Path(args.output)
    requested_statuses = {status.strip().lower() for status in args.status if status.strip()}
    requested_pickup_ids = {pickup_id for pickup_id in args.pickup_id if pickup_id > 0}

    payload = load_json(work_file_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))

    available_pickup_ids = {
        int(entry.get("pickupId"))
        for entry in entries
        if isinstance(entry, dict) and isinstance(entry.get("pickupId"), int) and int(entry.get("pickupId")) > 0
    }

    batch_entries: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        if not should_export_entry(entry, requested_statuses, requested_pickup_ids, args.include_complete):
            continue

        batch_entries.append(
            {
                "pickupId": entry["pickupId"],
                "category": entry.get("category", ""),
                "wikiKey": entry.get("wikiKey", ""),
                "internalName": entry.get("internalName", ""),
                "englishDisplayName": entry.get("englishDisplayName", ""),
                "chineseDisplayName": entry.get("chineseDisplayName", ""),
                "englishNotes": entry.get("englishNotes", ""),
                "chineseNotes": entry.get("chineseNotes", ""),
                "translationStatus": entry.get("translationStatus", ""),
                "sourceHash": entry.get("sourceHash", ""),
            }
        )

        if args.limit > 0 and len(batch_entries) >= args.limit:
            break

    batch_payload = {
        "sourceWorkFile": str(work_file_path),
        "entryCount": len(batch_entries),
        "entries": batch_entries,
    }
    write_json(output_path, batch_payload)
    print("Exported {0} translation entrie(s) to {1}".format(len(batch_entries), output_path))
    if requested_pickup_ids:
        missing_pickup_ids = sorted(requested_pickup_ids - available_pickup_ids)
        if missing_pickup_ids:
            print("Requested pickupIds not found in work file: {0}".format(", ".join(str(value) for value in missing_pickup_ids)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
