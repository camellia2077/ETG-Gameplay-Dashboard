from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from typing import Any

from gameplay_translation_workflow import load_json, summarize_entries, utc_now_text, write_json


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_WORK_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json"
DEFAULT_SYNERGY_PATH = REPO_ROOT / "tools" / "data" / "reference" / "etg-synergies.json"
DEFAULT_REPORT_PATH = REPO_ROOT / "temp" / "game-synergy-name-localization.report.json"
SPECIAL_UNTRANSLATED_NAMES = {
    "#SOULAIR": r"\o/",
}
LOCALIZED_TITLE_FIELD_PAIRS = (
    ("englishSynergyHighlights", "chineseSynergyHighlights"),
    ("englishUsageNotes", "chineseUsageNotes"),
)
SINGLE_WORD_TITLE_RE = re.compile(r"^([A-Za-z][A-Za-z'’.-]*)\s*:\s*")
SINGLE_WORD_TITLE_TRANSLATIONS = {
    "Bug": "漏洞",
    "Altered": "改动",
    "Removed": "移除",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Replace temporary synergy titles in zh-CN pickup gameplay text with in-game Chinese names."
    )
    parser.add_argument("--input", default=str(DEFAULT_WORK_PATH), help="Path to the zh-CN gameplay work file.")
    parser.add_argument("--synergies", default=str(DEFAULT_SYNERGY_PATH), help="Path to the extracted game synergy catalog.")
    parser.add_argument("--report", default=str(DEFAULT_REPORT_PATH), help="Path for the dry-run/apply report.")
    parser.add_argument("--dry-run", action="store_true", help="Preview replacements without writing the input work file (default).")
    parser.add_argument("--apply", action="store_true", help="Write approved replacements to the input work file.")
    return parser.parse_args()


def parse_synergy_catalog(payload: dict[str, Any]) -> dict[str, tuple[str, str]]:
    result: dict[str, tuple[str, str]] = {}
    synergies = payload.get("synergies", {})
    if not isinstance(synergies, dict):
        return result
    for key, synergy in synergies.items():
        if not isinstance(synergy, dict):
            continue
        names = synergy.get("names", {})
        if not isinstance(names, dict):
            continue
        english = str(names.get("en", "") or "").strip()
        chinese = str(names.get("zh-CN", "") or "").strip()
        if english:
            result[str(key).strip()] = (english, chinese)
    return result


def split_segments(value: Any) -> list[str]:
    text = str(value or "").strip()
    return [segment.strip() for segment in text.split(";") if segment.strip()]


def split_english_synergy_segments(value: Any, english_titles: list[str]) -> list[str]:
    text = str(value or "").strip()
    if not text:
        return []
    title_starts = tuple(sorted((title for title in english_titles if title), key=len, reverse=True))
    segments: list[str] = []
    start = 0
    for match in re.finditer(r";\s+", text):
        next_text = text[match.end() :]
        if title_starts and next_text.startswith(title_starts):
            segments.append(text[start : match.start()].strip())
            start = match.end()
    segments.append(text[start:].strip())
    return [segment for segment in segments if segment]


def find_english_synergy_titles(value: Any, names: list[tuple[str, str, str]]) -> list[tuple[str, str, str]]:
    text = str(value or "").strip()
    matches: list[tuple[int, tuple[str, str, str]]] = []
    for key, english, chinese in names:
        start = 0
        while True:
            position = text.find(english, start)
            if position < 0:
                break
            previous_text = text[:position].rstrip()
            if position == 0 or previous_text.endswith(";"):
                after = text[position + len(english) : position + len(english) + 1]
                if not after or after.isspace():
                    matches.append((position, (key, english, chinese)))
            start = position + len(english)
    matches.sort(key=lambda item: (item[0], -len(item[1][1])))
    result: list[tuple[str, str, str]] = []
    occupied_until = -1
    for position, match in matches:
        if position < occupied_until:
            continue
        result.append(match)
        occupied_until = position + len(match[1])
    return result


def find_game_synergy_title(text: str, names: list[tuple[str, str, str]]) -> tuple[str, str, str] | None:
    for key, english, chinese in names:
        if not chinese or english == chinese:
            continue
        if text == english or text.startswith(english + " "):
            return key, english, chinese
    return None


def find_title_in_chinese_segment(text: str, names: list[tuple[str, str, str]]) -> tuple[str, str, str] | None:
    for key, english, chinese in names:
        if not chinese or english == chinese:
            continue
        parenthetical = "（" + english + "）"
        parenthetical_position = text.find(parenthetical)
        has_title_separator_before_parenthetical = "：" in text[:parenthetical_position] or ":" in text[:parenthetical_position]
        if text.startswith(english) or (parenthetical_position >= 0 and not has_title_separator_before_parenthetical and parenthetical_position < 80):
            return key, english, chinese
    return None


def add_single_word_english_title(text: str, english_segment: str) -> tuple[str, str]:
    """Retain a one-word English header such as ``Bug:`` in Chinese text."""
    match = SINGLE_WORD_TITLE_RE.match(english_segment)
    if not match or match.group(1).lower() in {"http", "https"}:
        return text, "not-applicable"
    english_title = match.group(1)
    if "（" + english_title + "）" in text[:80]:
        return text, "already-canonical"
    chinese_header = re.match(r"^(?P<title>[^：:]{1,40})(?P<separator>[：:])(?P<rest>.*)$", text)
    if not chinese_header:
        return text, "ambiguous"
    title = chinese_header.group("title").strip()
    if not title:
        return text, "ambiguous"
    if title == english_title:
        title = SINGLE_WORD_TITLE_TRANSLATIONS.get(english_title, title)
    rest = chinese_header.group("rest").lstrip()
    return title + "（" + english_title + "）：" + rest, "single-word-title-with-english"


def replace_title(text: str, english: str, chinese: str, special_title: str = "") -> tuple[str, str]:
    if special_title:
        if text.startswith(special_title):
            return text, "already-special-title"
        if text.startswith("老骑士之药瓶（Old Knight's Flask）"):
            return special_title + "：" + text, "special-title-prefix"
        parenthetical_prefix = re.match(r"^阳炎标枪（" + re.escape("Sunlight Javelin") + r"）(?P<rest>.*)$", text)
        if parenthetical_prefix:
            rest = re.sub(r"^：+", "", parenthetical_prefix.group("rest").lstrip())
            return special_title + "：" + rest, "special-title"
        return text, "ambiguous"

    canonical = chinese if not chinese or chinese == english else chinese + "（" + english + "）"
    if text == english:
        return canonical, "english-exact"
    if canonical and text.startswith(canonical):
        original_rest = text[len(canonical) :]
        rest = re.sub(r"^：+", "", original_rest.lstrip()).lstrip()
        normalized = canonical + (("：" + rest) if rest else "")
        if normalized != text:
            return normalized, "normalized-title-separator"
        return text, "already-canonical"
    if chinese and text.startswith(chinese):
        rest = re.sub(r"^：+", "", text[len(chinese) :].lstrip())
        return canonical + "：" + rest, "chinese-title-with-english"
    if text.startswith(english):
        boundary = text[len(english) : len(english) + 1]
        if boundary and not boundary.isalnum() and boundary != "_":
            return canonical + text[len(english) :], "english-prefix"

    parenthetical = re.match(r"^(?P<temporary>.+?)（" + re.escape(english) + r"）(?P<rest>.*)$", text)
    if parenthetical and len(parenthetical.group("temporary")) <= 80:
        if chinese:
            return canonical + "：" + parenthetical.group("rest").lstrip(), "temporary-with-english"
        return text, "already-english-parenthetical"

    if not chinese and "（" in text[:80]:
        return text, "ambiguous"

    colon = re.match(r"^(?P<temporary>[^：:]{1,40})(?P<separator>[：:])(?P<rest>.*)$", text)
    if colon and not colon.group("temporary").startswith(("如果", "若", "当", "在", "持有")):
        if chinese:
            return canonical + colon.group("separator") + colon.group("rest"), "temporary-title"
        return colon.group("temporary") + "（" + english + "）" + colon.group("separator") + colon.group("rest"), "temporary-title-with-english-only"

    no_separator = re.match(r"^(?P<temporary>[^，。！？；：:\s]{1,40}(?:\s+[^，。！？；：:\s]{1,20})?)\s+(?P<rest>(?:如果|若|当|在|持有|接触|可以|会|这项|玩家).*)$", text)
    if no_separator:
        if chinese:
            return canonical + "：" + no_separator.group("rest"), "temporary-title-without-separator"
        return no_separator.group("temporary") + "（" + english + "）：" + no_separator.group("rest"), "temporary-title-with-english-only"

    if chinese and any(not character.isalnum() and not character.isspace() for character in english):
        no_separator_with_punctuation = re.match(r"^(?P<temporary>.+?)(?P<rest>如果|若|当|在|持有|接触|可以|会|这项|玩家).*$", text)
        if no_separator_with_punctuation:
            return canonical + text[len(no_separator_with_punctuation.group("temporary")) :], "temporary-title-without-separator"

    if not chinese:
        no_separator_without_space = re.match(r"^(?P<temporary>.+?)(?P<rest>如果|若|当|在|持有|接触|可以|会|这项|玩家).*$", text)
        if no_separator_without_space:
            return no_separator_without_space.group("temporary") + "（" + english + "）" + text[len(no_separator_without_space.group("temporary")) :], "temporary-title-with-english-only"

    if chinese and "（" not in text[:80] and "：" not in text[:80] and ":" not in text[:80]:
        return canonical + "：" + text, "missing-title-prefix"

    return text, "ambiguous"


def main() -> int:
    args = parse_args()
    work_path = Path(args.input).resolve()
    synergy_path = Path(args.synergies).resolve()
    report_path = Path(args.report).resolve()

    work_payload = load_json(work_path)
    synergy_payload = load_json(synergy_path)
    catalog = parse_synergy_catalog(synergy_payload)
    names = sorted(
        ((key, english, chinese) for key, (english, chinese) in catalog.items()),
        key=lambda item: len(item[1]),
        reverse=True,
    )

    changes: list[dict[str, Any]] = []
    skipped: list[dict[str, Any]] = []
    alignment_issues: list[dict[str, Any]] = []
    changed_entries: set[int] = set()

    for entry in work_payload.get("entries", []):
        if not isinstance(entry, dict):
            continue
        pickup_id = int(entry.get("pickupId", -1))
        entry_changed = False
        for english_field, chinese_field in LOCALIZED_TITLE_FIELD_PAIRS:
            english_titles = find_english_synergy_titles(entry.get(english_field), names)
            english_segments = split_segments(entry.get(english_field))
            chinese_segments = split_segments(entry.get(chinese_field))
            has_single_word_title = any(SINGLE_WORD_TITLE_RE.match(segment) for segment in english_segments)
            if (not english_titles and not has_single_word_title) or not chinese_segments:
                continue
            if len(english_titles) > len(chinese_segments):
                alignment_issues.append({"pickupId": pickup_id, "field": chinese_field, "englishCount": len(english_titles), "chineseCount": len(chinese_segments)})

            updated_segments = list(chinese_segments)
            for index, english_segment in enumerate(english_segments):
                if index >= len(updated_segments):
                    continue
                updated, method = add_single_word_english_title(updated_segments[index], english_segment)
                if method == "ambiguous":
                    skipped.append({"pickupId": pickup_id, "field": chinese_field, "segmentIndex": index, "englishTitle": SINGLE_WORD_TITLE_RE.match(english_segment).group(1), "text": updated_segments[index]})
                elif updated != updated_segments[index]:
                    before = updated_segments[index]
                    updated_segments[index] = updated
                    changed_entries.add(pickup_id)
                    entry_changed = True
                    changes.append({"pickupId": pickup_id, "field": chinese_field, "segmentIndex": index, "englishTitle": SINGLE_WORD_TITLE_RE.match(english_segment).group(1), "method": method, "before": before, "after": updated})

            for index, chinese_segment in enumerate(updated_segments):
                match = english_titles[index] if index < len(english_titles) else find_title_in_chinese_segment(chinese_segment, names)
                if match is None:
                    continue
                key, english_title, chinese_title = match
                updated, method = replace_title(
                    chinese_segment,
                    english_title,
                    chinese_title,
                    SPECIAL_UNTRANSLATED_NAMES.get(key, ""),
                )
                if method == "ambiguous":
                    skipped.append({"pickupId": pickup_id, "field": chinese_field, "segmentIndex": index, "key": key, "englishTitle": english_title, "chineseTitle": chinese_title, "text": chinese_segment})
                    continue
                if updated == chinese_segment:
                    continue
                updated_segments[index] = updated
                changed_entries.add(pickup_id)
                entry_changed = True
                changes.append({"pickupId": pickup_id, "field": chinese_field, "segmentIndex": index, "key": key, "englishTitle": english_title, "chineseTitle": chinese_title, "method": method, "before": chinese_segment, "after": updated})

            if entry_changed and args.apply:
                entry[chinese_field] = "; ".join(updated_segments)

        if entry_changed and args.apply:
            entry["updatedUtc"] = utc_now_text()

    report = {
        "workflow": "game-synergy-name-localization",
        "applied": bool(args.apply),
        "input": str(work_path),
        "synergyCatalog": str(synergy_path),
        "catalogEntryCount": len(catalog),
        "changedEntryCount": len(changed_entries),
        "changeCount": len(changes),
        "skippedAmbiguousCount": len(skipped),
        "alignmentIssueCount": len(alignment_issues),
        "specialUntranslated": [
            {"key": key, "englishTitle": english, "specialTitle": SPECIAL_UNTRANSLATED_NAMES[key]}
            for key, (english, _chinese) in catalog.items()
            if key in SPECIAL_UNTRANSLATED_NAMES
        ],
        "changes": changes,
        "skippedAmbiguous": skipped,
        "alignmentIssues": alignment_issues,
    }
    write_json(report_path, report)

    if args.apply:
        summary = summarize_entries(work_payload.get("entries", []))
        work_payload.update(summary)
        work_payload["updatedUtc"] = utc_now_text()
        write_json(work_path, work_payload)

    print(json.dumps({key: report[key] for key in ("applied", "catalogEntryCount", "changedEntryCount", "changeCount", "skippedAmbiguousCount", "alignmentIssueCount")}, ensure_ascii=False))
    print("Report: {0}".format(report_path))
    if not args.apply:
        print("Dry-run only. Re-run with --apply to write the replacements.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
