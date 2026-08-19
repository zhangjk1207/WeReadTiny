# 架构说明

## 目标

WeReadTiny 是一个 Windows 专用的轻量网页容器。它只负责窗口体验和 WebView2 生命周期，不代理、不抓取也不修改微信读书服务端数据。

## 组件

| 组件 | 职责 |
| --- | --- |
| `Program.cs` | 单实例互斥、现有窗口唤醒和应用异常边界 |
| `MainForm.cs` | 原生工具栏、WebView2、托盘、窗口状态和用户交互 |
| `HotkeyManager.cs` | 注册、识别并释放 Win32 全局快捷键 |
| `NativeMethods.cs` | 无边框拖动、缩放、窗口激活和图标句柄 API |
| `AppSettings.cs` | 可持久化设置模型、默认值和范围归一化 |
| `SettingsStore.cs` | JSON 加载、损坏恢复和原子替换保存 |
| `UiTheme.cs` | 浅色与深色原生控件配色 |
| `ToolbarVisibilityPolicy.cs` | 顶边唤出、停留与延迟隐藏规则 |
| `WebViewEnvironment.cs` | WebView2 浏览器参数与滚动条策略 |
| `WebViewPageBridge.cs` | 严格校验页面背景色消息，驱动滚动条遮罩配色 |

## 进程与数据流

```text
用户操作 / 全局快捷键
          |
          v
      MainForm --------------> SettingsStore
          |                         |
          |                         v
          |              %LOCALAPPDATA%/WeReadStealth/settings.json
          v
       WebView2 --------------> https://*.qq.com
          |
          v
%LOCALAPPDATA%/WeReadStealth/WebView2
```

应用没有向网页注入宿主对象。WebView2 登录态由系统运行时保存在本地用户目录，源码和发布包均不包含用户数据。

## 发布模型

项目针对 `.NET Framework 4.8` 和 `win-x64`。发布包只包含应用 EXE、WebView2 托管程序集、x64 Loader 和图标；Chromium/WebView2 Runtime 由操作系统的 Evergreen Runtime 提供。

这种模式显著降低分发体积，但意味着目标机器必须安装 .NET Framework 4.8 和 WebView2 Runtime。`build.ps1` 会裁剪非 x64 Loader，并验证必要文件存在后才创建 ZIP。
