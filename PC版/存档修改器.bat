@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
rem 图形界面版修改器（推荐）。命令行版：存档修改器-命令行.bat
start "" "%ROOT%工具\SaveEditorGui\SaveEditorGui.exe"
exit /b
