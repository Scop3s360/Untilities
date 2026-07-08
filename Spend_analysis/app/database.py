"""
app/database.py
===============
SQLite schema, seeding and all data-access queries for transactions
and imported statements.  The rule_engine uses its own separate DB.
"""
from __future__ import annotations

import sqlite3
from contextlib import contextmanager
from datetime import datetime, date, timedelta
from pathlib import Path
from typing import Any, Generator

# ── Schema ─────────────────────────────────────────────────────────────────────

_DDL = """
PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;

CREATE TABLE IF NOT EXISTS statements (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    filename          TEXT    NOT NULL,
    filepath          TEXT,
    bank              TEXT    NOT NULL DEFAULT 'Nationwide',
    period_label      TEXT,
    imported_at       TEXT    NOT NULL,
    transaction_count INTEGER NOT NULL DEFAULT 0,
    duplicate_count   INTEGER NOT NULL DEFAULT 0,
    status            TEXT    NOT NULL DEFAULT 'complete'
);

CREATE TABLE IF NOT EXISTS transactions (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    statement_id INTEGER REFERENCES statements(id) ON DELETE CASCADE,
    date         TEXT    NOT NULL,
    description  TEXT    NOT NULL,
    merchant     TEXT    NOT NULL DEFAULT '',
    category     TEXT    NOT NULL DEFAULT 'Other',
    debit        REAL,
    credit       REAL,
    balance      REAL,
    notes        TEXT    DEFAULT '',
    dedup_key    TEXT    GENERATED ALWAYS AS (
                     date || '|' || description || '|' ||
                     IFNULL(CAST(debit AS TEXT),'') || '|' ||
                     IFNULL(CAST(credit AS TEXT),'')
                 ) STORED,
    UNIQUE(dedup_key)
);

CREATE INDEX IF NOT EXISTS idx_tx_date     ON transactions(date);
CREATE INDEX IF NOT EXISTS idx_tx_cat      ON transactions(category);
CREATE INDEX IF NOT EXISTS idx_tx_merchant ON transactions(merchant);
CREATE INDEX IF NOT EXISTS idx_tx_stmt     ON transactions(statement_id);
"""

# ── Connection ─────────────────────────────────────────────────────────────────

@contextmanager
def _conn(db_path: Path) -> Generator[sqlite3.Connection, None, None]:
    con = sqlite3.connect(str(db_path))
    con.row_factory = sqlite3.Row
    try:
        yield con
        con.commit()
    except Exception:
        con.rollback()
        raise
    finally:
        con.close()

def initialise(db_path: Path) -> None:
    """Create tables.  Safe to call on every startup."""
    with _conn(db_path) as con:
        con.executescript(_DDL)

# ── Statement CRUD ─────────────────────────────────────────────────────────────

def insert_statement(db_path: Path, filename: str, filepath: str,
                     bank: str, period_label: str) -> int:
    with _conn(db_path) as con:
        cur = con.execute(
            """INSERT INTO statements (filename,filepath,bank,period_label,imported_at)
               VALUES (?,?,?,?,?)""",
            (filename, filepath, bank, period_label, datetime.now().isoformat()))
        return cur.lastrowid

def finalise_statement(db_path: Path, stmt_id: int,
                       tx_count: int, dup_count: int) -> None:
    with _conn(db_path) as con:
        con.execute(
            "UPDATE statements SET transaction_count=?, duplicate_count=? WHERE id=?",
            (tx_count, dup_count, stmt_id))

def get_all_statements(db_path: Path) -> list[dict]:
    with _conn(db_path) as con:
        rows = con.execute(
            "SELECT * FROM statements ORDER BY imported_at DESC").fetchall()
    return [dict(r) for r in rows]

def delete_statement(db_path: Path, stmt_id: int) -> None:
    with _conn(db_path) as con:
        con.execute("DELETE FROM statements WHERE id=?", (stmt_id,))

# ── Transaction CRUD ───────────────────────────────────────────────────────────

def insert_transaction(db_path: Path, stmt_id: int, date_str: str,
                       description: str, merchant: str, category: str,
                       debit: float | None, credit: float | None,
                       balance: float | None) -> bool:
    """Returns True if inserted, False if duplicate."""
    with _conn(db_path) as con:
        try:
            con.execute("""
                INSERT INTO transactions
                  (statement_id,date,description,merchant,category,debit,credit,balance)
                VALUES (?,?,?,?,?,?,?,?)""",
                (stmt_id, date_str, description, merchant, category,
                 debit, credit, balance))
            return True
        except sqlite3.IntegrityError:
            return False

def update_transaction_category(db_path: Path, tx_id: int, category: str) -> None:
    with _conn(db_path) as con:
        con.execute("UPDATE transactions SET category=? WHERE id=?", (category, tx_id))

# ── Dashboard queries ──────────────────────────────────────────────────────────

def get_dashboard_stats(db_path: Path, start: str | None = None,
                        end: str | None = None) -> dict:
    where, params = _date_where(start, end)
    with _conn(db_path) as con:
        r = con.execute(f"""
            SELECT
              COALESCE(SUM(credit),0)        AS total_income,
              COALESCE(SUM(debit),0)         AS total_spending,
              COUNT(*)                        AS tx_count,
              (SELECT COUNT(*) FROM statements) AS stmt_count
            FROM transactions {where}""", params).fetchone()
        savings = con.execute(f"""
            SELECT COALESCE(SUM(debit),0) FROM transactions
            WHERE category='Savings' {('AND ' + where[6:]) if where else ''}
        """, params).fetchone()[0]
    return {
        "total_income":    round(r["total_income"], 2),
        "total_spending":  round(r["total_spending"], 2),
        "total_saved":     round(savings, 2),
        "net":             round(r["total_income"] - r["total_spending"], 2),
        "tx_count":        r["tx_count"],
        "stmt_count":      r["stmt_count"],
    }

def get_spending_by_category(db_path: Path, start: str | None = None,
                              end: str | None = None) -> list[dict]:
    where, params = _date_where(start, end)
    exclude = ("Income","Salary","Benefits","Transfers","Savings")
    placeholders = ",".join("?" * len(exclude))
    with _conn(db_path) as con:
        rows = con.execute(f"""
            SELECT category, SUM(debit) AS total
            FROM transactions
            {where} {'AND' if where else 'WHERE'} debit IS NOT NULL
              AND category NOT IN ({placeholders})
            GROUP BY category ORDER BY total DESC
        """, params + list(exclude)).fetchall()
    return [dict(r) for r in rows]

def get_monthly_spending(db_path: Path, months: int = 12) -> list[dict]:
    with _conn(db_path) as con:
        rows = con.execute("""
            SELECT strftime('%Y-%m', date) AS month,
                   COALESCE(SUM(debit),0)  AS spending,
                   COALESCE(SUM(credit),0) AS income
            FROM transactions
            WHERE date >= date('now', ? || ' months')
            GROUP BY month ORDER BY month
        """, (f"-{months}",)).fetchall()
    return [dict(r) for r in rows]

def get_top_merchants(db_path: Path, start: str | None = None,
                      end: str | None = None, limit: int = 10) -> list[dict]:
    where, params = _date_where(start, end)
    with _conn(db_path) as con:
        rows = con.execute(f"""
            SELECT merchant, SUM(debit) AS total, COUNT(*) AS tx_count
            FROM transactions
            {where} {'AND' if where else 'WHERE'} debit IS NOT NULL AND merchant != ''
            GROUP BY merchant ORDER BY total DESC LIMIT ?
        """, params + [limit]).fetchall()
    return [dict(r) for r in rows]

def get_recent_transactions(db_path: Path, limit: int = 25) -> list[dict]:
    with _conn(db_path) as con:
        rows = con.execute("""
            SELECT t.*, s.filename AS source
            FROM transactions t
            LEFT JOIN statements s ON s.id = t.statement_id
            ORDER BY t.date DESC, t.id DESC LIMIT ?""", (limit,)).fetchall()
    return [dict(r) for r in rows]

# ── Transactions list ──────────────────────────────────────────────────────────

def get_transactions(db_path: Path, *, start: str | None = None,
                     end: str | None = None, category: str | None = None,
                     merchant: str | None = None, search: str | None = None,
                     income_only: bool = False, expense_only: bool = False,
                     uncategorised_only: bool = False,
                     limit: int = 2000) -> list[dict]:
    clauses, params = [], []
    if start:
        clauses.append("t.date >= ?"); params.append(start)
    if end:
        clauses.append("t.date <= ?"); params.append(end)
    if category:
        clauses.append("t.category = ?"); params.append(category)
    if merchant:
        clauses.append("t.merchant = ?"); params.append(merchant)
    if search:
        clauses.append("(t.description LIKE ? OR t.merchant LIKE ?)");
        params += [f"%{search}%", f"%{search}%"]
    if income_only:
        clauses.append("t.credit IS NOT NULL")
    if expense_only:
        clauses.append("t.debit IS NOT NULL")
    if uncategorised_only:
        clauses.append("t.category = 'Other'")
    where = ("WHERE " + " AND ".join(clauses)) if clauses else ""
    with _conn(db_path) as con:
        rows = con.execute(f"""
            SELECT t.*, s.filename AS source
            FROM transactions t
            LEFT JOIN statements s ON s.id = t.statement_id
            {where}
            ORDER BY t.date DESC, t.id DESC LIMIT ?
        """, params + [limit]).fetchall()
    return [dict(r) for r in rows]

# ── Categories ─────────────────────────────────────────────────────────────────

def get_category_summary(db_path: Path, start: str | None = None,
                          end: str | None = None) -> list[dict]:
    where, params = _date_where(start, end)
    with _conn(db_path) as con:
        rows = con.execute(f"""
            SELECT category,
                   COALESCE(SUM(debit),0)  AS total_debit,
                   COALESCE(SUM(credit),0) AS total_credit,
                   COUNT(*)                AS tx_count
            FROM transactions {where}
            GROUP BY category ORDER BY total_debit DESC
        """, params).fetchall()
    return [dict(r) for r in rows]

# ── Merchants ──────────────────────────────────────────────────────────────────

def get_merchant_summary(db_path: Path, start: str | None = None,
                          end: str | None = None) -> list[dict]:
    where, params = _date_where(start, end)
    with _conn(db_path) as con:
        rows = con.execute(f"""
            SELECT merchant,
                   category,
                   COALESCE(SUM(debit),0) AS total,
                   COUNT(*) AS tx_count,
                   MAX(debit) AS largest,
                   AVG(debit) AS average,
                   MIN(date)  AS first_date,
                   MAX(date)  AS last_date
            FROM transactions
            {where} {'AND' if where else 'WHERE'} merchant != ''
            GROUP BY merchant ORDER BY total DESC
        """, params).fetchall()
    return [dict(r) for r in rows]

def get_merchant_monthly(db_path: Path, merchant: str) -> list[dict]:
    with _conn(db_path) as con:
        rows = con.execute("""
            SELECT strftime('%Y-%m', date) AS month, SUM(debit) AS total
            FROM transactions
            WHERE merchant=? AND debit IS NOT NULL
            GROUP BY month ORDER BY month
        """, (merchant,)).fetchall()
    return [dict(r) for r in rows]

# ── Search ─────────────────────────────────────────────────────────────────────

def search_all(db_path: Path, query: str) -> list[dict]:
    q = f"%{query}%"
    with _conn(db_path) as con:
        rows = con.execute("""
            SELECT t.*, s.filename AS source
            FROM transactions t
            LEFT JOIN statements s ON s.id = t.statement_id
            WHERE t.description LIKE ? OR t.merchant LIKE ? OR t.category LIKE ?
            ORDER BY t.date DESC LIMIT 200
        """, (q, q, q)).fetchall()
    return [dict(r) for r in rows]

# ── Helpers ────────────────────────────────────────────────────────────────────

def _date_where(start: str | None, end: str | None) -> tuple[str, list]:
    clauses, params = [], []
    if start:
        clauses.append("date >= ?"); params.append(start)
    if end:
        clauses.append("date <= ?"); params.append(end)
    return (("WHERE " + " AND ".join(clauses)) if clauses else ""), params

def date_range_for_preset(preset: str) -> tuple[str | None, str | None]:
    today = date.today()
    end   = today.isoformat()
    if preset == "today":
        return end, end
    if preset == "7d":
        return (today - timedelta(days=7)).isoformat(), end
    if preset == "month":
        return today.replace(day=1).isoformat(), end
    if preset == "last_month":
        first = (today.replace(day=1) - timedelta(days=1)).replace(day=1)
        last  = today.replace(day=1) - timedelta(days=1)
        return first.isoformat(), last.isoformat()
    if preset == "3m":
        return (today - timedelta(days=90)).isoformat(), end
    if preset == "6m":
        return (today - timedelta(days=180)).isoformat(), end
    if preset == "year":
        return today.replace(month=1, day=1).isoformat(), end
    if preset == "last_year":
        y = today.year - 1
        return f"{y}-01-01", f"{y}-12-31"
    return None, None   # "all"
