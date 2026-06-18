import tkinter as tk
from datetime import datetime, timedelta
from tkcalendar import DateEntry
from tkinter import ttk
from tkinter import messagebox, filedialog
from config.config import Config
from utils.helpers import validate_amount, validate_description, sanitize_input, generate_filename

class FinanceTrackerApp:
    def __init__(self, db_manager):
        self.db = db_manager
        self.root = tk.Tk()
        self.root.title(Config.APP_TITLE)
        self.root.geometry(Config.APP_GEOMETRY)
        self.setup_gui()

    def setup_gui(self):
        #Add filter frame before input frame
        self.filter_frame = ttk.Frame(self.root, padding="10")
        self.filter_frame.pack(fill=tk.X)

        # Create main frames
        self.input_frame = ttk.Frame(self.root, padding="10")
        self.input_frame.pack(fill=tk.X)

        self.display_frame = ttk.Frame(self.root, padding="10")
        self.display_frame.pack(fill=tk.BOTH, expand=True)

        #Add summery frame
        self.summary_frame = ttk.Frame(self.root, padding="10")
        self.summary_frame.pack(fill=tk.X)

        # Setup input fields
        self.setup_filters()
        self.setup_input_fields()
        self.setup_transaction_display()
        self.setup_summary()

    def setup_summary(self):
        # Create summary labels
        self.total_income_var = tk.StringVar(value=f"Total Income: {Config.format_currency(0)}")
        self.total_expense_var = tk.StringVar(value=f"Total Expenses: {Config.format_currency(0)}")
        self.net_total_var = tk.StringVar(value=f"Net Total: {Config.format_currency(0)}")

        ttk.Label(self.summary_frame, textvariable=self.total_income_var).pack(side=tk.LEFT, padx=10)
        ttk.Label(self.summary_frame, textvariable=self.total_expense_var).pack(side=tk.LEFT, padx=10)
        ttk.Label(self.summary_frame, textvariable=self.net_total_var).pack(side=tk.LEFT, padx=10)

    def set_date_filter(self, period):
        today = datetime.now()
        if period == 'today':
            self.date_from.set_date(today)
            self.date_to.set_date(today)
        elif period == 'week':
            self.date_from.set_date(today - timedelta(days=today.weekday()))
            self.date_to.set_date(today)
        elif period == 'month':
            self.date_from.set_date(today.replace(day=1))
            self.date_to.set_date(today)
        elif period == 'all':
            self.date_from.set_date(today - timedelta(days=3650))  # Approximately 10 years
            self.date_to.set_date(today)

    def apply_filters(self):
        self.refresh_transactions()

    def update_summary(self, transactions):
        total_income = sum(float(t[2]) for t in transactions if float(t[2]) > 0)
        total_expense = sum(float(t[2]) for t in transactions if float(t[2]) < 0)
        net_total = total_income + total_expense

        self.total_income_var.set(f"Total Income: {Config.format_currency(total_income)}")
        self.total_expense_var.set(f"Total Expenses: {Config.format_currency(abs(total_expense))}")
        self.net_total_var.set(f"Net Total: {Config.format_currency(net_total)}")


    def setup_filters(self):
        # Date filter section
        filter_label = ttk.Label(self.filter_frame, text="Date Filter:")
        filter_label.pack(side=tk.LEFT, padx=5)

        # From date
        ttk.Label(self.filter_frame, text="From:").pack(side=tk.LEFT, padx=5)
        self.date_from = DateEntry(self.filter_frame, width=12, background='darkblue',
                                foreground='white', borderwidth=2)
        self.date_from.pack(side=tk.LEFT, padx=5)

        # To date
        ttk.Label(self.filter_frame, text="To:").pack(side=tk.LEFT, padx=5)
        self.date_to = DateEntry(self.filter_frame, width=12, background='darkblue',
                            foreground='white', borderwidth=2)
        self.date_to.pack(side=tk.LEFT, padx=5)

        # Quick filter buttons
        ttk.Button(self.filter_frame, text="Today", 
                command=lambda: self.set_date_filter('today')).pack(side=tk.LEFT, padx=2)
        ttk.Button(self.filter_frame, text="This Week", 
                command=lambda: self.set_date_filter('week')).pack(side=tk.LEFT, padx=2)
        ttk.Button(self.filter_frame, text="This Month", 
                command=lambda: self.set_date_filter('month')).pack(side=tk.LEFT, padx=2)
        ttk.Button(self.filter_frame, text="All Time", 
                command=lambda: self.set_date_filter('all')).pack(side=tk.LEFT, padx=2)
        
        # Apply filter button
        ttk.Button(self.filter_frame, text="Apply Filter", 
                command=self.apply_filters).pack(side=tk.LEFT, padx=5)

    def setup_input_fields(self):
        # Amount input
        ttk.Label(self.input_frame, text="Amount:").pack(side=tk.LEFT, padx=5)
        self.amount_var = tk.StringVar()
        ttk.Entry(self.input_frame, textvariable=self.amount_var).pack(side=tk.LEFT, padx=5)

        # Transaction Type (Income/Expense)
        ttk.Label(self.input_frame, text="Type:").pack(side=tk.LEFT, padx=5)
        self.type_var = tk.StringVar()
        type_combo = ttk.Combobox(self.input_frame, textvariable=self.type_var, 
                                values=["Income", "Expense"])
        type_combo.pack(side=tk.LEFT, padx=5)
        type_combo.bind('<<ComboboxSelected>>', self.update_categories)

        # Category input
        ttk.Label(self.input_frame, text="Category:").pack(side=tk.LEFT, padx=5)
        self.category_var = tk.StringVar()
        self.category_combo = ttk.Combobox(self.input_frame, textvariable=self.category_var)
        self.category_combo.pack(side=tk.LEFT, padx=5)

        # Description input
        ttk.Label(self.input_frame, text="Description:").pack(side=tk.LEFT, padx=5)
        self.description_var = tk.StringVar()
        ttk.Entry(self.input_frame, textvariable=self.description_var).pack(side=tk.LEFT, padx=5)

        # Add button
        ttk.Button(self.input_frame, text="Add Transaction", 
                command=self.add_transaction).pack(side=tk.LEFT, padx=5)

    def update_categories(self, event=None):
        """Update category dropdown based on transaction type"""
        if self.type_var.get() == "Income":
            self.category_combo['values'] = Config.INCOME_CATEGORIES
        else:
            self.category_combo['values'] = Config.EXPENSE_CATEGORIES
        self.category_var.set('')  # Clear current selection

    def setup_transaction_display(self):
        # Create treeview for transactions
        columns = ("Reference", "Date", "Amount", "Category", "Description")
        self.tree = ttk.Treeview(self.display_frame, columns=columns, show='headings')
        
        # Set column headings
        for col in columns:
            self.tree.heading(col, text=col)
            self.tree.column(col, width=100)

        # Configure tags for all categories
        for category, color in Config.CATEGORY_COLORS.items():
            self.tree.tag_configure(category, background=color)

        self.tree.pack(fill=tk.BOTH, expand=True)
        
        # Add scrollbar
        scrollbar = ttk.Scrollbar(self.display_frame, orient=tk.VERTICAL, command=self.tree.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.tree.configure(yscrollcommand=scrollbar.set)

        # Add button frame
        self.button_frame = ttk.Frame(self.display_frame)
        self.button_frame.pack(fill=tk.X, pady=5)

        # Add Amend and Delete buttons
        ttk.Button(self.button_frame, text="Amend Selected", 
                command=self.amend_selected).pack(side=tk.LEFT, padx=5)
        ttk.Button(self.button_frame, text="Delete Selected", 
                command=self.delete_selected).pack(side=tk.LEFT, padx=5)
        
        # Add Export buttons
        ttk.Button(self.button_frame, text="Export CSV", 
                command=self.export_csv).pack(side=tk.LEFT, padx=5)
        ttk.Button(self.button_frame, text="Export Excel", 
                command=self.export_excel).pack(side=tk.LEFT, padx=5)

    def add_transaction(self):
        # Validate inputs
        amount_str = self.amount_var.get().strip()
        category = self.category_var.get().strip()
        description = sanitize_input(self.description_var.get())
        transaction_type = self.type_var.get()
        
        # Validation checks
        if not amount_str:
            messagebox.showerror("Error", "Please enter an amount")
            return
            
        if not validate_amount(amount_str):
            messagebox.showerror("Error", f"Please enter a valid amount between {Config.format_currency(Config.MIN_AMOUNT)} and {Config.format_currency(Config.MAX_AMOUNT)}")
            return
            
        if not transaction_type:
            messagebox.showerror("Error", "Please select transaction type (Income/Expense)")
            return
            
        if not category:
            messagebox.showerror("Error", "Please select a category")
            return
            
        if not validate_description(description):
            messagebox.showerror("Error", f"Description must be less than {Config.MAX_DESCRIPTION_LENGTH} characters")
            return
        
        try:
            amount = float(amount_str)
            # Make amount negative for expenses
            if transaction_type == "Expense":
                amount = -abs(amount)
            else:
                amount = abs(amount)

            if self.db.add_transaction(amount, category, description):
                self.refresh_transactions()
                self.clear_inputs()
                messagebox.showinfo("Success", "Transaction added successfully")
            else:
                messagebox.showerror("Error", "Failed to add transaction")
                
        except ValueError:
            messagebox.showerror("Error", "Please enter a valid amount")
    
    def clear_inputs(self):
        """Clear all input fields"""
        self.amount_var.set("")
        self.category_var.set("")
        self.description_var.set("")
        self.type_var.set("")

    def amend_selected(self):
        selected_item = self.tree.selection()
        if not selected_item:
            messagebox.showwarning("Warning", "Please select a transaction to amend")
            return
            
        reference = self.tree.item(selected_item)['values'][0]
        
        self.edit_window = tk.Toplevel(self.root)
        self.edit_window.title("Amend Transaction")
        
        ttk.Label(self.edit_window, text="Amount:").pack()
        amount_var = tk.StringVar()
        ttk.Entry(self.edit_window, textvariable=amount_var).pack()
        
        ttk.Label(self.edit_window, text="Category:").pack()
        category_var = tk.StringVar()
        ttk.Entry(self.edit_window, textvariable=category_var).pack()
        
        ttk.Label(self.edit_window, text="Description:").pack()
        description_var = tk.StringVar()
        ttk.Entry(self.edit_window, textvariable=description_var).pack()
        
        ttk.Button(self.edit_window, text="Save", 
                command=lambda: self.save_amendments(reference, amount_var.get(), 
                                                    category_var.get(), description_var.get())).pack()

    def save_amendments(self, reference, amount, category, description):
        try:
            if amount: amount = float(amount)
            if self.db.update_transaction(reference, amount, category, description):
                self.refresh_transactions()
                self.edit_window.destroy()
            else:
                messagebox.showerror("Error", "Failed to update transaction")
        except ValueError:
            messagebox.showerror("Error", "Please enter a valid amount")

    def delete_selected(self):
        selected_item = self.tree.selection()
        if not selected_item:
            messagebox.showwarning("Warning", "Please select a transaction to delete")
            return
            
        reference = self.tree.item(selected_item)['values'][0]
        
        if messagebox.askyesno("Confirm Delete", "Are you sure you want to delete this transaction?"):
            if self.db.delete_transaction(reference):
                self.refresh_transactions()
            else:
                messagebox.showerror("Error", "Failed to delete transaction")

    def refresh_transactions(self):
        # Clear current items
        for item in self.tree.get_children():
            self.tree.delete(item)
                
        # Get date range
        date_from = self.date_from.get_date()
        date_to = self.date_to.get_date()
        
        # Load transactions from database with date filter
        transactions = self.db.get_transactions_by_date(date_from, date_to)
        
        for trans in transactions:
            values = trans[1:6]
            item_id = self.tree.insert("", tk.END, values=values)
            
            # Get category and set color
            category = values[3]
            if category in Config.CATEGORY_COLORS:
                self.tree.item(item_id, tags=(category,))
        
        # Update summary after refreshing transactions
        self.update_summary(transactions)

    def run(self):
        self.refresh_transactions()
        self.root.mainloop()
    
    def export_csv(self):
        """Export transactions to CSV"""
        try:
            date_from = self.date_from.get_date()
            date_to = self.date_to.get_date()
            
            filename = generate_filename("transactions", "csv", date_from, date_to)
            filepath = filedialog.asksaveasfilename(
                defaultextension=".csv",
                filetypes=[("CSV files", "*.csv")],
                initialname=filename,
                initialdir=str(Config.DEFAULT_EXPORT_PATH)
            )
            
            if filepath:
                if self.db.export_to_csv(filepath, date_from, date_to):
                    messagebox.showinfo("Success", f"Transactions exported to {filepath}")
                else:
                    messagebox.showerror("Error", "Failed to export transactions")
                    
        except Exception as e:
            messagebox.showerror("Error", f"Export failed: {str(e)}")
    
    def export_excel(self):
        """Export transactions to Excel"""
        try:
            date_from = self.date_from.get_date()
            date_to = self.date_to.get_date()
            
            filename = generate_filename("transactions", "xlsx", date_from, date_to)
            filepath = filedialog.asksaveasfilename(
                defaultextension=".xlsx",
                filetypes=[("Excel files", "*.xlsx")],
                initialname=filename,
                initialdir=str(Config.DEFAULT_EXPORT_PATH)
            )
            
            if filepath:
                if self.db.export_to_excel(filepath, date_from, date_to):
                    messagebox.showinfo("Success", f"Transactions exported to {filepath}")
                else:
                    messagebox.showerror("Error", "Failed to export transactions")
                    
        except Exception as e:
            messagebox.showerror("Error", f"Export failed: {str(e)}")