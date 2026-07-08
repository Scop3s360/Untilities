"""app/ui/pages/reports.py — Built-in reports page."""
from __future__ import annotations

import csv
from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QPushButton, QTabWidget, QFileDialog, QFrame,
                              QScrollArea, QTableWidget, QTableWidgetItem,
                              QAbstractItemView, QHeaderView, QSizePolicy)
from PyQt6.QtCore import Qt

from app.ui.theme import C
from app.ui.widgets.filters import DateFilterBar
from app.ui.charts import HBarChart, VBarChart, LineChart
from app import database as db
from app.config import DB_PATH, EXPORTS_DIR
from datetime import datetime


def _section(title: str) -> QLabel:
    l = QLabel(title)
    l.setStyleSheet(f"color:{C['text']};font-size:15px;font-weight:700;padding:8px 0;")
    return l


def _simple_table(headers: list[str], rows: list[list]) -> QTableWidget:
    tbl = QTableWidget(len(rows), len(headers))
    tbl.setHorizontalHeaderLabels(headers)
    tbl.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
    tbl.setAlternatingRowColors(True)
    tbl.verticalHeader().setVisible(False)
    tbl.horizontalHeader().setStretchLastSection(True)
    tbl.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)
    for r, row in enumerate(rows):
        for c, val in enumerate(row):
            item = QTableWidgetItem(str(val))
            if str(val).startswith("£"):
                item.setTextAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
            tbl.setItem(r, c, item)
    tbl.resizeRowsToContents()
    tbl.setFixedHeight(min(40 * len(rows) + 34, 400))
    return tbl


class ReportsPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self._start = self._end = ""
        self._build()

    def _build(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(28, 24, 28, 24)
        root.setSpacing(16)

        hdr = QHBoxLayout()
        title = QLabel("Reports")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        hdr.addWidget(title)
        hdr.addStretch()
        export_btn = QPushButton("⬇ Export All CSV")
        export_btn.setFixedHeight(36)
        export_btn.setProperty("flat", True)
        export_btn.clicked.connect(self._export_all)
        hdr.addWidget(export_btn)
        root.addLayout(hdr)

        self._date_filter = DateFilterBar()
        self._date_filter.filter_changed.connect(self._on_date_changed)
        root.addWidget(self._date_filter)

        # Tabs
        tabs = QTabWidget()
        root.addWidget(tabs)

        # ── Tab 1: Top Categories ──────────────────────────────────
        self._cat_tab = QScrollArea()
        self._cat_tab.setWidgetResizable(True)
        self._cat_tab.setFrameShape(QFrame.Shape.NoFrame)
        self._cat_inner = QWidget()
        self._cat_lay = QVBoxLayout(self._cat_inner)
        self._cat_lay.setAlignment(Qt.AlignmentFlag.AlignTop)
        self._cat_tab.setWidget(self._cat_inner)
        tabs.addTab(self._cat_tab, "Top Categories")

        # ── Tab 2: Top Merchants ──────────────────────────────────
        self._merch_tab = QScrollArea()
        self._merch_tab.setWidgetResizable(True)
        self._merch_tab.setFrameShape(QFrame.Shape.NoFrame)
        self._merch_inner = QWidget()
        self._merch_lay = QVBoxLayout(self._merch_inner)
        self._merch_lay.setAlignment(Qt.AlignmentFlag.AlignTop)
        self._merch_tab.setWidget(self._merch_inner)
        tabs.addTab(self._merch_tab, "Top Merchants")

        # ── Tab 3: Monthly Trend ──────────────────────────────────
        self._trend_tab = QWidget()
        self._trend_lay = QVBoxLayout(self._trend_tab)
        self._trend_lay.setAlignment(Qt.AlignmentFlag.AlignTop)
        self._line_chart = LineChart()
        self._trend_lay.addWidget(self._line_chart)
        self._trend_tbl_wrap = QVBoxLayout()
        self._trend_lay.addLayout(self._trend_tbl_wrap)
        tabs.addTab(self._trend_tab, "Monthly Trend")

        # ── Tab 4: Income vs Spending ─────────────────────────────
        self._ivs_tab = QWidget()
        self._ivs_lay = QVBoxLayout(self._ivs_tab)
        self._ivs_lay.setAlignment(Qt.AlignmentFlag.AlignTop)
        tabs.addTab(self._ivs_tab, "Income vs Spending")

    def _on_date_changed(self, start: str, end: str):
        self._start = start
        self._end   = end
        self._load()

    def _load(self):
        s, e = self._start or None, self._end or None
        self._load_categories(s, e)
        self._load_merchants(s, e)
        self._load_trend()
        self._load_income_vs_spending(s, e)

    def _load_categories(self, s, e):
        self._clear(self._cat_lay)
        data = db.get_category_summary(DB_PATH, s, e)
        chart = HBarChart()
        chart.setFixedHeight(300)
        chart.update([{"merchant": d["category"], "total": d.get("total_debit",0) or 0}
                      for d in data if d.get("total_debit")],
                     label_key="merchant")
        self._cat_lay.addWidget(chart)
        rows = [[d["category"],
                 f"£{d.get('total_debit',0) or 0:,.2f}",
                 str(d.get("tx_count",0))] for d in data]
        self._cat_lay.addWidget(_simple_table(["Category","Total Spent","Transactions"], rows))

    def _load_merchants(self, s, e):
        self._clear(self._merch_lay)
        data = db.get_top_merchants(DB_PATH, s, e, limit=20)
        chart = HBarChart()
        chart.setFixedHeight(300)
        chart.update(data)
        self._merch_lay.addWidget(chart)
        rows = [[d["merchant"],
                 f"£{d.get('total',0) or 0:,.2f}",
                 str(d.get("tx_count",0))] for d in data]
        self._merch_lay.addWidget(_simple_table(["Merchant","Total Spent","Purchases"], rows))

    def _load_trend(self):
        data = db.get_monthly_spending(DB_PATH, months=18)
        self._line_chart.update(data)
        self._clear(self._trend_tbl_wrap)
        rows = [[d["month"],
                 f"£{d.get('spending',0) or 0:,.2f}",
                 f"£{d.get('income',0) or 0:,.2f}"] for d in data]
        self._trend_tbl_wrap.addWidget(
            _simple_table(["Month","Spending","Income"], rows))

    def _load_income_vs_spending(self, s, e):
        self._clear(self._ivs_lay)
        stats = db.get_dashboard_stats(DB_PATH, s, e)
        rows = [
            ["Total Income",   f"£{stats['total_income']:,.2f}"],
            ["Total Spending", f"£{stats['total_spending']:,.2f}"],
            ["Saved",          f"£{stats['total_saved']:,.2f}"],
            ["Net Position",   f"£{stats['net']:,.2f}"],
        ]
        self._ivs_lay.addWidget(_simple_table(["Metric","Amount"], rows))

    def _export_all(self):
        path, _ = QFileDialog.getSaveFileName(
            self, "Export Report", str(EXPORTS_DIR / "report.csv"),
            "CSV Files (*.csv)")
        if not path: return
        data = db.get_category_summary(DB_PATH, self._start or None, self._end or None)
        with open(path, "w", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            w.writerow(["Category","Total Spent","Transactions"])
            for d in data:
                w.writerow([d["category"],
                             round(d.get("total_debit",0) or 0, 2),
                             d.get("tx_count",0)])

    @staticmethod
    def _clear(layout):
        while layout.count():
            item = layout.takeAt(0)
            if item.widget():
                item.widget().deleteLater()

    def showEvent(self, e):
        super().showEvent(e)
        self._load()
