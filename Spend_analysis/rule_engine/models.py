"""
rule_engine/models.py
=====================
Pure data models for the categorisation engine.
No database logic lives here — only dataclass definitions.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional


@dataclass
class Category:
    """A spend category stored in the database."""
    id:           int
    name:         str
    display_order: int = 0        # controls sort order in UI dropdowns

    def __str__(self) -> str:
        return self.name


@dataclass
class Rule:
    """
    A categorisation rule.

    Matching is performed by checking whether *keyword* appears anywhere
    inside a normalised transaction description (case-insensitive, punctuation
    stripped).

    priority
        Lower numbers are checked first.
        User rules are seeded at priority 0; built-in rules at priority 100.
        This ensures user rules always win without extra conditional logic.

    is_user_rule
        Informational flag — True for rules the user created, False for
        built-in defaults.  Used for display and export only; priority governs
        actual ordering.
    """
    id:           int
    keyword:      str
    category_name: str            # denormalised for fast in-memory lookup
    priority:     int = 100       # 0 = user rule, 100 = built-in
    enabled:      bool = True
    is_user_rule: bool = False
    created_at:   datetime = field(default_factory=datetime.now)
    modified_at:  datetime = field(default_factory=datetime.now)

    def __post_init__(self):
        # Pre-compute a normalised keyword for fast matching at load time
        self._normalised = _normalise(self.keyword)

    @property
    def normalised_keyword(self) -> str:
        return self._normalised

    def __repr__(self) -> str:
        kind = "user" if self.is_user_rule else "built-in"
        return f"<Rule [{kind}] '{self.keyword}' → '{self.category_name}' (pri={self.priority})>"


@dataclass
class CategorizationResult:
    """
    Returned by the engine for every transaction processed.

    confidence : int  0–100
        How confident the engine is in this assignment.
        100 = user rule, 95 = merchant rule, 90 = type rule,
        80 = keyword rule, 70 = amount heuristic, 0 = default/Other.
    source : str
        Which layer produced the result.
        'user_rule' | 'merchant_rule' | 'type_rule' | 'keyword_rule' |
        'amount_heuristic' | 'default'
    """
    description:   str
    category:      str
    matched_rule:  Optional[Rule]
    was_default:   bool           # True when "Other" was assigned (no match)
    confidence:    int  = 0       # internal use only — not displayed in UI
    source:        str  = "default"


# ── Internal normalisation helper (also used by the engine) ──────────────────

def _normalise(text: str) -> str:
    """
    Lowercase, collapse whitespace, strip common punctuation.
    Used consistently for both keyword storage and description matching.
    """
    import re
    text = text.lower()
    text = re.sub(r"[*.,\-_/\\&#@!?]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text
