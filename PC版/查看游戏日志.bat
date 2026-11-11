@echo off
chcp 65001 >nul
setlocal
set "USERDIR=%APPDATA%\Godot\app_userdata\泡沫年代 - Bubble Years"
echo ================ 日志目录 ================
echo %USERDIR%\logs
echo.
echo =========== game.log 最后 50 行（引擎错误 / 保存失败原因）===========
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%USERDIR%\logs\game.log') { Get-Content '%USERDIR%\logs\game.log' -Tail 50 } else { '还没有 game.log（游戏尚未运行过）' }"
echo.
echo =========== 存档动作（save-activity.log 最后 30 行）===========
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%USERDIR%\logs\save-activity.log') { Get-Content '%USERDIR%\logs\save-activity.log' -Tail 30 } else { '还没有 save-activity.log' }"
echo.
echo =========== 性能采样（performance.log 最后 15 行）===========
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path '%USERDIR%\logs\performance.log') { Get-Content '%USERDIR%\logs\performance.log' -Tail 15 } else { '还没有 performance.log（进游戏后每秒记录一次）' }"
echo.
echo 游戏内快捷键：F1 帮助 / F5 快速保存 / F9 快速读档 / F11 全屏 / F3 性能浮层
start "" "%USERDIR%\logs"
pause
exit /b
