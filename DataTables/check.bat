@echo off
setlocal

set "CONF_ROOT=%~dp0"
for %%I in ("%CONF_ROOT%..") do set "PROJECT_ROOT=%%~fI"

set "LUBAN_DLL=%PROJECT_ROOT%\Tools\LubanExamples\Tools\Luban\Luban.dll"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%CONF_ROOT%export_localization.ps1"
if errorlevel 1 exit /b 1

if not exist "%LUBAN_DLL%" call "%PROJECT_ROOT%\Tools\setup_luban.bat"
if errorlevel 1 exit /b 1

dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%luban.conf" ^
    -t all ^
    -f ^
    --validationFailAsError

exit /b %ERRORLEVEL%
