import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext
import math
from typing import Optional
from calc import Calculator

class CalculatorGUI:
    """Modern GUI Calculator using tkinter."""
    
    def __init__(self):
        self.calc = Calculator()
        self.current_input = ""
        self.operation = None
        self.first_number = None
        self.result_displayed = False
        
        # Create main window
        self.root = tk.Tk()
        self.root.title("🧮 Enhanced Calculator")
        self.root.geometry("400x600")
        self.root.resizable(False, False)
        
        # Configure style
        self.setup_styles()
        
        # Create GUI elements
        self.create_widgets()
        
    def setup_styles(self):
        """Configure the visual style of the calculator."""
        self.root.configure(bg='#2c3e50')
        
        # Configure ttk styles
        style = ttk.Style()
        style.theme_use('clam')
        
        # Button styles
        style.configure('Number.TButton', 
                       font=('Arial', 14, 'bold'),
                       padding=10)
        
        style.configure('Operation.TButton',
                       font=('Arial', 14, 'bold'),
                       padding=10)
        
        style.configure('Special.TButton',
                       font=('Arial', 12, 'bold'),
                       padding=8)
    
    def create_widgets(self):
        """Create all GUI widgets."""
        # Main frame
        main_frame = tk.Frame(self.root, bg='#2c3e50', padx=10, pady=10)
        main_frame.pack(fill=tk.BOTH, expand=True)
        
        # Display frame
        display_frame = tk.Frame(main_frame, bg='#34495e', relief=tk.RAISED, bd=2)
        display_frame.pack(fill=tk.X, pady=(0, 10))
        
        # Current calculation display
        self.display_var = tk.StringVar(value="0")
        self.display = tk.Label(display_frame, 
                               textvariable=self.display_var,
                               font=('Arial', 24, 'bold'),
                               bg='#34495e', 
                               fg='white',
                               anchor='e',
                               padx=10, 
                               pady=15)
        self.display.pack(fill=tk.X)
        
        # Operation display
        self.operation_var = tk.StringVar(value="")
        self.operation_display = tk.Label(display_frame,
                                         textvariable=self.operation_var,
                                         font=('Arial', 12),
                                         bg='#34495e',
                                         fg='#bdc3c7',
                                         anchor='e',
                                         padx=10)
        self.operation_display.pack(fill=tk.X)
        
        # Button frame
        button_frame = tk.Frame(main_frame, bg='#2c3e50')
        button_frame.pack(fill=tk.BOTH, expand=True)
        
        # Create buttons
        self.create_buttons(button_frame)
        
        # History frame
        history_frame = tk.LabelFrame(main_frame, 
                                     text="History", 
                                     bg='#2c3e50', 
                                     fg='white',
                                     font=('Arial', 10, 'bold'))
        history_frame.pack(fill=tk.X, pady=(10, 0))
        
        # History display
        self.history_text = scrolledtext.ScrolledText(history_frame,
                                                     height=4,
                                                     font=('Arial', 9),
                                                     bg='#34495e',
                                                     fg='white',
                                                     state=tk.DISABLED)
        self.history_text.pack(fill=tk.X, padx=5, pady=5)
    
    def create_buttons(self, parent):
        """Create calculator buttons."""
        # Button layout
        buttons = [
            ['C', 'CE', '√', '^'],
            ['7', '8', '9', '/'],
            ['4', '5', '6', '*'],
            ['1', '2', '3', '-'],
            ['0', '.', '=', '+'],
            ['x²', '%', 'History', 'Clear History']
        ]
        
        # Color schemes
        number_color = '#3498db'
        operation_color = '#e74c3c'
        special_color = '#f39c12'
        
        for i, row in enumerate(buttons):
            row_frame = tk.Frame(parent, bg='#2c3e50')
            row_frame.pack(fill=tk.X, pady=2)
            
            for j, btn_text in enumerate(row):
                # Determine button color and command
                if btn_text.isdigit() or btn_text == '.':
                    bg_color = number_color
                    command = lambda t=btn_text: self.number_click(t)
                elif btn_text in ['+', '-', '*', '/', '^']:
                    bg_color = operation_color
                    command = lambda t=btn_text: self.operation_click(t)
                elif btn_text == '=':
                    bg_color = special_color
                    command = self.equals_click
                else:
                    bg_color = special_color
                    command = lambda t=btn_text: self.special_click(t)
                
                btn = tk.Button(row_frame,
                               text=btn_text,
                               font=('Arial', 12, 'bold'),
                               bg=bg_color,
                               fg='white',
                               relief=tk.RAISED,
                               bd=2,
                               command=command)
                btn.pack(side=tk.LEFT, fill=tk.BOTH, expand=True, padx=1)
    
    def number_click(self, number):
        """Handle number button clicks."""
        if self.result_displayed:
            self.current_input = ""
            self.result_displayed = False
        
        self.current_input += str(number)
        self.display_var.set(self.current_input)
    
    def operation_click(self, op):
        """Handle operation button clicks."""
        if self.current_input:
            if self.first_number is not None and self.operation and not self.result_displayed:
                self.equals_click()
            
            self.first_number = float(self.current_input)
            self.operation = op
            self.operation_var.set(f"{self.first_number} {op}")
            self.current_input = ""
            self.result_displayed = False
    
    def equals_click(self):
        """Handle equals button click."""
        if self.first_number is not None and self.operation and self.current_input:
            try:
                second_number = float(self.current_input)
                
                # Map GUI operations to calculator methods
                op_map = {
                    '+': 'add',
                    '-': 'subtract',
                    '*': 'multiply',
                    '/': 'divide',
                    '^': 'power'
                }
                
                if self.operation in op_map:
                    result = self.calc.calculate(op_map[self.operation], 
                                               self.first_number, 
                                               second_number)
                    
                    self.display_var.set(str(result))
                    self.operation_var.set(f"{self.first_number} {self.operation} {second_number} = {result}")
                    
                    # Update history
                    self.update_history()
                    
                    # Reset for next calculation
                    self.first_number = result
                    self.current_input = str(result)
                    self.operation = None
                    self.result_displayed = True
                    
            except ValueError as e:
                messagebox.showerror("Error", str(e))
                self.clear_all()
    
    def special_click(self, special):
        """Handle special button clicks."""
        if special == 'C':
            self.clear_all()
        elif special == 'CE':
            self.clear_entry()
        elif special == '√':
            self.square_root()
        elif special == 'x²':
            self.square()
        elif special == '%':
            self.percentage()
        elif special == 'History':
            self.show_history()
        elif special == 'Clear History':
            self.clear_history()
    
    def clear_all(self):
        """Clear all inputs and reset calculator."""
        self.current_input = ""
        self.first_number = None
        self.operation = None
        self.display_var.set("0")
        self.operation_var.set("")
        self.result_displayed = False
    
    def clear_entry(self):
        """Clear current entry."""
        self.current_input = ""
        self.display_var.set("0")
    
    def square_root(self):
        """Calculate square root of current input."""
        if self.current_input:
            try:
                number = float(self.current_input)
                result = self.calc.calculate('square_root', number)
                self.display_var.set(str(result))
                self.operation_var.set(f"√{number} = {result}")
                self.current_input = str(result)
                self.result_displayed = True
                self.update_history()
            except ValueError as e:
                messagebox.showerror("Error", str(e))
    
    def square(self):
        """Calculate square of current input."""
        if self.current_input:
            try:
                number = float(self.current_input)
                result = self.calc.calculate('square', number)
                self.display_var.set(str(result))
                self.operation_var.set(f"{number}² = {result}")
                self.current_input = str(result)
                self.result_displayed = True
                self.update_history()
            except ValueError as e:
                messagebox.showerror("Error", str(e))
    
    def percentage(self):
        """Handle percentage calculation."""
        if self.first_number is not None and self.current_input:
            try:
                second_number = float(self.current_input)
                result = self.calc.calculate('percentage', self.first_number, second_number)
                self.display_var.set(str(result))
                self.operation_var.set(f"{self.first_number}% of {second_number} = {result}")
                self.current_input = str(result)
                self.result_displayed = True
                self.update_history()
            except ValueError as e:
                messagebox.showerror("Error", str(e))
    
    def update_history(self):
        """Update the history display."""
        history = self.calc.get_history()
        if history:
            self.history_text.config(state=tk.NORMAL)
            self.history_text.delete(1.0, tk.END)
            
            # Show last 10 calculations
            recent_history = history[-10:]
            for calculation in recent_history:
                self.history_text.insert(tk.END, calculation + "\n")
            
            self.history_text.config(state=tk.DISABLED)
            self.history_text.see(tk.END)
    
    def show_history(self):
        """Show full history in a popup window."""
        history = self.calc.get_history()
        if not history:
            messagebox.showinfo("History", "No calculations in history")
            return
        
        # Create history window
        history_window = tk.Toplevel(self.root)
        history_window.title("Calculation History")
        history_window.geometry("400x300")
        history_window.configure(bg='#2c3e50')
        
        # History text widget
        history_text = scrolledtext.ScrolledText(history_window,
                                                font=('Arial', 10),
                                                bg='#34495e',
                                                fg='white')
        history_text.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
        
        # Add all history
        for i, calculation in enumerate(history, 1):
            history_text.insert(tk.END, f"{i}. {calculation}\n")
        
        history_text.config(state=tk.DISABLED)
    
    def clear_history(self):
        """Clear calculation history."""
        self.calc.clear_history()
        self.history_text.config(state=tk.NORMAL)
        self.history_text.delete(1.0, tk.END)
        self.history_text.config(state=tk.DISABLED)
        messagebox.showinfo("History", "History cleared successfully")
    
    def run(self):
        """Start the calculator GUI."""
        self.root.mainloop()

def main():
    """Main function to run the GUI calculator."""
    app = CalculatorGUI()
    app.run()

if __name__ == "__main__":
    main()