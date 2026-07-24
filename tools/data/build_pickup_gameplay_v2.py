from __future__ import annotations

import argparse
import json
from collections import OrderedDict
from pathlib import Path
from typing import Any



REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ENGLISH_SOURCE_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "EtgGameplayDashboard.pickup-gameplay.en.json"
DEFAULT_CHINESE_SOURCE_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json"
DEFAULT_GAMEPLAY_OUTPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "EtgGameplayDashboard.pickup-gameplay.json"
DEFAULT_TERMS_OUTPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "EtgGameplayDashboard.pickup-info-terms.json"

SECTION_LABELS_EN = OrderedDict(
    [
        ("quality", "Quality:"),
        ("type", "Type:"),
        ("summary", "Summary:"),
        ("effects", "Effects:"),
        ("synergies", "Synergies:"),
        ("notes", "Notes:"),
    ]
)

STAT_LABELS_EN = OrderedDict(
    [
        ("quality", "Q"),
        ("type", "Type"),
        ("class", "Class"),
        ("dps", "DPS"),
        ("damage", "DMG"),
        ("magazine", "Mag"),
        ("ammo", "Ammo"),
        ("reload", "Reload"),
        ("fire_rate", "Fire"),
        ("shot_speed", "Speed"),
        ("range", "Range"),
        ("force", "Force"),
        ("spread", "Spread"),
        ("duration", "Duration"),
        ("recharge", "Recharge"),
        ("sell", "Sell"),
    ]
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build pickup gameplay schema v3 runtime files from the legacy English and zh-CN gameplay sources."
    )
    parser.add_argument("--english-source", default=str(DEFAULT_ENGLISH_SOURCE_PATH), help="Path to the legacy English gameplay source JSON.")
    parser.add_argument("--chinese-source", default=str(DEFAULT_CHINESE_SOURCE_PATH), help="Path to the legacy zh-CN gameplay source JSON.")
    parser.add_argument("--gameplay-output", default=str(DEFAULT_GAMEPLAY_OUTPUT_PATH), help="Output path for EtgGameplayDashboard.pickup-gameplay.json.")
    parser.add_argument("--terms-output", default=str(DEFAULT_TERMS_OUTPUT_PATH), help="Output path for EtgGameplayDashboard.pickup-info-terms.json.")
    return parser.parse_args()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def to_repo_relative_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def split_lines(value: Any) -> list[str]:
    text = str(value or "").strip()
    if not text:
        return []
    return [segment.strip() for segment in text.split(";") if segment.strip()]


def build_pickup_entry(english_entry: dict[str, Any], chinese_entry: dict[str, Any] | None) -> OrderedDict[str, Any]:
    pickup_id = int(english_entry.get("pickupId", -1))
    names = OrderedDict(
        [
            ("en", str(english_entry.get("englishDisplayName", "") or "").strip()),
            ("zh-CN", str((chinese_entry or {}).get("chineseDisplayName", "") or "").strip()),
        ]
    )

    stat_sections: list[OrderedDict[str, Any]] = []
    for section in english_entry.get("statGroups", []):
        if not isinstance(section, dict):
            continue
        stats: list[OrderedDict[str, Any]] = []
        for stat in section.get("stats", []):
            if not isinstance(stat, dict):
                continue
            key = str(stat.get("labelKey", "") or "").strip()
            parts = stat.get("parts", [])
            if not key or not isinstance(parts, list) or not parts:
                continue
            stats.append(OrderedDict([("key", key), ("parts", parts)]))
        if not stats:
            continue
        stat_sections.append(
            OrderedDict(
                [
                    ("key", str(section.get("groupKey", "") or "").strip()),
                    ("stats", stats),
                ]
            )
        )

    text_block = OrderedDict(
        [
            (
                "summary",
                OrderedDict(
                    [
                        ("en", str(english_entry.get("englishGameplaySummary", "") or "").strip()),
                        ("zh-CN", str((chinese_entry or {}).get("chineseGameplaySummary", "") or "").strip()),
                    ]
                ),
            ),
            (
                "effects",
                OrderedDict(
                    [
                        ("en", split_lines(english_entry.get("englishEffectHighlights", ""))),
                        ("zh-CN", split_lines((chinese_entry or {}).get("chineseEffectHighlights", ""))),
                    ]
                ),
            ),
            (
                "synergies",
                OrderedDict(
                    [
                        ("en", split_lines(english_entry.get("englishSynergyHighlights", ""))),
                        ("zh-CN", split_lines((chinese_entry or {}).get("chineseSynergyHighlights", ""))),
                    ]
                ),
            ),
            (
                "notes",
                OrderedDict(
                    [
                        ("en", split_lines(english_entry.get("englishUsageNotes", ""))),
                        ("zh-CN", split_lines((chinese_entry or {}).get("chineseUsageNotes", ""))),
                    ]
                ),
            ),
        ]
    )

    return OrderedDict(
        [
            ("id", pickup_id),
            ("category", str(english_entry.get("category", "") or "").strip().lower()),
            ("names", names),
            ("wikiKey", str(english_entry.get("wikiKey", "") or "").strip()),
            ("quality", str(english_entry.get("quality", "") or "").strip()),
            ("type", str(english_entry.get("pickupType", "") or "").strip()),
            ("statSections", stat_sections),
            ("text", text_block),
        ]
    )


def build_terms_payload(chinese_payload: dict[str, Any]) -> OrderedDict[str, Any]:
    section_labels_zh = chinese_payload.get("sectionLabels", {}) if isinstance(chinese_payload.get("sectionLabels"), dict) else {}
    stat_labels_zh = chinese_payload.get("statLabels", {}) if isinstance(chinese_payload.get("statLabels"), dict) else {}
    display_values_zh = chinese_payload.get("valueMappings", {}) if isinstance(chinese_payload.get("valueMappings"), dict) else {}

    sections = OrderedDict()
    for key, english_value in SECTION_LABELS_EN.items():
        sections[key] = OrderedDict(
            [
                ("en", english_value),
                ("zh-CN", str(section_labels_zh.get(key, "") or "").strip()),
            ]
        )

    stats = OrderedDict()
    for key, english_value in STAT_LABELS_EN.items():
        stats[key] = OrderedDict(
            [
                ("en", english_value),
                ("zh-CN", str(stat_labels_zh.get(key, "") or "").strip()),
            ]
        )

    display_values = OrderedDict()
    for key in sorted(display_values_zh.keys(), key=str.casefold):
        display_values[str(key)] = OrderedDict(
            [
                ("en", str(key)),
                ("zh-CN", str(display_values_zh.get(key, "") or "").strip()),
            ]
        )

    return OrderedDict(
        [
            ("schemaVersion", 2),
            ("generatedUtc", str(chinese_payload.get("generatedUtc", "") or "").strip()),
            ("languages", ["en", "zh-CN"]),
            ("sections", sections),
            ("stats", stats),
            ("displayValues", display_values),
        ]
    )


def main() -> int:
    args = parse_args()
    english_path = Path(args.english_source)
    chinese_path = Path(args.chinese_source)
    gameplay_output_path = Path(args.gameplay_output)
    terms_output_path = Path(args.terms_output)

    english_payload = load_json(english_path)
    chinese_payload = load_json(chinese_path)

    english_entries = english_payload.get("entries", [])
    chinese_entries = chinese_payload.get("entries", [])
    if not isinstance(english_entries, list):
        raise ValueError("Legacy English source did not contain an 'entries' array: {0}".format(english_path))
    if not isinstance(chinese_entries, list):
        raise ValueError("Legacy zh-CN source did not contain an 'entries' array: {0}".format(chinese_path))

    chinese_entries_by_pickup_id: dict[int, dict[str, Any]] = {}
    for entry in chinese_entries:
        if not isinstance(entry, dict):
            continue
        pickup_id = entry.get("pickupId")
        if isinstance(pickup_id, int) and pickup_id >= 0 and pickup_id not in chinese_entries_by_pickup_id:
            chinese_entries_by_pickup_id[pickup_id] = entry

    pickups = OrderedDict()
    for english_entry in english_entries:
        if not isinstance(english_entry, dict):
            continue
        pickup_id = english_entry.get("pickupId")
        if not isinstance(pickup_id, int) or pickup_id < 0:
            continue
        pickups[str(pickup_id)] = build_pickup_entry(english_entry, chinese_entries_by_pickup_id.get(pickup_id))

    gameplay_payload = OrderedDict(
        [
            ("schemaVersion", 3),
            ("generatedUtc", str(english_payload.get("generatedUtc", "") or "").strip()),
            (
                "sourceLanguageFiles",
                OrderedDict([("en", to_repo_relative_path(english_path)), ("zh-CN", to_repo_relative_path(chinese_path))]),
            ),
            ("pickupCount", len(pickups)),
            ("languages", ["en", "zh-CN"]),
            ("pickups", pickups),
        ]
    )

    write_json(gameplay_output_path, gameplay_payload)
    write_json(terms_output_path, build_terms_payload(chinese_payload))

    print("Wrote schema v3 gameplay data to {0}".format(gameplay_output_path))
    print("Wrote schema v2 pickup info terms to {0}".format(terms_output_path))
    print("Pickup count: {0}".format(len(pickups)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
