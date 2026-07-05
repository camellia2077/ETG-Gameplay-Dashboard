from __future__ import annotations

import argparse
import json
import re
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import requests


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CATALOG_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickups.json"
DEFAULT_OUTPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-wiki-tips.en.json"
DEFAULT_CACHE_PATH = REPO_ROOT / "temp" / "wiki.gg-pickup-tip-cache.json"
WIKI_API_URL = "https://enterthegungeon.wiki.gg/api.php"
USER_AGENT = "ETG-Gameplay-Dashboard/1.0 (offline pickup tip generator)"
REQUEST_DELAY_SECONDS = 0.35
MAX_RETRY_ATTEMPTS = 4
MAX_NOTES_LENGTH = 720

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

PREFERRED_SECTION_NAMES = (
    "Effects",
    "Notes",
    "Trivia",
)

SKIPPED_SECTION_NAMES = {
    "Gallery",
    "History",
    "See also",
    "Synergies ETG",
    "Synergies A Farewell to Arms",
    "Other Appearances",
    "Unused and Cut Content",
    "In Other Media",
}

TITLE_OVERRIDES_BY_INTERNAL_NAME = {
    "Mega Buster": "Megahand",
    "Mega Buster (Air)": "Megahand",
    "Mega Buster (Bubble)": "Megahand",
    "Mega Buster (Crash)": "Megahand",
    "Mega Buster (Flash)": "Megahand",
    "Mega Buster (Heat)": "Megahand",
    "Mega Buster (Leaf)": "Megahand",
    "Mega Buster (Metal)": "Megahand",
    "Mega Buster (Quick)": "Megahand",
    "Samus Arm": "Heroine",
    "Samus Arm (Hyper)": "Heroine",
    "Samus Arm (Ice)": "Heroine",
    "Samus Arm (Plasma)": "Heroine",
    "Samus Arm (Wave)": "Heroine",
    "Upper_Case_R": "Lower Case r",
    "BabyDragunItem": "Serpent",
}

TITLE_OVERRIDES_BY_DISPLAY_NAME = {
    "Gunderfury Lv10": "Gunderfury",
    "Gunderfury Level 10": "Gunderfury",
    "Gunderfury Level 20": "Gunderfury",
    "Gunderfury Level 30": "Gunderfury",
    "Gunderfury Level 40": "Gunderfury",
    "Gunderfury Level 50": "Gunderfury",
}

MANUAL_NOTES_BY_INTERNAL_NAME = {
    "Tank_Treader_Gun": "Hidden Tank Treader-related gun variant. This internal pickup is excluded from normal loot pools and no standalone public wiki article was found.",
    "RogueShipFakeItem": "Internal-use Pilot ship item. This special active object is not a normal player-facing pickup and no standalone public wiki article was found.",
}


@dataclass(frozen=True)
class TipEntry:
    pickup_id: int
    category: str
    display_name: str
    internal_name: str
    wiki_key: str
    english_notes: str


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a pickupId -> English wiki tips catalog from the Enter the Gungeon wiki."
    )
    parser.add_argument(
        "--catalog",
        default=str(DEFAULT_CATALOG_PATH),
        help="Path to RandomLoadout.pickups.json.",
    )
    parser.add_argument(
        "--output",
        default=str(DEFAULT_OUTPUT_PATH),
        help="Output JSON path.",
    )
    parser.add_argument(
        "--cache",
        default=str(DEFAULT_CACHE_PATH),
        help="Local cache path for resolved wiki pages and extracts.",
    )
    parser.add_argument(
        "--max-notes-length",
        default=MAX_NOTES_LENGTH,
        type=int,
        help="Maximum number of characters kept in each generated note block.",
    )
    return parser.parse_args()


def normalize_lookup(value: str) -> str:
    if not value:
        return ""

    return "".join(ch.lower() for ch in value if ch.isalnum())


def is_placeholder_name(value: str) -> bool:
    normalized = normalize_lookup(value)
    return normalized in {
        "",
        "stringnotfound",
        "itemsstringnotfound",
        "rogueshipfakeitem",
    }


def slugify(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", value.strip().lower()).strip("-")


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


def prettify_internal_name(value: str) -> str:
    if not value:
        return ""

    spaced = value.replace("_", " ").replace("-", " ").strip()
    if not spaced:
        return ""

    words: list[str] = []
    for token in spaced.split():
        if token.isupper() or any(ch.isdigit() for ch in token):
            words.append(token.upper() if token.isalpha() and len(token) <= 4 else token)
        else:
            words.append(token.capitalize())
    return " ".join(words)


def build_title_candidates(entry: dict[str, Any]) -> list[str]:
    raw_candidates: list[str] = []
    internal_name = str(entry.get("internalName", "")).strip()
    display_name = str(entry.get("displayName", "")).strip()

    title_override = TITLE_OVERRIDES_BY_INTERNAL_NAME.get(internal_name)
    if title_override:
        raw_candidates.append(title_override)

    display_override = TITLE_OVERRIDES_BY_DISPLAY_NAME.get(display_name)
    if display_override:
        raw_candidates.append(display_override)

    for field_name in ("displayName", "primaryDisplayName"):
        value = str(entry.get(field_name, "")).strip()
        if value:
            raw_candidates.append(value)

    for variant in strip_internal_suffixes(internal_name):
        pretty = prettify_internal_name(variant)
        if pretty:
            raw_candidates.append(pretty)

    deduped: list[str] = []
    seen: set[str] = set()
    for candidate in raw_candidates:
        normalized = normalize_lookup(candidate)
        if not normalized or normalized in seen:
            continue
        seen.add(normalized)
        deduped.append(candidate)
    return deduped


def load_catalog_entries(path: Path) -> list[dict[str, Any]]:
    content = json.loads(path.read_text(encoding="utf-8"))
    return content.get("pickups", [])


def load_cache(path: Path) -> dict[str, dict[str, str]]:
    if not path.exists():
        return {}

    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return {}

    pages = data.get("pages")
    return pages if isinstance(pages, dict) else {}


def save_cache(path: Path, pages: dict[str, dict[str, str]]) -> None:
    payload = {
        "updatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S"),
        "pages": pages,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def make_session() -> requests.Session:
    session = requests.Session()
    session.headers.update(
        {
            "User-Agent": USER_AGENT,
            "Accept": "application/json, text/plain, */*",
        }
    )
    return session


def request_api(session: requests.Session, params: dict[str, Any]) -> dict[str, Any]:
    for attempt in range(MAX_RETRY_ATTEMPTS):
        response = session.get(WIKI_API_URL, params=params, timeout=30)
        if response.status_code == 429:
            time.sleep((attempt + 1) * 2.0)
            continue

        response.raise_for_status()
        payload = response.json()
        error = payload.get("error") if isinstance(payload, dict) else None
        if isinstance(error, dict) and error.get("code") == "ratelimited":
            time.sleep((attempt + 1) * 2.0)
            continue

        time.sleep(REQUEST_DELAY_SECONDS)
        return payload

    raise RuntimeError("Wiki API rate-limited too many times for params: {0}".format(params))


def get_extract_for_title(session: requests.Session, title: str) -> tuple[str, str] | None:
    payload = request_api(
        session,
        {
            "action": "query",
            "prop": "extracts",
            "explaintext": 1,
            "redirects": 1,
            "titles": title,
            "format": "json",
        },
    )
    pages = payload.get("query", {}).get("pages", {})
    if not isinstance(pages, dict) or not pages:
        return None

    page = next(iter(pages.values()))
    if not isinstance(page, dict) or "missing" in page:
        return None

    resolved_title = str(page.get("title", "")).strip()
    extract = str(page.get("extract", "")).strip()
    if not resolved_title or not extract:
        return None

    return resolved_title, extract


def search_title(session: requests.Session, query: str) -> str:
    payload = request_api(
        session,
        {
            "action": "opensearch",
            "search": query,
            "limit": 5,
            "namespace": 0,
            "format": "json",
        },
    )
    if not isinstance(payload, list) or len(payload) < 2:
        return ""

    titles = payload[1]
    if not isinstance(titles, list):
        return ""

    normalized_query = normalize_lookup(query)
    for title in titles:
        if normalize_lookup(str(title)) == normalized_query:
            return str(title)

    for title in titles:
        title_value = str(title).strip()
        if title_value:
            return title_value

    return ""


def parse_sections(extract: str) -> tuple[list[str], dict[str, list[str]]]:
    intro: list[str] = []
    sections: dict[str, list[str]] = {}
    current_section = "__intro__"

    for raw_line in extract.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        line = raw_line.strip()
        if not line:
            continue

        heading_match = re.fullmatch(r"==\s*(.+?)\s*==", line)
        if heading_match:
            current_section = heading_match.group(1).strip()
            sections.setdefault(current_section, [])
            continue

        if current_section == "__intro__":
            intro.append(line)
        else:
            sections.setdefault(current_section, []).append(line)

    return intro, sections


def trim_sentence_block(text: str, max_length: int) -> str:
    normalized = re.sub(r"\s+", " ", text).strip()
    if len(normalized) <= max_length:
        return normalized

    sentences = re.split(r"(?<=[.!?])\s+", normalized)
    kept: list[str] = []
    for sentence in sentences:
        candidate = " ".join(kept + [sentence]).strip()
        if not candidate:
            continue
        if len(candidate) > max_length:
            break
        kept.append(sentence)

    if kept:
        return " ".join(kept).strip()

    clipped = normalized[: max(0, max_length - 1)].rstrip(" ,;:")
    return clipped + "..."


def build_notes_from_extract(extract: str, max_length: int) -> str:
    intro_lines, sections = parse_sections(extract)
    chunks: list[str] = []

    if intro_lines:
        chunks.append(trim_sentence_block(" ".join(intro_lines[:2]), min(max_length, 220)))

    for section_name in PREFERRED_SECTION_NAMES:
        lines = sections.get(section_name)
        if not lines:
            continue

        cleaned_lines = [trim_sentence_block(line, 180) for line in lines[:3]]
        cleaned_lines = [line for line in cleaned_lines if line]
        if cleaned_lines:
            chunks.append(section_name + ": " + " ".join(cleaned_lines))

    if not chunks:
        fallback_lines: list[str] = []
        for section_name, lines in sections.items():
            if section_name in SKIPPED_SECTION_NAMES:
                continue
            fallback_lines.extend(lines[:2])
            if fallback_lines:
                break
        if fallback_lines:
            chunks.append(trim_sentence_block(" ".join(fallback_lines), min(max_length, 300)))

    combined = "\n\n".join(chunk for chunk in chunks if chunk).strip()
    return trim_sentence_block(combined, max_length).replace(" .", ".")


def resolve_page(
    entry: dict[str, Any],
    session: requests.Session,
    page_cache: dict[str, dict[str, str]],
    max_length: int,
) -> tuple[str, str] | None:
    candidates = build_title_candidates(entry)
    internal_name = str(entry.get("internalName", "")).strip()
    if not candidates:
        manual_notes = MANUAL_NOTES_BY_INTERNAL_NAME.get(internal_name, "").strip()
        return ("Internal", manual_notes) if manual_notes else None

    for candidate in candidates:
        cache_key = normalize_lookup(candidate)
        cached = page_cache.get(cache_key)
        if cached and cached.get("title") and cached.get("notes"):
            return cached["title"], cached["notes"]

        result = get_extract_for_title(session, candidate)
        if result is None:
            searched_title = search_title(session, candidate)
            result = get_extract_for_title(session, searched_title) if searched_title else None

        if result is None:
            page_cache[cache_key] = {}
            continue

        resolved_title, extract = result
        notes = build_notes_from_extract(extract, max_length)
        if not notes:
            page_cache[cache_key] = {}
            continue

        page_cache[cache_key] = {
            "title": resolved_title,
            "notes": notes,
        }
        return resolved_title, notes

    manual_notes = MANUAL_NOTES_BY_INTERNAL_NAME.get(internal_name, "").strip()
    return ("Internal", manual_notes) if manual_notes else None


def build_tip_entries(
    catalog_entries: list[dict[str, Any]],
    session: requests.Session,
    page_cache: dict[str, dict[str, str]],
    max_length: int,
) -> tuple[list[TipEntry], list[int]]:
    tip_entries: list[TipEntry] = []
    unmatched_pickup_ids: list[int] = []
    resolved_by_display_name: dict[str, tuple[str, str] | None] = {}

    for entry in catalog_entries:
        display_name = str(entry.get("displayName", "")).strip()
        internal_name = str(entry.get("internalName", "")).strip()
        if is_placeholder_name(display_name):
            shared_key = normalize_lookup(internal_name)
        else:
            shared_key = normalize_lookup(display_name) or normalize_lookup(internal_name)

        if shared_key in resolved_by_display_name:
            resolved = resolved_by_display_name[shared_key]
        else:
            resolved = resolve_page(entry, session, page_cache, max_length)
            resolved_by_display_name[shared_key] = resolved

        if resolved is None:
            unmatched_pickup_ids.append(int(entry.get("pickupId", -1)))
            continue

        resolved_title, notes = resolved
        tip_entries.append(
            TipEntry(
                pickup_id=int(entry["pickupId"]),
                category=str(entry.get("category", "")),
                display_name=display_name,
                internal_name=str(entry.get("internalName", "")),
                wiki_key=resolved_title.replace(" ", "_"),
                english_notes=notes,
            )
        )

    return tip_entries, unmatched_pickup_ids


def write_output(path: Path, tip_entries: list[TipEntry], catalog_entry_count: int, unmatched_pickup_ids: list[int]) -> None:
    payload = {
        "generatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S"),
        "sourceWikiBaseUrl": "https://enterthegungeon.wiki.gg/wiki/",
        "catalogEntryCount": catalog_entry_count,
        "tipEntryCount": len(tip_entries),
        "unmatchedPickupCount": len(unmatched_pickup_ids),
        "tips": [
            {
                "pickupId": entry.pickup_id,
                "category": entry.category,
                "displayName": entry.display_name,
                "internalName": entry.internal_name,
                "wikiKey": entry.wiki_key,
                "englishNotes": entry.english_notes,
            }
            for entry in tip_entries
        ],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def main() -> int:
    args = parse_args()
    catalog_path = Path(args.catalog)
    output_path = Path(args.output)
    cache_path = Path(args.cache)
    max_length = max(180, int(args.max_notes_length))

    catalog_entries = load_catalog_entries(catalog_path)
    page_cache = load_cache(cache_path)
    session = make_session()

    tip_entries, unmatched_pickup_ids = build_tip_entries(catalog_entries, session, page_cache, max_length)
    write_output(output_path, tip_entries, len(catalog_entries), unmatched_pickup_ids)
    save_cache(cache_path, page_cache)

    print(
        "Generated {0} tip entries from {1} catalog entries. Unmatched pickups: {2}. Output: {3}".format(
            len(tip_entries),
            len(catalog_entries),
            len(unmatched_pickup_ids),
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
