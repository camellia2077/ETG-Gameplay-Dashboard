from __future__ import annotations

import argparse
from pathlib import Path

from gameplay_translation_workflow import (
    DEFAULT_WORK_FILE_PATH,
    LOCALIZED_REFERENCE_FIELD_PAIRS,
    TRANSLATABLE_FIELD_PAIRS,
    build_default_batch_output_path,
    build_name_pairs,
    is_entry_complete,
    localize_known_pickup_names_in_text,
    load_json,
    to_repo_relative_path,
    utc_now_text,
    write_json,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Export a pickup gameplay translation batch from the Simplified Chinese gameplay work file."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to defaults/catalog/legacy/EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json.",
    )
    parser.add_argument(
        "--start-id",
        required=False,
        type=int,
        help="Inclusive pickupId lower bound.",
    )
    parser.add_argument(
        "--end-id",
        required=False,
        type=int,
        help="Inclusive pickupId upper bound.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional output JSON path. Defaults to temp/pickup-gameplay-translation-batches/pickup-gameplay.zh-CN.<start>-<end>.json.",
    )
    parser.add_argument(
        "--include-complete",
        action="store_true",
        help="Include entries whose gameplay translation fields are already complete.",
    )
    parser.add_argument(
        "--count",
        type=int,
        help="Limit the exported batch to the first N eligible entries after filtering.",
    )
    parser.add_argument(
        "--only-missing",
        action="store_true",
        help="Only export entries that still have untranslated Chinese fields.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if (args.start_id is None) != (args.end_id is None):
        raise ValueError("--start-id and --end-id must be provided together.")
    if args.start_id is not None and args.start_id < 0:
        raise ValueError("pickupId range must be non-negative.")
    if args.end_id is not None and args.end_id < 0:
        raise ValueError("pickupId range must be non-negative.")
    if args.count is not None and args.count <= 0:
        raise ValueError("--count must be greater than 0.")
    if args.start_id is not None and args.end_id is not None and args.start_id > args.end_id:
        raise ValueError("--start-id must be less than or equal to --end-id.")

    work_file_path = Path(args.work_file)
    if args.output.strip():
        output_path = Path(args.output)
    elif args.start_id is not None and args.end_id is not None:
        output_path = build_default_batch_output_path(args.start_id, args.end_id)
    elif args.count is not None:
        output_path = build_default_batch_output_path(count=args.count, only_missing=args.only_missing)
    else:
        raise ValueError("Either a pickupId range or --count is required.")

    work_payload = load_json(work_file_path)
    work_entries = work_payload.get("entries", [])
    if not isinstance(work_entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))
    name_pairs = build_name_pairs(work_entries)

    batch_entries: list[dict] = []
    for entry in work_entries:
        if not isinstance(entry, dict):
            continue

        pickup_id = entry.get("pickupId")
        if not isinstance(pickup_id, int):
            continue
        if args.start_id is not None and pickup_id < args.start_id:
            continue
        if args.end_id is not None and pickup_id > args.end_id:
            continue
        if args.only_missing and is_entry_complete(entry):
            continue
        if not args.include_complete and not args.only_missing and is_entry_complete(entry):
            continue

        batch_entries.append(
            {
                "pickupId": pickup_id,
                "category": entry.get("category", ""),
                "wikiKey": entry.get("wikiKey", ""),
                "internalName": entry.get("internalName", ""),
                "englishDisplayName": entry.get("englishDisplayName", ""),
                "chineseDisplayName": entry.get("chineseDisplayName", ""),
                "currentPickupEnglishDisplayName": entry.get("englishDisplayName", ""),
                "currentPickupChineseDisplayName": entry.get("chineseDisplayName", ""),
                "translationStatus": entry.get("translationStatus", ""),
                "sourceHash": entry.get("sourceHash", ""),
            }
        )

    if args.count is not None:
        batch_entries = batch_entries[: args.count]

    for exported_entry in batch_entries:
        source_entry = next(
            entry
            for entry in work_entries
            if isinstance(entry, dict) and entry.get("pickupId") == exported_entry["pickupId"]
        )
        for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
            exported_entry[english_key] = source_entry.get(english_key, "")
            exported_entry[chinese_key] = source_entry.get(chinese_key, "")
        for english_key, localized_key in LOCALIZED_REFERENCE_FIELD_PAIRS:
            english_text = str(source_entry.get(english_key, ""))
            exported_entry[localized_key] = localize_known_pickup_names_in_text(
                english_text,
                name_pairs,
                current_english_name=str(source_entry.get("englishDisplayName", "")),
                is_synergy_field=(english_key == "englishSynergyHighlights"),
            )

    if not batch_entries:
        raise ValueError("No eligible entries matched the requested selection.")

    batch_payload = {
        "workflow": "pickup-gameplay-translation-batch",
        "exportedUtc": utc_now_text(),
        "sourceWorkGeneratedUtc": work_payload.get("generatedUtc", ""),
        "sourceWorkFile": to_repo_relative_path(work_file_path),
        "pickupIdRange": {
            "start": min(entry["pickupId"] for entry in batch_entries) if batch_entries else -1,
            "end": max(entry["pickupId"] for entry in batch_entries) if batch_entries else -1,
        },
        "entryCount": len(batch_entries),
        "instructions": {
            "editOnlyFields": [chinese_key for _, chinese_key in TRANSLATABLE_FIELD_PAIRS],
            "translatorReferenceFields": [localized_key for _, localized_key in LOCALIZED_REFERENCE_FIELD_PAIRS],
            "currentPickupNameFields": [
                "currentPickupEnglishDisplayName",
                "currentPickupChineseDisplayName",
            ],
            "replaceOnlyConfirmedPickupNames": True,
            "leaveChineseFieldBlankWhenEnglishBlank": True,
            "preserveSourceHash": True,
        },
        "entries": batch_entries,
    }
    write_json(output_path, batch_payload)
    if args.count is not None:
        print(
            "Exported {0} gameplay translation entrie(s) to {1} using count={2}, only_missing={3}".format(
                len(batch_entries),
                output_path,
                args.count,
                args.only_missing,
            )
        )
    else:
        print("Exported {0} gameplay translation entrie(s) to {1}".format(len(batch_entries), output_path))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
