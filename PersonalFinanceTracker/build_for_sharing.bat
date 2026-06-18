@echo off
echo Personal Finance Tracker - Build for Sharing
echo =============================================
echo.

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed or not in PATH
    echo Please install Python 3.7 or higher
    pause
    exit /b 1
)

echo Building distributable app...
python build.py

echo.
echo =============================================
echo Build complete! Check the output above for details.
echo.
pause