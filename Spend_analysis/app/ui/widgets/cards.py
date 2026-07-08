"""app/ui/widgets/cards.py — Reusable summary card and category/merchant cards."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QSizePolicy, QFrame, QGridLayout)
from PyQt6.QtCore import Qt, pyqtSignal
from PyQt6.QtGui import QFont, QCursor

from app.ui.theme import C


def _lbl(text, size=13, bold=False, color=None) -> QLabel:
    l = QLabel(text)
    f = QFont()
    f.setPointSize(size)
    if bold: f.setWeight(QFont.Weight.Bold)
    l.setFont(f)
    if color: l.setStyleSheet(f"color: {color};")
    return l


class SummaryCard(QWidget):
    clicked = pyqtSignal()

    def __init__(self, title: str, value: str = "—",
                 subtitle: str = "", icon: str = "",
                 accent: str = C["accent"], parent=None):
        super().__init__(parent)
        self.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.setFixedHeight(110)
        self.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self._accent = accent
        self._build(icon, title, value, subtitle)
        self._style_normal()

    def _build(self, icon, title, value, subtitle):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(18, 14, 18, 14)
        lay.setSpacing(4)

        top = QHBoxLayout()
        if icon:
            ico = _lbl(icon, size=18)
            ico.setFixedWidth(30)
            top.addWidget(ico)
        self._title_lbl = _lbl(title, size=11, color=C["text2"])
        top.addWidget(self._title_lbl)
        top.addStretch()
        lay.addLayout(top)

        self._value_lbl = _lbl(value, size=22, bold=True, color=C["text"])
        lay.addWidget(self._value_lbl)

        self._sub_lbl = _lbl(subtitle, size=10, color=C["text3"])
        lay.addWidget(self._sub_lbl)

    def _style_normal(self):
        self.setStyleSheet(f"""
            SummaryCard {{
                background: {C['bg2']};
                border: 1px solid {C['border']};
                border-radius: 14px;
            }}
            SummaryCard:hover {{
                background: {C['card_hover']};
                border-color: {self._accent};
            }}
        """)

    def set_value(self, value: str, subtitle: str = ""):
        self._value_lbl.setText(value)
        if subtitle: self._sub_lbl.setText(subtitle)

    def mousePressEvent(self, e):
        self.clicked.emit()


class SummaryCardRow(QWidget):
    """A row of SummaryCards."""
    def __init__(self, cards: list[SummaryCard], parent=None):
        super().__init__(parent)
        lay = QHBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.setSpacing(14)
        for c in cards:
            lay.addWidget(c)


class MetricCard(QWidget):
    """Compact key/value metric card used in detail panels."""
    def __init__(self, label: str, value: str, color: str = C["text"], parent=None):
        super().__init__(parent)
        self.setFixedHeight(72)
        self.setStyleSheet(f"""
            MetricCard {{
                background: {C['bg3']};
                border: 1px solid {C['border']};
                border-radius: 10px;
            }}
        """)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(14, 10, 14, 10)
        lay.setSpacing(2)
        lay.addWidget(_lbl(label, size=10, color=C["text2"]))
        self._val = _lbl(value, size=15, bold=True, color=color)
        lay.addWidget(self._val)

    def set_value(self, v: str):
        self._val.setText(v)


class CategoryCard(QFrame):
    """Clickable category card for the Categories page."""
    clicked = pyqtSignal(str)

    def __init__(self, name: str, total: float, tx_count: int, parent=None):
        super().__init__(parent)
        self._name = name
        self.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.setFixedSize(200, 110)
        self.setStyleSheet(f"""
            CategoryCard {{
                background: {C['bg2']};
                border: 1px solid {C['border']};
                border-radius: 14px;
            }}
            CategoryCard:hover {{
                background: {C['card_hover']};
                border-color: {C['accent']};
            }}
        """)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(16, 12, 16, 12)
        lay.setSpacing(4)
        lay.addWidget(_lbl(name, size=12, bold=True))
        lay.addWidget(_lbl(f"£{total:,.2f}", size=18, bold=True, color=C["accent"]))
        lay.addWidget(_lbl(f"{tx_count} transactions", size=10, color=C["text2"]))

    def mousePressEvent(self, e):
        self.clicked.emit(self._name)


class MerchantCard(QFrame):
    """Clickable merchant card for the Merchants page."""
    clicked = pyqtSignal(str)

    def __init__(self, name: str, total: float, tx_count: int,
                 category: str = "", parent=None):
        super().__init__(parent)
        self._name = name
        self.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.setFixedSize(210, 120)
        self.setStyleSheet(f"""
            MerchantCard {{
                background: {C['bg2']};
                border: 1px solid {C['border']};
                border-radius: 14px;
            }}
            MerchantCard:hover {{
                background: {C['card_hover']};
                border-color: {C['accent']};
            }}
        """)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(16, 12, 16, 12)
        lay.setSpacing(3)
        n = _lbl(name[:22], size=12, bold=True)
        n.setWordWrap(True)
        lay.addWidget(n)
        lay.addWidget(_lbl(f"£{total:,.2f}", size=17, bold=True, color=C["accent"]))
        lay.addWidget(_lbl(f"{tx_count} purchases", size=10, color=C["text2"]))
        if category:
            lay.addWidget(_lbl(category, size=9, color=C["text3"]))

    def mousePressEvent(self, e):
        self.clicked.emit(self._name)
