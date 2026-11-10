@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
echo ================ 打包备份存档 ================
"%ROOT%工具\SaveDoctor\SaveDoctor.exe" "" backup
pause
exit /b
