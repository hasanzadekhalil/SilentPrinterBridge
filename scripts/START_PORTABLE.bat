@echo off
REM SilentPrintBridge Portable Launcher
REM Double-click to run

echo ========================================
echo   SilentPrintBridge Portable Launcher
echo ========================================
echo.

REM Get current directory
set "APP_DIR=%~dp0"
cd /d "%APP_DIR%"

REM Check if EXE exists
if not exist "SilentPrintBridge.exe" (
    echo ERROR: SilentPrintBridge.exe not found!
    echo Please ensure you're running this from the correct directory.
    pause
    exit /b 1
)

REM Create data directories if they don't exist
if not exist "data\logs" mkdir "data\logs"
if not exist "data\temp" mkdir "data\temp"

REM Update config to use local directories
powershell -Command "(Get-Content appsettings.json) -replace 'C:\\\\ProgramData\\\\SilentPrintBridge', '%APP_DIR%data' | Set-Content appsettings.json"

echo Starting SilentPrintBridge...
echo.
echo Service will run on: http://127.0.0.1:17878
echo.
echo Press Ctrl+C to stop the service
echo.
echo ========================================
echo.

REM Run the application
SilentPrintBridge.exe --console

pause
