"""
rule_engine/engine.py
=====================
The categorisation engine — the only module the rest of the application
should import.

Public API
----------
    engine = RuleEngine(db_path, debug=False)
    engine.start()                      # loads rules into memory

    result = engine.categorise(description)
    # result.category    — the assigned category name (str)
    # result.matched_rule — the Rule that matched, or None
    # result.was_default  — True if "Other" was assigned

    engine.reload_rules()               # after user adds/edits a rule

Design principles
-----------------
* Zero database access after start().
* Deterministic — same description always produces same category.
* Purely offline — no network, no AI.
* All normalisation is identical to what the loader applies to keywords,
  so comparisons are always apples-to-apples.
"""

from __future__ import annotations

import logging
from pathlib import Path
from typing import Protocol, runtime_checkable

from .db import initialise
from .loader import RuleLoader
from .models import CategorizationResult, Rule, _normalise

logger = logging.getLogger(__name__)

DEFAULT_CATEGORY = "Other"


# ── Optional transaction protocol ─────────────────────────────────────────────
# The engine accepts any object that has a .description attribute,
# so it remains decoupled from a specific Transaction model.

@runtime_checkable
class HasDescription(Protocol):
    description: str


# ── Engine ─────────────────────────────────────────────────────────────────────

class RuleEngine:
    """
    The categorisation engine.

    Parameters
    ----------
    db_path : Path
        Path to the SQLite database file.
    debug : bool
        When True, each categorisation decision is logged at DEBUG level.
        Set to False in production for maximum throughput.
    """

    def __init__(self, db_path: Path, *, debug: bool = False) -> None:
        self._db_path = db_path
        self._debug   = debug
        self._loader  = RuleLoader(db_path)
        self._started = False

    # ── Lifecycle ──────────────────────────────────────────────────────────────

    def start(self) -> None:
        """
        Initialise the database (idempotent) and load all rules into memory.
        Call once at application startup.
        """
        initialise(self._db_path)
        self._loader.load()
        self._started = True
        logger.info(
            "RuleEngine started. %d rules ready.", self._loader.rule_count
        )

    def reload_rules(self) -> None:
        """
        Refresh the in-memory rule cache from the database.
        Call after the user adds, edits, or deletes a rule via the UI.
        """
        self._require_started()
        self._loader.reload()

    # ── Primary API ────────────────────────────────────────────────────────────

    def categorise(self, description: str) -> CategorizationResult:
        """
        Categorise a single transaction description.

        Parameters
        ----------
        description : str
            The raw transaction description from the bank statement.

        Returns
        -------
        CategorizationResult
            Contains the assigned category name and the matched Rule (if any).
        """
        self._require_started()

        normalised = _normalise(description)
        matched: Rule | None = None

        for rule in self._loader.rules:
            if rule.normalised_keyword in normalised:
                matched = rule
                break

        category = matched.category_name if matched else DEFAULT_CATEGORY
        was_default = matched is None

        if self._debug:
            if matched:
                logger.debug(
                    "Transaction: %-50s | Matched Rule: %-30s | Assigned: %s",
                    description[:50],
                    matched.keyword,
                    category,
                )
            else:
                logger.debug(
                    "Transaction: %-50s | No match — assigned: %s",
                    description[:50],
                    DEFAULT_CATEGORY,
                )

        return CategorizationResult(
            description=description,
            category=category,
            matched_rule=matched,
            was_default=was_default,
        )

    def categorise_many(
        self, descriptions: list[str]
    ) -> list[CategorizationResult]:
        """
        Categorise a batch of descriptions in one call.
        Marginally faster than calling categorise() in a loop (avoids
        repeated attribute lookups), suitable for bulk import.
        """
        self._require_started()
        return [self.categorise(d) for d in descriptions]

    def categorise_transaction(self, transaction: HasDescription) -> str:
        """
        Convenience overload: accepts any object with a .description attribute.
        Updates nothing — returns the category string for the caller to apply.

        Example
        -------
            tx.category = engine.categorise_transaction(tx)
        """
        result = self.categorise(transaction.description)
        return result.category

    # ── Introspection ──────────────────────────────────────────────────────────

    @property
    def rule_count(self) -> int:
        """Total number of loaded rules."""
        return self._loader.rule_count

    @property
    def is_started(self) -> bool:
        return self._started

    # ── Internal ───────────────────────────────────────────────────────────────

    def _require_started(self) -> None:
        if not self._started:
            raise RuntimeError(
                "RuleEngine.start() must be called before categorising transactions."
            )
