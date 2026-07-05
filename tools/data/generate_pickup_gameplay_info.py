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
DEFAULT_OUTPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "RandomLoadout.pickup-gameplay.en.json"
DEFAULT_CACHE_PATH = REPO_ROOT / "temp" / "wiki.gg-pickup-gameplay-cache.json"
WIKI_API_URL = "https://enterthegungeon.wiki.gg/api.php"
USER_AGENT = "ETG-Gameplay-Dashboard/1.0 (pickup gameplay generator)"
REQUEST_DELAY_SECONDS = 0.35
MAX_RETRY_ATTEMPTS = 8
CACHE_SAVE_EVERY = 25
PLACEHOLDER_TEXT_VALUES = {"n/a", "na", "none", "null", "tbd"}

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
    "Hero's Sword": "Hero Sword",
}

INFOBOX_FIELD_MAP = {
    "quality": "quality",
    "type": "pickupType",
    "class": "gunClass",
    "dps": "dps",
    "clipsize": "magazineSize",
    "maxammo": "ammoCapacity",
    "damage": "damage",
    "firerate": "fireRate",
    "reload": "reloadTime",
    "shotspeed": "shotSpeed",
    "range": "range",
    "force": "force",
    "spread": "spread",
    "duration": "duration",
    "recharge": "recharge",
    "sold": "sellPrice",
    "unlock": "unlock",
}

INFOBOX_VARIANT_PREFIXES = ("etg_", "agd_", "hotg_", "xtg_", "powerup_")

STAT_GROUP_DEFINITIONS = (
    ("core", ("class", "dps", "damage")),
    ("ammo", ("magazine", "ammo", "reload")),
    ("handling", ("fire_rate", "shot_speed", "range")),
    ("impact", ("force", "spread")),
    ("timing", ("duration", "recharge", "sell")),
)

STAT_FIELD_BY_LABEL_KEY = {
    "class": "gunClass",
    "dps": "dps",
    "damage": "damage",
    "magazine": "magazineSize",
    "ammo": "ammoCapacity",
    "reload": "reloadTime",
    "fire_rate": "fireRate",
    "shot_speed": "shotSpeed",
    "range": "range",
    "force": "force",
    "spread": "spread",
    "duration": "duration",
    "recharge": "recharge",
    "sell": "sellPrice",
}


@dataclass(frozen=True)
class GameplayEntry:
    pickup_id: int
    category: str
    display_name: str
    internal_name: str
    wiki_key: str
    fields: dict[str, str]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a gameplay-focused pickup info catalog from the Enter the Gungeon wiki."
    )
    parser.add_argument("--catalog", default=str(DEFAULT_CATALOG_PATH), help="Path to RandomLoadout.pickups.json.")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT_PATH), help="Output JSON path.")
    parser.add_argument("--cache", default=str(DEFAULT_CACHE_PATH), help="Local cache path for resolved wiki pages.")
    parser.add_argument(
        "--sample-count",
        default=0,
        type=int,
        help="Optional maximum number of sample entries to generate. Use 0 for full catalog generation.",
    )
    return parser.parse_args()


def normalize_lookup(value: str) -> str:
    return "".join(ch.lower() for ch in value if ch.isalnum()) if value else ""


def get_catalog_quality_fallback(entry: dict[str, Any]) -> str:
    quality = str(entry.get("quality", "")).strip().upper()
    if quality in {"D", "C", "B", "A", "S"}:
        return quality
    if quality == "COMMON":
        return "Common"
    if quality == "SPECIAL":
        return "Special"
    return ""


def get_catalog_pickup_type(entry: dict[str, Any]) -> str:
    category = str(entry.get("category", "")).strip()
    return category if category in {"Passive", "Active"} else ""


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

    if internal_name in TITLE_OVERRIDES_BY_INTERNAL_NAME:
        raw_candidates.append(TITLE_OVERRIDES_BY_INTERNAL_NAME[internal_name])
    if display_name in TITLE_OVERRIDES_BY_DISPLAY_NAME:
        raw_candidates.append(TITLE_OVERRIDES_BY_DISPLAY_NAME[display_name])

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


def load_cache(path: Path) -> dict[str, dict[str, Any]]:
    if not path.exists():
        return {}
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return {}
    pages = data.get("pages")
    return pages if isinstance(pages, dict) else {}


def save_cache(path: Path, pages: dict[str, dict[str, Any]]) -> None:
    payload = {
        "updatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S"),
        "pages": pages,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def make_session() -> requests.Session:
    session = requests.Session()
    session.headers.update({"User-Agent": USER_AGENT, "Accept": "application/json, text/plain, */*"})
    return session


def request_api(session: requests.Session, params: dict[str, Any]) -> dict[str, Any]:
    for attempt in range(MAX_RETRY_ATTEMPTS):
        response = session.get(WIKI_API_URL, params=params, timeout=30)
        if response.status_code == 429:
            time.sleep(min(60.0, 2.0 * (2**attempt)))
            continue
        response.raise_for_status()
        payload = response.json()
        error = payload.get("error") if isinstance(payload, dict) else None
        if isinstance(error, dict) and error.get("code") == "ratelimited":
            time.sleep(min(60.0, 2.0 * (2**attempt)))
            continue
        time.sleep(REQUEST_DELAY_SECONDS)
        return payload
    raise RuntimeError("Wiki API rate-limited too many times for params: {0}".format(params))


def get_wikitext_for_title(session: requests.Session, title: str) -> tuple[str, str] | None:
    payload = request_api(
        session,
        {"action": "parse", "page": title, "prop": "wikitext", "redirects": 1, "format": "json"},
    )
    parse = payload.get("parse", {})
    resolved_title = str(parse.get("title", "")).strip()
    wikitext = str(parse.get("wikitext", {}).get("*", "")).strip()
    if not resolved_title or not wikitext:
        return None
    return resolved_title, wikitext


def search_title(session: requests.Session, query: str) -> str:
    payload = request_api(
        session,
        {"action": "opensearch", "search": query, "limit": 5, "namespace": 0, "format": "json"},
    )
    if not isinstance(payload, list) or len(payload) < 2 or not isinstance(payload[1], list):
        return ""
    normalized_query = normalize_lookup(query)
    for title in payload[1]:
        if normalize_lookup(str(title)) == normalized_query:
            return str(title)
    for title in payload[1]:
        title_value = str(title).strip()
        if title_value:
            return title_value
    return ""


def extract_template_block(wikitext: str, template_name: str) -> str:
    start_token = "{{" + template_name
    start_index = wikitext.find(start_token)
    if start_index < 0:
        return ""
    depth = 0
    index = start_index
    while index < len(wikitext) - 1:
        pair = wikitext[index : index + 2]
        if pair == "{{":
            depth += 1
            index += 2
            continue
        if pair == "}}":
            depth -= 1
            index += 2
            if depth == 0:
                return wikitext[start_index:index]
            continue
        index += 1
    return ""


def split_template_parameters(template_block: str) -> list[str]:
    if not template_block.startswith("{{"):
        return []

    parameters: list[str] = []
    current: list[str] = []
    depth = 0
    index = 0
    seen_first_separator = False

    while index < len(template_block):
        pair = template_block[index : index + 2]
        if pair == "{{":
            depth += 1
            if seen_first_separator:
                current.append(pair)
            index += 2
            continue
        if pair == "}}":
            if seen_first_separator and depth > 1:
                current.append(pair)
            depth = max(0, depth - 1)
            index += 2
            continue

        char = template_block[index]
        if depth == 1 and char == "|":
            if seen_first_separator:
                parameter = "".join(current).strip()
                if parameter:
                    parameters.append(parameter)
                current = []
            else:
                seen_first_separator = True
            index += 1
            continue

        if seen_first_separator:
            current.append(char)
        index += 1

    trailing = "".join(current).strip()
    if trailing:
        parameters.append(trailing)
    return parameters


def normalize_infobox_key(key_name: str) -> tuple[str, int]:
    for index, prefix in enumerate(INFOBOX_VARIANT_PREFIXES):
        if key_name.startswith(prefix):
            return key_name[len(prefix) :], index + 1
    return key_name, 0


def parse_infobox_fields(wikitext: str) -> dict[str, str]:
    for template_name in ("NewGunInfobox", "NewItemInfobox", "Item infobox", "Item Infobox"):
        block = extract_template_block(wikitext, template_name)
        if not block:
            continue

        fields: dict[str, str] = {}
        field_priorities: dict[str, int] = {}
        for parameter in split_template_parameters(block):
            if "=" not in parameter:
                continue
            key, value = parameter.split("=", 1)
            key_name = key.strip().lower().replace(" ", "")
            normalized_key_name, key_priority = normalize_infobox_key(key_name)
            if normalized_key_name in INFOBOX_FIELD_MAP:
                cleaned_value = normalize_whitespace(clean_wiki_markup(value.strip()))
                cleaned_value = re.sub(r"\}\}+$", "", cleaned_value).strip()
                field_name = INFOBOX_FIELD_MAP[normalized_key_name]
                existing_priority = field_priorities.get(field_name, 999)
                if cleaned_value and key_priority <= existing_priority:
                    fields[field_name] = cleaned_value
                    field_priorities[field_name] = key_priority
            elif normalized_key_name == "desc":
                fields["englishGameplaySummary"] = normalize_summary(clean_wiki_markup(value.strip()))
        return fields
    return {}


def parse_sections(wikitext: str) -> dict[str, list[str]]:
    sections: dict[str, list[str]] = {}
    current_heading = ""
    for raw_line in wikitext.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        line = raw_line.strip()
        if not line:
            continue
        heading_match = re.fullmatch(r"==+\s*(.+?)\s*==+", line)
        if heading_match:
            current_heading = normalize_heading(heading_match.group(1))
            sections.setdefault(current_heading, [])
            continue
        if not current_heading:
            continue
        if line.startswith("*"):
            bullet_text = clean_wiki_markup(line.lstrip("*").strip())
            bullet_text = normalize_whitespace(bullet_text)
            if bullet_text:
                sections.setdefault(current_heading, []).append(bullet_text)
    return sections


def normalize_heading(value: str) -> str:
    cleaned = clean_wiki_markup(value)
    cleaned = re.sub(r"\s+", " ", cleaned).strip()
    return cleaned.lower()


def normalize_summary(value: str) -> str:
    text = normalize_whitespace(value.replace("\n", " "))
    if not text or text.lower() in PLACEHOLDER_TEXT_VALUES:
        return ""
    return text


def extract_highlight(sections: dict[str, list[str]], heading_prefixes: tuple[str, ...], max_items: int, max_length: int) -> str:
    items: list[str] = []
    for heading, bullets in sections.items():
        if not any(heading.startswith(prefix) for prefix in heading_prefixes):
            continue
        for bullet in bullets:
            normalized = normalize_whitespace(bullet)
            if normalized:
                items.append(normalized)
            if len(items) >= max_items:
                break
        if len(items) >= max_items:
            break
    if not items:
        return ""
    return "; ".join(items)


def clean_wiki_markup(text: str) -> str:
    value = text
    replacements = [
        (r"<!--.*?-->", ""),
        (r"<br\s*/?>", "\n"),
        (r"</?div[^>]*>", ""),
        (r"</?span[^>]*>", ""),
        (r"</?small>", ""),
        (r"</?code>", ""),
        (r"</?ref[^>]*>", ""),
        (r"<ref[^>]*>.*?</ref>", ""),
        (r"</?gallery>", ""),
    ]
    for pattern, replacement in replacements:
        value = re.sub(pattern, replacement, value, flags=re.IGNORECASE | re.DOTALL)

    value = re.sub(r"\[\[(?:File|Image):[^\]]+\]\]", "", value, flags=re.IGNORECASE)
    value = re.sub(r"\[\[(?:[^|\]]+\|)?([^\]]+)\]\]", r"\1", value)
    value = re.sub(r"\[https?://[^\s\]]+\s+([^\]]+)\]", r"\1", value)
    value = re.sub(r"\[https?://[^\]]+\]", "", value)
    value = re.sub(
        r"(?:\{\{quality\|([^}]+)\}\}\s*){2,}",
        lambda match: "/".join(
            part
            for part in re.findall(r"\{\{quality\|([^}]+)\}\}", match.group(0), flags=re.IGNORECASE)
            if part
        ),
        value,
        flags=re.IGNORECASE,
    )
    value = re.sub(r"\{\{quality\|([^}]+)\}\}", r"\1", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{rechargedmg\|([^}]+)\}\}", r"\1 damage", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{rechargeroom\|([^}]+)\}\}", r"\1 rooms", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{rechargetime\|([^}]+)\}\}", r"\1 sec", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{statuseffect\|([^|}]+)(?:\|[^}]*)?\}\}", r"\1", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{synergy\|([^|}]+)(?:\|[^}]*)?\}\}", r"\1", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{horizitem\|([^|}]+)(?:\|[^}]*)?\}\}", r"\1", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{hover\|([^|}]+)\|[^}]+\}\}", r"\1", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{multishotdmg\|([^|}]+)\|([^|}]+)\|([^|}]+)(?:\|[^}]*)?\}\}", r"\3", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{hc\}\}", "HC", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{coin\}\}", "coin", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{infinity\}\}", "Infinity", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{check\|[^}]+\}\}", "", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{game\|[^}]+\}\}", "", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{bug\}\}", "Bug:", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{pbug\}\}", "Bug:", value, flags=re.IGNORECASE)
    value = re.sub(r"\{\{[^{}]*\|([^{}|]+)\}\}", r"\1", value)
    value = re.sub(r"\{\{[^{}]+\}\}", "", value)
    value = re.sub(r"<[^>]+>", "", value, flags=re.IGNORECASE)
    value = value.replace("{{", "").replace("}}", "")
    value = value.replace("'''", "").replace("''", "")
    value = value.replace("&nbsp;", " ")
    return value


def normalize_whitespace(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def is_placeholder_text(value: str) -> bool:
    normalized = normalize_whitespace(value).strip(" .;:,").lower()
    return not normalized or normalized in PLACEHOLDER_TEXT_VALUES


def has_truncated_cached_text(fields: dict[str, Any]) -> bool:
    for field_name in (
        "englishGameplaySummary",
        "englishEffectHighlights",
        "englishSynergyHighlights",
        "englishUsageNotes",
    ):
        value = normalize_whitespace(str(fields.get(field_name, "")))
        if value.endswith("..."):
            return True
    return False


def format_quality_display(value: str) -> str:
    normalized = normalize_whitespace(value).replace(" ", "")
    if not normalized:
        return ""

    compact_match = re.fullmatch(r"([DCBASN]){2,}", normalized, flags=re.IGNORECASE)
    if compact_match:
        unique_tokens: list[str] = []
        seen: set[str] = set()
        for token in normalized.upper():
            if token in seen:
                continue
            seen.add(token)
            unique_tokens.append(token)
        return "/".join(unique_tokens) + " (multi-tier)"

    tokens = [token for token in normalized.split("/") if token]
    if len(tokens) <= 1:
        return normalized

    deduped_tokens: list[str] = []
    seen: set[str] = set()
    for token in tokens:
        upper_token = token.upper()
        if upper_token in seen:
            continue
        seen.add(upper_token)
        deduped_tokens.append(upper_token)

    return "/".join(deduped_tokens) + " (multi-tier)"


def sanitize_enum_field_value(value: str) -> str:
    normalized = normalize_whitespace(value)
    if not normalized:
        return ""

    normalized = re.sub(r"\|+\s*[A-Za-z][A-Za-z0-9_]*\s*=.*$", "", normalized).strip()
    normalized = re.sub(r"\s+\(multi-tier\)\s+\(multi-tier\)$", " (multi-tier)", normalized, flags=re.IGNORECASE)
    return normalized.strip("| ").strip()


def normalize_field_value(field_name: str, value: str) -> str:
    normalized = normalize_whitespace(clean_wiki_markup(value))
    normalized = normalized.strip("|").strip()
    normalized = re.sub(r"\}\}+$", "", normalized).strip()
    normalized = re.sub(r"\s+\.", ".", normalized)
    normalized = re.sub(r"\s+,", ",", normalized)
    if field_name in {"quality", "pickupType", "gunClass"}:
        normalized = normalized.replace("}}", "").strip()
        normalized = sanitize_enum_field_value(normalized)
    if field_name == "quality":
        normalized = format_quality_display(normalized)
    if field_name == "unlock":
        normalized = normalized.strip(" ,;:")
    return normalized


def clean_entry_fields(fields: dict[str, str]) -> dict[str, str]:
    cleaned: dict[str, str] = {}
    for key, value in fields.items():
        cleaned[key] = normalize_field_value(key, value)
    return cleaned


def apply_catalog_fallbacks(entry: dict[str, Any], fields: dict[str, str]) -> dict[str, str]:
    enriched = dict(fields)

    catalog_pickup_type = get_catalog_pickup_type(entry)
    if catalog_pickup_type:
        enriched["pickupType"] = catalog_pickup_type

    if not enriched.get("quality", ""):
        catalog_quality = get_catalog_quality_fallback(entry)
        if catalog_quality:
            enriched["quality"] = catalog_quality
        elif get_catalog_pickup_type(entry):
            enriched["quality"] = "Not listed"

    summary = str(enriched.get("englishGameplaySummary", "")).strip()
    if is_placeholder_text(summary):
        enriched["englishGameplaySummary"] = ""

    if not enriched.get("englishGameplaySummary", ""):
        effect_highlights = str(enriched.get("englishEffectHighlights", "")).strip()
        if effect_highlights and not is_placeholder_text(effect_highlights):
            first_highlight = effect_highlights.split(";", 1)[0].strip()
            enriched["englishGameplaySummary"] = normalize_summary(first_highlight)

    return enriched


def build_stat_groups(fields: dict[str, str]) -> list[dict[str, Any]]:
    groups: list[dict[str, Any]] = []
    for group_key, label_keys in STAT_GROUP_DEFINITIONS:
        stats: list[dict[str, str]] = []
        for label_key in label_keys:
            field_name = STAT_FIELD_BY_LABEL_KEY[label_key]
            value = str(fields.get(field_name, "")).strip()
            if not value:
                continue
            stats.append({"labelKey": label_key, "value": value})

        if stats:
            groups.append({"groupKey": group_key, "stats": stats})

    return groups


def build_entry_from_wikitext(entry: dict[str, Any], resolved_title: str, wikitext: str) -> GameplayEntry | None:
    infobox_fields = parse_infobox_fields(wikitext)
    sections = parse_sections(wikitext)

    english_summary = infobox_fields.get("englishGameplaySummary", "")
    if not english_summary:
        english_summary = extract_highlight(sections, ("effects",), 1, 260)

    fields = {
        "quality": infobox_fields.get("quality", ""),
        "pickupType": infobox_fields.get("pickupType", ""),
        "gunClass": infobox_fields.get("gunClass", ""),
        "dps": infobox_fields.get("dps", ""),
        "magazineSize": infobox_fields.get("magazineSize", ""),
        "ammoCapacity": infobox_fields.get("ammoCapacity", ""),
        "damage": infobox_fields.get("damage", ""),
        "fireRate": infobox_fields.get("fireRate", ""),
        "reloadTime": infobox_fields.get("reloadTime", ""),
        "shotSpeed": infobox_fields.get("shotSpeed", ""),
        "range": infobox_fields.get("range", ""),
        "force": infobox_fields.get("force", ""),
        "spread": infobox_fields.get("spread", ""),
        "duration": infobox_fields.get("duration", ""),
        "recharge": infobox_fields.get("recharge", ""),
        "sellPrice": infobox_fields.get("sellPrice", ""),
        "unlock": infobox_fields.get("unlock", ""),
        "englishGameplaySummary": english_summary,
        "englishEffectHighlights": extract_highlight(sections, ("effects",), 2, 320),
        "englishSynergyHighlights": extract_highlight(sections, ("synergies",), 2, 320),
        "englishUsageNotes": extract_highlight(sections, ("notes",), 2, 320),
    }

    if not any(value for value in fields.values()):
        return None

    return GameplayEntry(
        pickup_id=int(entry["pickupId"]),
        category=str(entry.get("category", "")),
        display_name=str(entry.get("displayName", "")).strip(),
        internal_name=str(entry.get("internalName", "")).strip(),
        wiki_key=resolved_title.replace(" ", "_"),
        fields=fields,
    )


def resolve_page(
    entry: dict[str, Any],
    session: requests.Session,
    page_cache: dict[str, dict[str, Any]],
) -> GameplayEntry | None:
    candidates = build_title_candidates(entry)
    for candidate in candidates:
        cache_key = normalize_lookup(candidate)
        cached = page_cache.get(cache_key)
        if cached and cached.get("title") and cached.get("entry") and not is_stale_cached_entry(entry, cached):
            return build_cached_entry(entry, cached)

        result = get_wikitext_for_title(session, candidate)
        if result is None:
            searched_title = search_title(session, candidate)
            result = get_wikitext_for_title(session, searched_title) if searched_title else None
        if result is None:
            page_cache[cache_key] = {}
            continue

        resolved_title, wikitext = result
        gameplay_entry = build_entry_from_wikitext(entry, resolved_title, wikitext)
        if gameplay_entry is None:
            page_cache[cache_key] = {}
            continue

        page_cache[cache_key] = {
            "title": resolved_title,
            "entry": gameplay_entry.fields,
        }
        return gameplay_entry
    return None


def build_cached_entry(entry: dict[str, Any], cached: dict[str, Any]) -> GameplayEntry | None:
    cached_fields = cached.get("entry")
    if not isinstance(cached_fields, dict):
        return None
    return GameplayEntry(
        pickup_id=int(entry["pickupId"]),
        category=str(entry.get("category", "")),
        display_name=str(entry.get("displayName", "")).strip(),
        internal_name=str(entry.get("internalName", "")).strip(),
        wiki_key=str(cached.get("title", "")).replace(" ", "_"),
        fields={key: str(value) for key, value in cached_fields.items()},
    )


def is_stale_cached_entry(entry: dict[str, Any], cached: dict[str, Any]) -> bool:
    cached_fields = cached.get("entry")
    if not isinstance(cached_fields, dict):
        return True

    if has_truncated_cached_text(cached_fields):
        return True

    category = str(entry.get("category", "")).strip().lower()
    pickup_type = str(cached_fields.get("pickupType", "")).strip()
    quality = str(cached_fields.get("quality", "")).strip()
    summary = str(cached_fields.get("englishGameplaySummary", "")).strip()

    if category in {"passive", "active"} and not pickup_type and not quality and not summary:
        return True
    if category == "gun" and not quality and not summary:
        return True
    return False


def should_take_sample(sample_count: int, taken_count: int) -> bool:
    return sample_count <= 0 or taken_count < sample_count


def build_sample_entries(
    catalog_entries: list[dict[str, Any]],
    session: requests.Session,
    page_cache: dict[str, dict[str, Any]],
    sample_count: int,
    cache_path: Path | None = None,
) -> tuple[list[GameplayEntry], list[int]]:
    sample_entries: list[GameplayEntry] = []
    unmatched_pickup_ids: list[int] = []

    for entry in catalog_entries:
        if not should_take_sample(sample_count, len(sample_entries)):
            continue
        gameplay_entry = resolve_page(entry, session, page_cache)
        if gameplay_entry is None:
            unmatched_pickup_ids.append(int(entry.get("pickupId", -1)))
            continue
        sample_entries.append(
            GameplayEntry(
                pickup_id=gameplay_entry.pickup_id,
                category=gameplay_entry.category,
                display_name=gameplay_entry.display_name,
                internal_name=gameplay_entry.internal_name,
                wiki_key=gameplay_entry.wiki_key,
                fields=apply_catalog_fallbacks(entry, clean_entry_fields(gameplay_entry.fields)),
            )
        )
        if cache_path is not None and len(sample_entries) % CACHE_SAVE_EVERY == 0:
            save_cache(cache_path, page_cache)
        if sample_count > 0 and len(sample_entries) >= sample_count:
            break

    return sample_entries, unmatched_pickup_ids


def write_output(
    path: Path,
    sample_entries: list[GameplayEntry],
    catalog_entry_count: int,
    unmatched_pickup_ids: list[int],
    sample_count: int,
) -> None:
    sorted_entries = sorted(sample_entries, key=lambda entry: entry.pickup_id)
    payload = {
        "generatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S"),
        "sourceWikiBaseUrl": "https://enterthegungeon.wiki.gg/wiki/",
        "catalogEntryCount": catalog_entry_count,
        "sampleLimit": sample_count if sample_count > 0 else None,
        "entryCount": len(sorted_entries),
        "unmatchedPickupCount": len(unmatched_pickup_ids),
        "entries": [
            {
                "pickupId": entry.pickup_id,
                "category": entry.category,
                "englishDisplayName": entry.display_name,
                "internalName": entry.internal_name,
                "wikiKey": entry.wiki_key,
                "quality": entry.fields.get("quality", ""),
                "pickupType": entry.fields.get("pickupType", ""),
                "statGroups": build_stat_groups(entry.fields),
                "unlock": entry.fields.get("unlock", ""),
                "englishGameplaySummary": entry.fields.get("englishGameplaySummary", ""),
                "englishEffectHighlights": entry.fields.get("englishEffectHighlights", ""),
                "englishSynergyHighlights": entry.fields.get("englishSynergyHighlights", ""),
                "englishUsageNotes": entry.fields.get("englishUsageNotes", ""),
            }
            for entry in sorted_entries
        ],
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def main() -> int:
    args = parse_args()
    catalog_path = Path(args.catalog)
    output_path = Path(args.output)
    cache_path = Path(args.cache)
    sample_count = max(0, int(args.sample_count))

    catalog_entries = load_catalog_entries(catalog_path)
    page_cache = load_cache(cache_path)
    session = make_session()

    sample_entries, unmatched_pickup_ids = build_sample_entries(
        catalog_entries,
        session,
        page_cache,
        sample_count,
        cache_path,
    )
    write_output(output_path, sample_entries, len(catalog_entries), unmatched_pickup_ids, sample_count)
    save_cache(cache_path, page_cache)

    print(
        "Generated {0} gameplay entries from {1} catalog entries. Unmatched pickups: {2}. Output: {3}".format(
            len(sample_entries),
            len(catalog_entries),
            len(unmatched_pickup_ids),
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
