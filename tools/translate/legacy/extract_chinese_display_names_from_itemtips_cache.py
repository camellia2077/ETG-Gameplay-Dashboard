from __future__ import annotations

import argparse
import html
import json
import re
from pathlib import Path

from translation_workflow import normalize_lookup_key, write_json


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CACHE_DIR_PATH = REPO_ROOT / "temp" / "etg-itemtips-cn" / "cache"
TITLE_PAIR_PATTERN = re.compile(
    r'<div class="title"><span class="cn" data-title="(?P<chinese>[^"]+)">.*?</span><br>\s*'
    r'<span class="en" data-title="(?P<english>[^"]+)">',
    re.IGNORECASE | re.DOTALL,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Extract Chinese display names from etg-itemtips-cn cache HTML for a small set of English item names."
    )
    parser.add_argument(
        "--input",
        required=True,
        help=(
            "Relative path from the repository root to a UTF-8 input file. "
            "Supported formats: .txt line list, JSON string array, "
            "or JSON object with englishNames/entries."
        ),
    )
    parser.add_argument(
        "--output",
        help=(
            "Optional relative path from the repository root for the output JSON file. "
            "Defaults to the input path with '.resolved.json' appended."
        ),
    )
    parser.add_argument(
        "--cache-dir",
        default=str(DEFAULT_CACHE_DIR_PATH.relative_to(REPO_ROOT)),
        help="Relative path from the repository root to the etg-itemtips-cn cache directory.",
    )
    return parser.parse_args()


def resolve_repo_relative_path(relative_path_text: str) -> Path:
    candidate = Path(relative_path_text)
    if candidate.is_absolute():
        return candidate
    return REPO_ROOT / candidate


def load_english_names(path: Path) -> list[str]:
    suffix = path.suffix.lower()
    if suffix == ".txt":
        return [line.strip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]

    payload = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(payload, list):
        return [str(value).strip() for value in payload if str(value).strip()]

    if not isinstance(payload, dict):
        raise ValueError("Input JSON must be an array or object.")

    if isinstance(payload.get("englishNames"), list):
        return [str(value).strip() for value in payload["englishNames"] if str(value).strip()]

    if isinstance(payload.get("entries"), list):
        results: list[str] = []
        for entry in payload["entries"]:
            if not isinstance(entry, dict):
                continue
            english_name = str(entry.get("englishDisplayName", "")).strip()
            if english_name:
                results.append(english_name)
        return results

    raise ValueError("Input JSON must contain either an array, 'englishNames', or 'entries'.")


def build_cache_filename_candidates(english_name: str) -> list[str]:
    candidates: list[str] = []
    seen: set[str] = set()

    def add_candidate(stem: str) -> None:
        normalized_stem = stem.strip().lower()
        if not normalized_stem or normalized_stem in seen:
            return
        seen.add(normalized_stem)
        candidates.append(normalized_stem + ".html")

    normalized = normalize_lookup_key(english_name)
    if normalized:
        add_candidate(normalized)
        add_candidate(normalized.replace("_", "-"))

    compact = re.sub(r"[^a-z0-9]+", "", english_name.strip().lower())
    if compact:
        add_candidate(compact)

    raw_variants = {
        english_name.strip().lower(),
        english_name.strip().lower().replace("'", ""),
        english_name.strip().lower().replace("'", "-"),
        english_name.strip().lower().replace("'", "_"),
    }
    for variant in raw_variants:
        slug = re.sub(r"[^a-z0-9]+", "-", variant).strip("-")
        underscored = re.sub(r"[^a-z0-9]+", "_", variant).strip("_")
        add_candidate(slug)
        add_candidate(underscored)

    return candidates


def extract_title_pairs(cache_html_path: Path) -> dict[str, str]:
    text = cache_html_path.read_text(encoding="utf-8", errors="ignore")
    pairs: dict[str, str] = {}
    for match in TITLE_PAIR_PATTERN.finditer(text):
        english_name = html.unescape(match.group("english")).strip()
        chinese_name = html.unescape(match.group("chinese")).strip()
        if english_name and chinese_name and english_name not in pairs:
            pairs[english_name] = chinese_name
    return pairs


def resolve_chinese_name(english_name: str, cache_dir: Path) -> tuple[str, str]:
    for candidate_filename in build_cache_filename_candidates(english_name):
        cache_html_path = cache_dir / candidate_filename
        if not cache_html_path.is_file():
            continue

        title_pairs = extract_title_pairs(cache_html_path)
        chinese_name = title_pairs.get(english_name, "").strip()
        if chinese_name:
            return chinese_name, str(cache_html_path.relative_to(REPO_ROOT))

    return "", ""


def build_output_path(input_path: Path, output_arg: str | None) -> Path:
    if output_arg:
        return resolve_repo_relative_path(output_arg)
    return input_path.with_suffix(input_path.suffix + ".resolved.json")


def main() -> int:
    args = parse_args()
    input_path = resolve_repo_relative_path(args.input)
    output_path = build_output_path(input_path, args.output)
    cache_dir = resolve_repo_relative_path(args.cache_dir)

    english_names = load_english_names(input_path)
    results: list[dict] = []
    matched_count = 0

    for english_name in english_names:
        chinese_name, cache_file = resolve_chinese_name(english_name, cache_dir)
        if chinese_name:
            matched_count += 1
        results.append(
            {
                "englishDisplayName": english_name,
                "chineseDisplayName": chinese_name,
                "cacheFile": cache_file,
            }
        )

    payload = {
        "inputFile": str(input_path.relative_to(REPO_ROOT)),
        "cacheDir": str(cache_dir.relative_to(REPO_ROOT)),
        "entryCount": len(results),
        "matchedCount": matched_count,
        "entries": results,
    }
    write_json(output_path, payload)

    print(
        "Resolved {0} / {1} English name(s) from {2}. Output: {3}".format(
            matched_count,
            len(results),
            cache_dir.relative_to(REPO_ROOT),
            output_path.relative_to(REPO_ROOT),
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
