# WeRead Stealth

一个轻量、非官方的微信读书 Windows 第三方客户端，专为上班摸鱼看书设计：通过紧凑、置顶、透明度可调的小窗口，方便在工作间隙随时打开阅读。使用 WinForms 和系统 WebView2 Runtime 构建。

> 本项目与腾讯、微信或微信读书没有隶属、授权或合作关系。微信读书名称及相关商标归其权利人所有。

## 特性

- 完整加载微信读书网页并持久化本机登录状态
- 无边框、可缩放、可拖动的紧凑窗口
- 窗口置顶与 30%～100% 透明度调节
- 系统托盘、关闭到托盘和单实例运行
- 浅色/深色原生控制栏
- 全局快捷键及冲突提示
- 自动恢复窗口位置、大小和偏好设置
- 仅允许 HTTPS `qq.com` 域系在应用内顶级导航

## 为什么体积很小

旧版本使用 Electron，将 Chromium 一起打包。2.0 改为复用 Windows 的 WebView2 Runtime：

| 产物 | 体积 |
| --- | ---: |
| 旧 Electron 安装包 | 约 72.86 MB |
| 新版完整运行目录 | 约 0.92 MB |
| 新版便携 ZIP | 约 0.27 MB |

代价是目标机器必须已经安装 .NET Framework 4.8 和 WebView2 Runtime。

## 使用

### 系统要求

- Windows 10 或 Windows 11 x64
- .NET Framework 4.8
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2)

从 GitHub Releases 下载便携 ZIP，完整解压后运行 `WeReadStealth.exe`。不要只复制 EXE，旁边的 DLL 和 `Assets` 目录也是运行所需文件。

### 快捷键

| 快捷键 | 功能 |
| --- | --- |
| `Ctrl + Shift + W` | 显示或隐藏窗口 |
| `Ctrl + Shift + B` | 立即隐藏到托盘 |
| `Ctrl + Shift + T` | 切换窗口置顶 |
| `Ctrl + Shift + O` | 循环切换 100%、80%、60% 透明度 |
| `Ctrl + Shift + M` | 切换浅色/深色主题 |

## 从源码构建

构建需要 Windows 和 [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)：

```powershell
dotnet restore WeReadStealth.sln --locked-mode
dotnet build WeReadStealth.sln -c Release --no-restore
dotnet run --project tests/WeReadStealth.Tests/WeReadStealth.Tests.csproj -c Release --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File ./build.ps1
```

构建产物位于 `publish/`，其中 `WeReadStealth.zip` 可直接分发。

## 数据与隐私

设置和浏览器数据仅保存在当前 Windows 用户目录：

```text
%LOCALAPPDATA%\WeReadStealth
```

应用不收集遥测，不内置账号凭据，也不会把登录数据写入项目或发布包。删除上述目录会同时重置窗口设置和微信读书登录状态。

## 项目结构

```text
src/WeReadStealth/        应用源码
tests/WeReadStealth.Tests 设置层测试
docs/                     架构文档
.github/                  CI 与协作模板
build.ps1                 可复现发布脚本
```

架构细节见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)，参与开发前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。安全问题请按 [SECURITY.md](SECURITY.md) 私密报告。

## 许可证

本项目使用 [MIT License](LICENSE)。
