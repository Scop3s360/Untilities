"""
app/services/import_service.py
===============================
Orchestrates: parse → categorise → store → return stats.
Supports PDF and basic CSV files.
"""
from __future__ import annotations
import csv, os
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from app import database as db
from app.services.pdf_parser import parse_pdf, extract_merchant
from app.config import DB_PATH, RULES_DB_PATH
from rule_engine import RuleEngine
from rule_engine.db import lookup_merchant

# Shared engine instance (caller must call engine.start() before importing)
_engine: RuleEngine | None = None

def get_engine() -> RuleEngine:
    global _engine
    if _engine is None:
        _engine = RuleEngine(RULES_DB_PATH, debug=False)
        _engine.start()
    return _engine

@dataclass
class ImportResult:
    filename:      str
    bank:          str
    period_label:  str
    tx_count:      int
    dup_count:     int
    error:         str | None = None

def import_file(filepath: str) -> ImportResult:
    path = Path(filepath)
    ext  = path.suffix.lower()
    if ext == ".pdf":
        return _import_pdf(filepath)
    elif ext == ".csv":
        return _import_csv(filepath)
    else:
        return ImportResult(path.name, "Unknown", "", 0, 0,
                            error=f"Unsupported file type: {ext}")

def _import_pdf(filepath: str) -> ImportResult:
    path = Path(filepath)
    try:
        raw_txs = parse_pdf(filepath)
    except Exception as e:
        return ImportResult(path.name, "Nationwide", "", 0, 0, error=str(e))

    # Get page count
    try:
        import pdfplumber
        with pdfplumber.open(filepath) as pdf:
            pages = len(pdf.pages)
    except Exception:
        pages = 0

    period  = _infer_period(filepath)
    stmt_id = db.insert_statement(DB_PATH, path.name, filepath,
                                  "Nationwide", period, pages)
    engine  = get_engine()
    inserted, dupes = 0, 0
    for tx in raw_txs:
        # Layer 0: merchant dictionary lookup (canonical name + category override)
        merchant_entry = lookup_merchant(RULES_DB_PATH, tx["description"])
        if merchant_entry:
            merchant = merchant_entry["canonical_name"]
            cat      = merchant_entry["category"]
        else:
            merchant  = tx.get("merchant") or extract_merchant(tx["description"])
            is_credit = tx["credit"] is not None
            cat = engine.categorise(
                tx["description"],
                amount=tx.get("credit") or tx.get("debit"),
                is_credit=is_credit,
            ).category

        ok = db.insert_transaction(
            DB_PATH, stmt_id,
            tx["date"].strftime("%Y-%m-%d"),
            tx["description"],
            merchant,
            cat,
            tx["debit"],
            tx["credit"],
            tx["balance"],
        )
        if ok: inserted += 1
        else:  dupes   += 1
    db.finalise_statement(DB_PATH, stmt_id, inserted, dupes)
    return ImportResult(path.name, "Nationwide", period, inserted, dupes)

def _import_csv(filepath: str) -> ImportResult:
    path = Path(filepath)
    try:
        txs = _parse_generic_csv(filepath)
    except Exception as e:
        return ImportResult(path.name, "CSV", "", 0, 0, error=str(e))

    stmt_id = db.insert_statement(DB_PATH, path.name, filepath, "CSV", "")
    engine  = get_engine()
    inserted, dupes = 0, 0
    for tx in txs:
        is_credit = tx.get("credit") is not None
        cat = engine.categorise(
            tx["description"],
            amount=tx.get("credit") or tx.get("debit"),
            is_credit=is_credit,
        ).category
        ok  = db.insert_transaction(
            DB_PATH, stmt_id, tx["date"], tx["description"],
            extract_merchant(tx["description"]), cat,
            tx.get("debit"), tx.get("credit"), tx.get("balance"))
        if ok: inserted += 1
        else:  dupes   += 1
    db.finalise_statement(DB_PATH, stmt_id, inserted, dupes)
    return ImportResult(path.name, "CSV", "", inserted, dupes)

def _parse_generic_csv(filepath: str) -> list[dict]:
    results = []
    with open(filepath, newline="", encoding="utf-8-sig") as f:
        reader = csv.DictReader(f)
        for row in reader:
            # Try common Nationwide CSV column names
            date_raw = (row.get("Date") or row.get("date") or "").strip()
            desc     = (row.get("Description") or row.get("description") or "").strip()
            debit_s  = (row.get("Debits") or row.get("Debit") or row.get("debit") or "").strip()
            credit_s = (row.get("Credits") or row.get("Credit") or row.get("credit") or "").strip()
            bal_s    = (row.get("Balance") or row.get("balance") or "").strip()
            if not date_raw or not desc: continue
            try:
                from app.services.pdf_parser import parse_date as _pd, parse_amount as _pa
                d = _pd(date_raw)
                if d is None: continue
                results.append(dict(
                    date=d.strftime("%Y-%m-%d"), description=desc,
                    debit=float(_pa(debit_s)) if _pa(debit_s) else None,
                    credit=float(_pa(credit_s)) if _pa(credit_s) else None,
                    balance=float(_pa(bal_s)) if _pa(bal_s) else None,
                ))
            except Exception:
                continue
    return results

def _infer_period(filepath: str) -> str:
    import re
    name = os.path.basename(filepath)
    m = re.search(r"([A-Za-z]+ \d{4})", name)
    return m.group(1) if m else name.replace(".pdf", "")
