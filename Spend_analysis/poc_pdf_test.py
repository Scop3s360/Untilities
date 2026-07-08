"""
Spend Analysis — Phase 0 PDF Validation
========================================
Proves that pdfplumber.extract_tables() can extract transactions from a
Nationwide bank statement before any application code is written.

Usage:
    python poc_pdf_test.py "path\\to\\your\\statement.pdf"

Pass-criteria:
    >= 95% of rows successfully parsed into transactions.

Nationwide format notes
-----------------------
* Dates are abbreviated: "25 Mar" with no year.  Year is inferred from the
  filename (e.g. "Apr 2026 Statement.pdf") or falls back to the current year.
* Each transaction spans 2-3 PDF table rows:
    Row 1 (anchor)     : Date | Transaction type | Amount columns
    Row 2 (merchant)   : ""   | Merchant name    | (usually empty)
    Row 3 (ref, opt.)  : ""   | Google Pay ref   | (usually empty)
  Continuation rows that carry a non-zero amount begin a *new* transaction
  sharing the most-recent anchor date.
* Some 1-column tables contain only running-balance lines — they are skipped.
"""

import os
import sys
import re
from datetime import datetime
from decimal import Decimal, InvalidOperation
from dataclasses import dataclass
from typing import Optional

try:
    import pdfplumber
except ImportError:
    print("ERROR: pdfplumber is not installed.")
    print("Run:  pip install pdfplumber")
    sys.exit(1)


# ── Transaction dataclass ──────────────────────────────────────────────────────

@dataclass
class Transaction:
    date:        datetime
    description: str
    debit:       Optional[Decimal]
    credit:      Optional[Decimal]
    balance:     Optional[Decimal]


# ── Year inference ─────────────────────────────────────────────────────────────

def infer_year_from_path(pdf_path: str) -> int:
    """
    Try to extract a 4-digit year from the PDF filename.
    E.g. "Apr 2026 Statement.pdf" → 2026.
    Falls back to the current calendar year.
    """
    basename = os.path.basename(pdf_path)
    match = re.search(r"(20\d{2})", basename)
    if match:
        return int(match.group(1))
    return datetime.now().year


# ── Date / amount helpers ──────────────────────────────────────────────────────

# Formats tried with a full year already appended
_DATE_FORMATS_WITH_YEAR = [
    "%d %b %Y",   # 25 Mar 2026  (Nationwide short month)
    "%d/%m/%Y",   # 25/03/2026
    "%d-%m-%Y",   # 25-03-2026
    "%Y-%m-%d",   # 2026-03-25
    "%d %B %Y",   # 25 March 2026
]


def parse_date(raw: str, year: Optional[int] = None) -> Optional[datetime]:
    """
    Parse a date string.  If the string has no year component (e.g. "25 Mar"),
    append *year* before trying.  Returns None if unparseable.
    """
    if not raw:
        return None
    raw = raw.strip()

    # First try as-is (handles dates that already include a year)
    for fmt in _DATE_FORMATS_WITH_YEAR:
        try:
            return datetime.strptime(raw, fmt)
        except ValueError:
            continue

    # Then try with year appended (Nationwide "25 Mar" style)
    if year:
        suffixed = f"{raw} {year}"
        for fmt in _DATE_FORMATS_WITH_YEAR:
            try:
                return datetime.strptime(suffixed, fmt)
            except ValueError:
                continue

    return None


def parse_amount(raw: str) -> Optional[Decimal]:
    """Strip currency symbols, commas, spaces and parse as Decimal."""
    if not raw:
        return None
    cleaned = re.sub(r"[£$€,\s]", "", raw.strip())
    if not cleaned or cleaned in ("-", "–"):
        return None
    try:
        return Decimal(cleaned)
    except InvalidOperation:
        return None


def is_header_row(row: list) -> bool:
    """Return True if this row looks like a header, not a data row."""
    if not row:
        return False
    joined = " ".join(str(c) for c in row if c).lower()
    header_words = {"date", "description", "debit", "credit", "balance",
                    "transaction", "payments", "receipts", "details"}
    matches = sum(1 for w in header_words if w in joined)
    return matches >= 2


def is_balance_only_table(table: list) -> bool:
    """
    Nationwide sometimes produces narrow (1-col) tables that contain only
    running-balance text.  Skip these entirely.
    """
    if not table:
        return False
    max_cols = max((len(r) for r in table), default=0)
    return max_cols <= 1


# ── Multi-row merger ───────────────────────────────────────────────────────────

def merge_nationwide_rows(table: list, year: int) -> list:
    """
    Collapse Nationwide's multi-row transactions into single logical rows.

    Rules
    -----
    * An *anchor* row has a parseable date in column 0.
    * A *continuation* row has an empty column 0.
      - If it carries a non-empty amount (col 2 or col 3), it starts a new
        virtual transaction that inherits the most-recent anchor date.
      - Otherwise its description is appended to the current anchor's
        description (col 1), building the full "Type – Merchant" label.
    * Completely blank rows and header rows are discarded.
    * Corrupted/junk rows (where col 0 contains something that is neither a
      date nor empty, e.g. "2026 2,766.42") are silently dropped.
    """
    merged: list = []
    current: Optional[list] = None
    last_date_str: Optional[str] = None   # raw date string of most-recent anchor

    for row in table:
        # Normalise cells
        cells = [str(c).strip() if c is not None else "" for c in row]

        # Skip completely empty rows and header rows
        non_empty = [c for c in cells if c]
        if not non_empty:
            continue
        if is_header_row(row):
            continue

        date_cell = cells[0] if cells else ""
        date_obj  = parse_date(date_cell, year) if date_cell else None

        if date_obj is not None:
            # ── Anchor row ────────────────────────────────────────────────
            if current is not None:
                merged.append(current)
            last_date_str = date_cell
            current = list(cells)

        elif date_cell == "":
            # ── Continuation row ──────────────────────────────────────────
            if current is None and last_date_str is None:
                # Nothing to attach to — skip
                continue

            desc_cont  = cells[1] if len(cells) > 1 else ""
            amt_debit  = cells[2] if len(cells) > 2 else ""
            amt_credit = cells[3] if len(cells) > 3 else ""
            amt_bal    = cells[4] if len(cells) > 4 else ""

            has_amount = bool(parse_amount(amt_debit) or parse_amount(amt_credit))

            if has_amount and current is not None:
                # New transaction sharing the anchor date — flush current first
                merged.append(current)
                # Build a new row with inherited date
                new_row = ([last_date_str] + cells[1:] + [""] * 5)[:5]
                current = new_row
            else:
                # Pure continuation — append description text
                if current is not None and desc_cont:
                    existing_desc = current[1] if len(current) > 1 else ""
                    sep = " – " if existing_desc else ""
                    if len(current) > 1:
                        current[1] = existing_desc + sep + desc_cont
                # Backfill missing balance
                if current is not None and len(current) > 4 and not current[4] and amt_bal:
                    current[4] = amt_bal
        else:
            # ── Junk / corrupted row (date cell is non-empty but unparseable)
            continue

    if current is not None:
        merged.append(current)

    return merged


# ── Row → Transaction ──────────────────────────────────────────────────────────

def try_parse_row(row: list, year: Optional[int] = None) -> Optional[Transaction]:
    """
    Attempt to extract a Transaction from a (already-merged) table row.
    Returns None if the row is a header, blank, or unparseable.
    """
    non_empty = [c for c in row if c and str(c).strip()]
    if len(non_empty) < 2:
        return None
    if is_header_row(row):
        return None

    cells = [str(c).strip() if c else "" for c in row]

    date    = None
    desc    = ""
    debit   = None
    credit  = None
    balance = None

    n = len(cells)

    if n >= 5:
        date    = parse_date(cells[0], year)
        desc    = cells[1]
        debit   = parse_amount(cells[2])
        credit  = parse_amount(cells[3])
        balance = parse_amount(cells[4])
    elif n == 4:
        date    = parse_date(cells[0], year)
        desc    = cells[1]
        amt     = parse_amount(cells[2])
        balance = parse_amount(cells[3])
        if amt is not None:
            debit = abs(amt) if amt < 0 else None
            credit = amt     if amt >= 0 else None
    elif n == 3:
        date    = parse_date(cells[0], year)
        desc    = cells[1]
        amt     = parse_amount(cells[2])
        if amt is not None:
            debit  = abs(amt) if amt < 0 else None
            credit = amt      if amt >= 0 else None
    else:
        return None

    if date is None or not desc.strip():
        return None
    if debit is None and credit is None:
        return None

    return Transaction(date=date, description=desc.strip(),
                       debit=debit, credit=credit, balance=balance)


# ── Main validation ────────────────────────────────────────────────────────────

def run_validation(pdf_path: str):
    print("=" * 70)
    print("  Spend Analysis — Phase 0 PDF Validation")
    print("=" * 70)
    print(f"  File: {pdf_path}")

    year = infer_year_from_path(pdf_path)
    print(f"  Inferred statement year: {year}")
    print()

    total_pages   = 0
    total_tables  = 0
    total_rows    = 0
    transactions  = []
    failed_rows   = []

    try:
        with pdfplumber.open(pdf_path) as pdf:
            total_pages = len(pdf.pages)
            print(f"  Pages: {total_pages}")
            print()

            for page_num, page in enumerate(pdf.pages, start=1):
                tables = page.extract_tables()

                if not tables:
                    print(f"  Page {page_num}: no tables found")
                    continue

                print(f"  Page {page_num}: {len(tables)} table(s) found")
                total_tables += len(tables)

                for t_idx, table in enumerate(tables):
                    raw_cols = max((len(r) for r in table), default=0)
                    print(f"    Table {t_idx + 1}: {len(table)} rows × {raw_cols} cols", end="")

                    if is_balance_only_table(table):
                        print("  [skipped — balance-only]")
                        continue

                    # Merge Nationwide multi-row transactions
                    merged = merge_nationwide_rows(table, year)
                    total_rows += len(merged)
                    print(f"  → {len(merged)} merged rows")

                    for row in merged:
                        tx = try_parse_row(row, year)
                        if tx:
                            transactions.append(tx)
                        else:
                            if row and any(c and str(c).strip() for c in row):
                                if not is_header_row(row):
                                    failed_rows.append(row)

    except FileNotFoundError:
        print(f"\nERROR: File not found: {pdf_path}")
        sys.exit(1)
    except Exception as e:
        print(f"\nERROR opening PDF: {e}")
        sys.exit(1)

    # ── Print transactions ──────────────────────────────────────────────────
    print()
    print("=" * 70)
    print("  EXTRACTED TRANSACTIONS")
    print("=" * 70)
    print(f"  {'Date':<14} {'Description':<45} {'Debit':>10} {'Credit':>10} {'Balance':>12}")
    print("  " + "-" * 93)

    for tx in sorted(transactions, key=lambda t: t.date):
        date_str  = tx.date.strftime("%d %b %Y")
        debit_s   = f"£{tx.debit:.2f}"   if tx.debit   is not None else ""
        credit_s  = f"£{tx.credit:.2f}"  if tx.credit  is not None else ""
        balance_s = f"£{tx.balance:.2f}" if tx.balance  is not None else ""
        desc      = tx.description[:45]
        print(f"  {date_str:<14} {desc:<45} {debit_s:>10} {credit_s:>10} {balance_s:>12}")

    # ── Print failed rows ───────────────────────────────────────────────────
    if failed_rows:
        print()
        print("=" * 70)
        print("  FAILED ROWS (could not parse after merging)")
        print("=" * 70)
        for i, row in enumerate(failed_rows[:20]):
            print(f"  [{i+1}] {row}")
        if len(failed_rows) > 20:
            print(f"  ... and {len(failed_rows) - 20} more")

    # ── Summary ─────────────────────────────────────────────────────────────
    data_rows    = len(transactions) + len(failed_rows)
    success_rate = (len(transactions) / data_rows * 100) if data_rows > 0 else 0

    print()
    print("=" * 70)
    print("  SUMMARY")
    print("=" * 70)
    print(f"  Pages examined:      {total_pages}")
    print(f"  Tables found:        {total_tables}")
    print(f"  Merged rows:         {total_rows}")
    print(f"  Transactions parsed: {len(transactions)}")
    print(f"  Failed rows:         {len(failed_rows)}")
    print(f"  Success rate:        {success_rate:.1f}%")
    print()

    if success_rate >= 95:
        print("  RESULT: PASS ✓")
        print("  pdfplumber.extract_tables() is viable for this statement.")
        print("  Proceed to application development.")
    elif success_rate >= 50:
        print("  RESULT: PARTIAL")
        print("  pdfplumber extracted some data but not enough.")
        print("  Review the failed rows above.")
        print("  Do NOT begin application development yet.")
    else:
        print("  RESULT: FAIL ✗")
        print("  pdfplumber.extract_tables() did not work for this statement.")
        print("  Stop. Do not attempt to fix the parser.")
        print("  Report back for alternative library recommendation.")

    print("=" * 70)


# ── Entry point ────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python poc_pdf_test.py \"path\\to\\statement.pdf\"")
        print()
        print("Drag your Nationwide PDF onto this script, or provide the path above.")
        sys.exit(1)

    run_validation(sys.argv[1])
