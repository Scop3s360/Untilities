"""app/ui/charts.py — Matplotlib chart widgets embedded in PyQt6."""
from __future__ import annotations

import matplotlib
matplotlib.use("QtAgg")
import matplotlib.pyplot as plt
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg as Canvas
from matplotlib.figure import Figure
from matplotlib.patches import FancyBboxPatch
import numpy as np

from PyQt6.QtWidgets import QWidget, QVBoxLayout, QSizePolicy
from PyQt6.QtCore import Qt

from app.ui.theme import C, CHART_COLORS, MPL_STYLE


def _apply_style():
    plt.rcParams.update(MPL_STYLE)


class BaseChart(QWidget):
    def __init__(self, parent=None, figsize=(5, 3.5)):
        super().__init__(parent)
        _apply_style()
        self.fig = Figure(figsize=figsize, layout="constrained")
        self.canvas = Canvas(self.fig)
        self.canvas.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Expanding)
        lay = QVBoxLayout(self)
        lay.setContentsMargins(0, 0, 0, 0)
        lay.addWidget(self.canvas)

    def refresh(self):
        self.canvas.draw()


class DonutChart(BaseChart):
    """Spending by category donut chart."""

    def __init__(self, parent=None):
        super().__init__(parent, figsize=(5, 4))
        self.ax = self.fig.add_subplot(111)

    def update(self, data: list[dict]):
        """data: list of {'category': str, 'total': float}"""
        self.ax.clear()
        if not data:
            self.ax.text(0.5, 0.5, "No data", ha="center", va="center",
                         color=C["text2"], fontsize=14, transform=self.ax.transAxes)
            self.ax.set_aspect("equal")
            self.refresh()
            return

        labels = [d["category"] for d in data[:12]]
        sizes  = [d["total"]    for d in data[:12]]
        colors = CHART_COLORS[:len(labels)]

        wedges, texts = self.ax.pie(
            sizes, labels=None, colors=colors,
            startangle=90,
            wedgeprops=dict(width=0.55, edgecolor=C["bg2"], linewidth=2),
        )

        # Centre total
        total = sum(sizes)
        self.ax.text(0, 0.08, "Total",   ha="center", va="center",
                     color=C["text2"], fontsize=10)
        self.ax.text(0, -0.12, f"£{total:,.0f}", ha="center", va="center",
                     color=C["text"], fontsize=16, fontweight="bold")

        # Legend
        self.ax.legend(
            wedges, [f"{l}  £{s:,.0f}" for l, s in zip(labels, sizes)],
            loc="center left", bbox_to_anchor=(1, 0, 0.5, 1),
            fontsize=9, frameon=True,
        )
        self.ax.set_aspect("equal")
        self.refresh()


class LineChart(BaseChart):
    """Monthly spending/income trend."""

    def __init__(self, parent=None):
        super().__init__(parent, figsize=(7, 3))
        self.ax = self.fig.add_subplot(111)

    def update(self, data: list[dict]):
        """data: list of {'month': 'YYYY-MM', 'spending': float, 'income': float}"""
        self.ax.clear()
        if not data:
            self.ax.text(0.5, 0.5, "No data", ha="center", va="center",
                         color=C["text2"], fontsize=14, transform=self.ax.transAxes)
            self.refresh()
            return

        months   = [d["month"][-5:].replace("-", "/") for d in data]
        spending = [d.get("spending", 0) or 0 for d in data]
        income   = [d.get("income", 0) or 0 for d in data]
        x = np.arange(len(months))

        self.ax.plot(x, spending, color=C["red"],   linewidth=2.5, marker="o",
                     markersize=5, label="Spending", zorder=3)
        self.ax.fill_between(x, spending, alpha=0.12, color=C["red"])

        self.ax.plot(x, income, color=C["green"], linewidth=2.5, marker="o",
                     markersize=5, label="Income", zorder=3)
        self.ax.fill_between(x, income, alpha=0.12, color=C["green"])

        self.ax.set_xticks(x)
        self.ax.set_xticklabels(months, rotation=30, ha="right", fontsize=9)
        self.ax.yaxis.set_major_formatter(plt.FuncFormatter(lambda v, _: f"£{v:,.0f}"))
        self.ax.legend(loc="upper left", fontsize=9)
        self.refresh()


class HBarChart(BaseChart):
    """Horizontal bar chart for top merchants."""

    def __init__(self, parent=None):
        super().__init__(parent, figsize=(6, 3.5))
        self.ax = self.fig.add_subplot(111)

    def update(self, data: list[dict], label_key="merchant", value_key="total"):
        self.ax.clear()
        if not data:
            self.ax.text(0.5, 0.5, "No data", ha="center", va="center",
                         color=C["text2"], fontsize=14, transform=self.ax.transAxes)
            self.refresh()
            return

        labels = [d[label_key][:22] for d in data]
        values = [d[value_key] or 0 for d in data]
        y = np.arange(len(labels))

        bars = self.ax.barh(y, values, color=CHART_COLORS[0], height=0.65,
                            edgecolor="none")
        # Gradient-ish: lighten bars towards top
        for i, bar in enumerate(bars):
            alpha = 0.6 + 0.4 * (i / max(len(bars) - 1, 1))
            bar.set_alpha(alpha)

        self.ax.set_yticks(y)
        self.ax.set_yticklabels(labels, fontsize=9)
        self.ax.xaxis.set_major_formatter(plt.FuncFormatter(lambda v, _: f"£{v:,.0f}"))
        self.ax.invert_yaxis()
        self.ax.tick_params(axis="x", labelsize=9)
        self.refresh()


class VBarChart(BaseChart):
    """Vertical bar chart for monthly breakdown per category/merchant."""

    def __init__(self, parent=None):
        super().__init__(parent, figsize=(6, 3))
        self.ax = self.fig.add_subplot(111)

    def update(self, data: list[dict], label_key="month", value_key="total",
               color=None):
        self.ax.clear()
        if not data:
            self.ax.text(0.5, 0.5, "No data", ha="center", va="center",
                         color=C["text2"], fontsize=14, transform=self.ax.transAxes)
            self.refresh()
            return

        labels = [str(d[label_key])[-5:].replace("-", "/") for d in data]
        values = [d[value_key] or 0 for d in data]
        x = np.arange(len(labels))

        self.ax.bar(x, values, color=color or C["accent"], width=0.6,
                    edgecolor="none", alpha=0.9)
        self.ax.set_xticks(x)
        self.ax.set_xticklabels(labels, rotation=30, ha="right", fontsize=9)
        self.ax.yaxis.set_major_formatter(plt.FuncFormatter(lambda v, _: f"£{v:,.0f}"))
        self.refresh()
