@echo off
echo Personal Finance Tracker Launcher
echo ==================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed or not in PATH
    echo Please install Python 3.7 or higher
    pause
    exit /b 1
)

echo Select interface:
echo 1. Modern Interface (Recommended)
echo 2. Classic Interface
echo 3. UI Demo (Choose at runtime)
echo.
set /p choice="Enter your choice (1-3): "

if "%choice%"=="1" (
    echo Starting Modern Interface...
    python finance_tracker\main.py --ui modern
) else if "%choice%"=="2" (
    echo Starting Classic Interface...
    python finance_tracker\main.py --ui classic
) else if "%choice%"=="3" (
    echo Starting UI Demo...
    python ui_demo.py
) else (
    echo Invalid choice. Starting Modern Interface...
    python finance_tracker\main.py --ui modern
)

REM Keep window open if there's an error
if errorlevel 1 (
    echo.
    echo Application exited with an error
    pause
)