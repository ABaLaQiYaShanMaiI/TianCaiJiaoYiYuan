@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
echo ================ 存档修改器 ================
echo 提示：请先完全退出游戏，改完后再启动游戏读取对应槽位。
echo 建议：先用游戏内 F5 保存一次，再修改；改坏了可用「存档修复.bat」或 .editor-backup 目录还原。
echo.
"%ROOT%工具\SaveEditor\SaveEditor.exe"
echo.
echo ---------------- 交互编辑 ----------------
set /p SLOT=输入槽位（如 manual-1，直接回车退出）: 
if "%SLOT%"=="" exit /b
"%ROOT%工具\SaveEditor\SaveEditor.exe" edit %SLOT%
pause
exit /b
