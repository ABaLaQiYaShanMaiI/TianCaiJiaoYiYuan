@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
rem 带控制台启动，出错时能直接看到日志（排障用）
"%ROOT%engine\Godot_v4.7.2-stable_mono_win64_console.exe" --path "%ROOT%game" --verbose
pause
exit /b
