@echo off
chcp 65001 >nul
setlocal
set "USERDIR=%APPDATA%\Godot\app_userdata\泡沫年代 - Bubble Years"
echo ================ 日志目录 ================
echo %USERDIR%\logs
echo.
echo =========== game.log 最后 50 行 ===========
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%USERDIR%\logs\game.log') { Get-Content '%USERDIR%\logs\game.log' -Tail 50 } else { '还没有 game.log（游戏尚未运行过）' }"
echo.
echo =========== 存档动作日志 ===========
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%USERDIR%\logs\save-activity.log') { Get-Content '%USERDIR%\logs\save-activity.log' -Tail 30 } else { '还没有 save-activity.log' }"
echo.
start "" "%USERDIR%\logs"
pause
exit /b
