"""app/ui/pages/dashboard.py — Dashboard home page."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QScrollArea, QSizePolicy, QFrame,
                              QPushButton, QMessageBox)
from PyQt6.QtCore import Qt, pyqtSignal

from app.ui.theme import C
from app.ui.charts import DonutChart, LineChart, HBarChart
from app.ui.widgets.cards import SummaryCard, SummaryCardRow
from app.ui.widgets.filters import DateFilterBar
from app.ui.widgets.tx_table import TransactionTable
from app import database as db
from app.config import DB_PATH
from app.ui.widgets.cards import MetricCard


def _heading(text: str, size=16) -> QLabel:
    l = QLabel(text)
    l.setStyleSheet(f"color:{C['text']};font-size:{size}px;font-weight:700;")
    return l


class DashboardPage(QWidget):
    navigate = pyqtSignal(str, dict)  # page_name, params

    def __init__(self, parent=None):
        super().__init__(parent)
        self._start = self._end = ""
        self._build()

    def _build(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(28, 24, 28, 24)
        root.setSpacing(20)

        # Header
        hdr = QHBoxLayout()
        title = QLabel("Dashboard")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        hdr.addWidget(title)
        hdr.addStretch()

        clear_btn = QPushButton("Clear Data")
        clear_btn.setFixedHeight(36)
        clear_btn.setProperty("danger", True)
        clear_btn.setToolTip("Delete all imported statements and transactions for a fresh start.\nCategories and rules are not affected.")
        clear_btn.clicked.connect(self._clear_data)
        hdr.addWidget(clear_btn)

        root.addLayout(hdr)

        # Date filter
        self._date_filter = DateFilterBar()
        self._date_filter.filter_changed.connect(self._on_date_changed)
        root.addWidget(self._date_filter)

        # Scrollable body
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QFrame.Shape.NoFrame)
        body_w = QWidget()
        body = QVBoxLayout(body_w)
        body.setContentsMargins(0, 0, 0, 0)
        body.setSpacing(20)
        scroll.setWidget(body_w)
        root.addWidget(scroll)

        # ── Summary cards ─────────────────────────────────────────
        self._card_income   = SummaryCard("Total Income",   "£0",  "This period", "💰", C["green"])
        self._card_spend    = SummaryCard("Total Spending", "£0",  "This period", "💳", C["red"])
        self._card_balance  = SummaryCard("Current Balance", "—",  "Latest statement", "🏦", C["blue"])
        self._card_net      = SummaryCard("Net Position",   "£0",  "This period",     "📊", C["accent"])
        self._card_txcount  = SummaryCard("Transactions",   "0",   "Imported",        "📋")
        self._card_stmts    = SummaryCard("Statements",     "0",   "Imported",        "📄")

        for card, page in [
            (self._card_income,  "transactions"),
            (self._card_spend,   "transactions"),
            (self._card_balance, "imports"),
            (self._card_txcount, "transactions"),
        ]:
            card.clicked.connect(lambda p=page: self.navigate.emit(p, {}))

        body.addWidget(SummaryCardRow([
            self._card_income,   self._card_spend,
            self._card_balance,  self._card_net,
            self._card_txcount,  self._card_stmts,
        ]))

        # ── Charts row ────────────────────────────────────────────
        charts_row = QHBoxLayout()
        charts_row.setSpacing(16)

        # Donut — spending by category
        donut_wrap = self._wrap("Spending by Category  (click a slice to explore)")
        self._donut = DonutChart()
        self._donut.category_clicked.connect(
            lambda cat: self.navigate.emit(
                "transactions",
                {"category": cat,
                 "_crumbs": [("Dashboard", "dashboard", {}), (cat, None, {})]}))
        donut_wrap.layout().addWidget(self._donut)
        charts_row.addWidget(donut_wrap, 5)

        # Top merchants
        merch_wrap = self._wrap("Top Merchants  (click to explore)")
        self._hbar = HBarChart()
        merch_wrap.layout().addWidget(self._hbar)
        charts_row.addWidget(merch_wrap, 5)

        body.addLayout(charts_row)

        # ── Trend chart ───────────────────────────────────────────
        trend_wrap = self._wrap("Monthly Spending Trend")
        self._line = LineChart()
        trend_wrap.layout().addWidget(self._line)
        body.addWidget(trend_wrap)

        # ── Recent transactions ───────────────────────────────────
        body.addWidget(_heading("Recent Transactions", 14))
        self._tx_table = TransactionTable(show_export=False)
        self._tx_table.setFixedHeight(280)
        self._tx_table.row_selected.connect(
            lambda r: self.navigate.emit("transactions", {"tx": r}))
        body.addWidget(self._tx_table)

        # ── Import Summary strip ──────────────────────────────────
        imp_wrap = self._wrap("📥  Import Summary")
        imp_row = QHBoxLayout()
        imp_row.setSpacing(12)
        self._imp_stmts   = MetricCard("Statements Imported",  "0")
        self._imp_txs     = MetricCard("Transactions Imported", "0")
        self._imp_unique  = MetricCard("Unique Merchants",      "0")
        self._imp_uncat   = MetricCard("Uncategorised",         "0",  C["red"])
        for c in [self._imp_stmts, self._imp_txs, self._imp_unique, self._imp_uncat]:
            imp_row.addWidget(c)
        imp_wrap.layout().addLayout(imp_row)
        body.addWidget(imp_wrap)

        body.addStretch()

    def _wrap(self, title: str) -> QFrame:
        frame = QFrame()
        frame.setStyleSheet(f"""
            QFrame {{
                background:{C['bg2']};
                border:1px solid {C['border']};
                border-radius:14px;
            }}
        """)
        lay = QVBoxLayout(frame)
        lay.setContentsMargins(16, 14, 16, 16)
        lay.setSpacing(10)
        h = QLabel(title)
        h.setStyleSheet(f"color:{C['text']};font-size:14px;font-weight:700;")
        lay.addWidget(h)
        return frame

    def _on_date_changed(self, start: str, end: str):
        self._start = start
        self._end   = end
        self.refresh()

    def _clear_data(self):
        reply = QMessageBox.warning(
            self,
            "Clear All Data",
            "This will permanently delete all imported statements and transactions.\n\n"
            "Your categories and categorisation rules will not be affected.\n\n"
            "Are you sure you want to start fresh?",
            QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.Cancel,
            QMessageBox.StandardButton.Cancel,
        )
        if reply != QMessageBox.StandardButton.Yes:
            return

        deleted = db.clear_all_data(DB_PATH)

        QMessageBox.information(
            self,
            "Data Cleared",
            f"Deleted {deleted['transactions']:,} transactions "
            f"from {deleted['statements']:,} statement(s).\n\n"
            "You can now import fresh statements.",
        )

        self.refresh()
        # Navigate to Imports so the user can upload immediately
        self.navigate.emit("imports", {})

    def refresh(self):
        s, e = self._start or None, self._end or None
        stats = db.get_dashboard_stats(DB_PATH, s, e)
        self._card_income.set_value(f"£{stats['total_income']:,.2f}")
        self._card_spend.set_value(f"£{stats['total_spending']:,.2f}")
        net = stats["net"]
        col = C["green"] if net >= 0 else C["red"]
        self._card_net._value_lbl.setStyleSheet(f"color:{col};font-size:22px;font-weight:700;")
        self._card_net.set_value(f"£{net:,.2f}")
        self._card_txcount.set_value(str(stats["tx_count"]))
        self._card_stmts.set_value(str(stats["stmt_count"]))

        # ── Current Balance: source of truth = latest statement closing balance ──
        latest = db.get_latest_statement(DB_PATH)
        if latest and latest.get("closing_balance") is not None:
            bal = latest["closing_balance"]
            period = latest.get("period_label") or latest.get("statement_date") or latest["filename"]
            self._card_balance.set_value(f"£{bal:,.2f}")
            self._card_balance._sub_lbl.setText(f"Source: {period}")
            bal_col = C["green"] if bal >= 0 else C["red"]
            self._card_balance._value_lbl.setStyleSheet(
                f"color:{bal_col};font-size:22px;font-weight:700;")
        else:
            self._card_balance.set_value("—")
            self._card_balance._sub_lbl.setText("No statement imported yet")

        cat_data = db.get_spending_by_category(DB_PATH, s, e)
        self._donut.update(cat_data)

        merch_data = db.get_top_merchants(DB_PATH, s, e, limit=8)
        self._hbar.update(merch_data)

        trend = db.get_monthly_spending(DB_PATH, months=12)
        self._line.update(trend)

        recent = db.get_recent_transactions(DB_PATH, limit=20)
        self._tx_table.load(recent)

        imp = db.get_import_summary(DB_PATH)
        self._imp_stmts.set_value(str(imp["statements"]))
        self._imp_txs.set_value(str(imp["transactions"]))
        self._imp_unique.set_value(str(imp["unique_merchants"]))
        self._imp_uncat.set_value(str(imp["uncategorised"]))

    def showEvent(self, e):
        super().showEvent(e)
        self.refresh()
