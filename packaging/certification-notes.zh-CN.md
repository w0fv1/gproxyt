# GProxyT Microsoft Store 认证备注

GProxyT 是一个完整信任的 WPF 桌面应用，用于通过用户提供的代理启动 Microsoft Store ChatGPT。测试需要 Microsoft Store ChatGPT 和认证专用 Clash 订阅；订阅仅提供网络连接，不包含可执行代码。

测试步骤：

1. 从 Microsoft Store 安装当前版 ChatGPT/Codex 统一桌面应用。
2. 从 GitHub 官方发布页安装 Clash Verge Rev Windows x64 稳定版：`https://github.com/clash-verge-rev/clash-verge-rev/releases/latest`。
3. 在 Clash Verge Rev 中导入认证备注提供的专用订阅 URL：`{{CERTIFICATION_SUBSCRIPTION_URL}}`。
4. 将 Mixed Port 设置为 `7890`，切换到 Global 模式，并选择认证备注指定的日本节点。无需启用 TUN 或系统代理。
5. 确认 Clash Verge Rev 正在运行，然后启动 GProxyT。主页面应显示 `Current proxy: http://127.0.0.1:7890`。
6. 点击中央启动按钮。GProxyT 检查本地代理端口，关闭已有的目标 ChatGPT 程序包进程，并使用 Windows 程序包身份和代理参数重新激活 ChatGPT。

提交到 Partner Center 时必须将占位符替换为认证专用订阅 URL，并填写经过验证的日本节点完整名称。认证凭据不得提交到代码仓库或公开商店资料。

`runFullTrust` 用于枚举当前用户注册的 ChatGPT 程序包、终止该目标程序包的已有进程、通过 AUMID 激活 ChatGPT，以及检测本地代理端口。应用不会请求管理员权限，不安装驱动或服务，也不会修改 Windows 系统代理。

程序包声明的 `windows.startupTask` 对应设置中的“开机自启”。该功能默认关闭，用户启用后只在登录 Windows 时运行 `gproxyt-startup.exe`，随后通过代理启动 ChatGPT 并退出。

应用不要求账户，不收集或上传个人数据。代理配置只保存在当前用户本机。调试日志仅在用户显式使用 `--debug` 时生成，不会自动上传。

GProxyT 是独立的第三方工具，与 OpenAI 不存在隶属、赞助或认可关系。
