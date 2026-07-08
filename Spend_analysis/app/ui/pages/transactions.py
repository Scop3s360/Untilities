"""app/ui/pages/transactions.py — Full transaction explorer with side panel."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QSplitter, QFrame, QComboBox, QPushButton,
                              QScrollArea, QSizePolicy, QMessageBox)
from PyQt6.QtCore import Qt, pyqtSignal

from app.ui.theme import C
from app.ui.widgets.tx_table import TransactionTable
from app.ui.widgets.filters import DateFilterBar, FilterBar
from app import database as db
from app.config import DB_PATH
from rule_engine import db as rule_db
from app.config import RULES_DB_PATH


def _lbl(text, size=12, bold=False, color=None) -> QLabel:
    l = QLabel(text)
    l.setStyleSheet(
        f"color:{color or C['text']};font-size:{size}px;"
        + ("font-weight:700;" if bold else ""))
    l.setWordWrap(True)
    return l


class DetailPanel(QFrame):
    """Right-side transaction detail and category editor."""
    category_changed = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._tx: dict | None = None
        self.setFixedWidth(320)
        self.setStyleSheet(f"""
            DetailPanel {{
                background:{C['bg2']};
                border-left:1px solid {C['border']};
            }}
        """)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(20, 20, 20, 20)
        lay.setSpacing(14)

        self._title = _lbl("Select a transaction", 15, bold=True)
        lay.addWidget(self._title)

        # Fields
        self._fields: dict[str, QLabel] = {}
        for key in ["Date", "Merchant", "Description", "Category",
                    "Debit", "Credit", "Balance", "Source"]:
            row = QHBoxLayout()
            row.addWidget(_lbl(key + ":", 11, color=C["text2"]))
            val = _lbl("—", 11)
            val.setAlignment(Qt.AlignmentFlag.AlignRight)
            row.addWidget(val)
            self._fields[key] = val
            lay.addLayout(row)

        # Category editor
        sep = QFrame()
        sep.setFrameShape(QFrame.Shape.HLine)
        sep.setStyleSheet(f"color:{C['border']};")
        lay.addWidget(sep)

        lay.addWidget(_lbl("Change Category", 12, bold=True))
        self._cat_combo = QComboBox()
        self._cat_combo.setFixedHeight(36)
        lay.addWidget(self._cat_combo)

        save_btn = QPushButton("Save Category")
        save_btn.clicked.connect(self._save_category)
        lay.addWidget(save_btn)

        lay.addStretch()

        # Load categories
        cats = [c.name for c in rule_db.get_all_categories(RULES_DB_PATH)]
        self._cat_combo.addItems(sorted(cats))

    def load(self, tx: dict):
        self._tx = tx
        from datetime import datetime
        try:
            date_str = datetime.strptime(tx.get("date",""), "%Y-%m-%d").strftime("%d %b %Y")
        except:
            date_str = tx.get("date","—")
        debit  = tx.get("debit")
        credit = tx.get("credit")
        bal    = tx.get("balance")

        self._title.setText(tx.get("merchant") or tx.get("description",""))
        self._fields["Date"].setText(date_str)
        self._fields["Merchant"].setText(tx.get("merchant","—") or "—")
        self._fields["Description"].setText(tx.get("description","—"))
        self._fields["Category"].setText(tx.get("category","—"))
        self._fields["Debit"].setText(f"£{debit:,.2f}" if debit else "—")
        self._fields["Credit"].setText(f"£{credit:,.2f}" if credit else "—")
        self._fields["Balance"].setText(f"£{bal:,.2f}" if bal else "—")
        self._fields["Source"].setText(tx.get("source","—") or "—")

        # Set combo to current category
        idx = self._cat_combo.findText(tx.get("category","Other"))
        if idx >= 0:
            self._cat_combo.setCurrentIndex(idx)

    def _save_category(self):
        if not self._tx: return
        new_cat = self._cat_combo.currentText()
        db.update_transaction_category(DB_PATH, self._tx["id"], new_cat)
        self._tx["category"] = new_cat
        self._fields["Category"].setText(new_cat)
        self.category_changed.emit()


class TransactionsPage(QWidget):
    navigate = pyqtSignal(str, dict)

    def __init__(self, initial_filters: dict | None = None, parent=None):
        super().__init__(parent)
        self._start = self._end = ""
        self._initial_filters = initial_filters or {}
        self._build()

    def _build(self):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(28, 24, 0, 24)
        lay.setSpacing(16)

        # Header
        hdr = QHBoxLayout()
        title = QLabel("Transactions")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        hdr.addWidget(title)
        hdr.addStretch()
        hdr.setContentsMargins(0, 0, 28, 0)
        lay.addLayout(hdr)

        # Date filter
        self._date_filter = DateFilterBar()
        self._date_filter.filter_changed.connect(self._on_date_changed)
        self._date_filter.setContentsMargins(0, 0, 28, 0)
        lay.addWidget(self._date_filter)

        # Category / search filter
        cats = [c.name for c in rule_db.get_all_categories(RULES_DB_PATH)]
        self._filter = FilterBar(cats)
        self._filter.changed.connect(self._load)
        self._filter.setContentsMargins(0, 0, 28, 0)
        lay.addWidget(self._filter)

        # Splitter: table | detail
        splitter = QSplitter(Qt.Orientation.Horizontal)
        splitter.setHandleWidth(1)

        self._table = TransactionTable()
        self._table.row_selected.connect(self._on_row_selected)
        splitter.addWidget(self._table)

        self._detail = DetailPanel()
        self._detail.category_changed.connect(self._load)
        splitter.addWidget(self._detail)
        splitter.setSizes([900, 320])
        splitter.setCollapsible(1, True)
        lay.addWidget(splitter)

    def _on_date_changed(self, start: str, end: str):
        self._start = start
        self._end   = end
        self._load()

    def _on_row_selected(self, row: dict):
        self._detail.load(row)

    def _load(self):
        s = self._start or None
        e = self._end   or None
        cat = self._filter.category
        search = self._filter.search or None
        tx_type = self._filter.tx_type

        rows = db.get_transactions(
            DB_PATH, start=s, end=e, category=cat, search=search,
            income_only=(tx_type == "Income"),
            expense_only=(tx_type == "Expenses"),
            uncategorised_only=(tx_type == "Uncategorised"),
        )
        self._table.load(rows)

    def set_filters(self, filters: dict):
        """Called from navigation to pre-filter."""
        if "category" in filters:
            idx = self._filter._cat.findText(filters["category"])
            if idx >= 0:
                self._filter._cat.setCurrentIndex(idx)
        if "merchant" in filters:
            self._filter._search.setText(filters["merchant"])

    def showEvent(self, e):
        super().showEvent(e)
        self._load()
