@echo off
chcp 65001 >nul
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 未找到 .NET SDK，请先安装 .NET 8 SDK。
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if errorlevel 1 (
    echo.
    echo [错误] 构建失败，请查看上方日志。
    pause
    exit /b 1
)

echo.
echo 构建成功，输出目录：%~dp0publish
start "" "%~dp0publish"
pause
