"""
app/services/pdf_parser.py
==========================
Importable PDF parsing adapted from poc_pdf_test.py.
Does NOT modify that file.
"""
from __future__ import annotations
import os, re
from datetime import datetime
from decimal import Decimal, InvalidOperation
from typing import Optional

try:
    import pdfplumber
except ImportError:
    pdfplumber = None  # type: ignore

_DATE_FMTS = ["%d %b %Y", "%d/%m/%Y", "%d-%m-%Y", "%Y-%m-%d", "%d %B %Y"]

def infer_year(path: str) -> int:
    m = re.search(r"(20\d{2})", os.path.basename(path))
    return int(m.group(1)) if m else datetime.now().year

def _norm(text: str) -> str:
    text = text.lower()
    text = re.sub(r"[*.,\-_/\\&#@!?]", " ", text)
    return re.sub(r"\s+", " ", text).strip()

def parse_date(raw: str, year: int | None = None) -> Optional[datetime]:
    if not raw: return None
    raw = raw.strip()
    for fmt in _DATE_FMTS:
        try: return datetime.strptime(raw, fmt)
        except ValueError: pass
    if year:
        for fmt in _DATE_FMTS:
            try: return datetime.strptime(f"{raw} {year}", fmt)
            except ValueError: pass
    return None

def parse_amount(raw: str) -> Optional[Decimal]:
    if not raw: return None
    cleaned = re.sub(r"[£$€,\s]", "", raw.strip())
    if not cleaned or cleaned in ("-", "–"): return None
    try: return Decimal(cleaned)
    except InvalidOperation: return None

def is_header(row: list) -> bool:
    joined = " ".join(str(c) for c in row if c).lower()
    hits = sum(1 for w in {"date","description","debit","credit","balance",
                           "transaction","payments","receipts"} if w in joined)
    return hits >= 2

def is_balance_only(table: list) -> bool:
    return bool(table) and max((len(r) for r in table), default=0) <= 1

def merge_rows(table: list, year: int) -> list:
    merged, current, last_date = [], None, None
    for row in table:
        cells = [str(c).strip() if c is not None else "" for c in row]
        if not any(cells): continue
        if is_header(row): continue
        date_cell = cells[0] if cells else ""
        date_obj  = parse_date(date_cell, year) if date_cell else None
        if date_obj:
            if current: merged.append(current)
            last_date = date_cell
            current = list(cells)
        elif date_cell == "":
            if current is None and last_date is None: continue
            desc = cells[1] if len(cells) > 1 else ""
            amt_d = cells[2] if len(cells) > 2 else ""
            amt_c = cells[3] if len(cells) > 3 else ""
            amt_b = cells[4] if len(cells) > 4 else ""
            has_amt = bool(parse_amount(amt_d) or parse_amount(amt_c))
            if has_amt and current:
                merged.append(current)
                new = ([last_date] + cells[1:] + [""] * 5)[:5]
                current = new
            else:
                if current and desc:
                    existing = current[1] if len(current) > 1 else ""
                    sep = " – " if existing else ""
                    current[1] = existing + sep + desc
                if current and len(current) > 4 and not current[4] and amt_b:
                    current[4] = amt_b
    if current: merged.append(current)
    return merged

def parse_row(cells: list, year: int) -> Optional[dict]:
    n = len(cells)
    date_obj = debit = credit = balance = None
    desc = ""
    if n >= 5:
        date_obj = parse_date(cells[0], year)
        desc, debit, credit, balance = (cells[1],
            parse_amount(cells[2]), parse_amount(cells[3]), parse_amount(cells[4]))
    elif n == 4:
        date_obj = parse_date(cells[0], year)
        desc = cells[1]; amt = parse_amount(cells[2]); balance = parse_amount(cells[3])
        if amt: debit, credit = (abs(amt), None) if amt < 0 else (None, amt)
    elif n == 3:
        date_obj = parse_date(cells[0], year)
        desc = cells[1]; amt = parse_amount(cells[2])
        if amt: debit, credit = (abs(amt), None) if amt < 0 else (None, amt)
    else:
        return None
    if date_obj is None or not desc.strip(): return None
    if debit is None and credit is None: return None
    return dict(date=date_obj, description=desc.strip(),
                debit=float(debit) if debit else None,
                credit=float(credit) if credit else None,
                balance=float(balance) if balance else None)

def extract_merchant(description: str) -> str:
    """Extract merchant from merged description (after the ' – ' separator)."""
    if " – " in description:
        parts = description.split(" – ")
        return parts[1].strip() if len(parts) > 1 else description
    return description

def parse_pdf(path: str) -> list[dict]:
    """Parse a Nationwide PDF and return a list of raw transaction dicts."""
    if pdfplumber is None:
        raise ImportError("pdfplumber is required: pip install pdfplumber")
    year = infer_year(path)
    results = []
    with pdfplumber.open(path) as pdf:
        for page in pdf.pages:
            for table in (page.extract_tables() or []):
                if is_balance_only(table): continue
                for row in merge_rows(table, year):
                    cells = [str(c).strip() if c else "" for c in row]
                    tx = parse_row(cells, year)
                    if tx:
                        tx["merchant"] = extract_merchant(tx["description"])
                        results.append(tx)
    return results


# ── Statement metadata extraction ──────────────────────────────────────────────

def parse_statement_meta(path: str) -> dict:
    """
    Scan every page of the PDF for statement-level metadata.
    Does NOT modify parse_pdf — called separately from the import service.

    Returns a dict with keys:
        opening_balance : float | None
        closing_balance : float | None
        statement_date  : str | None   (ISO YYYY-MM-DD)
        expected_tx_count : int | None
    """
    if pdfplumber is None:
        return {}

    # Patterns for common Nationwide statement layouts
    # "Opening balance   £1,234.56" or "Balance brought forward  £1,234.56"
    _OPEN_PAT  = re.compile(
        r"(?:opening\s+balance|balance\s+brought\s+forward|brought\s+forward)"
        r"\s*[:\-]?\s*£?([\d,]+\.\d{2})",
        re.IGNORECASE)
    _CLOSE_PAT = re.compile(
        r"(?:closing\s+balance|balance\s+carried\s+forward|carried\s+forward"
        r"|new\s+balance|current\s+balance)"
        r"\s*[:\-]?\s*£?([\d,]+\.\d{2})",
        re.IGNORECASE)
    # Statement date: "Statement date: 30 April 2026" or "30 April 2026"
    _DATE_PAT  = re.compile(
        r"(?:statement\s+date|statement\s+period|period\s+ending|as\s+at)"
        r"[\s:\-]+(\d{1,2}\s+\w+\s+\d{4})",
        re.IGNORECASE)
    # "X transactions" for expected count
    _TX_COUNT_PAT = re.compile(
        r"(\d+)\s+transaction",
        re.IGNORECASE)

    opening = closing = stmt_date = expected_count = None
    year = infer_year(path)

    try:
        with pdfplumber.open(path) as pdf:
            for page in pdf.pages:
                text = page.extract_text() or ""

                if opening is None:
                    m = _OPEN_PAT.search(text)
                    if m:
                        try:
                            opening = float(m.group(1).replace(",", ""))
                        except (ValueError, AttributeError):
                            pass

                if closing is None:
                    m = _CLOSE_PAT.search(text)
                    if m:
                        try:
                            closing = float(m.group(1).replace(",", ""))
                        except (ValueError, AttributeError):
                            pass

                if stmt_date is None:
                    m = _DATE_PAT.search(text)
                    if m:
                        d = parse_date(m.group(1).strip())
                        if d:
                            stmt_date = d.strftime("%Y-%m-%d")

                if expected_count is None:
                    m = _TX_COUNT_PAT.search(text)
                    if m:
                        try:
                            expected_count = int(m.group(1))
                        except ValueError:
                            pass

        # Fallback: use the balance column of the first and last valid transaction
        # to infer opening/closing if not found in text
        if opening is None or closing is None:
            txs = parse_pdf(path)
            if txs:
                balances = [t["balance"] for t in txs if t.get("balance") is not None]
                if balances:
                    if closing is None:
                        closing = balances[-1]
                    if opening is None and len(balances) > 1:
                        # opening is before the first tx — not directly stored,
                        # but closing of previous statement = opening of this one.
                        # Best approximation: don't guess.
                        pass

    except Exception:
        pass

    return {
        "opening_balance":   opening,
        "closing_balance":   closing,
        "statement_date":    stmt_date,
        "expected_tx_count": expected_count,
    }

