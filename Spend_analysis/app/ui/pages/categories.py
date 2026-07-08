"""app/ui/pages/categories.py — Categories grid with drill-down detail."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QScrollArea, QFrame, QGridLayout, QPushButton,
                              QSizePolicy, QStackedWidget)
from PyQt6.QtCore import Qt, pyqtSignal
from PyQt6.QtGui import QFont

from app.ui.theme import C, CHART_COLORS
from app.ui.widgets.cards import CategoryCard, MetricCard
from app.ui.widgets.filters import DateFilterBar
from app.ui.widgets.tx_table import TransactionTable
from app.ui.charts import VBarChart
from app import database as db
from app.config import DB_PATH


def _heading(text, size=16) -> QLabel:
    l = QLabel(text)
    l.setStyleSheet(f"color:{C['text']};font-size:{size}px;font-weight:700;")
    return l


class CategoryDetail(QWidget):
    back_requested = pyqtSignal()
    navigate = pyqtSignal(str, dict)

    def __init__(self, parent=None):
        super().__init__(parent)
        self._cat = ""
        self._build()

    def _build(self):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(16)

        # Back
        back = QPushButton("← All Categories")
        back.setProperty("flat", True)
        back.setFixedWidth(180)
        back.clicked.connect(self.back_requested)
        lay.addWidget(back)

        self._title = _heading("", 22)
        lay.addWidget(self._title)

        # Metrics
        metric_row = QHBoxLayout()
        self._m_total   = MetricCard("Total Spent",    "£0",  C["red"])
        self._m_count   = MetricCard("Transactions",   "0")
        self._m_avg     = MetricCard("Avg Purchase",   "£0")
        self._m_largest = MetricCard("Largest",        "£0",  C["accent"])
        for m in [self._m_total, self._m_count, self._m_avg, self._m_largest]:
            metric_row.addWidget(m)
        lay.addLayout(metric_row)

        # Chart
        self._chart = VBarChart()
        self._chart.setFixedHeight(200)
        lay.addWidget(self._chart)

        # Transactions
        lay.addWidget(_heading("Transactions", 14))
        self._tbl = TransactionTable()
        self._tbl.row_selected.connect(
            lambda r: self.navigate.emit("transactions", {"tx": r}))
        lay.addWidget(self._tbl)

    def load(self, category: str, start: str | None, end: str | None):
        self._cat = category
        self._title.setText(category)

        # Metrics from summary
        all_cats = db.get_category_summary(DB_PATH, start, end)
        row = next((r for r in all_cats if r["category"] == category), {})
        total  = row.get("total_debit", 0) or 0
        count  = row.get("tx_count", 0) or 0
        avg    = total / count if count else 0

        # Largest tx
        txs = db.get_transactions(DB_PATH, start=start, end=end,
                                   category=category, expense_only=True)
        largest = max((t.get("debit") or 0 for t in txs), default=0)

        self._m_total.set_value(f"£{total:,.2f}")
        self._m_count.set_value(str(count))
        self._m_avg.set_value(f"£{avg:,.2f}")
        self._m_largest.set_value(f"£{largest:,.2f}")

        # Monthly chart
        monthly = db.get_monthly_spending(DB_PATH, months=12)
        # rebuild per-category via transactions
        self._chart.update(
            [{"month": t["date"][:7], "total": t.get("debit") or 0} for t in txs
             if t.get("debit")],
            color=CHART_COLORS[0])

        self._tbl.load(txs)


class CategoriesPage(QWidget):
    navigate = pyqtSignal(str, dict)

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

        # Stack: grid vs detail
        self._stack = QStackedWidget()
        root.addWidget(self._stack)

        # Grid page
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

        # Detail page
        self._detail = CategoryDetail()
        self._detail.back_requested.connect(lambda: self._stack.setCurrentIndex(0))
        self._detail.navigate.connect(self.navigate)
        self._stack.addWidget(self._detail)

    def _on_date_changed(self, start: str, end: str):
        self._start = start
        self._end   = end
        self._load_grid()

    def _load_grid(self):
        # Clear grid
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
                row.get("tx_count", 0) or 0,
            )
            card.clicked.connect(self._open_category)
            self._grid.addWidget(card, i // cols, i % cols)

    def _open_category(self, name: str):
        self._detail.load(name, self._start or None, self._end or None)
        self._stack.setCurrentIndex(1)

    def showEvent(self, e):
        super().showEvent(e)
        self._load_grid()
