from __future__ import annotations

import argparse
import hashlib
import json
from collections import OrderedDict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ENGLISH_GAMEPLAY_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "RandomLoadout.pickup-gameplay.en.json"
DEFAULT_OUTPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "RandomLoadout.pickup-gameplay.zh-CN.work.json"
DEFAULT_LEGACY_ZH_TIPS_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.zh-CN.work.json"

DEFAULT_SECTION_LABELS = OrderedDict(
    [
        ("quality", "品质："),
        ("type", "类型："),
        ("summary", "摘要："),
        ("effects", "效果："),
        ("synergies", "协同："),
        ("notes", "备注："),
    ]
)

DEFAULT_STAT_LABELS = OrderedDict(
    [
        ("quality", "品质"),
        ("type", "类型"),
        ("class", "类别"),
        ("dps", "DPS"),
        ("damage", "伤害"),
        ("magazine", "弹匣"),
        ("ammo", "备弹"),
        ("reload", "换弹"),
        ("fire_rate", "射速"),
        ("shot_speed", "弹速"),
        ("range", "射程"),
        ("force", "击退"),
        ("spread", "散布"),
        ("duration", "持续"),
        ("recharge", "充能"),
        ("sell", "售价"),
    ]
)

DEFAULT_VALUE_MAPPINGS = OrderedDict(
    [
        ("A", "A级"),
        ("B", "B级"),
        ("C", "C级"),
        ("D", "D级"),
        ("S", "S级"),
        ("N", "N级"),
        ("Not listed", "未列出"),
        ("C/B/A (multi-tier)", "C/B/A级（多档）"),
        ("D/N (multi-tier)", "D/N级（多档）"),
        ("D/S (multi-tier)", "D/S级（多档）"),
        ("B/A (multi-tier)", "B/A级（多档）"),
        ("CBA", "C/B/A级"),
        ("Common", "普通"),
        ("Special", "特殊"),
        ("Passive", "被动"),
        ("Active", "主动"),
        ("Semiautomatic", "半自动"),
        ("Semiautomatic (functionally Automatic)", "半自动（机制上近似全自动）"),
        ("Semiautomatic Charged", "半自动蓄力"),
        ("Automatic", "全自动"),
        ("Burst", "点射"),
        ("Charged", "蓄力"),
        ("Beam", "光束"),
        ("Explosive", "爆炸"),
        ("Thrown", "投掷"),
        ("Melee", "近战"),
        ("Poison", "毒"),
        ("Ice", "冰"),
        ("Fire", "火"),
        ("Charm", "魅惑"),
        ("None", "无"),
        ("SHITTY", "破烂"),
        ("PISTOL", "手枪"),
        ("FULLAUTO", "全自动"),
        ("SHOTGUN", "霰弹枪"),
        ("RIFLE", "步枪"),
        ("BEAM", "光束"),
        ("CHARGE", "蓄力"),
        ("EXPLOSIVE", "爆炸"),
        ("FIRE", "火焰"),
        ("ICE", "寒冰"),
        ("POISON", "毒性"),
        ("SILLY", "特殊"),
        ("CHARM", "魅惑"),
        ("NONE", "无"),
    ]
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Initialize or refresh the Simplified Chinese pickup gameplay work file."
    )
    parser.add_argument(
        "--english-gameplay",
        default=str(DEFAULT_ENGLISH_GAMEPLAY_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.en.json.",
    )
    parser.add_argument(
        "--legacy-zh-tips",
        default=str(DEFAULT_LEGACY_ZH_TIPS_PATH),
        help="Optional path to RandomLoadout.pickup-wiki-tips.zh-CN.work.json for Chinese display-name migration.",
    )
    parser.add_argument(
        "--output",
        default=str(DEFAULT_OUTPUT_PATH),
        help="Output path for the zh-CN gameplay work file.",
    )
    return parser.parse_args()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: OrderedDict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def utc_now_text() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")


def load_entries_by_pickup_id(path: Path) -> dict[int, dict[str, Any]]:
    if not path.is_file():
        return {}

    payload = load_json(path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        return {}

    results: dict[int, dict[str, Any]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        pickup_id = parse_pickup_id(entry.get("pickupId"))
        if pickup_id >= 0 and pickup_id not in results:
            results[pickup_id] = entry
    return results


def parse_pickup_id(value: Any) -> int:
    try:
        return int(value)
    except Exception:
        return -1


def choose_existing_dict(existing_payload: dict[str, Any], key: str, default_value: OrderedDict[str, str]) -> OrderedDict[str, str]:
    value = existing_payload.get(key)
    if not isinstance(value, dict):
        return OrderedDict(default_value)

    results = OrderedDict()
    for default_key, default_text in default_value.items():
        existing_text = str(value.get(default_key, "")).strip()
        results[default_key] = existing_text if existing_text else default_text

    for existing_key, existing_text in value.items():
        normalized_key = str(existing_key).strip()
        if normalized_key and normalized_key not in results:
            results[normalized_key] = str(existing_text or "").strip()

    return results


def compute_source_hash(entry: dict[str, Any]) -> str:
    hash_source = {
        "englishDisplayName": entry.get("englishDisplayName", ""),
        "quality": entry.get("quality", ""),
        "pickupType": entry.get("pickupType", ""),
        "statGroups": entry.get("statGroups", []),
        "unlock": entry.get("unlock", ""),
        "englishGameplaySummary": entry.get("englishGameplaySummary", ""),
        "englishEffectHighlights": entry.get("englishEffectHighlights", ""),
        "englishSynergyHighlights": entry.get("englishSynergyHighlights", ""),
        "englishUsageNotes": entry.get("englishUsageNotes", ""),
    }
    serialized = json.dumps(hash_source, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return "sha1:" + hashlib.sha1(serialized.encode("utf-8")).hexdigest()


def choose_chinese_display_name(existing_entry: dict[str, Any], legacy_entry: dict[str, Any]) -> tuple[str, bool]:
    existing_name = str(existing_entry.get("chineseDisplayName", "") or "").strip()
    if existing_name:
        return existing_name, False

    legacy_name = str(legacy_entry.get("chineseDisplayName", "") or "").strip()
    return legacy_name, bool(legacy_name)


def choose_translation_status(existing_entry: dict[str, Any], has_any_chinese: bool, source_changed: bool) -> str:
    existing_status = str(existing_entry.get("translationStatus", "") or "").strip().lower()

    if existing_status == "approved":
        return "approved"
    if existing_status == "reviewed":
        return "stale" if source_changed else "reviewed"
    if existing_status == "draft":
        return "stale" if source_changed and has_any_chinese else ("draft" if has_any_chinese else "pending")
    if existing_status == "stale":
        return "stale" if has_any_chinese else "pending"

    if has_any_chinese:
        return "stale" if source_changed else "draft"
    return "pending"


def build_work_entry(
    english_entry: dict[str, Any],
    existing_entry: dict[str, Any] | None,
    legacy_entry: dict[str, Any] | None,
) -> tuple[dict[str, Any], bool]:
    existing_entry = existing_entry or {}
    legacy_entry = legacy_entry or {}
    source_hash = compute_source_hash(english_entry)
    previous_source_hash = str(existing_entry.get("sourceHash", "") or "").strip()
    source_changed = bool(previous_source_hash) and previous_source_hash != source_hash

    chinese_display_name, migrated_name = choose_chinese_display_name(existing_entry, legacy_entry)
    chinese_gameplay_summary = str(existing_entry.get("chineseGameplaySummary", "") or "").strip()
    chinese_effect_highlights = str(existing_entry.get("chineseEffectHighlights", "") or "").strip()
    chinese_synergy_highlights = str(existing_entry.get("chineseSynergyHighlights", "") or "").strip()
    chinese_usage_notes = str(existing_entry.get("chineseUsageNotes", "") or "").strip()

    has_any_chinese = any(
        (
            chinese_display_name,
            chinese_gameplay_summary,
            chinese_effect_highlights,
            chinese_synergy_highlights,
            chinese_usage_notes,
        )
    )

    translation_status = choose_translation_status(existing_entry, has_any_chinese, source_changed)

    return (
        {
            "pickupId": int(english_entry["pickupId"]),
            "category": str(english_entry.get("category", "") or "").strip(),
            "wikiKey": str(english_entry.get("wikiKey", "") or "").strip(),
            "internalName": str(english_entry.get("internalName", "") or "").strip(),
            "englishDisplayName": str(english_entry.get("englishDisplayName", "") or "").strip(),
            "chineseDisplayName": chinese_display_name,
            "quality": str(english_entry.get("quality", "") or "").strip(),
            "pickupType": str(english_entry.get("pickupType", "") or "").strip(),
            "statGroups": english_entry.get("statGroups", []),
            "unlock": str(english_entry.get("unlock", "") or "").strip(),
            "englishGameplaySummary": str(english_entry.get("englishGameplaySummary", "") or "").strip(),
            "chineseGameplaySummary": chinese_gameplay_summary,
            "englishEffectHighlights": str(english_entry.get("englishEffectHighlights", "") or "").strip(),
            "chineseEffectHighlights": chinese_effect_highlights,
            "englishSynergyHighlights": str(english_entry.get("englishSynergyHighlights", "") or "").strip(),
            "chineseSynergyHighlights": chinese_synergy_highlights,
            "englishUsageNotes": str(english_entry.get("englishUsageNotes", "") or "").strip(),
            "chineseUsageNotes": chinese_usage_notes,
            "translationStatus": translation_status,
            "sourceHash": source_hash,
            "updatedUtc": utc_now_text(),
        },
        migrated_name,
    )


def summarize_entries(entries: list[dict[str, Any]]) -> dict[str, int]:
    translated_count = 0
    stale_count = 0
    approved_count = 0
    pending_count = 0
    for entry in entries:
        status = str(entry.get("translationStatus", "")).strip().lower()
        if status == "stale":
            stale_count += 1
        if status == "approved":
            approved_count += 1
        if status == "pending":
            pending_count += 1
        if status in {"draft", "reviewed", "approved"}:
            translated_count += 1

    return {
        "entryCount": len(entries),
        "translatedCount": translated_count,
        "staleCount": stale_count,
        "approvedCount": approved_count,
        "pendingCount": pending_count,
    }


def to_repo_relative_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def build_output_payload(
    english_path: Path,
    legacy_path: Path,
    existing_payload: dict[str, Any],
    entries: list[dict[str, Any]],
    migrated_name_count: int,
) -> OrderedDict[str, Any]:
    sorted_entries = sorted(entries, key=lambda entry: parse_pickup_id(entry.get("pickupId")))
    summary = summarize_entries(sorted_entries)
    payload = OrderedDict()
    payload["generatedUtc"] = utc_now_text()
    payload["sourceEnglishGameplayFile"] = to_repo_relative_path(english_path)
    payload["sourceLegacyChineseTipsFile"] = to_repo_relative_path(legacy_path) if legacy_path.is_file() else ""
    payload["gameLanguageCode"] = "zh-CN"
    payload["entryCount"] = summary["entryCount"]
    payload["translatedCount"] = summary["translatedCount"]
    payload["staleCount"] = summary["staleCount"]
    payload["approvedCount"] = summary["approvedCount"]
    payload["pendingCount"] = summary["pendingCount"]
    payload["migratedChineseNameCount"] = migrated_name_count
    payload["sectionLabels"] = choose_existing_dict(existing_payload, "sectionLabels", DEFAULT_SECTION_LABELS)
    payload["statLabels"] = choose_existing_dict(existing_payload, "statLabels", DEFAULT_STAT_LABELS)
    payload["valueMappings"] = choose_existing_dict(existing_payload, "valueMappings", DEFAULT_VALUE_MAPPINGS)
    payload["entries"] = sorted_entries
    return payload


def main() -> int:
    args = parse_args()
    english_gameplay_path = Path(args.english_gameplay)
    legacy_zh_tips_path = Path(args.legacy_zh_tips)
    output_path = Path(args.output)

    english_payload = load_json(english_gameplay_path)
    english_entries = english_payload.get("entries", [])
    if not isinstance(english_entries, list):
        raise ValueError("English gameplay file did not contain an 'entries' array: {0}".format(english_gameplay_path))

    existing_payload = load_json(output_path) if output_path.is_file() else {}
    existing_by_pickup_id = load_entries_by_pickup_id(output_path)
    legacy_by_pickup_id = load_entries_by_pickup_id(legacy_zh_tips_path)

    entries: list[dict[str, Any]] = []
    migrated_name_count = 0
    for english_entry in english_entries:
        if not isinstance(english_entry, dict):
            continue

        pickup_id = parse_pickup_id(english_entry.get("pickupId"))
        if pickup_id < 0:
            continue

        work_entry, migrated_name = build_work_entry(
            english_entry,
            existing_by_pickup_id.get(pickup_id),
            legacy_by_pickup_id.get(pickup_id),
        )
        entries.append(work_entry)
        if migrated_name:
            migrated_name_count += 1

    payload = build_output_payload(
        english_gameplay_path,
        legacy_zh_tips_path,
        existing_payload,
        entries,
        migrated_name_count,
    )
    write_json(output_path, payload)

    print(
        "Initialized zh-CN gameplay work file with {0} entries. Translated: {1}. Approved: {2}. Stale: {3}. Output: {4}".format(
            payload["entryCount"],
            payload["translatedCount"],
            payload["approvedCount"],
            payload["staleCount"],
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
