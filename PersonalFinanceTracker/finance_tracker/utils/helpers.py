from datetime import datetime
from config.config import Config
import re

def format_currency(amount):
    """Format amount as currency using config settings"""
    return Config.format_currency(amount)

def validate_amount(amount_str):
    """Validate amount input with proper range checking"""
    try:
        amount = float(amount_str)
        return Config.MIN_AMOUNT <= amount <= Config.MAX_AMOUNT
    except ValueError:
        return False

def validate_description(description):
    """Validate description length"""
    return len(description) <= Config.MAX_DESCRIPTION_LENGTH

def sanitize_input(text):
    """Sanitize user input to prevent issues"""
    if not text:
        return ""
    # Remove potentially harmful characters
    return re.sub(r'[<>"\']', '', str(text).strip())

def get_current_date():
    """Get current date in standard format"""
    return datetime.now().strftime(Config.DATE_FORMAT)

def format_date_for_display(date_str):
    """Format date string for display"""
    try:
        date_obj = datetime.strptime(date_str, Config.DATE_FORMAT)
        return date_obj.strftime(Config.DISPLAY_DATE_FORMAT)
    except ValueError:
        return date_str

def calculate_percentage(part, total):
    """Calculate percentage with safe division"""
    if total == 0:
        return 0
    return (part / total) * 100

def generate_filename(base_name, extension, date_from=None, date_to=None):
    """Generate filename for exports"""
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    
    if date_from and date_to:
        date_range = f"_{date_from.strftime('%Y%m%d')}_to_{date_to.strftime('%Y%m%d')}"
    else:
        date_range = ""
    
    return f"{base_name}{date_range}_{timestamp}.{extension}"