"""
tests/test_rule_engine.py
=========================
Unit tests for the categorisation engine.

Run with:
    python -m pytest tests/ -v

Or directly:
    python tests/test_rule_engine.py
"""

from __future__ import annotations

import sys
import os
import tempfile
import unittest
from pathlib import Path
from dataclasses import dataclass
from typing import Optional
from decimal import Decimal
from datetime import datetime

# Allow running from project root
sys.path.insert(0, str(Path(__file__).parent.parent))

from rule_engine import RuleEngine, db
from rule_engine.models import _normalise


# ── Shared fixture ─────────────────────────────────────────────────────────────

def make_engine(debug: bool = False) -> tuple[RuleEngine, Path]:
    """Return a fresh engine backed by a temp DB."""
    tmp = tempfile.mktemp(suffix=".db")
    path = Path(tmp)
    engine = RuleEngine(path, debug=debug)
    engine.start()
    return engine, path


# ── Normalisation ──────────────────────────────────────────────────────────────

class TestNormalise(unittest.TestCase):

    def test_lowercase(self):
        self.assertEqual(_normalise("TESCO"), "tesco")

    def test_strips_asterisks(self):
        # Asterisks → spaces → whitespace collapsed → single space
        self.assertEqual(_normalise("GOOGLE ****8791"), "google 8791")

    def test_collapses_whitespace(self):
        self.assertEqual(_normalise("  TESCO  STORES  "), "tesco stores")

    def test_strips_punctuation(self):
        self.assertEqual(_normalise("BOOKING.COM"), "booking com")

    def test_strips_hyphens(self):
        self.assertEqual(_normalise("CO-OP FOOD"), "co op food")


# ── Built-in rule matching ─────────────────────────────────────────────────────

class TestBuiltinRules(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.engine, cls.db_path = make_engine()

    def _cat(self, description: str) -> str:
        return self.engine.categorise(description).category

    # Groceries
    def test_tesco(self):
        self.assertEqual(self._cat("TESCO STORES 6111 PETERBOROUGH"), "Groceries")

    def test_sainsburys(self):
        self.assertEqual(self._cat("SAINSBURYS S/MKTS LONDON"), "Groceries")

    def test_aldi(self):
        self.assertEqual(self._cat("ALDI STORES"), "Groceries")

    # Transport
    def test_uber_transport(self):
        # UBER without EATS → Transport
        self.assertEqual(self._cat("UBER *TRIP HELP.UBER.COM"), "Transport")

    def test_stagecoach(self):
        self.assertEqual(self._cat("STAGECOACH SERVICES STOP 12"), "Transport")

    def test_lner(self):
        self.assertEqual(self._cat("LNE RAILWAY PBO STN PTR"), "Transport")

    def test_black_taxi(self):
        self.assertEqual(self._cat("BLACK TAXI* BLACKTAXI HELP"), "Transport")

    # Takeaways
    def test_deliveroo(self):
        self.assertEqual(self._cat("DELIVEROO LONDON"), "Takeaways")

    def test_dominos(self):
        self.assertEqual(self._cat("DOMINO S PIZZA MILTON KEYNES"), "Takeaways")

    # Coffee
    def test_caf_local(self):
        self.assertEqual(self._cat("CAF LOCAL PL2 PETERBOROUGH"), "Coffee")

    def test_greggs(self):
        self.assertEqual(self._cat("GREGGS PLC HIGH HOLBORN"), "Coffee")

    # Subscriptions
    def test_linkedin(self):
        self.assertEqual(self._cat("LinkedIn*P3005718840 LINKEDIN.COM (Recurring)"), "Subscriptions")

    def test_google_play(self):
        self.assertEqual(self._cat("Google Play Apps London"), "Subscriptions")

    def test_uber_one(self):
        self.assertEqual(self._cat("UBER *ONE MEMBERSHIP UBER.COM/BI"), "Subscriptions")

    # Savings
    def test_sprive(self):
        self.assertEqual(self._cat("Transfer to Sprive Limited"), "Savings")

    # Income
    def test_aws_income(self):
        self.assertEqual(self._cat("Bank credit AMAZON WEB SERVICE J177"), "Income")

    # Health / Medical
    def test_optical(self):
        self.assertEqual(self._cat("Direct debit AO-OPTICALSERVICES"), "Medical")

    # Shopping
    def test_fiverr(self):
        self.assertEqual(self._cat("FiverrEU Limassol"), "Shopping")


# ── Case insensitivity ─────────────────────────────────────────────────────────

class TestCaseInsensitivity(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.engine, cls.db_path = make_engine()

    def test_lowercase_input(self):
        result = self.engine.categorise("tesco stores")
        self.assertEqual(result.category, "Groceries")

    def test_mixed_case(self):
        result = self.engine.categorise("Deliveroo London")
        self.assertEqual(result.category, "Takeaways")


# ── Default category ───────────────────────────────────────────────────────────

class TestDefaultCategory(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.engine, cls.db_path = make_engine()

    def test_unknown_returns_other(self):
        result = self.engine.categorise("RANDOM MERCHANT XYZ 12345")
        self.assertEqual(result.category, "Other")
        self.assertTrue(result.was_default)
        self.assertIsNone(result.matched_rule)

    def test_empty_string_returns_other(self):
        result = self.engine.categorise("")
        self.assertEqual(result.category, "Other")


# ── User rules override ────────────────────────────────────────────────────────

class TestUserRules(unittest.TestCase):

    def setUp(self):
        self.engine, self.db_path = make_engine()

    def test_user_rule_created_and_applied(self):
        db.add_user_rule(self.db_path, "DONNA HARTNETT", "Child Support")
        self.engine.reload_rules()
        result = self.engine.categorise("Payment to DONNA HARTNETT")
        self.assertEqual(result.category, "Child Support")
        self.assertTrue(result.matched_rule.is_user_rule)

    def test_user_rule_overrides_builtin(self):
        # AMAZON is a built-in → Shopping.
        # User rule remaps it to a custom category.
        db.add_user_rule(self.db_path, "AMAZON", "Work Expenses")
        self.engine.reload_rules()
        result = self.engine.categorise("AMAZON PURCHASE")
        self.assertEqual(result.category, "Work Expenses")

    def test_user_rule_priority_zero(self):
        rule = db.add_user_rule(self.db_path, "TESTMERCHANT", "Gifts")
        self.assertEqual(rule.priority, 0)
        self.assertTrue(rule.is_user_rule)

    def test_duplicate_user_rule_raises(self):
        db.add_user_rule(self.db_path, "DUPLICATE_KW", "Groceries")
        with self.assertRaises(ValueError):
            db.add_user_rule(self.db_path, "DUPLICATE_KW", "Shopping")

    def test_disable_user_rule(self):
        rule = db.add_user_rule(self.db_path, "MYMERCHANT", "Groceries")
        db.update_user_rule(self.db_path, rule.id, enabled=False)
        self.engine.reload_rules()
        result = self.engine.categorise("MYMERCHANT LONDON")
        # No match → Other
        self.assertEqual(result.category, "Other")

    def test_delete_user_rule(self):
        rule = db.add_user_rule(self.db_path, "DELETEMERCHANT", "Groceries")
        self.engine.reload_rules()
        db.delete_user_rule(self.db_path, rule.id)
        self.engine.reload_rules()
        result = self.engine.categorise("DELETEMERCHANT LONDON")
        self.assertEqual(result.category, "Other")


# ── Specificity: longer keyword wins ──────────────────────────────────────────

class TestSpecificity(unittest.TestCase):

    def test_longer_keyword_wins(self):
        """
        "AMAZON WEB SERVICE" → Income
        "AMAZON"             → Shopping
        The more specific keyword must win.
        """
        engine, _ = make_engine()
        result = engine.categorise("Bank credit AMAZON WEB SERVICE J177 00450543")
        self.assertEqual(result.category, "Income")

    def test_shorter_keyword_still_matches_other_transactions(self):
        engine, _ = make_engine()
        result = engine.categorise("AMAZON.CO.UK PURCHASE")
        self.assertEqual(result.category, "Shopping")


# ── Batch categorisation ───────────────────────────────────────────────────────

class TestBatchCategorisation(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.engine, cls.db_path = make_engine()

    def test_categorise_many(self):
        descriptions = [
            "TESCO STORES 6111",
            "DELIVEROO LONDON",
            "STARBUCKS CITY",
            "UNKNOWN MERCHANT",
        ]
        results = self.engine.categorise_many(descriptions)
        self.assertEqual(len(results), 4)
        self.assertEqual(results[0].category, "Groceries")
        self.assertEqual(results[1].category, "Takeaways")
        self.assertEqual(results[2].category, "Coffee")
        self.assertEqual(results[3].category, "Other")


# ── Transaction protocol ───────────────────────────────────────────────────────

class TestTransactionProtocol(unittest.TestCase):

    def test_categorise_transaction_object(self):
        @dataclass
        class Tx:
            description: str
            category: str = ""

        engine, _ = make_engine()
        tx = Tx(description="TESCO STORES")
        tx.category = engine.categorise_transaction(tx)
        self.assertEqual(tx.category, "Groceries")


# ── Database: category CRUD ────────────────────────────────────────────────────

class TestCategoryDB(unittest.TestCase):

    def setUp(self):
        _, self.db_path = make_engine()

    def test_default_categories_seeded(self):
        cats = db.get_all_categories(self.db_path)
        names = [c.name for c in cats]
        self.assertIn("Groceries", names)
        self.assertIn("Transport", names)
        self.assertIn("Other", names)

    def test_add_custom_category(self):
        cat = db.add_category(self.db_path, "Child Support", display_order=995)
        cats = db.get_all_categories(self.db_path)
        self.assertIn("Child Support", [c.name for c in cats])

    def test_duplicate_category_raises(self):
        with self.assertRaises(ValueError):
            db.add_category(self.db_path, "Groceries")


# ── Engine lifecycle ───────────────────────────────────────────────────────────

class TestEngineLifecycle(unittest.TestCase):

    def test_requires_start(self):
        path = Path(tempfile.mktemp(suffix=".db"))
        engine = RuleEngine(path)
        with self.assertRaises(RuntimeError):
            engine.categorise("TESCO")

    def test_start_is_idempotent(self):
        engine, _ = make_engine()
        engine.start()   # calling start() a second time should not crash
        result = engine.categorise("TESCO")
        self.assertEqual(result.category, "Groceries")

    def test_rule_count_positive(self):
        engine, _ = make_engine()
        self.assertGreater(engine.rule_count, 0)


# ── Real-world descriptions from the April 2026 statement ─────────────────────

class TestRealWorldDescriptions(unittest.TestCase):
    """
    Uses the actual transaction descriptions extracted from the POC PDF run
    to ensure the engine correctly classifies live data.
    """

    @classmethod
    def setUpClass(cls):
        cls.engine, cls.db_path = make_engine()
        # Add a user rule for the child-support payment present in the statement
        db.add_user_rule(cls.db_path, "DONNA HARTNETT", "Child Support")
        cls.engine.reload_rules()

    CASES: list[tuple[str, str]] = [
        ("Contactless Payment – CAF LOCAL PL2 PETERBOROUGH",        "Coffee"),
        ("UBR* PENDING.UBER.COM LONDON – GOOGLE ****8791",          "Transport"),
        ("Contactless Payment – GREGGS PLC HIGH HOLBORN",           "Coffee"),
        ("Payment to DONNA HARTNETT",                               "Child Support"),
        ("Bank credit AMAZON WEB SERVICE – J177 00450543",          "Income"),
        ("Contactless Payment – CAF LOCAL PL2 PETERBOROUGH",        "Coffee"),
        ("UBER *TRIP HELP.UBER.COM – GOOGLE ****8791",              "Transport"),
        ("Contactless Payment – SumUp *YUM YUM ORIENT H",           "Other"),
        ("Contactless Payment – CAF LOCAL PL2 PETERBOROUGH",        "Coffee"),
        ("DELIVEROO LONDON – Effective Date 29 Mar 2026",           "Takeaways"),
        ("Transfer to Sprive Limited",                              "Savings"),
        ("Payment to LLOYDS BANK – Effective Date 31 Ma",           "Other"),
        ("Direct debit AO-OPTICALSERVICES",                         "Medical"),
        ("Standing order JENNA LYNNS",                              "Other"),
        ("Direct debit VM PERSONAL LOANS",                          "Internet"),
        ("Google Play Apps London",                                 "Subscriptions"),
        ("UBER *ONE MEMBERSHIP UBER.COM/BI",                        "Subscriptions"),
        ("Contactless Payment – SAINSBURYS LONDON LONDON",          "Groceries"),
        ("Contactless Payment – LNE RAILWAY PBO STN PTR",           "Transport"),
        ("Contactless Payment – STAGECOACH SERVICES STO",           "Transport"),
        ("Contactless Payment – Zettle_*BAXTERSTOREY LI",           "Other"),
        ("Contactless Payment – SAINSBURYS S/MKTS LONDON",          "Groceries"),
        ("Bank credit J Lock – JAMES THERAPY",                      "Other"),
        ("Bank credit MRS N LOCK – FOOD",                           "Other"),
        ("Contactless Payment – CONDUCTOR EC4 LONDON",              "Other"),
        ("Contactless Payment – BLACK TAXI* BLACKTAXI H",           "Transport"),
        ("Contactless Payment – PASTY SHOP - 12431281",             "Other"),
        ("Contactless Payment – TESCO STORES 6111 PETER",           "Groceries"),
        ("Bank credit J Lock – Jamed",                              "Other"),
        ("DELIVEROO LONDON",                                        "Takeaways"),
        ("LinkedIn*P3005718840 LINKEDIN.COM – (Recurring)",         "Subscriptions"),
        ("DOMINO S PIZZA MILTON KEYNES – GOOGLE ****8791",          "Takeaways"),
        ("FiverrEU Limassol",                                       "Shopping"),
    ]

    def test_all_real_world_descriptions(self):
        failures = []
        for desc, expected in self.CASES:
            result = self.engine.categorise(desc)
            if result.category != expected:
                failures.append(
                    f"\n  '{desc}'\n"
                    f"    expected: {expected}\n"
                    f"    got:      {result.category}"
                )
        if failures:
            self.fail("Real-world categorisation failures:" + "".join(failures))


# ── Entry point ────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    unittest.main(verbosity=2)
