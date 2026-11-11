@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
set DOTNET_ROLL_FORWARD=LatestMajor
echo ================ 清理存档冗余（请先完全退出游戏）================
echo 只删除：孤儿 .parts 目录（对应槽位已不存在）+ 残留 *.tmp
echo 不会删除仍被槽位引用的分片，不会损坏存档。
echo.
"%ROOT%工具\SaveDoctor\SaveDoctor.exe" "" clean
echo.
pause
exit /b
