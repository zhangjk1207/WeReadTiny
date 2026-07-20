# 更新日志

本项目使用语义化版本号。公开发布后的变更会记录在此文件中。

## [2.0.1] - 2026-07-20

### 修复

- 恢复无边框窗口的原生可缩放样式，现在可以从窗口四边和四角自由调整大小。
- 增加窗口缩放样式回归测试。

## [2.0.0] - 2026-07-20

### 新增

- 使用 WinForms 和系统 WebView2 Runtime 重写 Windows 客户端。
- 支持无边框缩放、窗口置顶、透明度、浅色/深色工具栏和系统托盘。
- 支持全局快捷键冲突提示、单实例唤醒和窗口状态持久化。
- 增加导航限制、WebView2 错误页面和配置损坏恢复。
- 增加设置层测试、锁定依赖、便携 ZIP 构建和 GitHub Actions。

### 变更

- 分发 ZIP 从原 Electron 安装包约 72.86 MB 降至约 0.27 MB。
- 运行时改为复用 Windows 的 .NET Framework 4.8 和 WebView2 Runtime。

### 移除

- 移除 Electron、Node.js、`electron-store` 和旧 Chromium 运行时打包。
