"""app/ui/widgets/filters.py — Date range selector and search bar."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QHBoxLayout, QPushButton, QComboBox,
                              QLineEdit, QDateEdit, QLabel, QSizePolicy)
from PyQt6.QtCore import Qt, pyqtSignal, QDate
from PyQt6.QtGui import QFont

from app.ui.theme import C
from app import database as db


class SearchBar(QWidget):
    """Global search input with clear button."""
    search_changed = pyqtSignal(str)

    def __init__(self, placeholder="Search transactions, merchants, categories…",
                 parent=None):
        super().__init__(parent)
        lay = QHBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(6)

        self._input = QLineEdit()
        self._input.setPlaceholderText(placeholder)
        self._input.setFixedHeight(38)
        self._input.textChanged.connect(self.search_changed)
        lay.addWidget(self._input)

        self._clear = QPushButton("✕")
        self._clear.setFixedSize(38, 38)
        self._clear.setProperty("flat", True)
        self._clear.clicked.connect(self._input.clear)
        lay.addWidget(self._clear)

    @property
    def text(self) -> str:
        return self._input.text().strip()

    def clear(self):
        self._input.clear()


class DateFilterBar(QWidget):
    """Preset date range buttons + optional custom range."""
    filter_changed = pyqtSignal(str, str)   # start_date, end_date (ISO or "")

    PRESETS = [
        ("All Time",    "all"),
        ("This Month",  "month"),
        ("Last Month",  "last_month"),
        ("3 Months",    "3m"),
        ("6 Months",    "6m"),
        ("This Year",   "year"),
        ("Last Year",   "last_year"),
    ]

    def __init__(self, parent=None):
        super().__init__(parent)
        self._active = "all"
        lay = QHBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(4)
        self._btns: dict[str, QPushButton] = {}

        for label, key in self.PRESETS:
            btn = QPushButton(label)
            btn.setFixedHeight(32)
            btn.setProperty("flat", True)
            btn.clicked.connect(lambda _, k=key: self._select(k))
            self._btns[key] = btn
            lay.addWidget(btn)

        lay.addStretch()

        # Custom range
        lay.addWidget(QLabel("From:"))
        self._from = QDateEdit()
        self._from.setFixedHeight(32)
        self._from.setDisplayFormat("dd MMM yyyy")
        self._from.setCalendarPopup(True)
        self._from.setDate(QDate.currentDate().addDays(-90))
        lay.addWidget(self._from)

        lay.addWidget(QLabel("To:"))
        self._to = QDateEdit()
        self._to.setFixedHeight(32)
        self._to.setDisplayFormat("dd MMM yyyy")
        self._to.setCalendarPopup(True)
        self._to.setDate(QDate.currentDate())
        lay.addWidget(self._to)

        apply_btn = QPushButton("Apply")
        apply_btn.setFixedHeight(32)
        apply_btn.clicked.connect(self._apply_custom)
        lay.addWidget(apply_btn)

        self._select("all")   # default

    def _select(self, key: str):
        self._active = key
        # Highlight active button
        for k, btn in self._btns.items():
            btn.setStyleSheet(
                f"background:{C['accent_dim']};color:{C['accent']};border-color:{C['accent']};"
                if k == key else "")
        start, end = db.date_range_for_preset(key)
        self.filter_changed.emit(start or "", end or "")

    def _apply_custom(self):
        self._active = "custom"
        for btn in self._btns.values():
            btn.setStyleSheet("")
        start = self._from.date().toString("yyyy-MM-dd")
        end   = self._to.date().toString("yyyy-MM-dd")
        self.filter_changed.emit(start, end)

    @property
    def current_range(self) -> tuple[str, str]:
        start, end = db.date_range_for_preset(self._active)
        return start or "", end or ""


class FilterBar(QWidget):
    """Category + search filter row for the Transactions page."""
    changed = pyqtSignal()

    def __init__(self, categories: list[str], parent=None):
        super().__init__(parent)
        lay = QHBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(8)

        self._search = QLineEdit()
        self._search.setPlaceholderText("Search…")
        self._search.setFixedHeight(36)
        self._search.textChanged.connect(self.changed)
        lay.addWidget(self._search, 3)

        self._cat = QComboBox()
        self._cat.setFixedHeight(36)
        self._cat.addItem("All Categories")
        self._cat.addItems(sorted(categories))
        self._cat.currentTextChanged.connect(self.changed)
        lay.addWidget(self._cat, 2)

        self._type = QComboBox()
        self._type.setFixedHeight(36)
        self._type.addItems(["All", "Expenses", "Income", "Uncategorised"])
        self._type.currentTextChanged.connect(self.changed)
        lay.addWidget(self._type, 1)

    @property
    def search(self) -> str:
        return self._search.text().strip()

    @property
    def category(self) -> str | None:
        t = self._cat.currentText()
        return None if t == "All Categories" else t

    @property
    def tx_type(self) -> str:
        return self._type.currentText()
