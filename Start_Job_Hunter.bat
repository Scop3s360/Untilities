@echo off
title Job Hunter AI Launcher
echo ==============================================
echo        Starting Job Hunter AI...
echo ==============================================
echo.

:: 1. Check for Python
python --version >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Python is not installed or not added to your PATH.
    echo Please install Python from https://www.python.org/downloads/
    echo IMPORTANT: Make sure to check the box "Add python.exe to PATH" during installation!
    echo.
    pause
    exit /b
)

:: 2. Enter backend directory and install dependencies
cd backend
echo Upgrading pip...
python -m pip install --upgrade pip >nul 2>&1
echo Installing dependencies (this might take a minute)...
python -m pip install -r requirements.txt
IF %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Failed to install dependencies. The app might not work.
    pause
)

echo Installing browser tools for the scrapers (this might take a moment on first run)...
python -m playwright install chromium >nul 2>&1

:: 3. Run the application
echo.
echo Starting backend server...
start /B python -m uvicorn main:app --host 127.0.0.1 --port 8000

echo.
echo Waiting for the server to be ready...
timeout /t 5 /nobreak > NUL

echo.
echo Launching your browser...
start http://127.0.0.1:8000

echo.
echo Job Hunter AI is running in the background!
echo Keep the backend terminal window open to keep the server running.
pause
