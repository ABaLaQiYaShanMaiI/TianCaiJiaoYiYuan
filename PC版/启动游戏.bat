@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
rem 本机未安装 .NET 9 运行时，用 roll-forward 让 net9.0 程序集在 .NET 10 上运行
set DOTNET_ROLL_FORWARD=LatestMajor
start "" "%ROOT%engine\Godot_v4.7.2-stable_mono_win64.exe" --path "%ROOT%game"
exit /b
