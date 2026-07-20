# 安全策略

## 支持范围

当前仅维护最新的 `2.x` 版本。旧 Electron 版本已经停止维护，不再接收安全更新。

## 报告漏洞

请使用 GitHub 仓库的 **Security > Report a vulnerability** 私密报告入口。不要在公开 Issue、讨论区或 Pull Request 中披露漏洞细节、Cookie、登录二维码、个人书架或其他敏感数据。

报告中请包括：

- 受影响版本和 Windows 版本
- WebView2 Runtime 版本
- 可重复的最小步骤
- 潜在影响
- 建议的修复方向（如有）

## 安全边界

- 应用不内置腾讯或微信读书账号凭据。
- 顶级内嵌导航仅允许 HTTPS 的 `qq.com` 域系，其他网页交给系统浏览器。
- 应用没有向网页暴露本机对象或任意文件访问接口。
- 设置与 WebView2 浏览器数据保存在 `%LOCALAPPDATA%\WeReadStealth`，不应进入源码仓库或错误报告。
- 发布包依赖系统 WebView2 Evergreen Runtime，应保持 Windows 和 WebView2 更新。
