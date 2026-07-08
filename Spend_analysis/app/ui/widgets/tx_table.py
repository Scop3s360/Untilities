"""app/ui/widgets/tx_table.py — Reusable sortable transaction table."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QTableWidget, QTableWidgetItem,
                              QHeaderView, QAbstractItemView, QLabel, QHBoxLayout,
                              QPushButton, QFileDialog)
from PyQt6.QtCore import Qt, pyqtSignal
from PyQt6.QtGui import QColor, QFont
import csv, os
from datetime import datetime

from app.ui.theme import C
from app.config import EXPORTS_DIR, DB_PATH, RULES_DB_PATH
from rule_engine import db as rule_db
from app import database as db

try:
    from PyQt6.QtWidgets import QMenu
    from PyQt6.QtGui import QAction
except ImportError:
    pass


COLUMNS = [
    ("Date",        "date",        120),
    ("Merchant",    "merchant",    180),
    ("Description", "description", 280),
    ("Category",    "category",    130),
    ("Debit",       "debit",       100),
    ("Credit",      "credit",      100),
    ("Balance",     "balance",     100),
    ("Source",      "source",      160),
]


def _fmt_currency(v) -> str:
    if v is None: return ""
    try:    return f"£{float(v):,.2f}"
    except: return str(v)


class TransactionTable(QWidget):
    """
    Sortable, selectable transaction table.
    Emits `row_selected` with the full row dict when a row is clicked.
    Emits `category_changed` after an in-place right-click category edit.
    """
    row_selected    = pyqtSignal(dict)
    category_changed = pyqtSignal()

    def __init__(self, show_export: bool = True, parent=None):
        super().__init__(parent)
        self._data: list[dict] = []
        self._build(show_export)

    def _build(self, show_export: bool):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(8)

        # Toolbar
        toolbar = QHBoxLayout()
        self._count_lbl = QLabel("0 transactions")
        self._count_lbl.setStyleSheet(f"color:{C['text2']};font-size:12px;")
        toolbar.addWidget(self._count_lbl)
        toolbar.addStretch()
        if show_export:
            exp_btn = QPushButton("⬇ Export CSV")
            exp_btn.setFixedHeight(32)
            exp_btn.setProperty("flat", True)
            exp_btn.clicked.connect(self._export_csv)
            toolbar.addWidget(exp_btn)
        lay.addLayout(toolbar)

        # Table
        self._tbl = QTableWidget()
        self._tbl.setColumnCount(len(COLUMNS))
        self._tbl.setHorizontalHeaderLabels([c[0] for c in COLUMNS])
        self._tbl.setAlternatingRowColors(True)
        self._tbl.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
        self._tbl.setSelectionBehavior(QAbstractItemView.SelectionBehavior.SelectRows)
        self._tbl.setSelectionMode(QAbstractItemView.SelectionMode.SingleSelection)
        self._tbl.verticalHeader().setVisible(False)
        self._tbl.setSortingEnabled(True)
        hdr = self._tbl.horizontalHeader()
        for i, (_, _, w) in enumerate(COLUMNS):
            self._tbl.setColumnWidth(i, w)
        hdr.setStretchLastSection(True)
        self._tbl.cellClicked.connect(self._on_cell_click)
        # Right-click context menu
        self._tbl.setContextMenuPolicy(Qt.ContextMenuPolicy.CustomContextMenu)
        self._tbl.customContextMenuRequested.connect(self._show_context_menu)
        lay.addWidget(self._tbl)

    def load(self, rows: list[dict]):
        self._data = rows
        self._tbl.setSortingEnabled(False)
        self._tbl.setRowCount(len(rows))

        for r, row in enumerate(rows):
            for c, (_, key, _) in enumerate(COLUMNS):
                val = row.get(key, "") or ""
                if key in ("debit", "credit", "balance"):
                    display = _fmt_currency(val)
                elif key == "date":
                    try:
                        display = datetime.strptime(str(val), "%Y-%m-%d").strftime("%d %b %Y")
                    except:
                        display = str(val)
                else:
                    display = str(val)

                item = QTableWidgetItem(display)
                item.setData(Qt.ItemDataRole.UserRole, row)

                # Colour-code debit/credit columns
                if key == "debit" and val:
                    item.setForeground(QColor(C["red"]))
                elif key == "credit" and val:
                    item.setForeground(QColor(C["green"]))

                # Right-align numbers
                if key in ("debit", "credit", "balance"):
                    item.setTextAlignment(
                        Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)

                self._tbl.setItem(r, c, item)

        self._tbl.setSortingEnabled(True)
        self._count_lbl.setText(f"{len(rows):,} transactions")

    def _on_cell_click(self, row: int, _col: int):
        item = self._tbl.item(row, 0)
        if item:
            data = item.data(Qt.ItemDataRole.UserRole)
            if data:
                self.row_selected.emit(data)

    def _show_context_menu(self, pos):
        """Right-click menu for in-place category change."""
        row = self._tbl.rowAt(pos.y())
        if row < 0: return
        item = self._tbl.item(row, 0)
        if not item: return
        tx = item.data(Qt.ItemDataRole.UserRole)
        if not tx: return

        menu = QMenu(self._tbl)
        menu.setStyleSheet(
            f"background:{C['bg3']};color:{C['text']};"
            f"border:1px solid {C['border2']};")
        change_menu = menu.addMenu(f"Change Category  (currently: {tx.get('category','?')})") 

        cats = sorted(c.name for c in rule_db.get_all_categories(RULES_DB_PATH))
        for cat in cats:
            act = QAction(cat, self._tbl)
            act.triggered.connect(lambda _, c=cat, t=tx: self._change_category(t, c))
            change_menu.addAction(act)

        menu.exec(self._tbl.viewport().mapToGlobal(pos))

    def _change_category(self, tx: dict, new_cat: str):
        db.update_transaction_category(DB_PATH, tx["id"], new_cat)
        tx["category"] = new_cat  # update in-place
        # Find and update the category cell directly
        for r in range(self._tbl.rowCount()):
            cell = self._tbl.item(r, 0)
            if cell and cell.data(Qt.ItemDataRole.UserRole) is tx:
                cat_col = next((i for i, (h,_,_) in enumerate(COLUMNS) if h == "Category"), None)
                if cat_col is not None:
                    self._tbl.item(r, cat_col).setText(new_cat)
                break
        self.category_changed.emit()

    def _export_csv(self):
        path, _ = QFileDialog.getSaveFileName(
            self, "Export Transactions", str(EXPORTS_DIR / "transactions.csv"),
            "CSV Files (*.csv)")
        if not path: return
        with open(path, "w", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            w.writerow([c[0] for c in COLUMNS])
            for row in self._data:
                w.writerow([row.get(k, "") or "" for _, k, _ in COLUMNS])
