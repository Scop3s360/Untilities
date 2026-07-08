"""app/ui/pages/categories.py — Categories grid with drill-down detail and in-place category editing."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QScrollArea, QFrame, QGridLayout, QPushButton,
                              QSizePolicy, QStackedWidget, QSplitter, QComboBox,
                              QMessageBox)
from PyQt6.QtCore import Qt, pyqtSignal

from app.ui.theme import C, CHART_COLORS
from app.ui.widgets.cards import CategoryCard, MetricCard
from app.ui.widgets.filters import DateFilterBar
from app.ui.widgets.tx_table import TransactionTable
from app.ui.charts import VBarChart
from app import database as db
from app.config import DB_PATH, RULES_DB_PATH
from rule_engine import db as rule_db


def _heading(text, size=16) -> QLabel:
    l = QLabel(text)
    l.setStyleSheet(f"color:{C['text']};font-size:{size}px;font-weight:700;")
    return l

def _lbl(text, size=12, bold=False, color=None) -> QLabel:
    l = QLabel(str(text))
    l.setStyleSheet(f"color:{color or C['text']};font-size:{size}px;"
                    + ("font-weight:700;" if bold else ""))
    l.setWordWrap(True)
    return l


# ── Inline transaction editor (right panel) ────────────────────────────────────

class TxEditPanel(QFrame):
    """Right-side panel: shows selected transaction details + category editor."""
    category_saved = pyqtSignal()   # tells the detail view to reload the table

    def __init__(self, parent=None):
        super().__init__(parent)
        self._tx: dict | None = None
        self.setMinimumWidth(280)
        self.setMaximumWidth(340)
        self.setStyleSheet(f"""
            TxEditPanel {{
                background:{C['bg2']};
                border-left:1px solid {C['border']};
                border-radius:0;
            }}
        """)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(18, 18, 18, 18)
        lay.setSpacing(10)

        # Prompt shown before any selection
        self._prompt = _lbl("← Select a transaction to edit its category",
                            12, color=C["text3"])
        self._prompt.setAlignment(Qt.AlignmentFlag.AlignCenter)
        lay.addWidget(self._prompt)

        # Detail fields (hidden until a row is selected)
        self._detail_w = QWidget()
        detail_lay = QVBoxLayout(self._detail_w)
        detail_lay.setContentsMargins(0, 0, 0, 0)
        detail_lay.setSpacing(10)

        self._merchant_lbl = _lbl("", 15, bold=True)
        detail_lay.addWidget(self._merchant_lbl)

        self._fields: dict[str, QLabel] = {}
        for key in ("Date", "Description", "Debit", "Credit", "Balance"):
            row = QHBoxLayout()
            row.addWidget(_lbl(key + ":", 11, color=C["text2"]))
            val = _lbl("—", 11)
            val.setAlignment(Qt.AlignmentFlag.AlignRight)
            self._fields[key] = val
            row.addWidget(val)
            detail_lay.addWidget(QWidget())   # spacer line
            detail_lay.itemAt(detail_lay.count()-1).widget().setLayout(row)

        sep = QFrame()
        sep.setFrameShape(QFrame.Shape.HLine)
        sep.setStyleSheet(f"color:{C['border']};")
        detail_lay.addWidget(sep)

        detail_lay.addWidget(_lbl("Current Category", 11, color=C["text2"]))
        self._current_cat_lbl = _lbl("", 13, bold=True, color=C["accent"])
        detail_lay.addWidget(self._current_cat_lbl)

        detail_lay.addWidget(_lbl("Change to:", 11, color=C["text2"]))
        self._cat_combo = QComboBox()
        self._cat_combo.setFixedHeight(36)
        cats = sorted(c.name for c in rule_db.get_all_categories(RULES_DB_PATH))
        self._cat_combo.addItems(cats)
        detail_lay.addWidget(self._cat_combo)

        self._save_btn = QPushButton("Save Category")
        self._save_btn.setFixedHeight(38)
        self._save_btn.clicked.connect(self._save)
        detail_lay.addWidget(self._save_btn)

        self._saved_lbl = _lbl("✓ Saved", 11, color=C["green"])
        self._saved_lbl.setVisible(False)
        detail_lay.addWidget(self._saved_lbl)
        detail_lay.addStretch()

        self._detail_w.setVisible(False)
        lay.addWidget(self._detail_w)
        lay.addStretch()

    def load(self, tx: dict):
        self._tx = tx
        self._saved_lbl.setVisible(False)
        self._prompt.setVisible(False)
        self._detail_w.setVisible(True)

        from datetime import datetime
        try:
            date_str = datetime.strptime(tx.get("date",""), "%Y-%m-%d").strftime("%d %b %Y")
        except Exception:
            date_str = tx.get("date", "—")

        debit  = tx.get("debit")
        credit = tx.get("credit")
        bal    = tx.get("balance")

        self._merchant_lbl.setText(tx.get("merchant") or tx.get("description", ""))
        self._fields["Date"].setText(date_str)
        self._fields["Description"].setText(tx.get("description", "—"))
        self._fields["Debit"].setText(f"£{debit:,.2f}" if debit else "—")
        self._fields["Credit"].setText(f"£{credit:,.2f}" if credit else "—")
        self._fields["Balance"].setText(f"£{bal:,.2f}" if bal else "—")
        self._current_cat_lbl.setText(tx.get("category", "Other"))

        idx = self._cat_combo.findText(tx.get("category", "Other"))
        if idx >= 0:
            self._cat_combo.setCurrentIndex(idx)

    def _save(self):
        if not self._tx:
            return
        new_cat = self._cat_combo.currentText()
        db.update_transaction_category(DB_PATH, self._tx["id"], new_cat)
        self._tx["category"] = new_cat
        self._current_cat_lbl.setText(new_cat)
        self._saved_lbl.setVisible(True)
        self.category_saved.emit()


# ── Category detail view ───────────────────────────────────────────────────────

class CategoryDetail(QWidget):
    back_requested = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._cat = ""
        self._start = self._end = None
        self._build()

    def _build(self):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(12)

        # Back button
        back = QPushButton("← All Categories")
        back.setProperty("flat", True)
        back.setFixedWidth(180)
        back.clicked.connect(self.back_requested)
        lay.addWidget(back)

        self._title = _heading("", 22)
        lay.addWidget(self._title)

        # Metrics row
        metric_row = QHBoxLayout()
        self._m_total   = MetricCard("Total Spent",   "£0", C["red"])
        self._m_count   = MetricCard("Transactions",  "0")
        self._m_avg     = MetricCard("Avg Purchase",  "£0")
        self._m_largest = MetricCard("Largest",       "£0", C["accent"])
        for m in [self._m_total, self._m_count, self._m_avg, self._m_largest]:
            metric_row.addWidget(m)
        lay.addLayout(metric_row)

        # Monthly chart
        self._chart = VBarChart()
        self._chart.setFixedHeight(180)
        lay.addWidget(self._chart)

        # ── Splitter: transaction table | edit panel ─────────────────
        lay.addWidget(_heading("Transactions", 14))
        hint = _lbl("Click any transaction to edit its category →",
                    11, color=C["text3"])
        lay.addWidget(hint)

        splitter = QSplitter(Qt.Orientation.Horizontal)
        splitter.setHandleWidth(1)

        self._tbl = TransactionTable(show_export=False)
        # row_selected opens the edit panel — does NOT navigate away
        self._tbl.row_selected.connect(self._on_tx_selected)
        # right-click category change also triggers a reload
        self._tbl.category_changed.connect(self._reload_table)
        splitter.addWidget(self._tbl)

        self._edit_panel = TxEditPanel()
        self._edit_panel.category_saved.connect(self._reload_table)
        splitter.addWidget(self._edit_panel)
        splitter.setSizes([700, 300])
        splitter.setCollapsible(1, False)

        lay.addWidget(splitter)

    def _on_tx_selected(self, tx: dict):
        """Load selected transaction into the edit panel — stay on this page."""
        self._edit_panel.load(tx)

    def _reload_table(self):
        """Refresh transaction list after a category change."""
        txs = db.get_transactions(
            DB_PATH, start=self._start, end=self._end,
            category=self._cat, expense_only=True)
        self._tbl.load(txs)

    def load(self, category: str, start: str | None, end: str | None):
        self._cat   = category
        self._start = start
        self._end   = end
        self._title.setText(category)

        all_cats = db.get_category_summary(DB_PATH, start, end)
        row = next((r for r in all_cats if r["category"] == category), {})
        total   = row.get("total_debit", 0) or 0
        count   = row.get("tx_count",   0) or 0
        avg     = total / count if count else 0

        txs = db.get_transactions(
            DB_PATH, start=start, end=end,
            category=category, expense_only=True)
        largest = max((t.get("debit") or 0 for t in txs), default=0)

        self._m_total.set_value(f"£{total:,.2f}")
        self._m_count.set_value(str(count))
        self._m_avg.set_value(f"£{avg:,.2f}")
        self._m_largest.set_value(f"£{largest:,.2f}")

        self._chart.update(
            [{"month": t["date"][:7], "total": t.get("debit") or 0}
             for t in txs if t.get("debit")],
            color=CHART_COLORS[0])

        self._tbl.load(txs)


# ── Categories page ────────────────────────────────────────────────────────────

class CategoriesPage(QWidget):
    navigate = pyqtSignal(str, dict)   # kept for main_window compatibility

    def __init__(self, parent=None):
        super().__init__(parent)
        self._start = self._end = ""
        self._build()

    def _build(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(28, 24, 28, 24)
        root.setSpacing(16)

        # Header
        hdr = QHBoxLayout()
        title = QLabel("Categories")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        hdr.addWidget(title)
        hdr.addStretch()
        root.addLayout(hdr)

        # Date filter
        self._date_filter = DateFilterBar()
        self._date_filter.filter_changed.connect(self._on_date_changed)
        root.addWidget(self._date_filter)

        # Stack: card grid | category detail
        self._stack = QStackedWidget()
        root.addWidget(self._stack)

        # ── Grid page ────────────────────────────────────────────
        grid_w = QWidget()
        grid_lay = QVBoxLayout(grid_w)
        grid_lay.setContentsMargins(0, 0, 0, 0)
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QFrame.Shape.NoFrame)
        self._grid_inner = QWidget()
        self._grid = QGridLayout(self._grid_inner)
        self._grid.setSpacing(14)
        self._grid.setAlignment(Qt.AlignmentFlag.AlignTop | Qt.AlignmentFlag.AlignLeft)
        scroll.setWidget(self._grid_inner)
        grid_lay.addWidget(scroll)
        self._stack.addWidget(grid_w)

        # ── Detail page ──────────────────────────────────────────
        self._detail = CategoryDetail()
        self._detail.back_requested.connect(lambda: self._stack.setCurrentIndex(0))
        self._stack.addWidget(self._detail)

    def _on_date_changed(self, start: str, end: str):
        self._start = start
        self._end   = end
        self._load_grid()

    def _load_grid(self):
        while self._grid.count():
            item = self._grid.takeAt(0)
            if item.widget():
                item.widget().deleteLater()

        data = db.get_category_summary(DB_PATH, self._start or None, self._end or None)
        cols = 5
        for i, row in enumerate(data):
            card = CategoryCard(
                row["category"],
                row.get("total_debit", 0) or 0,
                row.get("tx_count",   0) or 0,
            )
            card.clicked.connect(self._open_category)
            self._grid.addWidget(card, i // cols, i % cols)

    def _open_category(self, name: str):
        self._detail.load(name, self._start or None, self._end or None)
        self._stack.setCurrentIndex(1)

    def showEvent(self, e):
        super().showEvent(e)
        self._load_grid()
