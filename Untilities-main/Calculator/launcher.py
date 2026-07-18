#!/usr/bin/env python3
"""
Calculator Launcher
Choose between CLI and GUI versions of the calculator.
"""

import sys
import subprocess

def show_menu():
    """Display launcher menu."""
    print("\n" + "="*50)
    print("🧮 ENHANCED CALCULATOR LAUNCHER")
    print("="*50)
    print("Choose your calculator interface:")
    print()
    print("1. 🖥️  Command Line Interface (CLI)")
    print("2. 🖼️  Graphical User Interface (GUI)")
    print("3. ❌ Exit")
    print("="*50)

def main():
    """Main launcher function."""
    print("🎉 Welcome to Enhanced Calculator!")
    
    while True:
        show_menu()
        choice = input("Enter your choice (1-3): ").strip()
        
        if choice == '1':
            print("\n🚀 Starting CLI Calculator...")
            try:
                subprocess.run([sys.executable, "calc.py"])
            except KeyboardInterrupt:
                print("\n👋 CLI Calculator closed.")
            except Exception as e:
                print(f"❌ Error starting CLI calculator: {e}")
        
        elif choice == '2':
            print("\n🚀 Starting GUI Calculator...")
            try:
                subprocess.run([sys.executable, "calculator_gui.py"])
            except KeyboardInterrupt:
                print("\n👋 GUI Calculator closed.")
            except Exception as e:
                print(f"❌ Error starting GUI calculator: {e}")
        
        elif choice == '3':
            print("👋 Thank you for using Enhanced Calculator!")
            break
        
        else:
            print("❌ Invalid choice. Please enter 1, 2, or 3.")
        
        print()  # Add spacing

if __name__ == "__main__":
    main()