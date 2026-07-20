# 贡献指南

感谢你愿意改进 WeRead Stealth。提交代码前，请先搜索现有 Issue，避免重复工作。

## 开发环境

- Windows 10 或 Windows 11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- .NET Framework 4.8
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2)

```powershell
dotnet restore WeReadStealth.sln --locked-mode
dotnet build WeReadStealth.sln -c Release --no-restore
dotnet run --project tests/WeReadStealth.Tests/WeReadStealth.Tests.csproj -c Release --no-build
```

运行 `build.bat` 或下面的命令可生成完整便携包：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./build.ps1
```

## 提交要求

- 一个 Pull Request 只解决一个明确问题。
- 新增可测试逻辑时同步增加测试。
- 保持现有 C# 命名和格式风格，避免无关重构。
- 构建必须保持零警告；CI 将警告视为错误。
- 不要提交 `%LOCALAPPDATA%\WeReadStealth`、登录信息、Cookie、截图中的个人书架或任何访问凭据。
- 修改依赖后运行 `dotnet restore WeReadStealth.sln --use-lock-file` 并提交更新后的锁文件。

## Pull Request 检查

提交前请确认：

```powershell
dotnet restore WeReadStealth.sln --locked-mode
dotnet build WeReadStealth.sln -c Release --no-restore
dotnet run --project tests/WeReadStealth.Tests/WeReadStealth.Tests.csproj -c Release --no-build
```

PR 描述应说明行为变化、验证方式，以及是否影响登录、导航、全局快捷键或本地数据。

安全问题不要创建公开 Issue，请遵循 [SECURITY.md](SECURITY.md)。
