"""app/ui/main_window.py — Application shell: sidebar + page stack."""
from __future__ import annotations

from PyQt6.QtWidgets import (QMainWindow, QWidget, QHBoxLayout, QVBoxLayout,
                              QLabel, QPushButton, QStackedWidget, QFrame,
                              QLineEdit, QSizePolicy, QScrollArea, QApplication)
from PyQt6.QtCore import Qt, pyqtSignal, QSize
from PyQt6.QtGui import QFont, QIcon, QKeySequence, QShortcut

from app.ui.theme import C
from app.ui.pages.dashboard    import DashboardPage
from app.ui.pages.imports      import ImportsPage
from app.ui.pages.transactions import TransactionsPage
from app.ui.pages.merchants    import MerchantsPage
from app.ui.pages.reports      import ReportsPage
from app.ui.pages.settings     import SettingsPage
from app import database as db
from app.config import DB_PATH, APP_NAME, APP_VERSION


# ── Nav item definition ────────────────────────────────────────────────────────

NAV_ITEMS = [
    ("🏠", "Dashboard",     "dashboard",    False),
    ("📥", "Imports",       "imports",      False),
    ("💳", "Transactions",  "transactions", False),
    ("🏪", "Merchants",     "merchants",    False),
    ("📈", "Reports",       "reports",      False),
    None,   # separator
    ("🤖", "AI Insights",   None,           True),   # future
    ("🎯", "Budgets",       None,           True),   # future
    ("🔔", "Goals",         None,           True),   # future
]

SETTINGS_ITEM = ("⚙", "Settings", "settings", False)


# ── Sidebar ────────────────────────────────────────────────────────────────────

class NavButton(QPushButton):
    def __init__(self, icon: str, label: str, future: bool = False, parent=None):
        super().__init__(f"  {icon}  {label}", parent)
        self.setProperty("nav", True)
        self.setProperty("future", future)
        self.setProperty("active", False)
        if future:
            self.setEnabled(False)
            self.setToolTip("Coming soon")
        self.setFixedHeight(44)
        self.setCheckable(False)

    def set_active(self, active: bool):
        self.setProperty("active", active)
        self.style().unpolish(self)
        self.style().polish(self)


class Sidebar(QWidget):
    page_requested = pyqtSignal(str)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedWidth(220)
        self.setStyleSheet(f"background:{C['sidebar']};border-right:1px solid {C['border']};")
        self._btns: dict[str, NavButton] = {}
        self._build()

    def _build(self):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(12, 16, 12, 16)
        lay.setSpacing(2)

        # Logo
        logo = QLabel(f"💰 {APP_NAME}")
        logo.setStyleSheet(
            f"color:{C['accent']};font-size:16px;font-weight:800;"
            f"padding:8px 10px 16px 10px;")
        lay.addWidget(logo)

        ver = QLabel(f"v{APP_VERSION}")
        ver.setStyleSheet(f"color:{C['text3']};font-size:10px;padding:0 10px 12px 10px;")
        lay.addWidget(ver)

        # Nav items
        for item in NAV_ITEMS:
            if item is None:
                sep = QFrame()
                sep.setFrameShape(QFrame.Shape.HLine)
                sep.setStyleSheet(f"color:{C['border']};margin:6px 0;")
                lay.addWidget(sep)
                continue
            icon, label, key, future = item
            btn = NavButton(icon, label, future)
            if key:
                btn.clicked.connect(lambda _, k=key: self.page_requested.emit(k))
                self._btns[key] = btn
            lay.addWidget(btn)

        lay.addStretch()

        # Settings at bottom
        icon, label, key, future = SETTINGS_ITEM
        settings_btn = NavButton(icon, label, future)
        settings_btn.clicked.connect(lambda: self.page_requested.emit(key))
        self._btns[key] = settings_btn
        lay.addWidget(settings_btn)

    def set_active(self, key: str):
        for k, btn in self._btns.items():
            btn.set_active(k == key)


# ── Global search overlay ──────────────────────────────────────────────────────

class SearchOverlay(QFrame):
    """Quick-search results shown as a floating panel."""
    navigate = pyqtSignal(str, dict)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setFixedWidth(600)
        self.setMaximumHeight(500)
        self.setStyleSheet(f"""
            SearchOverlay {{
                background:{C['bg3']};
                border:1px solid {C['border']};
                border-radius:14px;
            }}
        """)
        self.hide()
        lay = QVBoxLayout(self)
        lay.setContentsMargins(14, 14, 14, 14)
        lay.setSpacing(4)
        self._lbl = QLabel("Results")
        self._lbl.setStyleSheet(f"color:{C['text2']};font-size:11px;")
        lay.addWidget(self._lbl)
        self._scroll = QScrollArea()
        self._scroll.setWidgetResizable(True)
        self._scroll.setFrameShape(QFrame.Shape.NoFrame)
        self._inner = QWidget()
        self._inner_lay = QVBoxLayout(self._inner)
        self._inner_lay.setContentsMargins(0, 0, 0, 0)
        self._inner_lay.setSpacing(2)
        self._scroll.setWidget(self._inner)
        lay.addWidget(self._scroll)

    def show_results(self, results: list[dict]):
        while self._inner_lay.count():
            item = self._inner_lay.takeAt(0)
            if item.widget(): item.widget().deleteLater()

        if not results:
            self._inner_lay.addWidget(QLabel("No results"))
            self._lbl.setText("No matches")
            self.show()
            return

        self._lbl.setText(f"{len(results)} results")
        for row in results[:30]:
            btn = QPushButton(
                f"{row.get('date','')[:10]}  {row.get('merchant','') or row.get('description','')}  "
                f"{'£'+str(row['debit']) if row.get('debit') else '£'+str(row['credit']) if row.get('credit') else ''}"
            )
            btn.setProperty("flat", True)
            btn.setFixedHeight(32)
            btn.clicked.connect(lambda _, r=row: (
                self.navigate.emit("transactions", {"tx": r}), self.hide()))
            self._inner_lay.addWidget(btn)
        self._inner_lay.addStretch()
        self.show()
        self.adjustSize()


# ── Main window ────────────────────────────────────────────────────────────────

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(f"{APP_NAME}  —  v{APP_VERSION}")
        self.setMinimumSize(1280, 768)
        self._build()
        self._wire_signals()
        self._navigate("dashboard")
        QShortcut(QKeySequence("Ctrl+F"), self, self._focus_search)
        QShortcut(QKeySequence("Escape"), self, self._close_search)

    def _build(self):
        central = QWidget()
        self.setCentralWidget(central)
        root = QHBoxLayout(central)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        # Sidebar
        self._sidebar = Sidebar()
        root.addWidget(self._sidebar)

        # Right: topbar + page stack
        right = QVBoxLayout()
        right.setContentsMargins(0, 0, 0, 0)
        right.setSpacing(0)

        # Topbar with search
        topbar = QWidget()
        topbar.setFixedHeight(56)
        topbar.setStyleSheet(f"background:{C['bg']};border-bottom:1px solid {C['border']};")
        tbar_lay = QHBoxLayout(topbar)
        tbar_lay.setContentsMargins(20, 8, 20, 8)

        self._search_input = QLineEdit()
        self._search_input.setPlaceholderText("⌕  Search everything…  (Ctrl+F)")
        self._search_input.setFixedWidth(400)
        self._search_input.setFixedHeight(36)
        self._search_input.textChanged.connect(self._on_search)
        tbar_lay.addWidget(self._search_input)
        tbar_lay.addStretch()

        self._db_status = QLabel("● Ready")
        self._db_status.setStyleSheet(f"color:{C['green']};font-size:11px;")
        tbar_lay.addWidget(self._db_status)
        right.addWidget(topbar)

        # Page stack
        self._stack = QStackedWidget()
        self._stack.setStyleSheet(f"background:{C['bg']};")

        self._pages: dict[str, QWidget] = {
            "dashboard":    DashboardPage(),
            "imports":      ImportsPage(),
            "transactions": TransactionsPage(),
            "merchants":    MerchantsPage(),
            "reports":      ReportsPage(),
            "settings":     SettingsPage(),
        }
        for page in self._pages.values():
            self._stack.addWidget(page)

        right.addWidget(self._stack)
        root.addLayout(right, 1)

        # Search overlay (positioned over stack)
        self._search_overlay = SearchOverlay(self._stack)
        self._search_overlay.move(20, 20)

    def _wire_signals(self):
        self._sidebar.page_requested.connect(self._navigate)

        # Dashboard drill-down (category click → Transactions)
        self._pages["dashboard"].navigate.connect(self._navigate_with_params)

        # Merchants → Transactions drill-down
        self._pages["merchants"].navigate.connect(self._navigate_with_params)

        # Imports → refresh dashboard
        self._pages["imports"].imports_complete.connect(
            self._pages["dashboard"].refresh)

        # Search overlay navigation
        self._search_overlay.navigate.connect(self._navigate_with_params)

    def _navigate(self, key: str, params: dict | None = None):
        if key not in self._pages: return
        self._sidebar.set_active(key)
        page = self._pages[key]
        if params and hasattr(page, "set_filters"):
            page.set_filters(params)
        self._stack.setCurrentWidget(page)

    def _navigate_with_params(self, key: str, params: dict):
        self._navigate(key, params)

    def _focus_search(self):
        self._search_input.setFocus()
        self._search_input.selectAll()

    def _close_search(self):
        self._search_input.clear()
        self._search_overlay.hide()

    def _on_search(self, text: str):
        if len(text) < 2:
            self._search_overlay.hide()
            return
        results = db.search_all(DB_PATH, text)
        self._search_overlay.show_results(results)
