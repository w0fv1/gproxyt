# GProxyT Microsoft Store 认证备注

GProxyT 是一个完整信任的 WPF 桌面应用，用于通过用户提供的 HTTP 代理启动 Microsoft Store ChatGPT。

测试步骤：

1. 从 Microsoft Store 安装当前版 ChatGPT/Codex 统一桌面应用。
2. 确保 `http://127.0.0.1:7890` 存在可访问的 HTTP 或 mixed 代理，或在 GProxyT 设置中填写其他代理 URL。
3. 启动 GProxyT。
4. 点击中央启动按钮。
5. GProxyT 检查代理连通性，关闭已有的目标 ChatGPT 程序包进程，并使用 Windows 程序包身份和代理参数重新激活 ChatGPT。

`runFullTrust` 用于枚举当前用户注册的 ChatGPT 程序包、终止该目标程序包的已有进程、通过 AUMID 激活 ChatGPT，以及检测本地代理端口。应用不会请求管理员权限，不安装驱动或服务，也不会修改 Windows 系统代理。

程序包声明的 `windows.startupTask` 对应设置中的“开机自启”。该功能默认关闭，用户启用后只在登录 Windows 时运行 `gproxyt-startup.exe`，随后通过代理启动 ChatGPT 并退出。

应用不要求账户，不收集或上传个人数据。代理配置只保存在当前用户本机。调试日志仅在用户显式使用 `--debug` 时生成，不会自动上传。

GProxyT 是独立的第三方工具，与 OpenAI 不存在隶属、赞助或认可关系。
