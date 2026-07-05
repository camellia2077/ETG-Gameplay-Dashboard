from __future__ import annotations

import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable


VALID_TRANSLATION_STATUSES = ("pending", "draft", "reviewed", "approved", "stale")
MISSING_DISPLAY_NAME_VALUES = frozenset(("STRING_NOT_FOUND", "ITEMS_STRING_NOT_FOUND"))
CHINESE_CHARACTER_PATTERN = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]")
COMMON_INTERNAL_SUFFIXES = (
    "_synergy",
    "_alt",
    "_tutorial",
    "_island",
    "_cool",
    "_past",
    "_fakeitem",
    "_item",
    "_gun",
)


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def utc_now_text() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")


def build_source_hash(english_display_name: str, english_notes: str, wiki_key: str) -> str:
    digest = hashlib.sha1()
    digest.update((english_display_name or "").encode("utf-8"))
    digest.update(b"\n")
    digest.update((wiki_key or "").encode("utf-8"))
    digest.update(b"\n")
    digest.update((english_notes or "").encode("utf-8"))
    return "sha1:" + digest.hexdigest()


def is_entry_complete(entry: dict) -> bool:
    return bool(str(entry.get("chineseDisplayName", "")).strip()) and bool(str(entry.get("chineseNotes", "")).strip())


def is_entry_finalized(entry: dict) -> bool:
    if not is_entry_complete(entry):
        return False

    status = str(entry.get("translationStatus", "")).strip().lower()
    return status in {"reviewed", "approved"}


def normalize_status(value: str, source_changed: bool, entry_complete: bool) -> str:
    normalized = (value or "").strip().lower()
    if source_changed and (entry_complete or normalized in {"draft", "reviewed", "approved"}):
        return "stale"
    if entry_complete and normalized == "approved":
        return "approved"
    if entry_complete and normalized == "reviewed":
        return "reviewed"
    if entry_complete and normalized == "draft":
        return "draft"
    if entry_complete:
        return "draft"
    if normalized in VALID_TRANSLATION_STATUSES:
        return normalized if normalized not in {"approved", "reviewed"} else "draft"
    return "pending"


def validate_status(value: str) -> bool:
    return (value or "").strip().lower() in VALID_TRANSLATION_STATUSES


def build_name_map(path: Path | None) -> tuple[dict[int, dict], str]:
    if path is None or not path.is_file():
        return {}, ""

    payload = load_json(path)
    game_language_code = str(payload.get("gameLanguageCode", "")).strip()
    pickups = payload.get("pickups", [])
    results: dict[int, dict] = {}
    if not isinstance(pickups, list):
        return results, game_language_code

    for pickup in pickups:
        if not isinstance(pickup, dict):
            continue
        pickup_id = pickup.get("pickupId")
        if isinstance(pickup_id, int) and pickup_id > 0 and pickup_id not in results:
            results[pickup_id] = pickup
    return results, game_language_code


def contains_chinese_characters(value: str) -> bool:
    return bool(CHINESE_CHARACTER_PATTERN.search((value or "").strip()))


def normalize_chinese_display_name(game_display_name: str, english_display_name: str) -> str:
    normalized_game_display_name = (game_display_name or "").strip()
    normalized_english_display_name = (english_display_name or "").strip()
    if not normalized_game_display_name:
        return ""
    if normalized_game_display_name in MISSING_DISPLAY_NAME_VALUES:
        return ""
    if normalized_english_display_name and normalized_game_display_name == normalized_english_display_name:
        return ""
    if not contains_chinese_characters(normalized_game_display_name):
        return ""
    return normalized_game_display_name


def normalize_lookup_key(value: str) -> str:
    return re.sub(r"_+", "_", re.sub(r"[^a-z0-9]+", "_", (value or "").strip().lower())).strip("_")


def strip_internal_suffixes(internal_name: str) -> list[str]:
    values = [internal_name]
    current = internal_name
    while current:
        lowered = current.lower()
        matched_suffix = None
        for suffix in COMMON_INTERNAL_SUFFIXES:
            if lowered.endswith(suffix):
                matched_suffix = suffix
                break
        if not matched_suffix:
            break
        current = current[: -len(matched_suffix)]
        if current:
            values.append(current)
    return values


def load_existing_entries(path: Path) -> dict[int, dict]:
    if not path.is_file():
        return {}

    payload = load_json(path)
    return index_entries_by_pickup_id(payload.get("entries", []))


def index_entries_by_pickup_id(entries: Iterable[dict]) -> dict[int, dict]:
    results: dict[int, dict] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        pickup_id = entry.get("pickupId")
        if isinstance(pickup_id, int) and pickup_id > 0 and pickup_id not in results:
            results[pickup_id] = entry
    return results


def summarize_entries(entries: list[dict]) -> dict[str, int]:
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
        if str(entry.get("chineseNotes", "")).strip():
            translated_count += 1

    return {
        "entryCount": len(entries),
        "translatedCount": translated_count,
        "staleCount": stale_count,
        "approvedCount": approved_count,
        "pendingCount": pending_count,
    }


def build_work_entry(
    english_tip: dict,
    existing_entry: dict | None,
    name_entry: dict | None,
) -> dict:
    existing_entry = existing_entry or {}
    name_entry = name_entry or {}

    pickup_id = int(english_tip["pickupId"])
    english_display_name = str(english_tip.get("englishDisplayName", "")).strip()
    english_notes = str(english_tip.get("englishNotes", "")).strip()
    wiki_key = str(english_tip.get("wikiKey", "")).strip()
    internal_name = str(english_tip.get("internalName", "")).strip()
    category = str(english_tip.get("category", "")).strip()
    source_hash = build_source_hash(english_display_name, english_notes, wiki_key)

    chinese_display_name = str(name_entry.get("gameDisplayName", "")).strip()
    if not chinese_display_name:
        chinese_display_name = str(existing_entry.get("chineseDisplayName", "")).strip()

    chinese_notes = str(existing_entry.get("chineseNotes", "")).strip()
    previous_source_hash = str(existing_entry.get("sourceHash", "")).strip()
    source_changed = bool(previous_source_hash) and previous_source_hash != source_hash
    entry_complete = bool(chinese_display_name) and bool(chinese_notes)
    translation_status = normalize_status(str(existing_entry.get("translationStatus", "")).strip(), source_changed, entry_complete)

    updated_utc = str(existing_entry.get("updatedUtc", "")).strip()
    if source_changed and updated_utc:
        updated_utc = utc_now_text()

    return {
        "pickupId": pickup_id,
        "category": category,
        "wikiKey": wiki_key,
        "internalName": internal_name,
        "englishDisplayName": english_display_name,
        "chineseDisplayName": chinese_display_name,
        "englishNotes": english_notes,
        "chineseNotes": chinese_notes,
        "translationStatus": translation_status,
        "sourceHash": source_hash,
        "updatedUtc": updated_utc,
    }
