from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from gameplay_translation_workflow import REPO_ROOT, TRANSLATABLE_FIELD_PAIRS, load_json


DEFAULT_INPUT_PATH = REPO_ROOT / "defaults" / "catalog" / "legacy" / "EtgGameplayDashboard.pickup-gameplay.zh-CN.work.json"
WRAPPED_ENGLISH_PATTERN = re.compile(
    r"[《》〈〉「」『』“”‘’·\u4e00-\u9fff0-9A-Za-z0-9 _.\-:：,，、]{1,120}（[A-Za-zÀ-ÖØ-öø-ÿĀ-ž0-9'\"“”&_.:+,\- !?]+）"
)
DOUBLE_QUOTED_PATTERN = re.compile(r'"[^"\r\n]+"')

CONTROL_TERM_SPECS = [
    {
        "term": "Shift",
        "pattern": re.compile(r"\bShift\b", re.IGNORECASE),
        "suggestion": "切换键（Shift）",
    },
    {
        "term": "Ctrl",
        "pattern": re.compile(r"\bCtrl\b", re.IGNORECASE),
        "suggestion": "控制键（Ctrl）",
    },
    {
        "term": "Alt",
        "pattern": re.compile(r"\bAlt\b", re.IGNORECASE),
        "suggestion": "功能键（Alt）",
    },
    {
        "term": "Tab",
        "pattern": re.compile(r"\bTab\b", re.IGNORECASE),
        "suggestion": "制表键（Tab）",
    },
    {
        "term": "Space",
        "pattern": re.compile(r"\bSpace\b", re.IGNORECASE),
        "suggestion": "空格键（Space）",
    },
    {
        "term": "Enter",
        "pattern": re.compile(r"\bEnter\b", re.IGNORECASE),
        "suggestion": "回车键（Enter）",
    },
    {
        "term": "Esc",
        "pattern": re.compile(r"\bEsc\b", re.IGNORECASE),
        "suggestion": "退出键（Esc）",
    },
    {
        "term": "Escape",
        "pattern": re.compile(r"\bEscape\b", re.IGNORECASE),
        "suggestion": "退出键（Escape）",
    },
    {
        "term": "D-pad",
        "pattern": re.compile(r"\bD-pad\b", re.IGNORECASE),
        "suggestion": "十字键（D-pad）",
    },
    {
        "term": "UI",
        "pattern": re.compile(r"\bUI\b"),
        "suggestion": "界面（UI）",
    },
    {
        "term": "HUD",
        "pattern": re.compile(r"\bHUD\b"),
        "suggestion": "抬头显示（HUD）",
    },
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan Chinese gameplay text for raw control/UI/key-name English terms that should usually be localized or wrapped."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to a gameplay translation batch JSON or the zh-CN gameplay work JSON.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/ui-control-term-report.<input-name>.json.",
    )
    return parser.parse_args()


def default_output_path(input_path: Path) -> Path:
    safe_name = input_path.stem.replace(" ", "_")
    return REPO_ROOT / "temp" / "{0}.ui-control-term-report.json".format(safe_name)


def build_ignored_spans(text: str) -> list[tuple[int, int]]:
    spans: list[tuple[int, int]] = []
    for pattern in (WRAPPED_ENGLISH_PATTERN, DOUBLE_QUOTED_PATTERN):
        spans.extend((match.start(), match.end()) for match in pattern.finditer(text))
    return spans


def is_inside_ignored_spans(start: int, end: int, spans: list[tuple[int, int]]) -> bool:
    for span_start, span_end in spans:
        if start >= span_start and end <= span_end:
            return True
    return False


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
            chinese_text = str(entry.get(chinese_key, "")).strip()
            if not chinese_text:
                continue

            ignored_spans = build_ignored_spans(chinese_text)
            matches: list[dict[str, str]] = []
            seen_terms: set[str] = set()
            for spec in CONTROL_TERM_SPECS:
                for match in spec["pattern"].finditer(chinese_text):
                    if is_inside_ignored_spans(match.start(), match.end(), ignored_spans):
                        continue
                    if spec["term"] in seen_terms:
                        continue
                    seen_terms.add(spec["term"])
                    matches.append(
                        {
                            "term": spec["term"],
                            "matchedText": match.group(0),
                            "suggestedRewrite": spec["suggestion"],
                        }
                    )

            if not matches:
                continue

            issues.append(
                {
                    "pickupId": entry.get("pickupId"),
                    "englishDisplayName": entry.get("englishDisplayName", ""),
                    "field": chinese_key,
                    "sourceField": english_key,
                    "matches": matches,
                    "chineseText": chinese_text,
                    "englishText": str(entry.get(english_key, "")).strip(),
                }
            )

    report = {
        "inputFile": input_path.as_posix(),
        "issueCount": len(issues),
        "issues": issues,
        "notes": [
            "This scanner only looks at the four translatable Chinese gameplay fields.",
            "It flags raw English control/UI/key-name terms such as Shift, D-pad, UI, and HUD when they appear outside 中文（English） formatting.",
            "Use natural Chinese wording when possible, or wrap the English control term as 中文（English） if keeping the exact key name helps clarity.",
        ],
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(
        "Scanned {0}. Found {1} UI/control-term issue(s). Report: {2}".format(
            input_path,
            len(issues),
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
