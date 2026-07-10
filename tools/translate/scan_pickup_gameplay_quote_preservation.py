from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from gameplay_translation_workflow import REPO_ROOT, TRANSLATABLE_FIELD_PAIRS, load_json


QUOTE_PATTERN = re.compile(r'"([^"\r\n]+)"')
TRAILING_QUOTE_PUNCTUATION_PATTERN = re.compile(r"[.,;:!?]+$")
DEFAULT_INPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "RandomLoadout.pickup-gameplay.zh-CN.work.json"
MAX_QUOTE_WORDS_TO_PRESERVE = 5


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan gameplay translation text for English quoted fragments that were not preserved in Chinese text."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to a gameplay translation batch JSON or the zh-CN gameplay work JSON.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/quote-preservation-report.<input-name>.json.",
    )
    return parser.parse_args()


def default_output_path(input_path: Path) -> Path:
    safe_name = input_path.stem.replace(" ", "_")
    return REPO_ROOT / "temp" / "{0}.quote-preservation-report.json".format(safe_name)


def contains_preserved_quote(chinese_text: str, english_quote: str) -> bool:
    if not chinese_text or not english_quote:
        return False
    return english_quote in chinese_text


def normalize_quoted_english_fragment(value: str) -> str:
    return TRAILING_QUOTE_PUNCTUATION_PATTERN.sub("", value.strip())


def should_require_quote_preservation(quote: str) -> bool:
    normalized = normalize_quoted_english_fragment(quote)
    if not normalized:
        return False

    # Full-sentence lore quotes can be naturally translated into Chinese.
    # This check is meant to protect short English titles/catchphrases that
    # are commonly retained verbatim in the zh-CN text.
    words = re.findall(r"[A-Za-z0-9']+", normalized)
    if len(words) > MAX_QUOTE_WORDS_TO_PRESERVE:
        return False
    if any(punct in normalized for punct in (",", ";", ":")):
        return False
    return True


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    output_path = Path(args.output) if args.output.strip() else default_output_path(input_path)

    payload = load_json(input_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Input file did not contain an 'entries' array: {0}".format(input_path))

    issues: list[dict] = []
    for entry in entries:
        if not isinstance(entry, dict):
            continue

        for english_key, chinese_key in TRANSLATABLE_FIELD_PAIRS:
            english_text = str(entry.get(english_key, "")).strip()
            chinese_text = str(entry.get(chinese_key, "")).strip()
            english_quotes = [
                normalize_quoted_english_fragment(match.group(1))
                for match in QUOTE_PATTERN.finditer(english_text)
                if should_require_quote_preservation(match.group(1))
            ]
            if not english_quotes:
                continue

            missing_quotes = [quote for quote in english_quotes if not contains_preserved_quote(chinese_text, quote)]
            if not missing_quotes:
                continue

            issues.append(
                {
                    "pickupId": entry.get("pickupId"),
                    "englishDisplayName": entry.get("englishDisplayName", ""),
                    "field": chinese_key,
                    "sourceField": english_key,
                    "missingQuotedEnglishFragments": missing_quotes,
                    "englishText": english_text,
                    "chineseText": chinese_text,
                }
            )

    report = {
        "inputFile": input_path.as_posix(),
        "issueCount": len(issues),
        "issues": issues,
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("Scanned {0}. Found {1} quote-preservation issue(s). Report: {2}".format(input_path, len(issues), output_path))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
