from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[3]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "RandomLoadout.pickup-gameplay.zh-CN.work.json"
DEFAULT_TEMP_DIR = REPO_ROOT / "temp" / "pickup-gameplay-translation-batches"
VALID_TRANSLATION_STATUSES = {"pending", "draft", "reviewed", "approved", "stale"}
TRANSLATABLE_FIELD_PAIRS = (
    ("englishGameplaySummary", "chineseGameplaySummary"),
    ("englishEffectHighlights", "chineseEffectHighlights"),
    ("englishSynergyHighlights", "chineseSynergyHighlights"),
    ("englishUsageNotes", "chineseUsageNotes"),
)
LOCALIZED_REFERENCE_FIELD_PAIRS = (
    ("englishGameplaySummary", "localizedEnglishGameplaySummary"),
    ("englishEffectHighlights", "localizedEnglishEffectHighlights"),
    ("englishSynergyHighlights", "localizedEnglishSynergyHighlights"),
    ("englishUsageNotes", "localizedEnglishUsageNotes"),
)
ASCII_WORD_CHARACTER_PATTERN = re.compile(r"[A-Za-z0-9_]")
ENGLISH_SYNERGY_TITLE_SPLIT_PATTERN = re.compile(r"^(.*?)( If the player .*)$")
AMBIGUOUS_ENGLISH_DISPLAY_NAMES = {
    "Box",
    "Shell",
    "Scope",
}


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def utc_now_text() -> str:
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")


def to_repo_relative_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def parse_pickup_id(value: Any) -> int:
    if isinstance(value, bool):
        return -1
    if isinstance(value, int):
        return value
    if isinstance(value, str):
        trimmed = value.strip()
        if trimmed.lstrip("-").isdigit():
            return int(trimmed)
    return -1


def is_entry_complete(entry: dict[str, Any]) -> bool:
    for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
        english_value = str(entry.get(english_key, "")).strip()
        chinese_value = str(entry.get(chinese_key, "")).strip()
        if english_value and not chinese_value:
            return False
    return True


def has_any_translation(entry: dict[str, Any]) -> bool:
    return any(str(entry.get(chinese_key, "")).strip() for _, chinese_key in TRANSLATABLE_FIELD_PAIRS)


def index_entries_by_pickup_id(entries: list[dict[str, Any]]) -> dict[int, dict[str, Any]]:
    results: dict[int, dict[str, Any]] = {}
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        pickup_id = parse_pickup_id(entry.get("pickupId"))
        if pickup_id >= 0 and pickup_id not in results:
            results[pickup_id] = entry
    return results


def normalize_translation_status(requested_status: str, entry: dict[str, Any]) -> str:
    normalized = (requested_status or "").strip().lower()
    entry_complete = is_entry_complete(entry)
    has_translation = has_any_translation(entry)

    if normalized == "stale":
        return "stale"
    if normalized == "approved" and entry_complete:
        return "approved"
    if normalized == "reviewed" and entry_complete:
        return "reviewed"
    if normalized == "draft":
        return "draft"
    if normalized == "pending" and not has_translation:
        return "pending"
    if has_translation:
        return "draft"
    return "pending"


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


def build_default_batch_output_path(
    start_pickup_id: int | None = None,
    end_pickup_id: int | None = None,
    *,
    count: int | None = None,
    only_missing: bool = False,
    checked: bool = False,
) -> Path:
    if start_pickup_id is not None and end_pickup_id is not None:
        suffix = ".check.json" if checked else ".json"
        filename = "pickup-gameplay.zh-CN.{0:04d}-{1:04d}{2}".format(start_pickup_id, end_pickup_id, suffix)
        return DEFAULT_TEMP_DIR / filename
    if count is not None:
        mode = "missing" if only_missing else "all"
        suffix = ".check.json" if checked else ".json"
        filename = "pickup-gameplay.zh-CN.count-{0:04d}.{1}{2}".format(count, mode, suffix)
        return DEFAULT_TEMP_DIR / filename
    raise ValueError("A batch output path needs either a pickupId range or a count.")


def is_ascii_word_character(character: str) -> bool:
    return bool(character) and bool(ASCII_WORD_CHARACTER_PATTERN.match(character))


def replace_name_occurrences(text: str, english_name: str, chinese_name: str) -> str:
    if not text or not english_name or not chinese_name:
        return text

    pattern = re.compile(re.escape(english_name))
    prefix, separator, suffix = chinese_name.partition(english_name)
    result_parts: list[str] = []
    position = 0
    for match in pattern.finditer(text):
        start = match.start()
        end = match.end()
        previous_character = text[start - 1] if start > 0 else ""
        next_character = text[end] if end < len(text) else ""
        if is_ascii_word_character(previous_character) or is_ascii_word_character(next_character):
            continue
        if separator:
            # Some canonical zh-CN pickup names intentionally embed Latin text,
            # e.g. "英格拉姆Mac10式冲锋枪". If the English token is already inside
            # the full canonical Chinese name, do not replace it again.
            prefix_start = start - len(prefix)
            suffix_end = end + len(suffix)
            if prefix_start >= 0 and text[prefix_start:start] == prefix and suffix_end <= len(text) and text[end:suffix_end] == suffix:
                continue
        result_parts.append(text[position:start])
        result_parts.append(chinese_name)
        position = end

    if position == 0:
        return text

    result_parts.append(text[position:])
    return "".join(result_parts)


def cleanup_duplicate_parenthetical_names(text: str, chinese_name: str) -> str:
    if not text or not chinese_name:
        return text
    duplicate_pattern = re.compile(r"{0}（{0}）".format(re.escape(chinese_name)))
    return duplicate_pattern.sub(chinese_name, text)


def build_name_pairs(entries: list[dict[str, Any]]) -> list[tuple[str, str]]:
    name_pairs: list[tuple[str, str]] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        english_name = str(entry.get("englishDisplayName", "")).strip()
        chinese_name = str(entry.get("chineseDisplayName", "")).strip()
        if not english_name or not chinese_name or english_name == chinese_name:
            continue
        if english_name in AMBIGUOUS_ENGLISH_DISPLAY_NAMES:
            continue
        name_pairs.append((english_name, chinese_name))

    name_pairs.sort(key=lambda item: len(item[0]), reverse=True)
    return name_pairs


def normalize_text_with_name_pairs(
    text: str,
    name_pairs: list[tuple[str, str]],
    skipped_english_names: set[str],
) -> str:
    updated_text = text
    for english_name, chinese_name in name_pairs:
        if english_name in skipped_english_names:
            continue
        updated_text = replace_name_occurrences(updated_text, english_name, chinese_name)
        updated_text = cleanup_duplicate_parenthetical_names(updated_text, chinese_name)
    return updated_text


def normalize_synergy_highlight_text(
    text: str,
    name_pairs: list[tuple[str, str]],
    skipped_english_names: set[str],
) -> str:
    if not text:
        return text

    segments = text.split(";")
    normalized_segments: list[str] = []
    for segment in segments:
        english_title_match = ENGLISH_SYNERGY_TITLE_SPLIT_PATTERN.match(segment)
        if english_title_match:
            title = english_title_match.group(1)
            remainder = english_title_match.group(2)
            normalized_remainder = normalize_text_with_name_pairs(remainder, name_pairs, skipped_english_names)
            normalized_segments.append(title + normalized_remainder)
            continue

        if "：" in segment:
            title, remainder = segment.split("：", 1)
            normalized_remainder = normalize_text_with_name_pairs(remainder, name_pairs, skipped_english_names)
            normalized_segments.append(title + "：" + normalized_remainder)
            continue

        normalized_segments.append(normalize_text_with_name_pairs(segment, name_pairs, skipped_english_names))
    return ";".join(normalized_segments)


def localize_known_pickup_names_in_text(
    text: str,
    name_pairs: list[tuple[str, str]],
    current_english_name: str = "",
    is_synergy_field: bool = False,
) -> str:
    current_name = current_english_name.strip()
    skipped_english_names = {current_name} if current_name else set()
    if current_name:
        lowered_current_name = current_name.lower()
        for english_name, _ in name_pairs:
            lowered_candidate = english_name.lower()
            if lowered_candidate and lowered_candidate in lowered_current_name:
                skipped_english_names.add(english_name)
    if is_synergy_field:
        return normalize_synergy_highlight_text(text, name_pairs, skipped_english_names)
    return normalize_text_with_name_pairs(text, name_pairs, skipped_english_names)
