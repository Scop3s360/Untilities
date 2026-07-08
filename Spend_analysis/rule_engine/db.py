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

from .defaults import BUILTIN_RULES, DEFAULT_CATEGORIES, MERCHANT_DICTIONARY
from .models import Category, Rule, MerchantEntry

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

CREATE TABLE IF NOT EXISTS merchants (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    raw_keyword      TEXT    NOT NULL UNIQUE COLLATE NOCASE,
    canonical_name   TEXT    NOT NULL,
    merchant_type    TEXT    NOT NULL DEFAULT 'Merchant',
    category         TEXT    NOT NULL DEFAULT 'Other',
    enabled          INTEGER NOT NULL DEFAULT 1,
    created_at       TEXT    NOT NULL,
    modified_at      TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_merchants_canon ON merchants(canonical_name);
CREATE INDEX IF NOT EXISTS idx_merchants_type  ON merchants(merchant_type);
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
        _seed_merchants(conn)

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


def upsert_user_rule(db_path: Path, keyword: str, category_name: str) -> None:
    """
    Create or update a user rule for *keyword*.
    If a user rule already exists for this keyword it is updated in-place.
    Built-in rules are never touched.
    Reloads the in-memory engine so the change takes effect immediately.
    """
    now = _now()
    kw  = keyword.strip().upper()
    cat = category_name.strip()
    with _connect(db_path) as conn:
        existing = conn.execute(
            "SELECT id FROM rules WHERE keyword=? AND is_user_rule=1", (kw,)
        ).fetchone()
        if existing:
            conn.execute(
                "UPDATE rules SET category_name=?, modified_at=? WHERE id=?",
                (cat, now, existing["id"]))
        else:
            conn.execute("""
                INSERT INTO rules
                    (keyword, category_name, priority, enabled,
                     is_user_rule, created_at, modified_at)
                VALUES (?, ?, 0, 1, 1, ?, ?)""",
                (kw, cat, now, now))
    logger.info("Upserted user rule: '%s' -> '%s'", kw, cat)
    # Hot-reload the engine so the new rule is active immediately
    try:
        from app.services.import_service import get_engine
        get_engine().reload_rules()
    except Exception:
        pass  # Engine may not be initialised in test contexts


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


# ── Merchant Dictionary ────────────────────────────────────────────────────────

MERCHANT_TYPES = [
    "Employer", "Merchant", "Subscription", "Bank", "Savings Platform",
    "Investment Platform", "Healthcare", "Transport", "Government",
    "Person", "Family Transfer", "Standing Order", "Direct Debit",
    "Utility", "Holiday", "Entertainment", "Financial Services",
    "Internal Transfer", "Loan", "Other",
]


def _seed_merchants(conn: sqlite3.Connection) -> None:
    """Seed the merchant dictionary from defaults. INSERT OR IGNORE — never overwrites."""
    now = _now()
    conn.executemany("""
        INSERT OR IGNORE INTO merchants
            (raw_keyword, canonical_name, merchant_type, category, created_at, modified_at)
        VALUES (?, ?, ?, ?, ?, ?)""",
        [(m["raw_keyword"], m["canonical_name"], m["merchant_type"],
          m["category"], now, now)
         for m in MERCHANT_DICTIONARY])


def lookup_merchant(db_path: Path, description: str
                    ) -> dict | None:
    """
    Find the best matching merchant entry for a raw transaction description.
    Returns the matching row as a dict, or None if no match found.
    Matching is case-insensitive substring: raw_keyword in description.
    Longer keywords take priority (more specific match wins).
    """
    desc_upper = description.upper()
    with _connect(db_path) as conn:
        rows = conn.execute(
            "SELECT * FROM merchants WHERE enabled=1 ORDER BY LENGTH(raw_keyword) DESC"
        ).fetchall()
    for row in rows:
        if row["raw_keyword"].upper() in desc_upper:
            return dict(row)
    return None


def get_all_merchants_dict(db_path: Path) -> list[dict]:
    """Return all merchant dictionary entries."""
    with _connect(db_path) as conn:
        rows = conn.execute(
            "SELECT * FROM merchants ORDER BY canonical_name"
        ).fetchall()
    return [dict(r) for r in rows]


def upsert_merchant_dict(db_path: Path, raw_keyword: str, canonical_name: str,
                         merchant_type: str, category: str) -> None:
    """Create or update a merchant dictionary entry."""
    now = _now()
    kw = raw_keyword.strip()
    with _connect(db_path) as conn:
        existing = conn.execute(
            "SELECT id FROM merchants WHERE raw_keyword=?", (kw,)
        ).fetchone()
        if existing:
            conn.execute(
                """UPDATE merchants SET canonical_name=?, merchant_type=?,
                   category=?, modified_at=? WHERE id=?""",
                (canonical_name, merchant_type, category, now, existing["id"]))
        else:
            conn.execute(
                """INSERT INTO merchants
                   (raw_keyword, canonical_name, merchant_type, category, created_at, modified_at)
                   VALUES (?,?,?,?,?,?)""",
                (kw, canonical_name, merchant_type, category, now, now))
    logger.info("Upserted merchant dict: '%s' → %s (%s)", kw, canonical_name, category)


def merge_merchants(db_path: Path, transactions_db: "Path",
                    from_merchant: str, into_merchant: str) -> int:
    """
    Rename all transactions with merchant == from_merchant to into_merchant.
    Returns number of transactions updated.
    Used to merge duplicate merchants (e.g. 'Tesco Extra' → 'Tesco').
    """
    import sqlite3 as _s
    con = _s.connect(str(transactions_db))
    try:
        cur = con.execute(
            "UPDATE transactions SET merchant=? WHERE merchant=?",
            (into_merchant, from_merchant))
        con.commit()
        count = cur.rowcount
    finally:
        con.close()
    logger.info("Merged merchant '%s' → '%s' (%d transactions)", from_merchant, into_merchant, count)
    return count


# ── Category migration ─────────────────────────────────────────────────────────


_OLD_TO_NEW: dict[str, str] = {
    "Income": "Income", "Salary": "Income", "Benefits": "Income",
    "Refunds": "Income", "Gifts Received": "Income", "Interest": "Income",
    "Other Income": "Income",
    "Home": "Housing", "Mortgage": "Housing", "Rent": "Housing",
    "Council Tax": "Housing", "Utilities": "Housing", "Internet": "Housing",
    "Phone": "Housing", "Insurance": "Housing", "Home Maintenance": "Housing",
    "Living": "Food & Drink", "Groceries": "Food & Drink",
    "Takeaways": "Food & Drink", "Restaurants": "Food & Drink",
    "Coffee": "Food & Drink", "Alcohol": "Food & Drink",
    "Transport": "Transport", "Fuel": "Transport",
    "Public Transport": "Transport", "Taxi / Uber": "Transport",
    "Parking": "Transport", "Vehicle Maintenance": "Transport",
    "Shopping": "Shopping", "General Shopping": "Shopping",
    "Clothing": "Shopping", "Electronics": "Shopping", "DIY": "Shopping",
    "Books": "Hobbies", "Hobbies": "Hobbies", "Gaming": "Hobbies",
    "Neon Goblin": "Hobbies",
    "Lifestyle": "Entertainment", "Entertainment": "Entertainment",
    "Subscriptions": "Entertainment",
    "Holiday": "Travel", "Travel": "Travel", "Hotels": "Travel",
    "Flights": "Travel",
    "Finance": "Finance", "Savings": "Finance", "Investments": "Finance",
    "Bank Fees": "Finance", "Fees": "Finance",
    "Cash Withdrawal": "Finance", "Transfers": "Finance",
    "Health": "Health", "Medical": "Health", "Dental": "Health",
    "Pharmacy": "Health", "Fitness": "Health",
    "Family": "Family", "Child Support": "Family", "Childcare": "Family",
    "Education": "Family", "Pets": "Family", "Gifts": "Family",
    "Charity": "Other", "Unknown": "Other", "Other": "Other",
}

_NEW_CATS = frozenset(_OLD_TO_NEW.values())


def migrate_rule_categories(db_path: Path) -> dict[str, tuple[str, int]]:
    """
    Migrate user and built-in rules that reference old category names.
    Also resets the categories table so it re-seeds with the simplified list
    on the next call to initialise().
    Safe to call multiple times.
    Returns {old_cat: (new_cat, user_rules_updated)}.
    """
    results: dict[str, tuple[str, int]] = {}
    with _connect(db_path) as conn:
        # Migrate user rules
        for row in conn.execute(
                "SELECT DISTINCT category_name FROM rules WHERE is_user_rule=1"
        ).fetchall():
            old = row[0]
            if old in _NEW_CATS:
                continue
            new = _OLD_TO_NEW.get(old, "Other")
            cur = conn.execute(
                "UPDATE rules SET category_name=? "
                "WHERE category_name=? AND is_user_rule=1",
                (new, old))
            results[old] = (new, cur.rowcount)
            logger.info("Migrated user rules: '%s' -> '%s' (%d rules)", old, new, cur.rowcount)

        # Migrate built-in rules in DB (they'll also be re-seeded from defaults.py)
        for row in conn.execute(
                "SELECT DISTINCT category_name FROM rules WHERE is_user_rule=0"
        ).fetchall():
            old = row[0]
            if old in _NEW_CATS:
                continue
            new = _OLD_TO_NEW.get(old, "Other")
            conn.execute(
                "UPDATE rules SET category_name=? "
                "WHERE category_name=? AND is_user_rule=0",
                (new, old))

        # Reset categories table so initialise() re-seeds with the 12 new ones.
        # User-defined categories not in the new system are mapped to Other above.
        existing_cats = {r[0] for r in conn.execute(
            "SELECT name FROM categories").fetchall()}
        old_cats = existing_cats - _NEW_CATS
        for cat in old_cats:
            conn.execute("DELETE FROM categories WHERE name=?", (cat,))

    return results


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
