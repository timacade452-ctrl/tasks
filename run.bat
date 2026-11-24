@echo off
title StudyFlow - Student Workspace
echo ========================================================
echo   Starting StudyFlow Application...
echo ========================================================
echo.

set "PROJECT_DIR=%~dp0tasks"
set "DOTNET_CMD=%~dp0tools\dotnet8\dotnet.exe"

if not exist "%DOTNET_CMD%" (
    set "DOTNET_CMD=dotnet"
)

echo URL: http://localhost:5046
echo Press Ctrl+C in this window to stop the server.
echo.

cd /d "%PROJECT_DIR%"
start http://localhost:5046
"%DOTNET_CMD%" run --launch-profile "http"

pause
