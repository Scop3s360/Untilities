"""app/ui/theme.py — Dark theme palette and QSS stylesheet."""
from __future__ import annotations

# ── Palette ────────────────────────────────────────────────────────────────────
C = {
    "bg":          "#0D1117",
    "bg2":         "#161B22",
    "bg3":         "#1C2128",
    "sidebar":     "#0A0C12",
    "border":      "#21262D",
    "border2":     "#30363D",
    "accent":      "#7C6BF8",
    "accent_h":    "#9585FA",
    "accent_dim":  "#1E1B3E",
    "green":       "#3FB950",
    "green_dim":   "#1A3022",
    "red":         "#F85149",
    "red_dim":     "#3D1614",
    "blue":        "#58A6FF",
    "blue_dim":    "#1B2D4A",
    "amber":       "#D29922",
    "amber_dim":   "#362B00",
    "text":        "#E6EDF3",
    "text2":       "#8B949E",
    "text3":       "#484F58",
    "card_hover":  "#1C222B",
    "input_bg":    "#161B22",
    "scroll":      "#21262D",
}

# Chart colours (cycle for categories)
CHART_COLORS = [
    "#7C6BF8","#3FB950","#F85149","#58A6FF","#D29922",
    "#EC4899","#14B8A6","#F97316","#84CC16","#06B6D4",
    "#A855F7","#FB923C","#34D399","#60A5FA","#F472B6",
]

# Matplotlib dark rcParams
MPL_STYLE = {
    "figure.facecolor":  C["bg2"],
    "axes.facecolor":    C["bg2"],
    "axes.edgecolor":    C["border"],
    "text.color":        C["text"],
    "axes.labelcolor":   C["text2"],
    "xtick.color":       C["text2"],
    "ytick.color":       C["text2"],
    "grid.color":        C["border"],
    "grid.linewidth":    0.5,
    "axes.grid":         True,
    "axes.spines.top":   False,
    "axes.spines.right": False,
    "font.family":       "sans-serif",
    "font.size":         10,
    "legend.facecolor":  C["bg3"],
    "legend.edgecolor":  C["border"],
    "legend.labelcolor": C["text"],
}

def qss() -> str:
    c = C
    return f"""
/* ── Global ──────────────────────────────────────────────────────── */
QWidget {{
    background-color: {c['bg']};
    color: {c['text']};
    font-family: 'Segoe UI', 'Inter', sans-serif;
    font-size: 13px;
}}
QLabel {{ background: transparent; }}

/* ── Scrollbars ─────────────────────────────────────────────────── */
QScrollBar:vertical {{
    background: {c['bg']};  width: 6px;  margin: 0;
}}
QScrollBar::handle:vertical {{
    background: {c['scroll']};  border-radius: 3px;  min-height: 30px;
}}
QScrollBar::handle:vertical:hover {{ background: {c['border2']}; }}
QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical {{ height: 0; }}
QScrollBar:horizontal {{
    background: {c['bg']};  height: 6px;  margin: 0;
}}
QScrollBar::handle:horizontal {{
    background: {c['scroll']};  border-radius: 3px;  min-width: 30px;
}}
QScrollBar::add-line:horizontal, QScrollBar::sub-line:horizontal {{ width: 0; }}

/* ── Buttons ────────────────────────────────────────────────────── */
QPushButton {{
    background-color: {c['accent']};
    color: white;
    border: none;
    border-radius: 8px;
    padding: 8px 18px;
    font-weight: 600;
    font-size: 13px;
}}
QPushButton:hover  {{ background-color: {c['accent_h']}; }}
QPushButton:pressed {{ background-color: {c['accent']}; }}
QPushButton:disabled {{ background-color: {c['border']}; color: {c['text3']}; }}
QPushButton[flat="true"] {{
    background: transparent;
    color: {c['text2']};
    border: 1px solid {c['border']};
}}
QPushButton[flat="true"]:hover {{
    background: {c['bg3']};
    color: {c['text']};
    border-color: {c['border2']};
}}
QPushButton[danger="true"] {{
    background-color: {c['red_dim']};
    color: {c['red']};
    border: 1px solid {c['red']};
}}
QPushButton[danger="true"]:hover {{ background-color: {c['red']}; color: white; }}

/* ── Inputs ─────────────────────────────────────────────────────── */
QLineEdit, QComboBox, QDateEdit {{
    background-color: {c['input_bg']};
    border: 1px solid {c['border']};
    border-radius: 8px;
    padding: 8px 12px;
    color: {c['text']};
    selection-background-color: {c['accent']};
}}
QLineEdit:focus, QComboBox:focus, QDateEdit:focus {{
    border-color: {c['accent']};
}}
QComboBox::drop-down {{ border: none; width: 24px; }}
QComboBox::down-arrow {{ image: none; }}
QComboBox QAbstractItemView {{
    background: {c['bg3']};
    border: 1px solid {c['border']};
    selection-background-color: {c['accent_dim']};
    color: {c['text']};
}}

/* ── Tables ─────────────────────────────────────────────────────── */
QTableWidget {{
    background: {c['bg2']};
    border: 1px solid {c['border']};
    border-radius: 10px;
    gridline-color: {c['border']};
    alternate-background-color: {c['bg3']};
    selection-background-color: {c['accent_dim']};
    color: {c['text']};
}}
QTableWidget::item {{ padding: 6px 12px; border: none; }}
QTableWidget::item:selected {{
    background: {c['accent_dim']};
    color: {c['accent']};
}}
QHeaderView::section {{
    background-color: {c['bg3']};
    color: {c['text2']};
    border: none;
    border-bottom: 1px solid {c['border']};
    padding: 8px 12px;
    font-weight: 600;
    font-size: 12px;
    text-transform: uppercase;
}}
QHeaderView::section:first {{ border-top-left-radius: 10px; }}
QHeaderView::section:last  {{ border-top-right-radius: 10px; }}

/* ── Tab bars ───────────────────────────────────────────────────── */
QTabWidget::pane {{
    border: 1px solid {c['border']};
    border-radius: 10px;
    background: {c['bg2']};
}}
QTabBar::tab {{
    background: {c['bg3']};
    color: {c['text2']};
    border: 1px solid {c['border']};
    border-bottom: none;
    padding: 8px 18px;
    border-radius: 8px 8px 0 0;
    margin-right: 2px;
}}
QTabBar::tab:selected {{ background: {c['bg2']}; color: {c['accent']}; }}
QTabBar::tab:hover    {{ color: {c['text']}; }}

/* ── Splitter ───────────────────────────────────────────────────── */
QSplitter::handle {{ background: {c['border']}; }}
QSplitter::handle:horizontal {{ width: 1px; }}

/* ── Tooltip ────────────────────────────────────────────────────── */
QToolTip {{
    background: {c['bg3']};
    color: {c['text']};
    border: 1px solid {c['border']};
    border-radius: 6px;
    padding: 4px 8px;
}}

/* ── Sidebar nav buttons ────────────────────────────────────────── */
QPushButton[nav="true"] {{
    background: transparent;
    color: {c['text2']};
    border: none;
    border-radius: 10px;
    text-align: left;
    padding: 10px 14px;
    font-size: 14px;
    font-weight: 500;
}}
QPushButton[nav="true"]:hover  {{ background: {c['bg3']}; color: {c['text']}; }}
QPushButton[nav="true"][active="true"] {{
    background: {c['accent_dim']};
    color: {c['accent']};
    font-weight: 700;
}}
QPushButton[nav="true"][future="true"] {{
    color: {c['text3']};
    font-style: italic;
}}
QPushButton[nav="true"][future="true"]:hover {{
    background: transparent;
    color: {c['text3']};
    cursor: default;
}}
"""
