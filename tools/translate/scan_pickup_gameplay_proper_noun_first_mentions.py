from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from gameplay_translation_workflow import DEFAULT_WORK_FILE_PATH, REPO_ROOT, TRANSLATABLE_FIELD_PAIRS, load_json


DEFAULT_INPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-gameplay.zh-CN.work.json"
LATIN_FRAGMENT_PATTERN = re.compile(
    r"[A-Za-zÀ-ÖØ-öø-ÿĀ-ž][A-Za-z0-9À-ÖØ-öø-ÿĀ-ž'&_.:+\-]*(?:\s+[A-Za-z0-9À-ÖØ-öø-ÿĀ-ž'&_.:+\-]+)*"
)
WRAPPED_ENGLISH_PATTERN = re.compile(
    r"[《》〈〉「」『』“”‘’·\u4e00-\u9fff0-9A-Za-z0-9 _.\-:：,，、]{1,120}（[A-Za-zÀ-ÖØ-öø-ÿĀ-ž0-9'\"“”&_.:+,\- !?]+）"
)
DOUBLE_QUOTED_PATTERN = re.compile(r'"[^"\r\n]+"')
SINGLE_QUOTED_WRAPPED_ENGLISH_PATTERN = re.compile(
    r"（'([A-Za-zÀ-ÖØ-öø-ÿĀ-ž][A-Za-z0-9À-ÖØ-öø-ÿĀ-ž'&_.:+\-]*(?:\s+[A-Za-z0-9À-ÖØ-öø-ÿĀ-ž'&_.:+\-]+)*)'）"
)
SYNERGY_TITLE_PATTERN = re.compile(r"(^|;\s*)([^：;\r\n]+)：")
APPROVED_RAW_LATIN_FRAGMENTS = {
    "AC-15",
    "AK-47",
    "A.W.P.",
    "AU Gun",
    "Boss",
    "Bello",
    "CQC",
    "DPS",
    "Exit the Gungeon",
    "Hexagun",
    "M1911",
    "M1",
    "M16",
    "M9",
    "NES",
    "Nuign Spectre",
    "Professor Goopton",
    "RPG",
    "SAA",
    "SHOTGUN",
    "Shotgun Affinity",
    "Symbol P",
    "Thunderbolt",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan gameplay translation text for likely unwrapped foreign proper nouns that should be formatted as 中文（English）."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to a gameplay translation batch JSON or the zh-CN gameplay work JSON.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/proper-noun-first-mention-report.<input-name>.json.",
    )
    return parser.parse_args()


def default_output_path(input_path: Path) -> Path:
    safe_name = input_path.stem.replace(" ", "_")
    return REPO_ROOT / "temp" / "{0}.proper-noun-first-mention-report.json".format(safe_name)


def build_ignored_spans(text: str) -> list[tuple[int, int]]:
    spans: list[tuple[int, int]] = []
    for pattern in (WRAPPED_ENGLISH_PATTERN, DOUBLE_QUOTED_PATTERN):
        spans.extend((match.start(), match.end()) for match in pattern.finditer(text))
    return spans


def build_synergy_title_spans(text: str) -> list[tuple[int, int]]:
    spans: list[tuple[int, int]] = []
    for match in SYNERGY_TITLE_PATTERN.finditer(text):
        title_text = match.group(2).strip()
        if not title_text:
            continue
        if not LATIN_FRAGMENT_PATTERN.search(title_text):
            continue
        spans.append((match.start(2), match.end(2)))
    return spans


def build_localized_name_spans(text: str, localized_names_with_latin: list[str]) -> list[tuple[int, int]]:
    spans: list[tuple[int, int]] = []
    for localized_name in localized_names_with_latin:
        # Official zh-CN names can legitimately contain Latin fragments, so the
        # scanner should ignore the whole canonical localized name span.
        if not localized_name or localized_name not in text:
            continue
        start = 0
        while True:
            index = text.find(localized_name, start)
            if index < 0:
                break
            spans.append((index, index + len(localized_name)))
            start = index + len(localized_name)
    return spans


def is_inside_ignored_spans(start: int, end: int, spans: list[tuple[int, int]]) -> bool:
    for span_start, span_end in spans:
        if start >= span_start and end <= span_end:
            return True
    return False


def should_ignore_fragment(fragment: str) -> bool:
    stripped = fragment.strip().strip("'\"")
    if not stripped:
        return True
    if stripped in APPROVED_RAW_LATIN_FRAGMENTS:
        return True
    if re.fullmatch(r"[SABCDF]\s*级?", stripped, re.IGNORECASE):
        return True
    if re.fullmatch(r"[A-Z0-9.+\-]{2,}", stripped):
        return True
    if re.fullmatch(r"[A-Za-z]\d+(?:\.\d+)?", stripped):
        return True
    return False


def collect_suspicious_wrapped_single_quoted_fragments(text: str) -> list[str]:
    fragments: list[str] = []
    for match in SINGLE_QUOTED_WRAPPED_ENGLISH_PATTERN.finditer(text):
        fragment = match.group(1).strip().strip("'\"")
        if should_ignore_fragment(fragment):
            continue
        if fragment not in fragments:
            fragments.append(fragment)
    return fragments


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    output_path = Path(args.output) if args.output.strip() else default_output_path(input_path)

    payload = load_json(input_path)
    entries = payload.get("entries", [])
    name_source_payload = load_json(DEFAULT_WORK_FILE_PATH)
    localized_names_with_latin = sorted(
        {
            str(entry.get("chineseDisplayName", "")).strip()
            for entry in name_source_payload.get("entries", [])
            if isinstance(entry, dict)
            and LATIN_FRAGMENT_PATTERN.search(str(entry.get("chineseDisplayName", "")).strip())
        },
        key=len,
        reverse=True,
    )
    if not isinstance(entries, list):
        raise ValueError("Input file did not contain an 'entries' array: {0}".format(input_path))

    issues: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue

        for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
            chinese_text = str(entry.get(chinese_key, "")).strip()
            if not chinese_text:
                continue

            ignored_spans = build_ignored_spans(chinese_text)
            ignored_spans.extend(build_localized_name_spans(chinese_text, localized_names_with_latin))
            if chinese_key == "chineseSynergyHighlights":
                # Synergy titles are intentionally allowed to stay in English for now.
                # This scanner should focus on untranslated proper nouns inside the prose,
                # not on the title label before the Chinese colon.
                ignored_spans.extend(build_synergy_title_spans(chinese_text))
            suspicious_fragments = collect_suspicious_wrapped_single_quoted_fragments(chinese_text)
            for match in LATIN_FRAGMENT_PATTERN.finditer(chinese_text):
                fragment = match.group(0).strip().strip("'\"")
                if should_ignore_fragment(fragment):
                    continue
                if is_inside_ignored_spans(match.start(), match.end(), ignored_spans):
                    continue
                if fragment not in suspicious_fragments:
                    suspicious_fragments.append(fragment)

            if not suspicious_fragments:
                continue

            issues.append(
                {
                    "pickupId": entry.get("pickupId"),
                    "englishDisplayName": entry.get("englishDisplayName", ""),
                    "field": chinese_key,
                    "sourceField": english_key,
                    "suspiciousRawLatinFragments": suspicious_fragments,
                    "chineseText": chinese_text,
                    "englishText": str(entry.get(english_key, "")).strip(),
                }
            )

    report = {
        "inputFile": input_path.as_posix(),
        "issueCount": len(issues),
        "issues": issues,
        "notes": [
            "This is a heuristic review tool.",
            "It flags raw Latin-script fragments in Chinese text that are not already wrapped as 中文（English） and are not in the built-in allowlist.",
            "Single-quoted English fragments are treated as suspicious by default, because translations should usually prefer natural Chinese wording, canonical in-game Chinese names, or 中文（English） formatting.",
        ],
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(
        "Scanned {0}. Found {1} proper-noun first-mention issue(s). Report: {2}".format(
            input_path, len(issues), output_path
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
