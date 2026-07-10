from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from pathlib import Path

from gameplay_translation_workflow import DEFAULT_WORK_FILE_PATH, REPO_ROOT, load_json


DEFAULT_INPUT_PATH = DEFAULT_WORK_FILE_PATH
KNOWN_STAT_VALUE_LABEL_MAPPINGS = {
    "Arrow": "箭矢",
    "Automatic": "自动",
    "Ball": "主弹",
    "Beam": "光束",
    "Beam & Slash": "光束与斩击",
    "Beam OR Slash Only": "仅光束或仅斩击",
    "Bees": "蜜蜂",
    "Blank": "空响弹",
    "Bullet": "子弹",
    "Bullets": "子弹",
    "Both": "双模式",
    "Charged": "蓄力",
    "Cheese ball": "芝士球",
    "Cheese wheel": "芝士轮",
    "Combined": "合计",
    "Crystal": "晶体",
    "Crystals": "晶体",
    "Dart": "飞镖",
    "Double Reload": "双重装填",
    "Duck": "鸭子",
    "Electric Beam": "电流光束",
    "Energy Ball": "能量球",
    "Energy ball": "能量球",
    "Explosion": "爆炸",
    "Explosions": "爆炸",
    "Final Shot": "最后一发",
    "Fire": "火焰",
    "Fireball": "火球",
    "First Stage": "第一阶段",
    "Genie": "灯神",
    "Ghost": "幽灵",
    "Ghost Shots": "幽灵子弹",
    "Grappling Hook": "抓钩",
    "Grenade Launcher": "榴弹发射器",
    "Gun": "枪体",
    "Guns": "枪体",
    "Hammer": "铁锤",
    "Grenade Impact": "榴弹直击",
    "Impact": "撞击",
    "Including Explosions": "包含爆炸伤害",
    "Ice Bullets": "冰霜子弹",
    "Large": "大型",
    "Laser": "激光",
    "Leaves": "叶片",
    "Level 1": "1级",
    "Level 2": "2级",
    "Level 3": "3级",
    "Light": "光束",
    "Light Beams": "光束",
    "Machine Gun": "机枪",
    "Manual": "手动",
    "Medium": "中型",
    "N/A": "不适用",
    "Nail": "钉子",
    "No Swing": "不挥动",
    "Oil": "油液",
    "Pinecone": "松果",
    "Pistol": "手枪",
    "Pointy cheese": "尖角芝士",
    "Projectile": "弹丸",
    "Regular": "常规",
    "Regular & Poison": "常规与毒液",
    "Red Beam": "红色光束",
    "Rifle": "步枪",
    "RPG": "火箭推进式榴弹",
    "Second Stage": "第二阶段",
    "Single": "单发",
    "Single Reload": "单次装填",
    "Skull": "骷髅",
    "Slash": "斩击",
    "Slug": "独头弹",
    "Small": "小型",
    "Small Crystals": "小晶体",
    "Rocket": "火箭",
    "Shotguns": "霰弹",
    "Slow Bolts": "缓速弹束",
    "Shots": "子弹",
    "Small Explosion": "小爆炸",
    "Small Rocket": "小火箭",
    "Split": "分裂后",
    "Split Shots": "分裂弹",
    "Staff": "法杖",
    "Steak": "牛排",
    "Super": "超级",
    "Third Stage": "第三阶段",
    "Tiger": "老虎",
    "Total": "总计",
    "Triple": "三连发",
    "Triple Bolts": "三重弹束",
    "Uncharged": "未蓄力",
    "both": "双模式",
    "bullet": "子弹",
    "charged": "蓄力",
    "explosion": "爆炸",
    "impact": "撞击",
    "pistol": "手枪",
    "shotgun": "霰弹枪",
    "triple": "三连发",
    "uncharged": "未蓄力",
}
FRAGMENT_TOKEN_MAPPINGS = {
    "Beadie": "小眼怪",
    "Boss": "Boss",
    "Chains": "锁链",
    "Clip": "整个弹匣",
    "Fire Aura": "火焰光环",
    "ammo": "弹药",
    "blades": "刀刃",
    "bullets": "子弹",
    "crystals": "晶体",
    "electricity": "连锁电流",
    "hits": "命中",
    "invisibility": "隐身",
    "linking electricity": "连锁电流",
    "notes": "音符束",
    "projectile": "弹丸",
    "projectiles": "弹丸",
    "rebound": "反弹",
    "recharge": "充能",
    "small crystals": "小晶体",
    "souls": "灵魂",
    "split bullets": "分裂子弹",
    "split projectiles": "分裂弹丸",
    "stray bullets": "偏离的子弹",
    "super punches": "超级拳击",
}
EXACT_STAT_VALUE_PHRASE_MAPPINGS = {
    "Average": "平均",
    "Average damage": "平均伤害",
    "After 1s": "1秒后",
    "After 3s": "3秒后",
    "After Full Clip": "打完整个弹匣后",
    "Before split": "分裂前",
    "After split": "分裂后",
    "Critical Hit": "暴击",
    "Regular Hit": "普通命中",
    "Normal": "常规",
    "all 6 hits": "全部6次命中",
    "All Leaves Hit": "所有叶片命中",
    "all three hits rare": "三次全中",
    "at 0 reloads": "0次装填时",
    "bounced hit": "弹跳命中",
    "both modes": "两种模式合计",
    "depends on firing speed of player": "取决于玩家射速",
    "Egg": "蛋",
    "extra hits": "额外命中",
    "final shot": "最后一发",
    "full health": "满血时",
    "Grenade Launcher/Bullet Bore": "榴弹发射器/钻头枪",
    "hit once normal": "普通单次命中",
    "hit seven times rare": "罕见七次命中",
    "hit then bounced hit": "命中后再弹跳命中",
    "hit thrice": "命中三次",
    "hit twice": "命中两次",
    "if all bullets hit": "若全部子弹命中",
    "ignoring ammo": "不计弹药限制",
    "ignoring recharge": "不计充能时间",
    "Ignoring Fire Aura": "不计火焰光环",
    "initial": "初始命中",
    "line piece": "直线段",
    "maximum hits": "最大命中数",
    "no extras": "无额外弹体时",
    "No Leaves Hit": "没有叶片命中",
    "no souls collected": "未收集灵魂时",
    "normal hit": "普通命中",
    "no Fireball": "不含火球",
    "not counting the Beadie": "不计小眼怪",
    "on 1st reload": "第1次装填时",
    "on extra hits": "额外命中时",
    "Pots": "罐子",
    "Seekers": "追踪弹",
    "single black hole": "单个黑洞",
    "single hit": "单次命中",
    "slow": "缓速模式",
    "souls collected non-boss": "对非Boss收集灵魂时",
    "souls collected on bosses": "对Boss收集灵魂时",
    "three hits": "命中三次",
    "to bosses": "对Boss时",
    "twice hit": "命中两次",
    "Varies heavily": "浮动很大",
    "with 3 extras": "有3个额外弹体时",
    "after piercing": "穿透后",
    "explosions": "爆炸伤害",
}
PARENTHETICAL_FRAGMENT_PATTERN = re.compile(r"\(([A-Za-z][A-Za-z0-9 &+/\-]+)\)")
COLON_FRAGMENT_PATTERN = re.compile(r"\b([A-Za-z][A-Za-z0-9 &+/\-]+):")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Scan stats[*].value strings for structured English labels that should be covered by valueMappings in the zh-CN gameplay work file."
    )
    parser.add_argument(
        "--input",
        default=str(DEFAULT_INPUT_PATH),
        help="Path to defaults/catalog/legacy/RandomLoadout.pickup-gameplay.zh-CN.work.json.",
    )
    parser.add_argument(
        "--output",
        default="",
        help="Optional JSON report path. Defaults to temp/pickup-gameplay.stat-value-label-report.<input-name>.json.",
    )
    return parser.parse_args()


def default_output_path(input_path: Path) -> Path:
    safe_name = input_path.stem.replace(" ", "_")
    return REPO_ROOT / "temp" / "pickup-gameplay.stat-value-label-report.{0}.json".format(safe_name)


def normalize_fragment(fragment: str) -> str:
    return " ".join(fragment.strip().split())


def translate_fragment_token(token: str) -> str | None:
    normalized = normalize_fragment(token)
    if normalized in KNOWN_STAT_VALUE_LABEL_MAPPINGS:
        return KNOWN_STAT_VALUE_LABEL_MAPPINGS[normalized]
    if normalized in FRAGMENT_TOKEN_MAPPINGS:
        return FRAGMENT_TOKEN_MAPPINGS[normalized]
    lowered = normalized.lower()
    if lowered in FRAGMENT_TOKEN_MAPPINGS:
        return FRAGMENT_TOKEN_MAPPINGS[lowered]
    return None


def resolve_structured_fragment(fragment: str) -> str | None:
    normalized = normalize_fragment(fragment)
    direct = translate_fragment_token(normalized)
    if direct:
        return direct

    if normalized in EXACT_STAT_VALUE_PHRASE_MAPPINGS:
        return EXACT_STAT_VALUE_PHRASE_MAPPINGS[normalized]

    pattern_handlers: list[tuple[re.Pattern[str], callable]] = [
        (re.compile(r"^excluding (.+)$", re.IGNORECASE), lambda m: "不计{0}".format(resolve_structured_fragment(m.group(1)) or "")),
        (re.compile(r"^not counting (.+)$", re.IGNORECASE), lambda m: "不计{0}".format(resolve_structured_fragment(m.group(1)) or "")),
        (re.compile(r"^with (.+)$", re.IGNORECASE), lambda m: "有{0}时".format(resolve_structured_fragment(m.group(1)) or "")),
        (re.compile(r"^without (.+)$", re.IGNORECASE), lambda m: "无{0}时".format(resolve_structured_fragment(m.group(1)) or "")),
        (re.compile(r"^assuming (.+)$", re.IGNORECASE), lambda m: "假设{0}".format(resolve_condition_clause(m.group(1)))),
    ]
    for pattern, builder in pattern_handlers:
        match = pattern.match(normalized)
        if not match:
            continue
        built = builder(match)
        if built and not built.endswith("计"):
            return built

    return None


def resolve_condition_clause(clause: str) -> str:
    normalized = normalize_fragment(clause)
    phrase_mappings = {
        "an enemy is only hit by one stream of notes": "敌人只被一束音符命中",
        "blades do not rebound to hit enemies multiple times": "刀刃不会反弹并多次命中敌人",
        "invisibility at start of magazine": "弹匣开始时处于隐身状态",
        "projectile impacts enemy before exploding": "弹丸在爆炸前先直击敌人",
    }
    if normalized in phrase_mappings:
        return phrase_mappings[normalized]
    translated = translate_fragment_token(normalized)
    if translated:
        return translated
    return normalized


def collect_structured_fragments(value: str) -> list[dict[str, str]]:
    matches: list[dict[str, str]] = []
    for pattern_name, pattern in (
        ("parenthetical", PARENTHETICAL_FRAGMENT_PATTERN),
        ("colon", COLON_FRAGMENT_PATTERN),
    ):
        for match in pattern.finditer(value):
            fragment = normalize_fragment(match.group(1))
            if not fragment:
                continue
            matches.append(
                {
                    "kind": pattern_name,
                    "fragment": fragment,
                }
            )

    deduped: list[dict[str, str]] = []
    seen: set[tuple[str, str]] = set()
    for match in matches:
        key = (match["kind"], match["fragment"])
        if key in seen:
            continue
        seen.add(key)
        deduped.append(match)
    return deduped


def build_issue(entry: dict, stat: dict, group_key: str, matches: list[dict[str, str]]) -> dict:
    return {
        "pickupId": entry.get("pickupId"),
        "englishDisplayName": entry.get("englishDisplayName", ""),
        "groupKey": group_key,
        "labelKey": stat.get("labelKey", ""),
        "value": stat.get("value", ""),
        "matches": matches,
    }


def main() -> int:
    args = parse_args()
    input_path = Path(args.input)
    output_path = Path(args.output) if args.output.strip() else default_output_path(input_path)

    payload = load_json(input_path)
    entries = payload.get("entries", [])
    if not isinstance(entries, list):
        raise ValueError("Input file did not contain an 'entries' array: {0}".format(input_path))

    known_issues: list[dict] = []
    unknown_issues: list[dict] = []
    known_counter: Counter[str] = Counter()
    unknown_counter: Counter[str] = Counter()

    for entry in entries:
        if not isinstance(entry, dict):
            continue
        stat_groups = entry.get("statGroups", [])
        if not isinstance(stat_groups, list):
            continue

        for stat_group in stat_groups:
            if not isinstance(stat_group, dict):
                continue
            group_key = str(stat_group.get("groupKey", "")).strip()
            stats = stat_group.get("stats", [])
            if not isinstance(stats, list):
                continue

            for stat in stats:
                if not isinstance(stat, dict):
                    continue
                value = str(stat.get("value", "")).strip()
                if not value:
                    continue

                fragments = collect_structured_fragments(value)
                if not fragments:
                    continue

                known_matches: list[dict[str, str]] = []
                unknown_matches: list[dict[str, str]] = []
                for fragment_info in fragments:
                    fragment = fragment_info["fragment"]
                    resolved_fragment = resolve_structured_fragment(fragment)
                    if resolved_fragment:
                        known_counter[fragment] += 1
                        known_matches.append(
                            {
                                **fragment_info,
                                "suggestedChinese": resolved_fragment,
                            }
                        )
                    else:
                        unknown_counter[fragment] += 1
                        unknown_matches.append(fragment_info)

                if known_matches:
                    known_issues.append(build_issue(entry, stat, group_key, known_matches))
                if unknown_matches:
                    unknown_issues.append(build_issue(entry, stat, group_key, unknown_matches))

    report = {
        "inputFile": input_path.as_posix(),
        "knownMappingCount": len(KNOWN_STAT_VALUE_LABEL_MAPPINGS),
        "knownIssueCount": len(known_issues),
        "unknownIssueCount": len(unknown_issues),
        "knownIssues": known_issues,
        "unknownIssues": unknown_issues,
        "knownFragmentCounts": dict(sorted(known_counter.items(), key=lambda item: (-item[1], item[0]))),
        "unknownFragmentCounts": dict(sorted(unknown_counter.items(), key=lambda item: (-item[1], item[0]))),
        "notes": [
            "This scanner only looks at entries[*].statGroups[*].stats[*].value.",
            "stats[*].value is expected to remain aligned with the English source data.",
            "Known issues are structured English stat-value labels that already have a safe Chinese mapping and should be present in valueMappings.",
            "Unknown issues are structured English labels that still need mapping decisions before they should be added to valueMappings.",
        ],
    }
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(
        "Scanned {0}. Found {1} known stat-value mapping candidate(s) and {2} unknown candidate(s). Report: {3}".format(
            input_path,
            len(known_issues),
            len(unknown_issues),
            output_path,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
