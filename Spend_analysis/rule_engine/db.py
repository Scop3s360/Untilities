"""
rule_engine/db.py
=================
SQLite persistence for categories and rules.

Responsibilities
----------------
* Create the schema on first run (idempotent — safe to call every startup).
* Seed default categories and built-in rules if the tables are empty.
* CRUD helpers for user rules and categories.

The engine itself never calls this module after startup.
All runtime categorisation uses the in-memory RuleLoader.
"""

from __future__ import annotations

import sqlite3
import logging
from contextlib import contextmanager
from datetime import datetime
from pathlib import Path
from typing import Generator

from .defaults import BUILTIN_RULES, DEFAULT_CATEGORIES
from .models import Category, Rule

logger = logging.getLogger(__name__)

# ── Schema ─────────────────────────────────────────────────────────────────────

_DDL = """
PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;

CREATE TABLE IF NOT EXISTS categories (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    name          TEXT    NOT NULL UNIQUE,
    display_order INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS rules (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    keyword       TEXT    NOT NULL,
    category_name TEXT    NOT NULL,
    priority      INTEGER NOT NULL DEFAULT 100,
    enabled       INTEGER NOT NULL DEFAULT 1,
    is_user_rule  INTEGER NOT NULL DEFAULT 0,
    created_at    TEXT    NOT NULL,
    modified_at   TEXT    NOT NULL,
    UNIQUE(keyword, is_user_rule)       -- prevents duplicate seeds
);

CREATE INDEX IF NOT EXISTS idx_rules_priority  ON rules (priority);
CREATE INDEX IF NOT EXISTS idx_rules_enabled   ON rules (enabled);
"""


# ── Connection helper ──────────────────────────────────────────────────────────

@contextmanager
def _connect(db_path: Path) -> Generator[sqlite3.Connection, None, None]:
    conn = sqlite3.connect(str(db_path))
    conn.row_factory = sqlite3.Row
    try:
        yield conn
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


# ── Public API ─────────────────────────────────────────────────────────────────

def initialise(db_path: Path) -> None:
    """
    Create schema and seed defaults.  Safe to call on every application start.
    If tables already contain data, seeding is skipped.
    """
    db_path.parent.mkdir(parents=True, exist_ok=True)

    with _connect(db_path) as conn:
        conn.executescript(_DDL)
        _seed_categories(conn)
        _seed_builtin_rules(conn)

    logger.info("Database initialised: %s", db_path)


def get_all_categories(db_path: Path) -> list[Category]:
    """Return all categories ordered by display_order."""
    with _connect(db_path) as conn:
        rows = conn.execute(
            "SELECT id, name, display_order FROM categories ORDER BY display_order"
        ).fetchall()
    return [Category(id=r["id"], name=r["name"], display_order=r["display_order"])
            for r in rows]


def get_all_rules(db_path: Path) -> list[Rule]:
    """Return all enabled rules ordered by priority (ascending)."""
    with _connect(db_path) as conn:
        rows = conn.execute("""
            SELECT id, keyword, category_name, priority, enabled,
                   is_user_rule, created_at, modified_at
            FROM   rules
            WHERE  enabled = 1
            ORDER  BY priority ASC, LENGTH(keyword) DESC
        """).fetchall()
    return [_row_to_rule(r) for r in rows]


def add_user_rule(db_path: Path, keyword: str, category_name: str) -> Rule:
    """
    Create a new user rule (priority=0 so it always beats built-ins).
    Raises ValueError if the keyword already exists as a user rule.
    """
    now = _now()
    with _connect(db_path) as conn:
        try:
            cur = conn.execute("""
                INSERT INTO rules (keyword, category_name, priority, enabled,
                                   is_user_rule, created_at, modified_at)
                VALUES (?, ?, 0, 1, 1, ?, ?)
            """, (keyword.strip(), category_name.strip(), now, now))
            rule_id = cur.lastrowid
        except sqlite3.IntegrityError:
            raise ValueError(
                f"A user rule for keyword '{keyword}' already exists. "
                "Use update_user_rule() to modify it."
            )

        row = conn.execute(
            "SELECT * FROM rules WHERE id = ?", (rule_id,)
        ).fetchone()

    logger.info("User rule created: '%s' → '%s'", keyword, category_name)
    return _row_to_rule(row)


def update_user_rule(db_path: Path, rule_id: int,
                     keyword: str | None = None,
                     category_name: str | None = None,
                     enabled: bool | None = None) -> None:
    """Partially update a user rule by ID."""
    now = _now()
    with _connect(db_path) as conn:
        if keyword is not None:
            conn.execute(
                "UPDATE rules SET keyword=?, modified_at=? WHERE id=?",
                (keyword.strip(), now, rule_id)
            )
        if category_name is not None:
            conn.execute(
                "UPDATE rules SET category_name=?, modified_at=? WHERE id=?",
                (category_name.strip(), now, rule_id)
            )
        if enabled is not None:
            conn.execute(
                "UPDATE rules SET enabled=?, modified_at=? WHERE id=?",
                (int(enabled), now, rule_id)
            )
    logger.info("Rule %d updated.", rule_id)


def delete_user_rule(db_path: Path, rule_id: int) -> None:
    """Delete a user rule.  Built-in rules cannot be deleted via this call."""
    with _connect(db_path) as conn:
        conn.execute(
            "DELETE FROM rules WHERE id=? AND is_user_rule=1", (rule_id,)
        )
    logger.info("Rule %d deleted.", rule_id)


def add_category(db_path: Path, name: str, display_order: int = 500) -> Category:
    """Add a new user-defined category."""
    with _connect(db_path) as conn:
        try:
            cur = conn.execute(
                "INSERT INTO categories (name, display_order) VALUES (?, ?)",
                (name.strip(), display_order)
            )
            cat_id = cur.lastrowid
        except sqlite3.IntegrityError:
            raise ValueError(f"Category '{name}' already exists.")
    logger.info("Category added: '%s'", name)
    return Category(id=cat_id, name=name.strip(), display_order=display_order)


# ── Seeding ────────────────────────────────────────────────────────────────────

def _seed_categories(conn: sqlite3.Connection) -> None:
    """Additive seed — inserts any missing categories, never overwrites existing."""
    conn.executemany(
        "INSERT OR IGNORE INTO categories (name, display_order) VALUES (?, ?)",
        DEFAULT_CATEGORIES,
    )
    logger.debug("Category seed pass complete (%d definitions).", len(DEFAULT_CATEGORIES))


def _seed_builtin_rules(conn: sqlite3.Connection) -> None:
    """Additive seed — inserts any missing built-in rules, never overwrites existing."""
    now = _now()
    conn.executemany("""
        INSERT OR IGNORE INTO rules
            (keyword, category_name, priority, enabled, is_user_rule, created_at, modified_at)
        VALUES (?, ?, 100, 1, 0, ?, ?)
    """, [(kw, cat, now, now) for kw, cat in BUILTIN_RULES])
    logger.debug("Rule seed pass complete (%d definitions).", len(BUILTIN_RULES))


# ── Helpers ────────────────────────────────────────────────────────────────────

def _row_to_rule(row: sqlite3.Row) -> Rule:
    return Rule(
        id=row["id"],
        keyword=row["keyword"],
        category_name=row["category_name"],
        priority=row["priority"],
        enabled=bool(row["enabled"]),
        is_user_rule=bool(row["is_user_rule"]),
        created_at=datetime.fromisoformat(row["created_at"]),
        modified_at=datetime.fromisoformat(row["modified_at"]),
    )


def _now() -> str:
    return datetime.now().isoformat()
