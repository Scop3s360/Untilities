#!/usr/bin/env python3
"""
UI Demo - Compare different UI versions
"""

import tkinter as tk
from tkinter import ttk, messagebox
import sys
from pathlib import Path

# Add finance_tracker to path
sys.path.insert(0, str(Path(__file__).parent / "finance_tracker"))

def run_classic_ui():
    """Run the classic UI"""
    try:
        import subprocess
        subprocess.run([sys.executable, "finance_tracker/main.py", "--ui", "classic"])
    except Exception as e:
        messagebox.showerror("Error", f"Failed to start classic UI: {e}")

def run_modern_ui():
    """Run the modern UI"""
    try:
        import subprocess
        subprocess.run([sys.executable, "finance_tracker/main.py", "--ui", "modern"])
    except Exception as e:
        messagebox.showerror("Error", f"Failed to start modern UI: {e}")

def main():
    """Main demo launcher"""
    root = tk.Tk()
    root.title("Finance Tracker - UI Demo")
    root.geometry("400x300")
    root.resizable(False, False)
    
    # Center window
    root.update_idletasks()
    x = (root.winfo_screenwidth() // 2) - (400 // 2)
    y = (root.winfo_screenheight() // 2) - (300 // 2)
    root.geometry(f"400x300+{x}+{y}")
    
    # Main frame
    main_frame = ttk.Frame(root, padding="30")
    main_frame.pack(fill=tk.BOTH, expand=True)
    
    # Title
    title_label = ttk.Label(main_frame, text="💰 Finance Tracker", 
                           font=('Segoe UI', 18, 'bold'))
    title_label.pack(pady=(0, 20))
    
    subtitle_label = ttk.Label(main_frame, text="Choose your preferred interface:", 
                              font=('Segoe UI', 12))
    subtitle_label.pack(pady=(0, 30))
    
    # Buttons frame
    buttons_frame = ttk.Frame(main_frame)
    buttons_frame.pack(expand=True)
    
    # Classic UI button
    classic_btn = ttk.Button(buttons_frame, text="🔷 Classic Interface", 
                            command=lambda: [root.destroy(), run_classic_ui()],
                            width=25)
    classic_btn.pack(pady=10)
    
    classic_desc = ttk.Label(buttons_frame, text="Simple, functional interface",
                            font=('Segoe UI', 9), foreground='gray')
    classic_desc.pack(pady=(0, 20))
    
    # Modern UI button  
    modern_btn = ttk.Button(buttons_frame, text="✨ Modern Interface",
                           command=lambda: [root.destroy(), run_modern_ui()],
                           width=25)
    modern_btn.pack(pady=10)
    
    modern_desc = ttk.Label(buttons_frame, text="Enhanced UI with dashboard & analytics",
                           font=('Segoe UI', 9), foreground='gray')
    modern_desc.pack(pady=(0, 20))
    
    # Exit button
    exit_btn = ttk.Button(buttons_frame, text="❌ Exit", 
                         command=root.destroy, width=15)
    exit_btn.pack(pady=20)
    
    root.mainloop()

if __name__ == "__main__":
    main()