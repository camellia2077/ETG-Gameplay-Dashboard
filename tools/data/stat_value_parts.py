import re
from typing import Any


PARENTHETICAL_PART_PATTERN = re.compile(r"(?P<value>[^\s()]+)\s*\((?P<label>[^()]+)\)")
BARE_PART_PATTERN = re.compile(r"^[A-Za-z0-9.+/%xX×-]+$")


def build_stat_parts(raw_value: Any) -> list[dict[str, str]]:
    """Convert a compound stat value into structured value/label parts.

    Only unambiguous numeric-or-token values followed by parenthetical labels
    are split. Other values remain one raw part so source information is not
    silently reinterpreted during the schema migration.
    """
    text = str(raw_value or "").strip()
    if not text:
        return []

    matches = list(PARENTHETICAL_PART_PATTERN.finditer(text))
    if not matches:
        return [{"value": text}]

    parts: list[dict[str, str]] = []
    cursor = 0
    for match in matches:
        gap = text[cursor:match.start()].strip()
        if gap:
            if not BARE_PART_PATTERN.fullmatch(gap):
                return [{"value": text}]
            parts.append({"value": gap})

        parts.append(
            {
                "value": match.group("value").strip(),
                "label": match.group("label").strip(),
            }
        )
        cursor = match.end()

    tail = text[cursor:].strip()
    if tail:
        if not BARE_PART_PATTERN.fullmatch(tail):
            return [{"value": text}]
        parts.append({"value": tail})

    return parts or [{"value": text}]
