"""app/main.py — Application entry point."""
from __future__ import annotations

import sys
from pathlib import Path

# Ensure project root is on the path when running as a script
sys.path.insert(0, str(Path(__file__).parent.parent))

from PyQt6.QtWidgets import QApplication
from PyQt6.QtGui import QFont

from app import database as db
from app.config import DB_PATH, RULES_DB_PATH, APP_NAME
from app.ui.theme import qss
from app.ui.main_window import MainWindow
from rule_engine import RuleEngine
from rule_engine import db as rule_db


def main():
    # ── Initialise databases ───────────────────────────────────────────────
    db.initialise(DB_PATH)
    rule_db.initialise(RULES_DB_PATH)

    # ── Migrate to simplified category system (idempotent) ─────────────────
    from app.database import migrate_categories
    from rule_engine.db import migrate_rule_categories
    migrate_rule_categories(RULES_DB_PATH)   # migrate rules + reset old cats
    rule_db.initialise(RULES_DB_PATH)        # re-seed with 12 new categories
    migrate_categories(DB_PATH)              # migrate transaction categories

    # ── Pre-warm the rule engine ───────────────────────────────────────────
    from app.services.import_service import get_engine
    get_engine()   # loads rules into memory

    # ── Launch Qt application ──────────────────────────────────────────────
    app = QApplication(sys.argv)
    app.setApplicationName(APP_NAME)
    app.setStyle("Fusion")   # consistent cross-platform base

    # Apply global font
    font = QFont("Segoe UI", 10)
    app.setFont(font)

    # Apply dark stylesheet
    app.setStyleSheet(qss())

    # Show window
    window = MainWindow()
    window.show()

    sys.exit(app.exec())


if __name__ == "__main__":
    main()
