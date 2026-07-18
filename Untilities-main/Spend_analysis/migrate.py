"""
migrate.py
==========
One-time migration: inserts any missing categories and built-in rules into an
existing Spend Analysis database without touching existing data.

Safe to run multiple times — uses INSERT OR IGNORE throughout.

Usage:
    python migrate.py
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

from app.config import RULES_DB_PATH
from rule_engine import db as rule_db
from rule_engine.defaults import DEFAULT_CATEGORIES, BUILTIN_RULES

def migrate():
    print("=" * 60)
    print("  Spend Analysis — Category & Rule Migration")
    print("=" * 60)
    print(f"  Database: {RULES_DB_PATH}")
    print()

    # ── Snapshot before ─────────────────────────────────────────
    with rule_db._connect(RULES_DB_PATH) as conn:
        cats_before  = conn.execute("SELECT COUNT(*) FROM categories").fetchone()[0]
        rules_before = conn.execute(
            "SELECT COUNT(*) FROM rules WHERE is_user_rule=0").fetchone()[0]

    # ── Run additive seed (INSERT OR IGNORE) ──────────────────
    rule_db.initialise(RULES_DB_PATH)  # creates schema if missing, then seeds

    # ── Snapshot after ───────────────────────────────────────────
    with rule_db._connect(RULES_DB_PATH) as conn:
        cats_after  = conn.execute("SELECT COUNT(*) FROM categories").fetchone()[0]
        rules_after = conn.execute(
            "SELECT COUNT(*) FROM rules WHERE is_user_rule=0").fetchone()[0]
        user_rules  = conn.execute(
            "SELECT COUNT(*) FROM rules WHERE is_user_rule=1").fetchone()[0]

    cats_added  = cats_after  - cats_before
    rules_added = rules_after - rules_before

    print(f"  Categories:     {cats_before} -> {cats_after}  (+{cats_added} added)")
    print(f"  Built-in rules: {rules_before} -> {rules_after}  (+{rules_added} added)")
    print(f"  User rules:   {user_rules}  (untouched)")
    print()

    if cats_added == 0 and rules_added == 0:
        print("  [OK] Database already up to date -- nothing to do.")
    else:
        print(f"  [OK] Migration complete.")
        if cats_added:
            print(f"    {cats_added} new categories added.")
        if rules_added:
            print(f"    {rules_added} new built-in rules added.")
        print()
        print("  Restart the application for changes to take effect.")

    print("=" * 60)

if __name__ == "__main__":
    migrate()
