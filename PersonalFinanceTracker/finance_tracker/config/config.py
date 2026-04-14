import os
from pathlib import Path

class Config:
    """Configuration settings for the Finance Tracker application"""
    
    # Database settings
    DATABASE_NAME = "finance_tracker.db"
    DATABASE_PATH = Path(__file__).parent.parent / DATABASE_NAME
    
    # Application settings
    APP_TITLE = "Personal Finance Tracker"
    APP_GEOMETRY = "900x700"
    
    # Currency settings
    CURRENCY_SYMBOL = "£"
    CURRENCY_FORMAT = "{symbol}{amount:.2f}"
    
    # Date format
    DATE_FORMAT = "%Y-%m-%d"
    DISPLAY_DATE_FORMAT = "%d/%m/%Y"
    
    # Export settings
    EXPORT_FORMATS = ["CSV", "Excel"]
    DEFAULT_EXPORT_PATH = Path.home() / "Downloads"
    
    # Validation settings
    MAX_AMOUNT = 999999.99
    MIN_AMOUNT = 0.01
    MAX_DESCRIPTION_LENGTH = 200
    
    # Category definitions with colors
    CATEGORY_COLORS = {
        'Salary': '#c8e6c9',
        'Bonus': '#c8e6c9',
        'Investment': '#c8e6c9',
        'Freelance': '#c8e6c9',
        'Other Income': '#c8e6c9',
        'Groceries': '#ffe0b2',
        'Rent/Mortgage': '#bbdefb',
        'Utilities': '#bbdefb',
        'Transport': '#e1bee7',
        'Entertainment': '#e1bee7',
        'Healthcare': '#ffcdd2',
        'Shopping': '#f8bbd0',
        'Other Expense': '#f5f5f5'
    }

    INCOME_CATEGORIES = [
        "Salary",
        "Bonus",
        "Investment",
        "Freelance",
        "Other Income"
    ]

    EXPENSE_CATEGORIES = [
        "Groceries",
        "Rent/Mortgage",
        "Utilities",
        "Transport",
        "Entertainment",
        "Healthcare",
        "Shopping",
        "Other Expense"
    ]
    
    @classmethod
    def get_database_path(cls):
        """Get the full database path"""
        return str(cls.DATABASE_PATH)
    
    @classmethod
    def format_currency(cls, amount):
        """Format amount as currency"""
        return cls.CURRENCY_FORMAT.format(symbol=cls.CURRENCY_SYMBOL, amount=amount)