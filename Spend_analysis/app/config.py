"""app/config.py — Application paths and global config."""
from __future__ import annotations
import os
from pathlib import Path

APP_NAME    = "Spend Analysis"
APP_VERSION = "1.0.0"

# Store data in %APPDATA%\SpendAnalysis on Windows
_APPDATA = Path(os.environ.get("APPDATA", Path.home())) / "SpendAnalysis"
_APPDATA.mkdir(parents=True, exist_ok=True)

DB_PATH        = _APPDATA / "spend_analysis.db"
RULES_DB_PATH  = _APPDATA / "rules.db"
EXPORTS_DIR    = _APPDATA / "exports"
EXPORTS_DIR.mkdir(exist_ok=True)
