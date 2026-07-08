"""
rule_engine/__init__.py
=======================
Public surface of the rule_engine package.

Callers only need to import from here:

    from rule_engine import RuleEngine
    from rule_engine import db          # for rule CRUD
"""

from .engine import RuleEngine
from .models import Category, CategorizationResult, Rule
from . import db

__all__ = [
    "RuleEngine",
    "Category",
    "Rule",
    "CategorizationResult",
    "db",
]
