@echo off
title Job Hunter AI Launcher
echo ==============================================
echo        Starting Job Hunter AI...
echo ==============================================
echo.

:: 1. Check for Python
set PYTHON_CMD=
py -3.12 --version >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    set PYTHON_CMD=py -3.12
    goto py_found
)

py --version >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    set PYTHON_CMD=py
    goto py_found
)

python --version >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    set PYTHON_CMD=python
    goto py_found
)

echo [ERROR] Python is not installed or not added to your PATH.
echo Please install Python from https://www.python.org/downloads/
echo IMPORTANT: Make sure to check the box "Add python.exe to PATH" during installation!
echo.
pause
exit /b

:py_found

:: 2. Install dependencies
echo Upgrading pip...
%PYTHON_CMD% -m pip install --upgrade pip >nul 2>&1
echo Installing dependencies (this might take a minute)...
%PYTHON_CMD% -m pip install -r backend\requirements.txt
IF %ERRORLEVEL% NEQ 0 (
    echo [WARNING] Failed to install dependencies. The app might not work.
    pause
)

echo Installing browser tools for the scrapers (this might take a moment on first run)...
%PYTHON_CMD% -m playwright install chromium >nul 2>&1

:: 3. Run the application
echo.
echo Starting backend server...
start /B %PYTHON_CMD% -m uvicorn backend.main:app --host 127.0.0.1 --port 8000

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
