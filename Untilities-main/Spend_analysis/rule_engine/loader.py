"""
rule_engine/loader.py
=====================
Loads all rules from SQLite into memory once at application startup.

After load() is called, categorisation never touches the database.
Call reload() if rules change at runtime (e.g. after user adds a rule via UI).
"""

from __future__ import annotations

import logging
from pathlib import Path

from .db import get_all_rules
from .models import Rule

logger = logging.getLogger(__name__)


class RuleLoader:
    """
    In-memory rule cache.

    Rules are stored sorted by:
      1. priority ASC  (0 = user rules first, 100 = built-in)
      2. keyword length DESC  (longer keywords match before shorter ones,
         e.g. "AMAZON WEB SERVICES" wins over "AMAZON")

    This sort is applied once at load time — O(n log n) cost paid once,
    not per transaction.
    """

    def __init__(self, db_path: Path) -> None:
        self._db_path = db_path
        self._rules: list[Rule] = []
        self._loaded = False

    # ── Public interface ───────────────────────────────────────────────────────

    def load(self) -> None:
        """Load (or reload) all enabled rules from the database into memory."""
        raw = get_all_rules(self._db_path)

        # Sort: priority ASC, then keyword length DESC for specificity
        self._rules = sorted(
            raw,
            key=lambda r: (r.priority, -len(r.normalised_keyword))
        )
        self._loaded = True
        logger.info(
            "RuleLoader: %d rules loaded (%d user, %d built-in).",
            len(self._rules),
            sum(1 for r in self._rules if r.is_user_rule),
            sum(1 for r in self._rules if not r.is_user_rule),
        )

    def reload(self) -> None:
        """Refresh the in-memory cache from the database (e.g. after a rule edit)."""
        logger.debug("RuleLoader: reloading rules.")
        self.load()

    @property
    def rules(self) -> list[Rule]:
        if not self._loaded:
            raise RuntimeError(
                "RuleLoader.load() must be called before accessing rules."
            )
        return self._rules

    @property
    def rule_count(self) -> int:
        return len(self._rules)
