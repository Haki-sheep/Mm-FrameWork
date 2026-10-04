@echo off
setlocal

set "CONF_ROOT=%~dp0"
for %%I in ("%CONF_ROOT%..") do set "PROJECT_ROOT=%%~fI"

set "LUBAN_DLL=%PROJECT_ROOT%\Tools\LubanExamples\Tools\Luban\Luban.dll"
set "OUTPUT_CODE_DIR=%PROJECT_ROOT%\Assets\MieMieFrameTools\Scripts\Frame\C_Data\Luban\Generated"
set "OUTPUT_DATA_DIR=%PROJECT_ROOT%\Assets\StreamingAssets\DataTables"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%CONF_ROOT%export_localization.ps1"
if errorlevel 1 exit /b 1

if not exist "%LUBAN_DLL%" call "%PROJECT_ROOT%\Tools\setup_luban.bat"
if errorlevel 1 exit /b 1

dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%luban.conf" ^
    -t client ^
    -c cs-newtonsoft-json ^
    -d json ^
    -x outputCodeDir="%OUTPUT_CODE_DIR%" ^
    -x outputDataDir="%OUTPUT_DATA_DIR%" ^
    --validationFailAsError

exit /b %ERRORLEVEL%
