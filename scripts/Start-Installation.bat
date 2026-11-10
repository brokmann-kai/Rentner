@echo off
setlocal
cd /d "%~dp0"

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo Bitte Rechtsklick → "Als Administrator ausführen".
  powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo FrischStart Installation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\Install-FrischStart.ps1" -PackageRoot "%~dp0" -LaunchWizard
if errorlevel 1 (
  echo Installation fehlgeschlagen.
  pause
  exit /b 1
)
echo.
pause
