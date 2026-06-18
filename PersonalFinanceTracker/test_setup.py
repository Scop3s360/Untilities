#!/usr/bin/env python3
"""
Test script to verify the Personal Finance Tracker setup
"""

import sys
import os
from pathlib import Path

def test_imports():
    """Test if all required modules can be imported"""
    print("Testing imports...")
    
    try:
        # Add the finance_tracker directory to Python path
        sys.path.insert(0, str(Path(__file__).parent / "finance_tracker"))
        
        from config.config import Config
        from database.db_manager import DatabaseManager
        from utils.helpers import validate_amount, format_currency
        
        print("✓ All core modules imported successfully")
        return True
    except ImportError as e:
        print(f"✗ Import error: {e}")
        return False

def test_config():
    """Test configuration settings"""
    print("Testing configuration...")
    
    try:
        from config.config import Config
        
        # Test basic config values
        assert Config.APP_TITLE == "Personal Finance Tracker"
        assert Config.CURRENCY_SYMBOL == "£"
        assert len(Config.INCOME_CATEGORIES) > 0
        assert len(Config.EXPENSE_CATEGORIES) > 0
        
        # Test currency formatting
        formatted = Config.format_currency(123.45)
        assert "£" in formatted
        assert "123.45" in formatted
        
        print("✓ Configuration tests passed")
        return True
    except Exception as e:
        print(f"✗ Configuration test failed: {e}")
        return False

def test_database():
    """Test database functionality"""
    print("Testing database...")
    
    try:
        from database.db_manager import DatabaseManager
        
        # Create test database
        db = DatabaseManager()
        
        # Test adding a transaction
        success = db.add_transaction(100.0, "Salary", "Test transaction")
        assert success, "Failed to add transaction"
        
        # Test retrieving transactions
        transactions = db.get_all_transactions()
        assert len(transactions) > 0, "No transactions found"
        
        print("✓ Database tests passed")
        return True
    except Exception as e:
        print(f"✗ Database test failed: {e}")
        return False

def test_utilities():
    """Test utility functions"""
    print("Testing utilities...")
    
    try:
        from utils.helpers import validate_amount, validate_description, sanitize_input
        
        # Test amount validation
        assert validate_amount("100.50") == True
        assert validate_amount("abc") == False
        assert validate_amount("-10") == False
        
        # Test description validation
        assert validate_description("Valid description") == True
        assert validate_description("x" * 300) == False
        
        # Test input sanitization
        clean = sanitize_input("Test <script>")
        assert "<script>" not in clean
        
        print("✓ Utility tests passed")
        return True
    except Exception as e:
        print(f"✗ Utility test failed: {e}")
        return False

def main():
    """Run all tests"""
    print("Personal Finance Tracker - Setup Verification")
    print("=" * 50)
    
    tests = [
        test_imports,
        test_config,
        test_database,
        test_utilities
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        if test():
            passed += 1
        print()
    
    print("=" * 50)
    print(f"Tests passed: {passed}/{total}")
    
    if passed == total:
        print("✓ All tests passed! The application is ready to use.")
        print("\nTo start the application, run:")
        print("  python finance_tracker/main.py")
    else:
        print("✗ Some tests failed. Please check the setup.")
        sys.exit(1)

if __name__ == "__main__":
    main()