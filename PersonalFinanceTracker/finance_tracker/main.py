import sys
import logging
import argparse
from gui.app import FinanceTrackerApp
from gui.modern_app import ModernFinanceTrackerApp
from database.db_manager import DatabaseManager

def main():
    """Main application entry point with UI selection"""
    parser = argparse.ArgumentParser(description='Personal Finance Tracker')
    parser.add_argument('--ui', choices=['classic', 'modern'], default='modern',
                       help='Choose UI style (default: modern)')
    parser.add_argument('--debug', action='store_true',
                       help='Enable debug logging')
    
    args = parser.parse_args()
    
    try:
        # Setup logging
        log_level = logging.DEBUG if args.debug else logging.INFO
        logging.basicConfig(
            level=log_level,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
        
        # Initialize database
        db = DatabaseManager()
        
        # Initialize and run GUI based on selection
        if args.ui == 'classic':
            app = FinanceTrackerApp(db)
        else:
            app = ModernFinanceTrackerApp(db)
            
        app.run()
        
    except Exception as e:
        logging.error(f"Application failed to start: {e}")
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()