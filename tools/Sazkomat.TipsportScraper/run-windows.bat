@echo off
echo ==========================================
echo   Tipsport Scraper - Run in Windows
echo ==========================================
echo.
echo This script must be run from Windows CMD/PowerShell, NOT from WSL!
echo.

cd /d "%~dp0"
dotnet run

echo.
pause
