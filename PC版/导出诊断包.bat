@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
where pwsh >nul 2>nul
if %errorlevel%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%ROOT%工具\导出诊断包.ps1"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%工具\导出诊断包.ps1"
)
pause
exit /b
