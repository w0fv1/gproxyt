# gproxyt

`gproxyt` 是 Windows ChatGPT 桌面应用的独立代理启动器。它不修改 Windows 系统代理，通过 Windows 程序包身份启动 ChatGPT，并为 Chromium 网络栈传入代理参数。

默认代理：

```text
http://127.0.0.1:7890
```

## 前置条件

- Windows 10 或 Windows 11 x64
- Microsoft Store 当前版 ChatGPT/Codex 统一桌面应用
- 本地 Clash HTTP 或 mixed 代理端口

不需要管理员权限。gproxyt 通过 Windows 程序包身份和 AUMID 激活官方应用，不直接执行受保护的 `WindowsApps` 文件。

官方应用可以使用以下命令安装：

```powershell
winget install Codex -s msstore
```

## 使用

运行 `gproxyt.exe`，点击窗口中央的启动按钮。

普通窗口模式为当前 Windows 会话单实例。再次运行时不会创建第二个窗口，而是还原并置前已经打开的 gproxyt。

右上角设置按钮可以修改代理 URL 和开机自启。开机自启只写入当前用户的 Windows Run 项，并使用 `--launch` 直接通过代理启动 ChatGPT。

命令行直接启动：

```powershell
gproxyt.exe --launch
```

需要诊断启动问题时添加 `--debug`。日志会写入命令运行时的当前目录，文件名格式为 `gproxyt-debug-yyyyMMdd-HHmmss-PID.log`：

```powershell
gproxyt.exe --debug --launch
```

命令行创建快捷方式：

```powershell
gproxyt.exe --create-shortcut
```

配置保存在：

```text
%APPDATA%\gproxyt\settings.json
```

## 构建

运行：

```powershell
.\build.ps1
```

构建流程先执行全部测试，再发布自包含的 Windows x64 单文件版本：

```text
dist\gproxyt.exe
```

## 发布

运行：

```powershell
.\release.ps1
```

发布脚本会构建 `gproxyt.exe`，计算 SHA-256 和 SHA-512，通过 Nfirco 发布 API 上传公开版本，并回下载线上文件校验哈希。

下载地址：

```text
https://next.firco.cn/release/gproxyt
```

## 代理范围

启动器传入 Chromium 的 `--proxy-server` 与 loopback bypass 参数，使 ChatGPT 的 Chromium 网络请求使用指定代理。它不修改系统代理和用户环境变量。

启动前关闭进程时，只匹配 `OpenAI.Codex` Store 包族身份，并覆盖更新前后的所有版本；不会终止 VS Code 扩展或其他位置的 Codex 进程。安装位置只从当前用户已注册的主程序包解析，不会误用仅完成 Stage 的待更新版本。
