@echo off
setlocal enabledelayedexpansion

:: Set script directory and application directory
set SCRIPT_DIR=../../
set APP_PATH=%SCRIPT_DIR%Tools\Docs

echo ======================================
echo      VitePress Development Server
echo ======================================
echo.

:: Check if Node.js exists (assuming node is in PATH)
where node >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Node.js executable not found
    echo Please ensure node is properly installed and added to PATH
    pause
    exit /b 1
)

:: Check if application directory exists
if not exist "%APP_PATH%" (
    echo [ERROR] Application directory not found: %APP_PATH%
    pause
    exit /b 1
)

:: Check if dependencies are installed
if not exist "%APP_PATH%\node_modules" (
    echo [WARNING] node_modules directory not found
    echo Please run npm install first to install dependencies
    pause
    exit /b 1
)

:: Display version information
echo [INFO] Node.js version:
node --version
echo.

:: Switch to application directory
cd /d "%APP_PATH%"

echo [INFO] Starting VitePress development server...
echo [INFO] Application directory: %APP_PATH%
echo [INFO] Server will start at http://localhost:5173
echo.
echo Press Ctrl+C to stop the server
echo ======================================
echo.

:: Start VitePress development server (background)
start /b "VitePress Server" node node_modules\vitepress\bin\vitepress.js dev

:: Wait for server to start
echo [INFO] Waiting for server to start...
timeout /t 3 /nobreak >nul

:: Automatically open browser
echo [INFO] Opening browser...
start http://localhost:5173

:: Wait for user input to keep window open
echo.
echo [INFO] Browser opened, VitePress server is running
echo [INFO] Press any key to stop the server...
pause >nul

:: Stop VitePress server process
echo [INFO] Stopping server...
taskkill /f /im node.exe 2>nul

echo.
echo [INFO] VitePress development server stopped
pause
