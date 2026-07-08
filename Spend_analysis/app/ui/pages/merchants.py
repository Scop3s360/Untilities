"""app/ui/pages/merchants.py — Merchants grid with drill-down detail."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QScrollArea, QFrame, QGridLayout, QPushButton,
                              QStackedWidget, QLineEdit)
from PyQt6.QtCore import Qt, pyqtSignal

from app.ui.theme import C
from app.ui.widgets.cards import MerchantCard, MetricCard
from app.ui.widgets.filters import DateFilterBar
from app.ui.widgets.tx_table import TransactionTable
from app.ui.charts import VBarChart
from app import database as db
from app.config import DB_PATH
from datetime import datetime


def _lbl(text, size=13, bold=False, color=None) -> QLabel:
    l = QLabel(text)
    l.setStyleSheet(
        f"color:{color or C['text']};font-size:{size}px;"
        + ("font-weight:700;" if bold else ""))
    return l


class MerchantDetail(QWidget):
    back_requested = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._build()

    def _build(self):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(16)

        back = QPushButton("← All Merchants")
        back.setProperty("flat", True)
        back.setFixedWidth(180)
        back.clicked.connect(self.back_requested)
        lay.addWidget(back)

        self._title   = _lbl("", 22, bold=True)
        self._cat_lbl = _lbl("", 12, color=C["text2"])
        lay.addWidget(self._title)
        lay.addWidget(self._cat_lbl)

        metric_row = QHBoxLayout()
        self._m_total   = MetricCard("Total Spent",    "£0", C["red"])
        self._m_count   = MetricCard("Purchases",      "0")
        self._m_avg     = MetricCard("Avg Purchase",   "£0")
        self._m_largest = MetricCard("Largest",        "£0", C["accent"])
        self._m_first   = MetricCard("First Visit",    "—")
        self._m_last    = MetricCard("Latest Visit",   "—")
        for m in [self._m_total, self._m_count, self._m_avg,
                  self._m_largest, self._m_first, self._m_last]:
            metric_row.addWidget(m)
        lay.addLayout(metric_row)

        self._chart = VBarChart()
        self._chart.setFixedHeight(200)
        lay.addWidget(self._chart)

        lay.addWidget(_lbl("All Transactions", 14, bold=True))
        self._tbl = TransactionTable()
        lay.addWidget(self._tbl)

    def load(self, merchant_row: dict):
        name = merchant_row["merchant"]
        self._title.setText(name)
        self._cat_lbl.setText(merchant_row.get("category","") or "")

        total   = merchant_row.get("total",   0) or 0
        count   = merchant_row.get("tx_count",0) or 0
        avg     = merchant_row.get("average", 0) or 0
        largest = merchant_row.get("largest", 0) or 0
        first   = merchant_row.get("first_date","—") or "—"
        last    = merchant_row.get("last_date", "—") or "—"

        def _fmt_date(d):
            try: return datetime.strptime(d, "%Y-%m-%d").strftime("%d %b %Y")
            except: return d

        self._m_total.set_value(f"£{total:,.2f}")
        self._m_count.set_value(str(count))
        self._m_avg.set_value(f"£{avg:,.2f}")
        self._m_largest.set_value(f"£{largest:,.2f}")
        self._m_first.set_value(_fmt_date(first))
        self._m_last.set_value(_fmt_date(last))

        monthly = db.get_merchant_monthly(DB_PATH, name)
        self._chart.update(monthly, label_key="month", value_key="total")

        txs = db.get_transactions(DB_PATH, merchant=name)
        self._tbl.load(txs)


class MerchantsPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self._start = self._end = ""
        self._all_data: list[dict] = []
        self._build()

    def _build(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(28, 24, 28, 24)
        root.setSpacing(16)

        hdr = QHBoxLayout()
        title = QLabel("Merchants")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        hdr.addWidget(title)
        hdr.addStretch()

        self._search = QLineEdit()
        self._search.setPlaceholderText("Search merchants…")
        self._search.setFixedWidth(260)
        self._search.setFixedHeight(36)
        self._search.textChanged.connect(self._filter_grid)
        hdr.addWidget(self._search)
        root.addLayout(hdr)

        self._date_filter = DateFilterBar()
        self._date_filter.filter_changed.connect(self._on_date_changed)
        root.addWidget(self._date_filter)

        self._stack = QStackedWidget()
        root.addWidget(self._stack)

        # Grid
        grid_w = QWidget()
        glay = QVBoxLayout(grid_w)
        glay.setContentsMargins(0, 0, 0, 0)
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QFrame.Shape.NoFrame)
        self._grid_inner = QWidget()
        self._grid = QGridLayout(self._grid_inner)
        self._grid.setSpacing(12)
        self._grid.setAlignment(Qt.AlignmentFlag.AlignTop | Qt.AlignmentFlag.AlignLeft)
        scroll.setWidget(self._grid_inner)
        glay.addWidget(scroll)
        self._stack.addWidget(grid_w)

        # Detail
        self._detail = MerchantDetail()
        self._detail.back_requested.connect(lambda: self._stack.setCurrentIndex(0))
        self._stack.addWidget(self._detail)

    def _on_date_changed(self, start: str, end: str):
        self._start = start
        self._end   = end
        self._load_grid()

    def _load_grid(self):
        self._all_data = db.get_merchant_summary(
            DB_PATH, self._start or None, self._end or None)
        self._render_grid(self._all_data)

    def _filter_grid(self, text: str):
        q = text.lower()
        filtered = [d for d in self._all_data if q in d["merchant"].lower()] if q else self._all_data
        self._render_grid(filtered)

    def _render_grid(self, data: list[dict]):
        while self._grid.count():
            item = self._grid.takeAt(0)
            if item.widget():
                item.widget().deleteLater()
        cols = 5
        for i, row in enumerate(data):
            card = MerchantCard(
                row["merchant"],
                row.get("total",    0) or 0,
                row.get("tx_count", 0) or 0,
                row.get("category", "") or "",
            )
            card.clicked.connect(lambda _, r=row: self._open_merchant(r))
            self._grid.addWidget(card, i // cols, i % cols)

    def _open_merchant(self, row: dict):
        self._detail.load(row)
        self._stack.setCurrentIndex(1)

    def showEvent(self, e):
        super().showEvent(e)
        self._load_grid()
