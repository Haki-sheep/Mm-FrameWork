@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-LocalDemo.ps1" -ServerOnly
if errorlevel 1 pause
