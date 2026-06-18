#!/usr/bin/env python3
"""
Setup script for Personal Finance Tracker
"""

import subprocess
import sys
import os
from pathlib import Path

def install_requirements():
    """Install required packages"""
    try:
        print("Installing required packages...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", "-r", "requirements.txt"])
        print("✓ Requirements installed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"✗ Failed to install requirements: {e}")
        return False

def create_directories():
    """Create necessary directories"""
    try:
        # Create logs directory
        logs_dir = Path("logs")
        logs_dir.mkdir(exist_ok=True)
        
        # Create exports directory
        exports_dir = Path("exports")
        exports_dir.mkdir(exist_ok=True)
        
        print("✓ Directories created successfully")
        return True
    except Exception as e:
        print(f"✗ Failed to create directories: {e}")
        return False

def main():
    """Main setup function"""
    print("Setting up Personal Finance Tracker...")
    print("=" * 50)
    
    success = True
    
    # Install requirements
    if not install_requirements():
        success = False
    
    # Create directories
    if not create_directories():
        success = False
    
    print("=" * 50)
    if success:
        print("✓ Setup completed successfully!")
        print("\nTo run the application:")
        print("  python finance_tracker/main.py")
    else:
        print("✗ Setup failed. Please check the errors above.")
        sys.exit(1)

if __name__ == "__main__":
    main()