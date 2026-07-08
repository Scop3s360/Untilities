"""
rule_engine/engine.py
=====================
Layered categorisation engine.

Priority order (first match wins):
  1. User-defined rules        (DB priority 0, is_user_rule=True)   → 100 %
  2. Built-in merchant rules   (DB priority 100, is_user_rule=False) → 95 %
  3. Transaction-type rules    (prefix/token patterns)               → 90 %
  4. Description keyword rules (extended keyword list)               → 80 %
  5. Amount heuristic          (credit → Income)                     → 70 %
  6. Default: Other                                                   →  0 %

Public API (unchanged):
    engine = RuleEngine(db_path, debug=False)
    engine.start()
    result = engine.categorise(description, amount=None, is_credit=None)
    engine.reload_rules()
"""

from __future__ import annotations

import logging
import re
from pathlib import Path
from typing import Optional, Protocol, runtime_checkable

from .db import initialise
from .loader import RuleLoader
from .models import CategorizationResult, Rule, _normalise

logger = logging.getLogger(__name__)

DEFAULT_CATEGORY = "Other"


# ── Transaction-type patterns ─────────────────────────────────────────────────
# Matched against the START of the normalised description (token check).
# Each entry: (pattern, category, confidence)

_TYPE_RULES: list[tuple[re.Pattern, str, int]] = [
    # Credits → Income
    (re.compile(r"\b(bacs credit|bank credit|faster payment (in|received|credit)|"
                r"credit transfer|standing order credit|transfer in|"
                r"interest paid|interest credit|"
                r"cashback|refund|reversal|returned payment|payment received|"
                r"credit interest)\b"),
     "Income", 90),

    # Savings / ISA transfers → Finance
    (re.compile(r"\b(savings transfer|isa transfer|transfer to savings|"
                r"fixed rate|notice account)\b"),
     "Finance", 90),

    # Generic transfer out → Finance
    (re.compile(r"\b(transfer out|bank transfer|faster payment out|"
                r"online transfer|internet banking transfer)\b"),
     "Finance", 90),

    # ATM / cash → Finance
    (re.compile(r"\b(cash withdrawal|atm |cashpoint|cash machine)\b"),
     "Finance", 90),
]


# ── Description keyword rules ─────────────────────────────────────────────────
# Applied after merchant rules and type rules.
# Checked as substring in the normalised description.

_KEYWORD_RULES: list[tuple[str, str, int]] = [
    # Income
    ("salary",          "Income",   80),
    ("payroll",         "Income",   80),
    ("wages",           "Income",   80),
    ("employer",        "Income",   80),
    ("dividend",        "Income",   80),
    ("interest",        "Income",   80),
    ("pension",         "Income",   80),
    ("bursary",         "Income",   80),
    ("grant",           "Income",   75),
    ("refund",          "Income",   80),
    ("reversal",        "Income",   80),
    ("cashback",        "Income",   80),
    ("returned",        "Income",   75),

    # Housing
    ("mortgage",        "Housing",  80),
    ("council tax",     "Housing",  80),
    ("ground rent",     "Housing",  80),
    ("service charge",  "Housing",  75),
    ("rent",            "Housing",  75),
    ("landlord",        "Housing",  75),
    ("letting",         "Housing",  75),
    ("water rates",     "Housing",  80),

    # Finance
    ("loan",            "Finance",  80),
    ("credit card",     "Finance",  80),
    ("repayment",       "Finance",  75),
    ("investment",      "Finance",  75),
    ("isa",             "Finance",  75),
    ("savings",         "Finance",  75),
    ("premium bond",    "Finance",  80),
    ("bank charge",     "Finance",  80),
    ("overdraft",       "Finance",  80),

    # Transport
    ("petrol",          "Transport", 80),
    ("fuel",            "Transport", 80),
    ("toll",            "Transport", 75),
    ("road tax",        "Transport", 80),
    ("mot ",            "Transport", 80),
    ("bus pass",        "Transport", 80),
    ("train",           "Transport", 75),
    ("rail",            "Transport", 75),
    ("tram",            "Transport", 75),
    ("taxi",            "Transport", 75),

    # Health
    ("prescription",    "Health",   80),
    ("pharmacy",        "Health",   80),
    ("dentist",         "Health",   80),
    ("dental",          "Health",   80),
    ("optician",        "Health",   80),
    ("hospital",        "Health",   75),
    ("gp ",             "Health",   75),
    ("nhs",             "Health",   80),
    ("gym",             "Health",   75),
    ("fitness",         "Health",   75),

    # Family
    ("school",          "Family",   75),
    ("nursery",         "Family",   75),
    ("childcare",       "Family",   80),
    ("child",           "Family",   70),
    ("tuition",         "Family",   75),
    ("vet ",            "Family",   80),
    ("veterinary",      "Family",   80),
    ("pet ",            "Family",   75),

    # Shopping
    ("amazon",          "Shopping", 80),
    ("online purchase", "Shopping", 75),
    ("delivery",        "Shopping", 70),

    # Entertainment
    ("subscription",    "Entertainment", 75),
    ("streaming",       "Entertainment", 75),
    ("cinema",          "Entertainment", 75),
    ("ticket",          "Entertainment", 70),

    # Travel
    ("hotel",           "Travel",   75),
    ("flight",          "Travel",   75),
    ("airport",         "Travel",   75),
    ("holiday",         "Travel",   75),
    ("airbnb",          "Travel",   80),
]

# Pre-normalise keyword rules for fast matching
_KEYWORD_RULES_NORM: list[tuple[str, str, int]] = [
    (_normalise(kw), cat, conf) for kw, cat, conf in _KEYWORD_RULES
]


# ── Optional transaction protocol ─────────────────────────────────────────────

@runtime_checkable
class HasDescription(Protocol):
    description: str


# ── Engine ─────────────────────────────────────────────────────────────────────

class RuleEngine:
    """
    Layered categorisation engine.

    Parameters
    ----------
    db_path : Path
        Path to the SQLite database file.
    debug : bool
        When True, each categorisation decision is logged at DEBUG level.
    """

    def __init__(self, db_path: Path, *, debug: bool = False) -> None:
        self._db_path = db_path
        self._debug   = debug
        self._loader  = RuleLoader(db_path)
        self._started = False

    # ── Lifecycle ──────────────────────────────────────────────────────────────

    def start(self) -> None:
        """Initialise the database (idempotent) and load all rules into memory."""
        initialise(self._db_path)
        self._loader.load()
        self._started = True
        logger.info("RuleEngine started. %d rules ready.", self._loader.rule_count)

    def reload_rules(self) -> None:
        """Refresh the in-memory rule cache. Call after user edits a rule."""
        self._require_started()
        self._loader.reload()

    # ── Primary API ────────────────────────────────────────────────────────────

    def categorise(
        self,
        description: str,
        amount: float | None = None,
        is_credit: bool | None = None,
    ) -> CategorizationResult:
        """
        Categorise a single transaction description using the full rule stack.

        Parameters
        ----------
        description : str
            The raw transaction description.
        amount : float | None
            The absolute transaction amount (positive = money in/out).
            Used for the amount-heuristic layer.
        is_credit : bool | None
            True  → money entering the account (credit / income)
            False → money leaving the account (debit)
            None  → unknown direction; heuristic skipped.
        """
        self._require_started()

        normalised = _normalise(description)
        matched_rule: Rule | None = None
        confidence  = 0
        source      = "default"

        # ── Layer 1: User-defined rules (priority 0) ──────────────────────────
        for rule in self._loader.rules:
            if not rule.is_user_rule:
                break   # rules sorted priority ASC — once we hit non-user, stop
            if rule.normalised_keyword in normalised:
                matched_rule = rule
                confidence   = 100
                source       = "user_rule"
                break

        # ── Layer 2: Transaction-type rules ───────────────────────────────────
        # Checked before merchant rules: direction-specific patterns are more
        # reliable than generic merchant keyword matching.
        if matched_rule is None:
            for pattern, cat, conf in _TYPE_RULES:
                if pattern.search(normalised):
                    if self._debug:
                        logger.debug("  [type]    '%s' → %s", description[:60], cat)
                    return CategorizationResult(
                        description=description,
                        category=cat,
                        matched_rule=None,
                        was_default=False,
                        confidence=conf,
                        source="type_rule",
                    )

        # ── Layer 3: Built-in merchant rules (priority 100) ───────────────────
        if matched_rule is None:
            for rule in self._loader.rules:
                if rule.is_user_rule:
                    continue  # already checked above
                if rule.normalised_keyword in normalised:
                    matched_rule = rule
                    confidence   = 95
                    source       = "merchant_rule"
                    break

        # ── Layer 4: Description keyword rules ────────────────────────────────
        if matched_rule is None:
            for kw_norm, cat, conf in _KEYWORD_RULES_NORM:
                if kw_norm in normalised:
                    if self._debug:
                        logger.debug("  [keyword] '%s' matched '%s' → %s",
                                     description[:60], kw_norm, cat)
                    return CategorizationResult(
                        description=description,
                        category=cat,
                        matched_rule=None,
                        was_default=False,
                        confidence=conf,
                        source="keyword_rule",
                    )

        # ── Layer 5: Amount heuristic ─────────────────────────────────────────
        if matched_rule is None and is_credit is True:
            if self._debug:
                logger.debug("  [heuristic] credit with no rule → Income")
            return CategorizationResult(
                description=description,
                category="Income",
                matched_rule=None,
                was_default=False,
                confidence=70,
                source="amount_heuristic",
            )

        # ── Layer 6: Default ──────────────────────────────────────────────────
        category    = matched_rule.category_name if matched_rule else DEFAULT_CATEGORY
        was_default = matched_rule is None

        if self._debug:
            if matched_rule:
                logger.debug("  [%s] '%s' → %s  (rule: %s)",
                             source, description[:60], category, matched_rule.keyword)
            else:
                logger.debug("  [default] '%s' → Other", description[:60])

        return CategorizationResult(
            description=description,
            category=category,
            matched_rule=matched_rule,
            was_default=was_default,
            confidence=confidence,
            source=source,
        )

    def categorise_many(
        self, descriptions: list[str]
    ) -> list[CategorizationResult]:
        """Categorise a batch of descriptions."""
        self._require_started()
        return [self.categorise(d) for d in descriptions]

    def categorise_transaction(self, transaction: HasDescription) -> str:
        """
        Convenience overload: accepts any object with a .description attribute.
        Returns the category string.
        """
        result = self.categorise(transaction.description)
        return result.category

    # ── Introspection ──────────────────────────────────────────────────────────

    @property
    def rule_count(self) -> int:
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
