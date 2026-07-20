@echo off
chcp 65001 >nul
setlocal

if exist "%~dp0publish\WeReadStealth.exe" (
    start "" "%~dp0publish\WeReadStealth.exe"
    exit /b 0
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 尚未构建程序，并且未找到 .NET SDK。
    echo 请先运行 build.bat。
    pause
    exit /b 1
)

dotnet run --project "%~dp0src\WeReadStealth\WeReadStealth.csproj" -c Release
