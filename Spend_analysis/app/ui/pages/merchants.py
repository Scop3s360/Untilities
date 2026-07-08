"""app/ui/pages/merchants.py — Merchant Management: bulk categorisation interface."""
from __future__ import annotations

from datetime import datetime
from PyQt6.QtWidgets import (
    QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QComboBox,
    QLineEdit, QTableWidget, QTableWidgetItem, QHeaderView, QAbstractItemView,
    QSplitter, QFrame, QDialog, QDialogButtonBox, QRadioButton, QButtonGroup,
    QMessageBox, QScrollArea, QSizePolicy
)
from PyQt6.QtCore import Qt, pyqtSignal, QTimer
from PyQt6.QtGui import QColor

from app.ui.theme import C
from app.ui.widgets.tx_table import TransactionTable
from app.ui.charts import VBarChart
from app.ui.widgets.cards import MetricCard
from app import database as db
from app.config import DB_PATH, RULES_DB_PATH
from rule_engine import db as rule_db

_UNCATEGORISED = {"Other", "Unknown", ""}

# ── helpers ────────────────────────────────────────────────────────────────────

def _lbl(text, size=12, bold=False, color=None) -> QLabel:
    l = QLabel(str(text))
    l.setStyleSheet(f"color:{color or C['text']};font-size:{size}px;"
                    + ("font-weight:700;" if bold else ""))
    return l

def _fmt_date(d):
    try:    return datetime.strptime(str(d), "%Y-%m-%d").strftime("%d %b %Y")
    except: return str(d) if d else "—"

def _badge(cat: str | None) -> str:
    return "🟡" if (not cat or cat in _UNCATEGORISED) else "🟢"


# ── Category scope dialog ──────────────────────────────────────────────────────

class CategoryScopeDialog(QDialog):
    """Confirm scope of a category change: historical or future-only."""

    def __init__(self, merchant: str | None, new_cat: str,
                 bulk_count: int = 1, parent=None):
        super().__init__(parent)
        self.setWindowTitle("Change Category")
        self.setFixedWidth(480)
        self.setStyleSheet(f"background:{C['bg3']};color:{C['text']};")

        lay = QVBoxLayout(self)
        lay.setSpacing(14)
        lay.setContentsMargins(22, 22, 22, 22)

        if merchant:
            title_txt = f'Assign "{merchant}"  →  {new_cat}'
        else:
            title_txt = f"Assign {bulk_count} merchant(s)  →  {new_cat}"

        lay.addWidget(_lbl(title_txt, 14, bold=True))
        lay.addWidget(_lbl(
            f'A categorisation rule will be created automatically — '
            f'future imports will be categorised as "{new_cat}" immediately.',
            11, color=C["text2"]))

        self._grp = QButtonGroup(self)
        self._rb_all = QRadioButton(
            "Apply to ALL historical transactions  (recommended)")
        self._rb_all.setStyleSheet(f"color:{C['text']};font-size:12px;")
        self._rb_all.setChecked(True)

        self._rb_future = QRadioButton(
            "Future imports only  (leave existing transactions unchanged)")
        self._rb_future.setStyleSheet(f"color:{C['text2']};font-size:12px;")

        self._grp.addButton(self._rb_all,   0)
        self._grp.addButton(self._rb_future, 1)
        lay.addWidget(self._rb_all)
        lay.addWidget(self._rb_future)

        btns = QDialogButtonBox(
            QDialogButtonBox.StandardButton.Cancel |
            QDialogButtonBox.StandardButton.Ok)
        btns.button(QDialogButtonBox.StandardButton.Ok).setText("Apply")
        btns.accepted.connect(self.accept)
        btns.rejected.connect(self.reject)
        lay.addWidget(btns)

    @property
    def apply_historical(self) -> bool:
        return self._rb_all.isChecked()


# ── Detail panel ───────────────────────────────────────────────────────────────

class MerchantDetailPanel(QFrame):
    category_changed = pyqtSignal(str, str)  # merchant, new_cat

    def __init__(self, parent=None):
        super().__init__(parent)
        self._merchant = ""
        self.setMinimumWidth(320)
        self.setStyleSheet(f"""
            MerchantDetailPanel {{
                background:{C['bg2']};
                border-left:1px solid {C['border']};
            }}
        """)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(18, 18, 18, 18)
        lay.setSpacing(10)

        self._name   = _lbl("", 16, bold=True)
        self._cat    = _lbl("", 12, color=C["text2"])
        lay.addWidget(self._name)
        lay.addWidget(self._cat)

        # Metrics
        row1 = QHBoxLayout()
        self._m_total = MetricCard("Total Spent",   "£0", C["red"])
        self._m_count = MetricCard("Transactions",  "0")
        row1.addWidget(self._m_total); row1.addWidget(self._m_count)
        lay.addLayout(row1)

        row2 = QHBoxLayout()
        self._m_avg   = MetricCard("Avg Purchase",  "£0")
        self._m_last  = MetricCard("Last Seen",     "—")
        row2.addWidget(self._m_avg); row2.addWidget(self._m_last)
        lay.addLayout(row2)

        # Category change
        sep = QFrame()
        sep.setFrameShape(QFrame.Shape.HLine)
        sep.setStyleSheet(f"color:{C['border']};")
        lay.addWidget(sep)

        lay.addWidget(_lbl("Change Category", 12, bold=True))
        self._cat_combo = QComboBox()
        self._cat_combo.setFixedHeight(36)
        cats = sorted(c.name for c in rule_db.get_all_categories(RULES_DB_PATH))
        self._cat_combo.addItems(cats)
        lay.addWidget(self._cat_combo)

        apply_btn = QPushButton("Apply & Create Rule")
        apply_btn.clicked.connect(self._apply_category)
        lay.addWidget(apply_btn)

        # Monthly chart
        self._chart = VBarChart()
        self._chart.setFixedHeight(160)
        lay.addWidget(self._chart)

        # Transactions
        lay.addWidget(_lbl("Transactions", 12, bold=True))
        self._tbl = TransactionTable(show_export=False)
        lay.addWidget(self._tbl)

        self.setVisible(False)

    def load(self, row: dict):
        self._merchant = row["merchant"]
        self.setVisible(True)

        total  = row.get("total_spend", 0) or 0
        count  = row.get("tx_count",    0) or 0
        cat    = row.get("category",    "Other") or "Other"
        last   = row.get("last_date",   "")

        self._name.setText(self._merchant)
        self._cat.setText(f"Category: {cat}")
        self._m_total.set_value(f"£{total:,.2f}")
        self._m_count.set_value(str(count))
        self._m_avg.set_value(f"£{total/count:,.2f}" if count else "£0")
        self._m_last.set_value(_fmt_date(last))

        idx = self._cat_combo.findText(cat)
        if idx >= 0:
            self._cat_combo.setCurrentIndex(idx)

        monthly = db.get_merchant_monthly(DB_PATH, self._merchant)
        self._chart.update(monthly, label_key="month", value_key="total")

        txs = db.get_transactions(DB_PATH, merchant=self._merchant)
        self._tbl.load(txs)

    def _apply_category(self):
        if not self._merchant:
            return
        new_cat = self._cat_combo.currentText()
        dlg = CategoryScopeDialog(self._merchant, new_cat, parent=self)
        if dlg.exec() != QDialog.DialogCode.Accepted:
            return

        if dlg.apply_historical:
            db.update_merchant_category(DB_PATH, self._merchant, new_cat)

        rule_db.upsert_user_rule(RULES_DB_PATH, self._merchant, new_cat)
        self._cat.setText(f"Category: {new_cat}")
        self.category_changed.emit(self._merchant, new_cat)


# ── Merchants page ─────────────────────────────────────────────────────────────

TBL_COLS = [
    ("",         40),   # 0 checkbox
    ("",         36),   # 1 badge
    ("Merchant", 220),  # 2
    ("Category", 140),  # 3
    ("Txns",      60),  # 4
    ("Total",     100), # 5
    ("Last Seen", 110), # 6
]


class MerchantsPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self._data: list[dict] = []
        self._search_timer = QTimer()
        self._search_timer.setSingleShot(True)
        self._search_timer.timeout.connect(self._load)
        self._build()

    # ── Build ──────────────────────────────────────────────────────────────

    def _build(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(28, 24, 0, 24)
        root.setSpacing(10)

        # ── Header ──────────────────────────────────────────────
        hdr = QHBoxLayout()
        hdr.setContentsMargins(0, 0, 28, 0)
        title = QLabel("Merchants")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        hdr.addWidget(title)
        hdr.addStretch()
        self._count_lbl = _lbl("", 12, color=C["text2"])
        hdr.addWidget(self._count_lbl)
        root.addLayout(hdr)

        # ── Filter row ──────────────────────────────────────────
        filter_row = QHBoxLayout()
        filter_row.setContentsMargins(0, 0, 28, 0)
        filter_row.setSpacing(8)

        self._search = QLineEdit()
        self._search.setPlaceholderText("Search merchants…")
        self._search.setFixedHeight(36)
        self._search.textChanged.connect(lambda: self._search_timer.start(300))
        filter_row.addWidget(self._search, 3)

        self._view_combo = QComboBox()
        self._view_combo.setFixedHeight(36)
        self._view_combo.addItems(["All Merchants", "Categorised", "Uncategorised"])
        self._view_combo.currentIndexChanged.connect(self._load)
        filter_row.addWidget(self._view_combo, 2)

        self._cat_filter = QComboBox()
        self._cat_filter.setFixedHeight(36)
        self._cat_filter.addItem("All Categories")
        self._cat_filter.addItems(sorted(c.name for c in
                                         rule_db.get_all_categories(RULES_DB_PATH)))
        self._cat_filter.currentIndexChanged.connect(self._load)
        filter_row.addWidget(self._cat_filter, 2)

        self._sort_combo = QComboBox()
        self._sort_combo.setFixedHeight(36)
        self._sort_combo.addItems([
            "Sort: Highest Spend", "Sort: Most Transactions",
            "Sort: Merchant Name", "Sort: Last Used", "Sort: Category"])
        self._sort_combo.currentIndexChanged.connect(self._load)
        filter_row.addWidget(self._sort_combo, 2)

        root.addLayout(filter_row)

        # ── Bulk action row ─────────────────────────────────────
        bulk_row = QHBoxLayout()
        bulk_row.setContentsMargins(0, 0, 28, 0)
        bulk_row.setSpacing(8)

        self._sel_all_btn = QPushButton("Select All")
        self._sel_all_btn.setFixedHeight(32)
        self._sel_all_btn.setProperty("flat", True)
        self._sel_all_btn.clicked.connect(self._select_all)
        bulk_row.addWidget(self._sel_all_btn)

        self._sel_none_btn = QPushButton("Select None")
        self._sel_none_btn.setFixedHeight(32)
        self._sel_none_btn.setProperty("flat", True)
        self._sel_none_btn.clicked.connect(self._select_none)
        bulk_row.addWidget(self._sel_none_btn)

        bulk_row.addStretch()

        bulk_row.addWidget(_lbl("Assign:", color=C["text2"]))
        self._bulk_cat = QComboBox()
        self._bulk_cat.setFixedHeight(32)
        self._bulk_cat.setFixedWidth(200)
        self._bulk_cat.addItems(sorted(c.name for c in
                                       rule_db.get_all_categories(RULES_DB_PATH)))
        bulk_row.addWidget(self._bulk_cat)

        self._apply_btn = QPushButton("Apply to Selected")
        self._apply_btn.setFixedHeight(32)
        self._apply_btn.clicked.connect(self._bulk_apply)
        bulk_row.addWidget(self._apply_btn)

        root.addLayout(bulk_row)

        # ── Splitter: table | detail ────────────────────────────
        splitter = QSplitter(Qt.Orientation.Horizontal)
        splitter.setHandleWidth(1)

        self._tbl = QTableWidget()
        self._tbl.setColumnCount(len(TBL_COLS))
        self._tbl.setHorizontalHeaderLabels([c[0] for c in TBL_COLS])
        for i, (_, w) in enumerate(TBL_COLS):
            self._tbl.setColumnWidth(i, w)
        self._tbl.horizontalHeader().setStretchLastSection(True)
        self._tbl.verticalHeader().setVisible(False)
        self._tbl.setAlternatingRowColors(True)
        self._tbl.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
        self._tbl.setSelectionBehavior(QAbstractItemView.SelectionBehavior.SelectRows)
        self._tbl.setSelectionMode(QAbstractItemView.SelectionMode.SingleSelection)
        self._tbl.setSortingEnabled(True)
        self._tbl.cellClicked.connect(self._on_cell_click)
        self._tbl.itemChanged.connect(self._on_check_changed)
        splitter.addWidget(self._tbl)

        self._detail = MerchantDetailPanel()
        self._detail.category_changed.connect(self._on_category_changed)
        splitter.addWidget(self._detail)
        splitter.setSizes([860, 340])
        splitter.setCollapsible(1, True)
        root.addWidget(splitter)

    # ── Data ───────────────────────────────────────────────────────────────

    def _load(self):
        view_map = {0: "all", 1: "categorised", 2: "uncategorised"}
        sort_map = {0: "total_spend", 1: "tx_count",
                    2: "merchant",    3: "last_date",  4: "category"}
        cat = self._cat_filter.currentText()
        self._data = db.get_merchant_list(
            DB_PATH,
            view=view_map.get(self._view_combo.currentIndex(), "all"),
            search=self._search.text().strip() or None,
            category=None if cat == "All Categories" else cat,
            sort_by=sort_map.get(self._sort_combo.currentIndex(), "total_spend"),
        )
        self._render_table()

    def _render_table(self):
        self._tbl.blockSignals(True)
        self._tbl.setSortingEnabled(False)
        self._tbl.setRowCount(len(self._data))

        for r, row in enumerate(self._data):
            cat    = row.get("category", "") or ""
            total  = row.get("total_spend", 0) or 0
            count  = row.get("tx_count", 0) or 0
            last   = row.get("last_date", "") or ""

            # Col 0 — checkbox
            chk = QTableWidgetItem()
            chk.setCheckState(Qt.CheckState.Unchecked)
            chk.setData(Qt.ItemDataRole.UserRole, row)
            self._tbl.setItem(r, 0, chk)

            # Col 1 — status badge
            badge = QTableWidgetItem(_badge(cat))
            badge.setTextAlignment(Qt.AlignmentFlag.AlignCenter)
            badge.setFlags(badge.flags() & ~Qt.ItemFlag.ItemIsEditable)
            self._tbl.setItem(r, 1, badge)

            # Col 2 — merchant name
            merchant_item = QTableWidgetItem(row.get("merchant", ""))
            merchant_item.setData(Qt.ItemDataRole.UserRole, row)
            self._tbl.setItem(r, 2, merchant_item)

            # Col 3 — category (coloured if uncategorised)
            cat_item = QTableWidgetItem(cat or "—")
            if not cat or cat in _UNCATEGORISED:
                cat_item.setForeground(QColor(C["amber"]))
            else:
                cat_item.setForeground(QColor(C["green"]))
            self._tbl.setItem(r, 3, cat_item)

            # Col 4 — tx count
            count_item = QTableWidgetItem(str(count))
            count_item.setTextAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
            self._tbl.setItem(r, 4, count_item)

            # Col 5 — total spend
            spend_item = QTableWidgetItem(f"£{total:,.2f}")
            spend_item.setTextAlignment(Qt.AlignmentFlag.AlignRight | Qt.AlignmentFlag.AlignVCenter)
            spend_item.setForeground(QColor(C["red"]))
            self._tbl.setItem(r, 5, spend_item)

            # Col 6 — last date
            self._tbl.setItem(r, 6, QTableWidgetItem(_fmt_date(last)))

        self._tbl.setSortingEnabled(True)
        self._tbl.blockSignals(False)
        uncategorised = sum(1 for d in self._data
                            if not d.get("category") or d["category"] in _UNCATEGORISED)
        self._count_lbl.setText(
            f"{len(self._data)} merchants  ·  "
            f"🟡 {uncategorised} uncategorised")

    # ── Interaction ────────────────────────────────────────────────────────

    def _on_cell_click(self, row: int, col: int):
        if col == 0:
            return  # checkbox column — let itemChanged handle it
        item = self._tbl.item(row, 2)
        if item:
            data = item.data(Qt.ItemDataRole.UserRole)
            if data:
                self._detail.load(data)

    def _on_check_changed(self, item: QTableWidgetItem):
        if self._tbl.column(item) != 0:
            return
        checked_count = sum(
            1 for r in range(self._tbl.rowCount())
            if self._tbl.item(r, 0) and
               self._tbl.item(r, 0).checkState() == Qt.CheckState.Checked)
        self._apply_btn.setEnabled(checked_count > 0)
        self._apply_btn.setText(
            f"Apply to {checked_count} Selected"
            if checked_count else "Apply to Selected")

    def _select_all(self):
        self._tbl.blockSignals(True)
        for r in range(self._tbl.rowCount()):
            item = self._tbl.item(r, 0)
            if item:
                item.setCheckState(Qt.CheckState.Checked)
        self._tbl.blockSignals(False)
        self._on_check_changed(self._tbl.item(0, 0) if self._tbl.rowCount() else QTableWidgetItem())

    def _select_none(self):
        self._tbl.blockSignals(True)
        for r in range(self._tbl.rowCount()):
            item = self._tbl.item(r, 0)
            if item:
                item.setCheckState(Qt.CheckState.Unchecked)
        self._tbl.blockSignals(False)
        self._apply_btn.setText("Apply to Selected")

    def _bulk_apply(self):
        selected = []
        for r in range(self._tbl.rowCount()):
            item = self._tbl.item(r, 0)
            if item and item.checkState() == Qt.CheckState.Checked:
                data = item.data(Qt.ItemDataRole.UserRole)
                if data:
                    selected.append(data)
        if not selected:
            return

        new_cat = self._bulk_cat.currentText()
        dlg = CategoryScopeDialog(None, new_cat, bulk_count=len(selected), parent=self)
        if dlg.exec() != QDialog.DialogCode.Accepted:
            return

        tx_updated = 0
        for row in selected:
            merchant = row["merchant"]
            if dlg.apply_historical:
                tx_updated += db.update_merchant_category(DB_PATH, merchant, new_cat)
            rule_db.upsert_user_rule(RULES_DB_PATH, merchant, new_cat)

        QMessageBox.information(
            self, "Done",
            f"Updated {len(selected)} merchant(s).\n"
            f"{tx_updated} historical transactions recategorised.\n"
            f"{len(selected)} categorisation rules created/updated.")

        self._select_none()
        self._load()

    def _on_category_changed(self, merchant: str, new_cat: str):
        """Refresh the row for this merchant in the table after detail-panel change."""
        self._load()

    def showEvent(self, e):
        super().showEvent(e)
        self._load()
