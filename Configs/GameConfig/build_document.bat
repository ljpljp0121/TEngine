@echo off
setlocal enabledelayedexpansion

:: Set environment variable, doc path may not be root, can be modified externally
if "%1" == "" (
    set PUBLIC_URL=/
) else (
    set PUBLIC_URL=%1
)

:: Set script directory and application directory
set SCRIPT_DIR=../../
set APP_PATH=%SCRIPT_DIR%Tools\Docs

echo ======================================
echo      VitePress Production Build
echo ======================================
echo.

echo [INFO] Starting VitePress documentation build...
echo [INFO] Application directory: %APP_PATH%
echo [INFO] Public URL path: %PUBLIC_URL%
echo.

:: Check if Node.js exists (assuming node is in PATH)
where node >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Node.js executable not found
    echo Please ensure node is properly installed and added to PATH
    exit /b 1
)

:: Check if application directory exists
if not exist "%APP_PATH%" (
    echo [ERROR] Application directory not found: %APP_PATH%
    exit /b 1
)

:: Check if dependencies are installed
if not exist "%APP_PATH%\node_modules" (
    echo [WARNING] node_modules directory not found
    echo Please run npm install first to install dependencies
    exit /b 1
)

:: Switch to application directory
cd /d "%APP_PATH%"

echo [INFO] Building VitePress documentation...
echo [INFO] Application directory: %APP_PATH%
echo.

:: Build VitePress
node node_modules\vitepress\bin\vitepress.js build

if errorlevel 1 (
    echo.
    echo [ERROR] Build failed!
    exit /b 1
)

echo.
echo ======================================
echo [SUCCESS] Build completed!
echo.
echo Build output location: %APP_PATH%\.vitepress\dist
echo.
echo You can deploy the dist directory to any web server
echo ======================================
