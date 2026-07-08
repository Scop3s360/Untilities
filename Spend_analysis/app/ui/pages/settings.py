"""app/ui/pages/settings.py — Settings: categories and user rules management."""
from __future__ import annotations

from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QPushButton, QTabWidget, QTableWidget,
                              QTableWidgetItem, QAbstractItemView, QLineEdit,
                              QComboBox, QMessageBox, QFrame, QHeaderView)
from PyQt6.QtCore import Qt

from app.ui.theme import C
from rule_engine import db as rule_db
from app.config import RULES_DB_PATH
from app.services.import_service import get_engine


def _lbl(text, size=13, bold=False, color=None) -> QLabel:
    l = QLabel(text)
    l.setStyleSheet(
        f"color:{color or C['text']};font-size:{size}px;"
        + ("font-weight:700;" if bold else ""))
    return l


class SettingsPage(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self._build()

    def _build(self):
        root = QVBoxLayout(self)
        root.setContentsMargins(28, 24, 28, 24)
        root.setSpacing(16)

        title = QLabel("Settings")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        root.addWidget(title)

        tabs = QTabWidget()
        root.addWidget(tabs)

        # ── User Rules tab ─────────────────────────────────────────
        rules_w = QWidget()
        rules_lay = QVBoxLayout(rules_w)

        rules_lay.addWidget(_lbl(
            "User rules take priority over built-in rules.  "
            "Add a keyword and assign it a category to override or extend the default behaviour.",
            12, color=C["text2"]))

        # Add rule form
        form = QHBoxLayout()
        self._kw_input = QLineEdit()
        self._kw_input.setPlaceholderText("Keyword (e.g. DONNA HARTNETT)")
        self._kw_input.setFixedHeight(36)
        form.addWidget(self._kw_input, 3)

        self._cat_combo = QComboBox()
        self._cat_combo.setFixedHeight(36)
        cats = [c.name for c in rule_db.get_all_categories(RULES_DB_PATH)]
        self._cat_combo.addItems(sorted(cats))
        form.addWidget(self._cat_combo, 2)

        add_btn = QPushButton("Add Rule")
        add_btn.setFixedHeight(36)
        add_btn.clicked.connect(self._add_rule)
        form.addWidget(add_btn)
        rules_lay.addLayout(form)

        # Rules table
        self._rules_tbl = QTableWidget()
        self._rules_tbl.setColumnCount(5)
        self._rules_tbl.setHorizontalHeaderLabels(
            ["Keyword", "Category", "Type", "Priority", "Created"])
        self._rules_tbl.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
        self._rules_tbl.setAlternatingRowColors(True)
        self._rules_tbl.verticalHeader().setVisible(False)
        self._rules_tbl.setSelectionBehavior(QAbstractItemView.SelectionBehavior.SelectRows)
        self._rules_tbl.horizontalHeader().setStretchLastSection(True)
        rules_lay.addWidget(self._rules_tbl)

        del_btn = QPushButton("🗑 Delete Selected Rule")
        del_btn.setFixedWidth(220)
        del_btn.setProperty("danger", True)
        del_btn.clicked.connect(self._delete_rule)
        rules_lay.addWidget(del_btn)

        tabs.addTab(rules_w, "User Rules")

        # ── Categories tab ─────────────────────────────────────────
        cat_w = QWidget()
        cat_lay = QVBoxLayout(cat_w)

        cat_lay.addWidget(_lbl(
            "Add custom categories.  Built-in categories cannot be removed.", 12, color=C["text2"]))

        cadd = QHBoxLayout()
        self._new_cat_input = QLineEdit()
        self._new_cat_input.setPlaceholderText("New category name")
        self._new_cat_input.setFixedHeight(36)
        cadd.addWidget(self._new_cat_input, 3)
        cadd_btn = QPushButton("Add Category")
        cadd_btn.setFixedHeight(36)
        cadd_btn.clicked.connect(self._add_category)
        cadd.addWidget(cadd_btn)
        cat_lay.addLayout(cadd)

        self._cat_tbl = QTableWidget()
        self._cat_tbl.setColumnCount(2)
        self._cat_tbl.setHorizontalHeaderLabels(["Category Name", "Display Order"])
        self._cat_tbl.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
        self._cat_tbl.setAlternatingRowColors(True)
        self._cat_tbl.verticalHeader().setVisible(False)
        self._cat_tbl.horizontalHeader().setStretchLastSection(True)
        cat_lay.addWidget(self._cat_tbl)

        tabs.addTab(cat_w, "Categories")

    def _load_rules(self):
        rules = rule_db.get_all_rules(RULES_DB_PATH)
        self._rules_tbl.setRowCount(len(rules))
        for r, rule in enumerate(rules):
            vals = [
                rule.keyword,
                rule.category_name,
                "User" if rule.is_user_rule else "Built-in",
                str(rule.priority),
                rule.created_at.strftime("%d %b %Y"),
            ]
            for c, v in enumerate(vals):
                item = QTableWidgetItem(v)
                item.setData(Qt.ItemDataRole.UserRole, rule.id)
                if v == "User":
                    item.setForeground(
                        __import__("PyQt6.QtGui", fromlist=["QColor"]).QColor(C["accent"]))
                self._rules_tbl.setItem(r, c, item)

    def _load_categories(self):
        cats = rule_db.get_all_categories(RULES_DB_PATH)
        self._cat_tbl.setRowCount(len(cats))
        for r, cat in enumerate(cats):
            self._cat_tbl.setItem(r, 0, QTableWidgetItem(cat.name))
            self._cat_tbl.setItem(r, 1, QTableWidgetItem(str(cat.display_order)))

    def _add_rule(self):
        kw  = self._kw_input.text().strip()
        cat = self._cat_combo.currentText()
        if not kw:
            QMessageBox.warning(self, "Missing Keyword", "Please enter a keyword.")
            return
        try:
            rule_db.add_user_rule(RULES_DB_PATH, kw, cat)
            get_engine().reload_rules()
            self._kw_input.clear()
            self._load_rules()
        except ValueError as e:
            QMessageBox.warning(self, "Rule Exists", str(e))

    def _delete_rule(self):
        row = self._rules_tbl.currentRow()
        if row < 0:
            QMessageBox.information(self, "Select a Rule", "Please select a rule to delete.")
            return
        item = self._rules_tbl.item(row, 2)
        if item and item.text() == "Built-in":
            QMessageBox.warning(self, "Cannot Delete", "Built-in rules cannot be deleted.")
            return
        id_item = self._rules_tbl.item(row, 0)
        rule_id = id_item.data(Qt.ItemDataRole.UserRole) if id_item else None
        if rule_id and QMessageBox.question(
                self, "Confirm", "Delete this rule?",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
        ) == QMessageBox.StandardButton.Yes:
            rule_db.delete_user_rule(RULES_DB_PATH, rule_id)
            get_engine().reload_rules()
            self._load_rules()

    def _add_category(self):
        name = self._new_cat_input.text().strip()
        if not name:
            return
        try:
            rule_db.add_category(RULES_DB_PATH, name)
            self._new_cat_input.clear()
            self._load_categories()
        except ValueError as e:
            QMessageBox.warning(self, "Category Exists", str(e))

    def showEvent(self, e):
        super().showEvent(e)
        self._load_rules()
        self._load_categories()
