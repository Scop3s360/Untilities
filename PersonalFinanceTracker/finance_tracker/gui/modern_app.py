import tkinter as tk
from tkinter import ttk, messagebox, filedialog
from datetime import datetime, timedelta
from tkcalendar import DateEntry
import matplotlib.pyplot as plt
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure
from config.config import Config
from utils.helpers import validate_amount, validate_description, sanitize_input, generate_filename
from gui.themes import theme_manager

class ModernFinanceTrackerApp:
    def __init__(self, db_manager):
        self.db = db_manager
        self.root = tk.Tk()
        self.setup_window()
        self.setup_styles()
        self.setup_gui()
        
    def setup_window(self):
        """Configure main window"""
        self.root.title(Config.APP_TITLE)
        self.root.geometry("1200x800")
        self.root.minsize(1000, 600)
        
        # Center window on screen
        self.root.update_idletasks()
        x = (self.root.winfo_screenwidth() // 2) - (1200 // 2)
        y = (self.root.winfo_screenheight() // 2) - (800 // 2)
        self.root.geometry(f"1200x800+{x}+{y}")
        
        # Configure window icon and styling
        self.root.configure(bg='#f0f0f0')
        
    def setup_styles(self):
        """Configure modern styling using theme manager"""
        self.style = ttk.Style()
        self.style.theme_use('clam')
        
        # Get colors from theme manager
        self.colors = theme_manager.get_all_colors()
        
        # Apply theme styles
        theme_manager.apply_ttk_styles(self.style)
        
    def setup_gui(self):
        """Setup the main GUI layout"""
        # Create main container
        main_container = ttk.Frame(self.root, padding="20")
        main_container.pack(fill=tk.BOTH, expand=True)
        
        # Title
        title_label = ttk.Label(main_container, text="💰 Personal Finance Tracker", 
                               style='Title.TLabel')
        title_label.pack(pady=(0, 20))
        
        # Create notebook for tabs
        self.notebook = ttk.Notebook(main_container)
        self.notebook.pack(fill=tk.BOTH, expand=True)
        
        # Setup tabs
        self.setup_dashboard_tab()
        self.setup_transactions_tab()
        self.setup_analytics_tab()
        self.setup_settings_tab()
        
    def setup_dashboard_tab(self):
        """Setup dashboard tab with overview"""
        dashboard_frame = ttk.Frame(self.notebook, padding="20")
        self.notebook.add(dashboard_frame, text="📊 Dashboard")
        
        # Quick stats cards
        stats_frame = ttk.Frame(dashboard_frame)
        stats_frame.pack(fill=tk.X, pady=(0, 20))
        
        self.setup_stats_cards(stats_frame)
        
        # Quick add transaction
        quick_add_frame = ttk.LabelFrame(dashboard_frame, text="Quick Add Transaction", padding="15")
        quick_add_frame.pack(fill=tk.X, pady=(0, 20))
        
        self.setup_quick_add_form(quick_add_frame)
        
        # Recent transactions
        recent_frame = ttk.LabelFrame(dashboard_frame, text="Recent Transactions", padding="15")
        recent_frame.pack(fill=tk.BOTH, expand=True)
        
        self.setup_recent_transactions(recent_frame)
        
    def setup_stats_cards(self, parent):
        """Setup statistics cards"""
        # Create three cards for income, expenses, and balance
        cards_frame = ttk.Frame(parent)
        cards_frame.pack(fill=tk.X)
        
        # Income card
        income_card = ttk.Frame(cards_frame, style='Card.TFrame', padding="20")
        income_card.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(0, 10))
        
        ttk.Label(income_card, text="💚 Total Income", 
                 font=('Segoe UI', 12, 'bold'), foreground=self.colors['income']).pack()
        self.income_amount = ttk.Label(income_card, text=Config.format_currency(0),
                                      font=('Segoe UI', 18, 'bold'), foreground=self.colors['income'])
        self.income_amount.pack(pady=(5, 0))
        
        # Expenses card
        expense_card = ttk.Frame(cards_frame, style='Card.TFrame', padding="20")
        expense_card.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(5, 5))
        
        ttk.Label(expense_card, text="💸 Total Expenses",
                 font=('Segoe UI', 12, 'bold'), foreground=self.colors['expense']).pack()
        self.expense_amount = ttk.Label(expense_card, text=Config.format_currency(0),
                                       font=('Segoe UI', 18, 'bold'), foreground=self.colors['expense'])
        self.expense_amount.pack(pady=(5, 0))
        
        # Balance card
        balance_card = ttk.Frame(cards_frame, style='Card.TFrame', padding="20")
        balance_card.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(10, 0))
        
        ttk.Label(balance_card, text="💰 Net Balance",
                 font=('Segoe UI', 12, 'bold'), foreground=self.colors['primary']).pack()
        self.balance_amount = ttk.Label(balance_card, text=Config.format_currency(0),
                                       font=('Segoe UI', 18, 'bold'), foreground=self.colors['primary'])
        self.balance_amount.pack(pady=(5, 0))
        
    def setup_quick_add_form(self, parent):
        """Setup quick add transaction form"""
        form_frame = ttk.Frame(parent)
        form_frame.pack(fill=tk.X)
        
        # First row: Amount and Type
        row1 = ttk.Frame(form_frame)
        row1.pack(fill=tk.X, pady=(0, 10))
        
        ttk.Label(row1, text="Amount:", font=('Segoe UI', 10, 'bold')).pack(side=tk.LEFT, padx=(0, 5))
        self.quick_amount_var = tk.StringVar()
        amount_entry = ttk.Entry(row1, textvariable=self.quick_amount_var, width=15, font=('Segoe UI', 10))
        amount_entry.pack(side=tk.LEFT, padx=(0, 20))
        
        ttk.Label(row1, text="Type:", font=('Segoe UI', 10, 'bold')).pack(side=tk.LEFT, padx=(0, 5))
        self.quick_type_var = tk.StringVar()
        type_combo = ttk.Combobox(row1, textvariable=self.quick_type_var, 
                                 values=["Income", "Expense"], width=12, font=('Segoe UI', 10))
        type_combo.pack(side=tk.LEFT, padx=(0, 20))
        type_combo.bind('<<ComboboxSelected>>', self.update_quick_categories)
        
        # Second row: Category and Description
        row2 = ttk.Frame(form_frame)
        row2.pack(fill=tk.X, pady=(0, 15))
        
        ttk.Label(row2, text="Category:", font=('Segoe UI', 10, 'bold')).pack(side=tk.LEFT, padx=(0, 5))
        self.quick_category_var = tk.StringVar()
        self.quick_category_combo = ttk.Combobox(row2, textvariable=self.quick_category_var, width=15, font=('Segoe UI', 10))
        self.quick_category_combo.pack(side=tk.LEFT, padx=(0, 20))
        
        ttk.Label(row2, text="Description:", font=('Segoe UI', 10, 'bold')).pack(side=tk.LEFT, padx=(0, 5))
        self.quick_description_var = tk.StringVar()
        description_entry = ttk.Entry(row2, textvariable=self.quick_description_var, width=25, font=('Segoe UI', 10))
        description_entry.pack(side=tk.LEFT, padx=(0, 20))
        
        # Add button
        add_btn = ttk.Button(row2, text="➕ Add Transaction", command=self.quick_add_transaction,
                            style='Success.TButton')
        add_btn.pack(side=tk.RIGHT)
        
    def setup_recent_transactions(self, parent):
        """Setup recent transactions display"""
        # Create treeview for recent transactions
        columns = ("Date", "Type", "Amount", "Category", "Description")
        self.recent_tree = ttk.Treeview(parent, columns=columns, show='headings', height=8)
        
        # Configure columns
        self.recent_tree.heading("Date", text="Date")
        self.recent_tree.heading("Type", text="Type")
        self.recent_tree.heading("Amount", text="Amount")
        self.recent_tree.heading("Category", text="Category")
        self.recent_tree.heading("Description", text="Description")
        
        self.recent_tree.column("Date", width=100)
        self.recent_tree.column("Type", width=80)
        self.recent_tree.column("Amount", width=100)
        self.recent_tree.column("Category", width=120)
        self.recent_tree.column("Description", width=200)
        
        # Configure tags for income/expense
        self.recent_tree.tag_configure("income", background="#d4edda", foreground="#155724")
        self.recent_tree.tag_configure("expense", background="#f8d7da", foreground="#721c24")
        
        self.recent_tree.pack(fill=tk.BOTH, expand=True)
        
        # Add scrollbar
        recent_scrollbar = ttk.Scrollbar(parent, orient=tk.VERTICAL, command=self.recent_tree.yview)
        recent_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.recent_tree.configure(yscrollcommand=recent_scrollbar.set)
        
    def setup_transactions_tab(self):
        """Setup full transactions management tab"""
        transactions_frame = ttk.Frame(self.notebook, padding="20")
        self.notebook.add(transactions_frame, text="💳 Transactions")
        
        # Filters section
        filters_frame = ttk.LabelFrame(transactions_frame, text="Filters", padding="15")
        filters_frame.pack(fill=tk.X, pady=(0, 20))
        
        self.setup_transaction_filters(filters_frame)
        
        # Transactions table
        table_frame = ttk.LabelFrame(transactions_frame, text="All Transactions", padding="15")
        table_frame.pack(fill=tk.BOTH, expand=True)
        
        self.setup_transactions_table(table_frame)
        
    def setup_transaction_filters(self, parent):
        """Setup transaction filters"""
        filter_row1 = ttk.Frame(parent)
        filter_row1.pack(fill=tk.X, pady=(0, 10))
        
        # Date filters
        ttk.Label(filter_row1, text="From:", font=('Segoe UI', 10, 'bold')).pack(side=tk.LEFT, padx=(0, 5))
        self.date_from = DateEntry(filter_row1, width=12, background='darkblue',
                                  foreground='white', borderwidth=2, font=('Segoe UI', 9))
        self.date_from.pack(side=tk.LEFT, padx=(0, 15))
        
        ttk.Label(filter_row1, text="To:", font=('Segoe UI', 10, 'bold')).pack(side=tk.LEFT, padx=(0, 5))
        self.date_to = DateEntry(filter_row1, width=12, background='darkblue',
                                foreground='white', borderwidth=2, font=('Segoe UI', 9))
        self.date_to.pack(side=tk.LEFT, padx=(0, 20))
        
        # Quick filter buttons
        filter_row2 = ttk.Frame(parent)
        filter_row2.pack(fill=tk.X)
        
        quick_filters = [
            ("Today", 'today'),
            ("This Week", 'week'),
            ("This Month", 'month'),
            ("All Time", 'all')
        ]
        
        for text, period in quick_filters:
            btn = ttk.Button(filter_row2, text=text, 
                           command=lambda p=period: self.set_date_filter(p))
            btn.pack(side=tk.LEFT, padx=(0, 5))
        
        # Apply filter button
        apply_btn = ttk.Button(filter_row2, text="🔍 Apply Filter", 
                              command=self.apply_filters, style='Primary.TButton')
        apply_btn.pack(side=tk.LEFT, padx=(20, 0))
        
    def setup_transactions_table(self, parent):
        """Setup main transactions table"""
        # Create treeview
        columns = ("Reference", "Date", "Amount", "Category", "Description")
        self.transactions_tree = ttk.Treeview(parent, columns=columns, show='headings')
        
        # Configure columns
        for col in columns:
            self.transactions_tree.heading(col, text=col)
            
        self.transactions_tree.column("Reference", width=120)
        self.transactions_tree.column("Date", width=100)
        self.transactions_tree.column("Amount", width=100)
        self.transactions_tree.column("Category", width=120)
        self.transactions_tree.column("Description", width=200)
        
        # Configure category colors using theme
        category_colors = theme_manager.get_category_colors()
        for category, color in category_colors.items():
            self.transactions_tree.tag_configure(category, background=color)
        
        self.transactions_tree.pack(fill=tk.BOTH, expand=True, pady=(0, 10))
        
        # Add scrollbar
        trans_scrollbar = ttk.Scrollbar(parent, orient=tk.VERTICAL, command=self.transactions_tree.yview)
        trans_scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.transactions_tree.configure(yscrollcommand=trans_scrollbar.set)
        
        # Action buttons
        actions_frame = ttk.Frame(parent)
        actions_frame.pack(fill=tk.X)
        
        ttk.Button(actions_frame, text="✏️ Edit Selected", 
                  command=self.edit_transaction).pack(side=tk.LEFT, padx=(0, 5))
        ttk.Button(actions_frame, text="🗑️ Delete Selected", 
                  command=self.delete_transaction, style='Danger.TButton').pack(side=tk.LEFT, padx=(0, 20))
        
        ttk.Button(actions_frame, text="📄 Export CSV", 
                  command=self.export_csv).pack(side=tk.LEFT, padx=(0, 5))
        ttk.Button(actions_frame, text="📊 Export Excel", 
                  command=self.export_excel).pack(side=tk.LEFT)
        
    def setup_analytics_tab(self):
        """Setup analytics and charts tab"""
        analytics_frame = ttk.Frame(self.notebook, padding="20")
        self.notebook.add(analytics_frame, text="📈 Analytics")
        
        # Charts container
        charts_container = ttk.Frame(analytics_frame)
        charts_container.pack(fill=tk.BOTH, expand=True)
        
        # Category pie chart
        chart_frame1 = ttk.LabelFrame(charts_container, text="Expenses by Category", padding="10")
        chart_frame1.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=(0, 10))
        
        self.setup_pie_chart(chart_frame1)
        
        # Monthly trend chart
        chart_frame2 = ttk.LabelFrame(charts_container, text="Monthly Trend", padding="10")
        chart_frame2.pack(side=tk.RIGHT, fill=tk.BOTH, expand=True)
        
        self.setup_trend_chart(chart_frame2)
        
    def setup_pie_chart(self, parent):
        """Setup pie chart for category breakdown"""
        self.pie_figure = Figure(figsize=(6, 4), dpi=100)
        self.pie_canvas = FigureCanvasTkAgg(self.pie_figure, parent)
        self.pie_canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)
        
    def setup_trend_chart(self, parent):
        """Setup trend chart for monthly analysis"""
        self.trend_figure = Figure(figsize=(6, 4), dpi=100)
        self.trend_canvas = FigureCanvasTkAgg(self.trend_figure, parent)
        self.trend_canvas.get_tk_widget().pack(fill=tk.BOTH, expand=True)
        
    def setup_settings_tab(self):
        """Setup settings and preferences tab"""
        settings_frame = ttk.Frame(self.notebook, padding="20")
        self.notebook.add(settings_frame, text="⚙️ Settings")
        
        # Categories management
        categories_frame = ttk.LabelFrame(settings_frame, text="Manage Categories", padding="15")
        categories_frame.pack(fill=tk.X, pady=(0, 20))
        
        ttk.Label(categories_frame, text="Category management features coming soon...",
                 font=('Segoe UI', 10)).pack()
        
        # Export/Import settings
        export_frame = ttk.LabelFrame(settings_frame, text="Data Management", padding="15")
        export_frame.pack(fill=tk.X)
        
        ttk.Button(export_frame, text="📤 Backup Data", 
                  command=self.backup_data).pack(side=tk.LEFT, padx=(0, 10))
        ttk.Button(export_frame, text="📥 Restore Data", 
                  command=self.restore_data).pack(side=tk.LEFT)
        
    # Event handlers and utility methods
    def update_quick_categories(self, event=None):
        """Update category dropdown in quick add form"""
        if self.quick_type_var.get() == "Income":
            self.quick_category_combo['values'] = Config.INCOME_CATEGORIES
        else:
            self.quick_category_combo['values'] = Config.EXPENSE_CATEGORIES
        self.quick_category_var.set('')
        
    def quick_add_transaction(self):
        """Add transaction from quick form"""
        amount_str = self.quick_amount_var.get().strip()
        category = self.quick_category_var.get().strip()
        description = sanitize_input(self.quick_description_var.get())
        transaction_type = self.quick_type_var.get()
        
        if self.validate_and_add_transaction(amount_str, category, description, transaction_type):
            # Clear form
            self.quick_amount_var.set("")
            self.quick_category_var.set("")
            self.quick_description_var.set("")
            self.quick_type_var.set("")
            
            # Refresh displays
            self.refresh_dashboard()
            self.refresh_transactions()
            
    def validate_and_add_transaction(self, amount_str, category, description, transaction_type):
        """Validate and add transaction"""
        # Validation checks
        if not amount_str:
            messagebox.showerror("Error", "Please enter an amount")
            return False
            
        if not validate_amount(amount_str):
            messagebox.showerror("Error", f"Please enter a valid amount between {Config.format_currency(Config.MIN_AMOUNT)} and {Config.format_currency(Config.MAX_AMOUNT)}")
            return False
            
        if not transaction_type:
            messagebox.showerror("Error", "Please select transaction type (Income/Expense)")
            return False
            
        if not category:
            messagebox.showerror("Error", "Please select a category")
            return False
            
        if not validate_description(description):
            messagebox.showerror("Error", f"Description must be less than {Config.MAX_DESCRIPTION_LENGTH} characters")
            return False
        
        try:
            amount = float(amount_str)
            if transaction_type == "Expense":
                amount = -abs(amount)
            else:
                amount = abs(amount)

            if self.db.add_transaction(amount, category, description):
                messagebox.showinfo("Success", "Transaction added successfully")
                return True
            else:
                messagebox.showerror("Error", "Failed to add transaction")
                return False
                
        except ValueError:
            messagebox.showerror("Error", "Please enter a valid amount")
            return False
    
    def set_date_filter(self, period):
        """Set date filter based on period"""
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
            self.date_from.set_date(today - timedelta(days=3650))
            self.date_to.set_date(today)
        
        self.apply_filters()
    
    def apply_filters(self):
        """Apply date filters to transactions"""
        self.refresh_transactions()
        self.refresh_dashboard()
        
    def refresh_dashboard(self):
        """Refresh dashboard statistics and recent transactions"""
        # Get all transactions for stats
        all_transactions = self.db.get_all_transactions()
        
        total_income = sum(float(t[3]) for t in all_transactions if float(t[3]) > 0)
        total_expense = sum(float(t[3]) for t in all_transactions if float(t[3]) < 0)
        net_balance = total_income + total_expense
        
        # Update stats cards
        self.income_amount.config(text=Config.format_currency(total_income))
        self.expense_amount.config(text=Config.format_currency(abs(total_expense)))
        self.balance_amount.config(text=Config.format_currency(net_balance))
        
        # Update recent transactions (last 10)
        recent_transactions = all_transactions[:10]
        
        # Clear recent tree
        for item in self.recent_tree.get_children():
            self.recent_tree.delete(item)
        
        # Populate recent transactions
        for trans in recent_transactions:
            amount = float(trans[3])
            trans_type = "Income" if amount > 0 else "Expense"
            tag = "income" if amount > 0 else "expense"
            
            values = (trans[2], trans_type, Config.format_currency(abs(amount)), trans[4], trans[5])
            self.recent_tree.insert("", tk.END, values=values, tags=(tag,))
        
        # Update charts
        self.update_charts()
    
    def refresh_transactions(self):
        """Refresh main transactions table"""
        # Clear current items
        for item in self.transactions_tree.get_children():
            self.transactions_tree.delete(item)
        
        # Get filtered transactions
        date_from = self.date_from.get_date()
        date_to = self.date_to.get_date()
        transactions = self.db.get_transactions_by_date(date_from, date_to)
        
        # Populate transactions
        for trans in transactions:
            values = trans[1:6]  # Reference, Date, Amount, Category, Description
            item_id = self.transactions_tree.insert("", tk.END, values=values)
            
            # Set category color
            category = values[3]
            category_colors = theme_manager.get_category_colors()
            if category in category_colors:
                self.transactions_tree.item(item_id, tags=(category,))
    
    def update_charts(self):
        """Update analytics charts"""
        # Get category summary for pie chart
        category_data = self.db.get_category_summary()
        
        if category_data:
            # Pie chart for expenses
            expense_data = [(cat, abs(total)) for cat, total, count in category_data if total < 0]
            
            if expense_data:
                self.pie_figure.clear()
                ax = self.pie_figure.add_subplot(111)
                
                categories, amounts = zip(*expense_data)
                category_colors = theme_manager.get_category_colors()
                colors = [category_colors.get(cat, '#cccccc') for cat in categories]
                
                ax.pie(amounts, labels=categories, autopct='%1.1f%%', colors=colors)
                ax.set_title('Expenses by Category')
                
                self.pie_canvas.draw()
    
    def edit_transaction(self):
        """Edit selected transaction"""
        selected_item = self.transactions_tree.selection()
        if not selected_item:
            messagebox.showwarning("Warning", "Please select a transaction to edit")
            return
        
        # Implementation for edit dialog would go here
        messagebox.showinfo("Info", "Edit functionality coming soon!")
    
    def delete_transaction(self):
        """Delete selected transaction"""
        selected_item = self.transactions_tree.selection()
        if not selected_item:
            messagebox.showwarning("Warning", "Please select a transaction to delete")
            return
        
        reference = self.transactions_tree.item(selected_item)['values'][0]
        
        if messagebox.askyesno("Confirm Delete", "Are you sure you want to delete this transaction?"):
            if self.db.delete_transaction(reference):
                self.refresh_transactions()
                self.refresh_dashboard()
                messagebox.showinfo("Success", "Transaction deleted successfully")
            else:
                messagebox.showerror("Error", "Failed to delete transaction")
    
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
    
    def backup_data(self):
        """Backup database"""
        messagebox.showinfo("Info", "Backup functionality coming soon!")
    
    def restore_data(self):
        """Restore database"""
        messagebox.showinfo("Info", "Restore functionality coming soon!")
    
    def run(self):
        """Start the application"""
        self.refresh_dashboard()
        self.refresh_transactions()
        self.root.mainloop()