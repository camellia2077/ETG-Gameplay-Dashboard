from __future__ import annotations

import runpy
from pathlib import Path


if __name__ == "__main__":
    runpy.run_path(str(Path(__file__).resolve().parent / "logs" / "extract_etg_gameplay_dashboard_log.py"), run_name="__main__")
