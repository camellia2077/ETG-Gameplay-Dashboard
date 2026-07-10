from __future__ import annotations

import argparse
import json
from pathlib import Path

from gameplay_translation_workflow import DEFAULT_WORK_FILE_PATH, REPO_ROOT, load_json


DEFAULT_INPUT_PATH = DEFAULT_WORK_FILE_PATH
DEFAULT_ENGLISH_SOURCE_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "RandomLoadout.pickup-gameplay.en.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan for stats[*].value drift between the zh-CN gameplay work file and the English source file."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json.",
    )
    parser.add_argument(
        "--english-source",
        default=str(DEFAULT_ENGLISH_SOURCE_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.en.json.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/pickup-gameplay.stat-value-source-drift-report.<input-name>.json.",
    )
    return parser.parse_args()


def default_output_path(input_path: Path) -> Path:
    safe_name = input_path.stem.replace(" ", "_")
    return REPO_ROOT / "temp" / "pickup-gameplay.stat-value-source-drift-report.{0}.json".format(safe_name)


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    english_source_path = Path(args.english_source)
    output_path = Path(args.output) if args.output.strip() else default_output_path(input_path)

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

    issues: list[dict] = []
    missing_english_entries: list[int] = []

    for zh_entry in zh_entries:
        if not isinstance(zh_entry, dict):
            continue
        pickup_id = zh_entry.get("pickupId")
        if not isinstance(pickup_id, int):
            continue
        en_entry = english_by_pickup_id.get(pickup_id)
        if not isinstance(en_entry, dict):
            missing_english_entries.append(pickup_id)
            continue

        zh_stat_groups = zh_entry.get("statGroups", [])
        en_stat_groups = en_entry.get("statGroups", [])
        if not isinstance(zh_stat_groups, list) or not isinstance(en_stat_groups, list):
            continue

        for group_index, (zh_group, en_group) in enumerate(zip(zh_stat_groups, en_stat_groups)):
            if not isinstance(zh_group, dict) or not isinstance(en_group, dict):
                continue
            zh_stats = zh_group.get("stats", [])
            en_stats = en_group.get("stats", [])
            if not isinstance(zh_stats, list) or not isinstance(en_stats, list):
                continue

            for stat_index, (zh_stat, en_stat) in enumerate(zip(zh_stats, en_stats)):
                if not isinstance(zh_stat, dict) or not isinstance(en_stat, dict):
                    continue
                zh_value = str(zh_stat.get("value", ""))
                en_value = str(en_stat.get("value", ""))
                if zh_value == en_value:
                    continue

                issues.append(
                    {
                        "pickupId": pickup_id,
                        "englishDisplayName": zh_entry.get("englishDisplayName", ""),
                        "groupKey": zh_group.get("groupKey", ""),
                        "groupIndex": group_index,
                        "labelKey": zh_stat.get("labelKey", ""),
                        "statIndex": stat_index,
                        "zhValue": zh_value,
                        "englishSourceValue": en_value,
                    }
                )

    report = {
        "inputFile": input_path.as_posix(),
        "englishSourceFile": english_source_path.as_posix(),
        "issueCount": len(issues),
        "issuePickupCount": len({issue["pickupId"] for issue in issues}),
        "issues": issues,
        "missingEnglishEntryCount": len(missing_english_entries),
        "missingEnglishEntries": missing_english_entries,
        "notes": [
            "This scanner only compares entries[*].statGroups[*].stats[*].value.",
            "Under the current workflow, zh-CN stats[*].value is expected to stay aligned with the English source file.",
            "If drift is found, use restore-stat-values to restore stats[*].value from the English source file.",
        ],
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(
        "Scanned {0} against {1}. Found {2} stat-value source-drift issue(s). Report: {3}".format(
            input_path,
            english_source_path,
            len(issues),
            output_path,
        )
    )
    return 1 if issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
