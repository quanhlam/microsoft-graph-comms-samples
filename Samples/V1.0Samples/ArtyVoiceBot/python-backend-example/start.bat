@echo off
REM Quick start script for Windows

echo.
echo ========================================
echo Arty Voice Bot - Python Backend
echo ========================================
echo.

REM Check if venv exists
if not exist "venv\" (
    echo Creating virtual environment...
    python -m venv venv
    echo.
)

REM Activate venv
echo Activating virtual environment...
call venv\Scripts\activate
echo.

REM Install requirements
echo Installing dependencies...
pip install -r requirements.txt
echo.

REM Start the server
echo.
echo ========================================
echo Starting FastAPI server...
echo ========================================
echo.
python main.py

