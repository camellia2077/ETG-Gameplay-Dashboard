from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
SCRIPT_NAME_MAP = {
    "translate": "run_pickup_gameplay_translate_phase.py",
    "postcheck": "run_pickup_gameplay_postcheck_phase.py",
    "export": "export_pickup_gameplay_translation_batch.py",
    "check": "check_pickup_gameplay_translation_batch.py",
    "scan-quotes": "scan_pickup_gameplay_quote_preservation.py",
    "scan-proper": "scan_pickup_gameplay_proper_noun_first_mentions.py",
    "scan-ui-controls": "scan_pickup_gameplay_ui_control_terms.py",
    "scan-residue": "scan_pickup_gameplay_source_residue.py",
    "scan-stat-values": "scan_pickup_gameplay_stat_value_labels.py",
    "scan-stat-source-drift": "scan_pickup_gameplay_stat_value_source_drift.py",
    "normalize": "normalize_pickup_gameplay_item_names.py",
    "localize-stat-values": "localize_pickup_gameplay_stat_value_labels.py",
    "restore-stat-values": "restore_pickup_gameplay_stat_values_from_english.py",
    "apply": "apply_pickup_gameplay_translation_batch.py",
}


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="python .\\tools\\translate\\new\\main.py",
        description="Unified entrypoint for pickup gameplay translation tooling.",
        epilog=(
            "Examples:\n"
            "  python .\\tools\\translate\\new\\main.py translate --start-id 33 --end-id 43 --include-complete --summary\n"
            "  python .\\tools\\translate\\new\\main.py translate --count 5 --only-missing --summary\n"
            "  python .\\tools\\translate\\new\\main.py translate --input .\\temp\\pickup-gameplay-translation-batches\\pickup-gameplay.zh-CN.0033-0043.check.json --normalize --summary\n"
            "  python .\\tools\\translate\\new\\main.py postcheck --input .\\temp\\pickup-gameplay-translation-batches\\pickup-gameplay.zh-CN.0033-0043.check.json --summary\n"
            "  python .\\tools\\translate\\new\\main.py postcheck --input .\\temp\\pickup-gameplay-translation-batches\\pickup-gameplay.zh-CN.0033-0043.check.json --apply --summary\n"
            "\n"
            "Low-level tools:\n"
            "  python .\\tools\\translate\\new\\main.py check --start-id 33 --end-id 43 --include-complete\n"
            "  python .\\tools\\translate\\new\\main.py export --start-id 66 --end-id 76\n"
            "  python .\\tools\\translate\\new\\main.py scan-proper --input .\\temp\\pickup-gameplay-translation-batches\\pickup-gameplay.zh-CN.0033-0043.check.json\n"
            "  python .\\tools\\translate\\new\\main.py scan-ui-controls --input .\\temp\\pickup-gameplay-translation-batches\\pickup-gameplay.zh-CN.0033-0043.check.json\n"
            "  python .\\tools\\translate\\new\\main.py scan-residue --input .\\temp\\pickup-gameplay-translation-batches\\pickup-gameplay.zh-CN.0033-0043.check.json\n"
            "  python .\\tools\\translate\\new\\main.py scan-stat-values\n"
            "  python .\\tools\\translate\\new\\main.py scan-stat-source-drift\n"
            "  python .\\tools\\translate\\new\\main.py localize-stat-values\n"
            "  python .\\tools\\translate\\new\\main.py restore-stat-values"
        ),
        formatter_class=argparse.RawTextHelpFormatter,
    )
    subparsers = parser.add_subparsers(dest="command")

    subcommand_help = {
        "translate": "High-level translation phase workflow: export/check a batch, then optionally normalize and re-check.",
        "postcheck": "High-level post-translation workflow: automated checks first, then optional apply after manual review.",
        "export": "Export a translation batch for a pickupId range.",
        "check": "Run the range-level translation validation workflow.",
        "scan-quotes": "Scan for missing quoted English fragments.",
        "scan-proper": "Scan for unwrapped foreign proper nouns.",
        "scan-ui-controls": "Scan for raw English control/UI/key-name terms in Chinese text.",
        "scan-residue": "Scan for HTML/wiki/source-residue pollution in English or Chinese fields.",
        "scan-stat-values": "Scan stats[*].parts labels for structured English label coverage/mapping guidance.",
        "scan-stat-source-drift": "Scan whether zh-CN stats[*].parts has drifted away from the English source file.",
        "normalize": "Replace confirmed in-game English item names with canonical Chinese names.",
        "localize-stat-values": "Sync known structured English stat-value label mappings into valueMappings.",
        "restore-stat-values": "Restore stats[*].parts from the English source file.",
        "apply": "Apply a translated batch back into the zh-CN work file.",
    }

    for command, help_text in subcommand_help.items():
        subparsers.add_parser(command, help=help_text, add_help=False)

    return parser


def dispatch_to_script(command: str, passthrough_args: list[str]) -> int:
    script_name = SCRIPT_NAME_MAP[command]
    script_path = SCRIPT_DIR / script_name
    completed = subprocess.run(
        [sys.executable, str(script_path)] + passthrough_args,
        text=True,
        encoding="utf-8",
        check=False,
    )
    return completed.returncode


def main() -> int:
    parser = build_parser()
    args, passthrough_args = parser.parse_known_args()

    if not args.command:
        parser.print_help()
        return 0

    return dispatch_to_script(args.command, passthrough_args)


if __name__ == "__main__":
    raise SystemExit(main())
