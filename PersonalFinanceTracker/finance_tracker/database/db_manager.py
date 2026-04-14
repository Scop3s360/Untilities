import sqlite3
import logging
from datetime import datetime
from pathlib import Path
from config.config import Config

class DatabaseManager:
    def __init__(self):
        self.db_path = Config.get_database_path()
        self.conn = None
        self.cursor = None
        self.setup_logging()
        self.setup_database()
    
    def setup_logging(self):
        """Setup logging for database operations"""
        logging.basicConfig(level=logging.INFO)
        self.logger = logging.getLogger(__name__)

    def update_transaction(self, reference, new_amount=None, new_category=None, new_description=None):
        try:
            updates = []
            values = []
            
            if new_amount is not None:
                updates.append("amount = ?")
                values.append(new_amount)
            if new_category is not None:
                updates.append("category = ?")
                values.append(new_category)
            if new_description is not None:
                updates.append("description = ?")
                values.append(new_description)
                
            if not updates:
                return False
                
            values.append(reference)
            query = f"UPDATE transactions SET {', '.join(updates)} WHERE reference = ?"
            
            self.cursor.execute(query, values)
            self.conn.commit()
            return True
            
        except sqlite3.Error as e:
            self.logger.error(f"Error updating transaction: {e}")
            return False

    def delete_transaction(self, reference):
        try:
            self.cursor.execute("DELETE FROM transactions WHERE reference = ?", (reference,))
            self.conn.commit()
            return True
        except sqlite3.Error as e:
            self.logger.error(f"Error deleting transaction: {e}")
            return False


    def setup_database(self):
        """Create database and tables if they don't exist"""
        try:
            # Ensure database directory exists
            db_dir = Path(self.db_path).parent
            db_dir.mkdir(parents=True, exist_ok=True)
            
            self.conn = sqlite3.connect(self.db_path)
            self.cursor = self.conn.cursor()
            
            # Create transactions table
            self.cursor.execute('''
                CREATE TABLE IF NOT EXISTS transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    reference TEXT UNIQUE NOT NULL,
                    date TEXT NOT NULL,
                    amount REAL NOT NULL,
                    category TEXT NOT NULL,
                    description TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            ''')
            self.conn.commit()
            self.logger.info(f"Database initialized at {self.db_path}")
        except sqlite3.Error as e:
            self.logger.error(f"Database setup error: {e}")
            raise

    def generate_reference(self, category):
        """Generate unique reference number based on category"""
        try:
            # Convert category to uppercase and take first 4 letters
            cat_prefix = category.upper()[:4]
            current_date = datetime.now().strftime("%Y%m%d")
            
            # Get the last reference for this category and date
            self.cursor.execute("""
                SELECT MAX(reference) 
                FROM transactions 
                WHERE reference LIKE ? AND reference LIKE ?
            """, (f"{cat_prefix}-%", f"%-{current_date}-%"))
            
            last_ref = self.cursor.fetchone()[0]
            
            if last_ref:
                # Extract the number part and increment
                last_num = int(last_ref.split('-')[-1])
                new_num = last_num + 1
            else:
                new_num = 1
                
            # Format: CATG-YYYYMMDD-XXX
            return f"{cat_prefix}-{current_date}-{new_num:03d}"
        except sqlite3.Error as e:
            self.logger.error(f"Error generating reference: {e}")
            return None

    def add_transaction(self, amount, category, description=""):
        """Add a new transaction"""
        try:
            date = datetime.now().strftime("%Y-%m-%d")
            reference = self.generate_reference(category)
            
            if not reference:
                return False
                
            self.cursor.execute('''
                INSERT INTO transactions (reference, date, amount, category, description)
                VALUES (?, ?, ?, ?, ?)
            ''', (reference, date, amount, category, description))
            self.conn.commit()
            return True
        except sqlite3.Error as e:
            self.logger.error(f"Error adding transaction: {e}")
            return False

    def get_all_transactions(self):
        """Retrieve all transactions"""
        try:
            self.cursor.execute("SELECT * FROM transactions ORDER BY date DESC")
            return self.cursor.fetchall()
        except sqlite3.Error as e:
            self.logger.error(f"Error retrieving transactions: {e}")
            return []

    def get_transactions_by_date(self, date_from, date_to):
        """Retrieve transactions within a date range"""
        try:
            self.cursor.execute("""
                SELECT * FROM transactions 
                WHERE date BETWEEN ? AND ?
                ORDER BY date DESC
            """, (date_from.strftime('%Y-%m-%d'), date_to.strftime('%Y-%m-%d')))
            return self.cursor.fetchall()
        except sqlite3.Error as e:
            self.logger.error(f"Error retrieving transactions by date: {e}")
            return []

    def __del__(self):
        """Close database connection when object is destroyed"""
        if self.conn:
            self.conn.close()
    def export_to_csv(self, filepath, date_from=None, date_to=None):
        """Export transactions to CSV file"""
        try:
            import csv
            
            if date_from and date_to:
                transactions = self.get_transactions_by_date(date_from, date_to)
            else:
                transactions = self.get_all_transactions()
            
            with open(filepath, 'w', newline='', encoding='utf-8') as csvfile:
                writer = csv.writer(csvfile)
                writer.writerow(['Reference', 'Date', 'Amount', 'Category', 'Description'])
                
                for trans in transactions:
                    writer.writerow([trans[1], trans[2], trans[3], trans[4], trans[5]])
            
            self.logger.info(f"Exported {len(transactions)} transactions to {filepath}")
            return True
            
        except Exception as e:
            self.logger.error(f"Error exporting to CSV: {e}")
            return False
    
    def export_to_excel(self, filepath, date_from=None, date_to=None):
        """Export transactions to Excel file"""
        try:
            import pandas as pd
            
            if date_from and date_to:
                transactions = self.get_transactions_by_date(date_from, date_to)
            else:
                transactions = self.get_all_transactions()
            
            # Convert to DataFrame
            df = pd.DataFrame(transactions, columns=['ID', 'Reference', 'Date', 'Amount', 'Category', 'Description', 'Created_At'])
            df = df.drop('ID', axis=1)  # Remove ID column for export
            df = df.drop('Created_At', axis=1)  # Remove Created_At column for export
            
            # Save to Excel
            df.to_excel(filepath, index=False, sheet_name='Transactions')
            
            self.logger.info(f"Exported {len(transactions)} transactions to {filepath}")
            return True
            
        except Exception as e:
            self.logger.error(f"Error exporting to Excel: {e}")
            return False
    
    def get_category_summary(self, date_from=None, date_to=None):
        """Get summary of transactions by category"""
        try:
            if date_from and date_to:
                self.cursor.execute("""
                    SELECT category, SUM(amount) as total, COUNT(*) as count
                    FROM transactions 
                    WHERE date BETWEEN ? AND ?
                    GROUP BY category
                    ORDER BY total DESC
                """, (date_from.strftime('%Y-%m-%d'), date_to.strftime('%Y-%m-%d')))
            else:
                self.cursor.execute("""
                    SELECT category, SUM(amount) as total, COUNT(*) as count
                    FROM transactions 
                    GROUP BY category
                    ORDER BY total DESC
                """)
            
            return self.cursor.fetchall()
            
        except sqlite3.Error as e:
            self.logger.error(f"Error getting category summary: {e}")
            return []