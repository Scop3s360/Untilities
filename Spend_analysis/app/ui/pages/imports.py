"""app/ui/pages/imports.py — Import page with drag-and-drop."""
from __future__ import annotations

import os
from PyQt6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                              QPushButton, QFileDialog, QFrame, QTableWidget,
                              QTableWidgetItem, QHeaderView, QAbstractItemView,
                              QProgressDialog, QMessageBox, QSizePolicy)
from PyQt6.QtCore import Qt, pyqtSignal, QThread, pyqtSlot, QObject
from PyQt6.QtGui import QDragEnterEvent, QDropEvent, QColor
from datetime import datetime

from app.ui.theme import C
from app import database as db
from app.config import DB_PATH
from app.services import import_service


class _ImportWorker(QObject):
    finished = pyqtSignal(list)  # list of ImportResult
    progress = pyqtSignal(str)

    def __init__(self, paths: list[str]):
        super().__init__()
        self._paths = paths

    @pyqtSlot()
    def run(self):
        results = []
        for p in self._paths:
            self.progress.emit(f"Importing {os.path.basename(p)}…")
            results.append(import_service.import_file(p))
        self.finished.emit(results)


class DropZone(QFrame):
    files_dropped = pyqtSignal(list)

    def __init__(self, parent=None):
        super().__init__(parent)
        self.setAcceptDrops(True)
        self.setFixedHeight(200)
        self.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        self._set_idle()

        lay = QVBoxLayout(self)
        lay.setAlignment(Qt.AlignmentFlag.AlignCenter)
        lay.setSpacing(10)

        self._icon = QLabel("📥")
        self._icon.setStyleSheet("font-size:48px;background:transparent;")
        self._icon.setAlignment(Qt.AlignmentFlag.AlignCenter)
        lay.addWidget(self._icon)

        self._main_lbl = QLabel("Drag & Drop bank statements here")
        self._main_lbl.setStyleSheet(f"color:{C['text']};font-size:16px;font-weight:700;background:transparent;")
        self._main_lbl.setAlignment(Qt.AlignmentFlag.AlignCenter)
        lay.addWidget(self._main_lbl)

        sub = QLabel("Supports PDF and CSV — multiple files at once")
        sub.setStyleSheet(f"color:{C['text2']};font-size:12px;background:transparent;")
        sub.setAlignment(Qt.AlignmentFlag.AlignCenter)
        lay.addWidget(sub)

        browse = QPushButton("Browse Files")
        browse.setFixedWidth(160)
        browse.clicked.connect(self._browse)
        lay.addWidget(browse, alignment=Qt.AlignmentFlag.AlignCenter)

    def _set_idle(self):
        self.setStyleSheet(f"""
            DropZone {{
                background: {C['bg2']};
                border: 2px dashed {C['border2']};
                border-radius: 18px;
            }}
        """)

    def _set_hover(self):
        self.setStyleSheet(f"""
            DropZone {{
                background: {C['accent_dim']};
                border: 2px dashed {C['accent']};
                border-radius: 18px;
            }}
        """)

    def _browse(self):
        paths, _ = QFileDialog.getOpenFileNames(
            self, "Select Bank Statements", "",
            "Bank Statements (*.pdf *.csv);;PDF Files (*.pdf);;CSV Files (*.csv)")
        if paths:
            self.files_dropped.emit(paths)

    def dragEnterEvent(self, e: QDragEnterEvent):
        if e.mimeData().hasUrls():
            e.acceptProposedAction()
            self._set_hover()

    def dragLeaveEvent(self, e):
        self._set_idle()

    def dropEvent(self, e: QDropEvent):
        self._set_idle()
        paths = [u.toLocalFile() for u in e.mimeData().urls()
                 if u.toLocalFile().lower().endswith((".pdf", ".csv"))]
        if paths:
            self.files_dropped.emit(paths)


class ImportsPage(QWidget):
    imports_complete = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._thread = None
        self._build()

    def _build(self):
        lay = QVBoxLayout(self)
        lay.setContentsMargins(28, 24, 28, 24)
        lay.setSpacing(20)

        title = QLabel("Import Statements")
        title.setStyleSheet(f"color:{C['text']};font-size:24px;font-weight:800;")
        lay.addWidget(title)

        sub = QLabel("Import your bank statements and Spend Analysis will automatically categorise every transaction.")
        sub.setStyleSheet(f"color:{C['text2']};font-size:13px;")
        sub.setWordWrap(True)
        lay.addWidget(sub)

        self._zone = DropZone()
        self._zone.files_dropped.connect(self._start_import)
        lay.addWidget(self._zone)

        # Import history
        hist_lbl = QLabel("Import History")
        hist_lbl.setStyleSheet(f"color:{C['text']};font-size:16px;font-weight:700;")
        lay.addWidget(hist_lbl)

        self._hist_tbl = QTableWidget()
        self._hist_tbl.setColumnCount(8)
        self._hist_tbl.setHorizontalHeaderLabels(
            ["Statement", "Bank", "Period", "Pages", "Transactions", "Duplicates", "Imported", ""])
        self._hist_tbl.setEditTriggers(QAbstractItemView.EditTrigger.NoEditTriggers)
        self._hist_tbl.setAlternatingRowColors(True)
        self._hist_tbl.verticalHeader().setVisible(False)
        hdr = self._hist_tbl.horizontalHeader()
        hdr.setSectionResizeMode(0, QHeaderView.ResizeMode.Stretch)
        hdr.setSectionResizeMode(7, QHeaderView.ResizeMode.Fixed)
        self._hist_tbl.setColumnWidth(7, 140)
        self._hist_tbl.setSelectionBehavior(QAbstractItemView.SelectionBehavior.SelectRows)
        lay.addWidget(self._hist_tbl)

        # Delete button
        del_btn = QPushButton("🗑 Delete Selected Import")
        del_btn.setFixedWidth(240)
        del_btn.setProperty("danger", True)
        del_btn.clicked.connect(self._delete_selected)
        lay.addWidget(del_btn)

    def _start_import(self, paths: list[str]):
        # Duplicate detection: warn if any filename already imported
        existing = {s["filename"] for s in db.get_all_statements(DB_PATH)}
        dupes = [os.path.basename(p) for p in paths if os.path.basename(p) in existing]
        if dupes:
            msg = "\n".join(dupes)
            reply = QMessageBox.question(
                self, "Duplicate Statements Detected",
                f"The following statements have already been imported:\n\n{msg}\n\n"
                "Import anyway? (Identical transactions will be skipped automatically.)",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No)
            if reply == QMessageBox.StandardButton.No:
                return

        self._prog = QProgressDialog("Importing…", None, 0, 0, self)
        self._prog.setWindowModality(Qt.WindowModality.WindowModal)
        self._prog.setMinimumDuration(0)
        self._prog.show()

        self._thread = QThread()
        self._worker = _ImportWorker(paths)
        self._worker.moveToThread(self._thread)
        self._thread.started.connect(self._worker.run)
        self._worker.progress.connect(lambda m: self._prog.setLabelText(m))
        self._worker.finished.connect(self._on_done)
        self._worker.finished.connect(self._thread.quit)
        self._thread.start()

    def _on_done(self, results: list):
        self._prog.close()
        errors = [r for r in results if r.error]
        if errors:
            QMessageBox.warning(self, "Import Warning",
                "\n".join(f"{r.filename}: {r.error}" for r in errors))
        self._load_history()
        self.imports_complete.emit()

    def _load_history(self):
        stmts = db.get_all_statements(DB_PATH)
        self._hist_tbl.setRowCount(len(stmts))
        for r, s in enumerate(stmts):
            self._hist_tbl.setRowHeight(r, 38)
            vals = [
                s["filename"],
                s["bank"],
                s.get("period_label", ""),
                str(s.get("pages", 0)),
                str(s["transaction_count"]),
                str(s["duplicate_count"]),
                s["imported_at"][:19].replace("T", " "),
            ]
            for c, v in enumerate(vals):
                item = QTableWidgetItem(v)
                item.setData(Qt.ItemDataRole.UserRole, s["id"])
                item.setTextAlignment(Qt.AlignmentFlag.AlignVCenter | Qt.AlignmentFlag.AlignLeft)
                self._hist_tbl.setItem(r, c, item)

            # "View Transactions" button in last column
            btn = QPushButton("View Transactions →")
            btn.setProperty("flat", True)
            btn.setStyleSheet(f"color:{C['accent']};font-size:11px;padding:0 8px;")
            stmt_id = s["id"]
            btn.clicked.connect(lambda _, sid=stmt_id: self._view_transactions(sid))
            self._hist_tbl.setCellWidget(r, 7, btn)

    def _view_transactions(self, stmt_id: int):
        """Open the Transactions page filtered to this statement's period."""
        stmts = db.get_all_statements(DB_PATH)
        s = next((x for x in stmts if x["id"] == stmt_id), None)
        if s:
            period = s.get("period_label", "") or s["filename"]
            QMessageBox.information(
                self, "View Transactions",
                f"Navigate to the Transactions page and search for:\n\n\"{period}\"\n\n"
                f"  •  {s['transaction_count']} transactions imported\n"
                f"  •  {s.get('pages', 0)} pages\n"
                f"  •  Imported {s['imported_at'][:19].replace('T', ' ')}")

    def _delete_selected(self):
        row = self._hist_tbl.currentRow()
        if row < 0:
            QMessageBox.information(self, "Select an import",
                                    "Please select an import to delete.")
            return
        item = self._hist_tbl.item(row, 0)
        if not item: return
        stmt_id = item.data(Qt.ItemDataRole.UserRole)
        if QMessageBox.question(self, "Confirm Delete",
                "Delete this import and all its transactions?",
                QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No
            ) == QMessageBox.StandardButton.Yes:
            db.delete_statement(DB_PATH, stmt_id)
            self._load_history()
            self.imports_complete.emit()

    def showEvent(self, e):
        super().showEvent(e)
        self._load_history()

