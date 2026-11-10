@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
echo ================ 用 .bak 修复损坏存档 ================
"%ROOT%工具\SaveDoctor\SaveDoctor.exe" "" fix
pause
exit /b
