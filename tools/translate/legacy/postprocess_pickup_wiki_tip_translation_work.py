from __future__ import annotations

import argparse
import re
from pathlib import Path

from translation_workflow import load_json, utc_now_text, write_json


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_FILE_PATH = REPO_ROOT / "defaults" / "catalog" / "EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json"
CHINESE_CHARACTER_PATTERN = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Postprocess Chinese pickup wiki tips by aligning English item names to Chinese display names."
    )
    parser.add_argument(
        "--work-file",
        default=str(DEFAULT_WORK_FILE_PATH),
        help="Path to EtgGameplayDashboard.pickup-wiki-tips.zh-CN.work.json.",
    )
    parser.add_argument(
        "--pickup-id",
        action="append",
        type=int,
        default=[],
        help="Only postprocess these pickupIds. Can be repeated.",
    )
    parser.add_argument(
        "--wiki-key",
        action="append",
        default=[],
        help="Only postprocess entries with these wikiKeys. Can be repeated.",
    )
    return parser.parse_args()


def append_candidate(
    candidates: list[tuple[str, str]],
    seen_sources: set[str],
    source_text: str,
    target_text: str,
) -> None:
    normalized_source = source_text.strip()
    normalized_target = target_text.strip()
    if not normalized_source or not normalized_target:
        return
    if normalized_source == normalized_target or normalized_source in seen_sources:
        return

    seen_sources.add(normalized_source)
    candidates.append((normalized_source, normalized_target))


def build_global_replacement_candidates(entries: list[dict]) -> list[tuple[str, str]]:
    candidates: list[tuple[str, str]] = []
    seen_sources: set[str] = set()

    for entry in entries:
        if not isinstance(entry, dict):
            continue

        chinese_display_name = str(entry.get("chineseDisplayName", "")).strip()
        english_display_name = str(entry.get("englishDisplayName", "")).strip()
        wiki_key = str(entry.get("wikiKey", "")).strip()
        internal_name = str(entry.get("internalName", "")).strip()
        if not chinese_display_name:
            continue

        append_candidate(candidates, seen_sources, english_display_name, chinese_display_name)
        append_candidate(candidates, seen_sources, wiki_key, chinese_display_name)
        append_candidate(candidates, seen_sources, wiki_key.replace("_", " "), chinese_display_name)
        append_candidate(candidates, seen_sources, internal_name, chinese_display_name)

    candidates.sort(key=lambda item: len(item[0]), reverse=True)
    return candidates


def build_entry_replacement_candidates(entry: dict) -> list[tuple[str, str]]:
    candidates: list[tuple[str, str]] = []
    seen_sources: set[str] = set()
    chinese_display_name = str(entry.get("chineseDisplayName", "")).strip()
    if not chinese_display_name:
        return candidates

    append_candidate(candidates, seen_sources, str(entry.get("englishDisplayName", "")).strip(), chinese_display_name)
    append_candidate(candidates, seen_sources, str(entry.get("wikiKey", "")).strip(), chinese_display_name)
    append_candidate(candidates, seen_sources, str(entry.get("wikiKey", "")).strip().replace("_", " "), chinese_display_name)
    append_candidate(candidates, seen_sources, str(entry.get("internalName", "")).strip(), chinese_display_name)
    candidates.sort(key=lambda item: len(item[0]), reverse=True)
    return candidates


def replace_exact_term(text: str, source_text: str, target_text: str) -> tuple[str, bool]:
    if not text or not source_text or source_text == target_text:
        return text, False

    pattern = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(source_text) + r"(?![A-Za-z0-9_])")
    replaced_text, count = pattern.subn(target_text, text)
    return replaced_text, count > 0


def replace_leading_display_name(text: str, target_text: str) -> tuple[str, bool]:
    normalized_target = target_text.strip()
    if not text or not normalized_target:
        return text, False

    # Keep the rest of the translated note untouched and only align the leading item name.
    patterns = (
        re.compile(r"^\s*(?P<name>.+?)(?P<suffix>\s*[是为]\s*)", re.UNICODE),
        re.compile(r"^\s*(?P<name>.+?)(?P<suffix>\s+(?:is|was|are|appears)\s+)", re.IGNORECASE),
    )

    match = None
    for pattern in patterns:
        match = pattern.match(text)
        if match is not None:
            break

    if match is None:
        return text, False

    current_name = match.group("name").strip()
    if not current_name or current_name == normalized_target:
        return text, False

    replaced_text = normalized_target + match.group("suffix") + text[match.end():]
    return replaced_text, True


def should_process_entry(entry: dict, requested_pickup_ids: set[int], requested_wiki_keys: set[str]) -> bool:
    if not isinstance(entry, dict):
        return False

    pickup_id = entry.get("pickupId")
    wiki_key = str(entry.get("wikiKey", "")).strip()
    chinese_display_name = str(entry.get("chineseDisplayName", "")).strip()
    if not chinese_display_name:
        return False

    if requested_pickup_ids and pickup_id not in requested_pickup_ids:
        return False

    if requested_wiki_keys and wiki_key not in requested_wiki_keys:
        return False

    return True


def seed_chinese_notes(entry: dict) -> str:
    chinese_notes = str(entry.get("chineseNotes", "")).strip()
    if chinese_notes:
        if should_reset_to_english_seed(entry, chinese_notes):
            return str(entry.get("englishNotes", "")).strip()
        return chinese_notes

    return str(entry.get("englishNotes", "")).strip()


def should_reset_to_english_seed(entry: dict, chinese_notes: str) -> bool:
    english_notes = str(entry.get("englishNotes", "")).strip()
    if not chinese_notes or not english_notes:
        return False

    chinese_character_count = len(CHINESE_CHARACTER_PATTERN.findall(chinese_notes))
    if chinese_character_count <= 0:
        return True

    # Keep real translated notes intact, but reset seeded-English notes that only picked up a few Chinese names.
    return chinese_character_count < 20


def main() -> int:
    args = parse_args()
    work_file_path = Path(args.work_file)
    payload = load_json(work_file_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Work file did not contain an 'entries' array: {0}".format(work_file_path))

    requested_pickup_ids = {pickup_id for pickup_id in args.pickup_id if pickup_id > 0}
    requested_wiki_keys = {wiki_key.strip() for wiki_key in args.wiki_key if wiki_key.strip()}
    changed_pickup_ids: list[int] = []
    replacement_count = 0
    now_text = utc_now_text()

    for entry in entries:
        if not should_process_entry(entry, requested_pickup_ids, requested_wiki_keys):
            continue

        chinese_notes = seed_chinese_notes(entry)
        original_notes = chinese_notes
        chinese_notes, replaced = replace_leading_display_name(chinese_notes, str(entry.get("chineseDisplayName", "")).strip())
        if replaced:
            replacement_count += 1

        for source_text, target_text in build_entry_replacement_candidates(entry):
            chinese_notes, replaced = replace_exact_term(chinese_notes, source_text, target_text)
            if replaced:
                replacement_count += 1

        if chinese_notes != original_notes:
            entry["chineseNotes"] = chinese_notes
            if str(entry.get("translationStatus", "")).strip().lower() == "pending":
                entry["translationStatus"] = "draft"
            entry["updatedUtc"] = now_text
            changed_pickup_ids.append(int(entry["pickupId"]))

    if changed_pickup_ids:
        payload["generatedUtc"] = utc_now_text()
        write_json(work_file_path, payload)

    print(
        "Postprocessed {0} entrie(s) with {1} replacement(s): {2}".format(
            len(changed_pickup_ids),
            replacement_count,
            ", ".join(str(pickup_id) for pickup_id in changed_pickup_ids) if changed_pickup_ids else "<none>",
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
