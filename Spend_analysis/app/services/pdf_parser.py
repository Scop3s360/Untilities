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
