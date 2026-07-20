## 变更说明

说明这个 PR 解决的问题以及采用的方案。

## 验证

- [ ] `dotnet restore WeReadStealth.sln --locked-mode`
- [ ] `dotnet build WeReadStealth.sln -c Release --no-restore`
- [ ] 设置层测试通过
- [ ] 涉及界面时已在 100% 和高 DPI 缩放下检查
- [ ] 涉及登录、导航、托盘或快捷键时已完成手动验证

## 安全与隐私

- [ ] 不包含 Token、Cookie、登录二维码、个人书架或本地 WebView2 数据
- [ ] 没有扩大网页可访问的本机能力
- [ ] 依赖变更已更新并提交 `packages.lock.json`
