@echo off
chcp 65001 >nul
setlocal
set "ROOT=%~dp0"
rem 本机可能只装了 .NET 8/10，允许 net9.0 程序集跨大版本运行
set DOTNET_ROLL_FORWARD=LatestMajor
echo ================ 存档自检 ================
"%ROOT%工具\SaveDoctor\SaveDoctor.exe"
echo.
echo 提示：把上面的输出整段复制给我，就能定位存档/读档失败的具体原因。
pause
exit /b
