# gproxyt

`gproxyt` 是 Windows ChatGPT 桌面应用的进程级代理启动器。它不修改 Windows 系统代理，只为新启动的 ChatGPT 进程树注入代理环境变量和 Chromium 代理参数。

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

右上角设置按钮可以修改代理 URL 和开机自启。开机自启只写入当前用户的 Windows Run 项，并使用 `--launch` 直接通过代理启动 ChatGPT。

命令行直接启动：

```powershell
gproxyt.exe --launch
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

启动器在程序包激活期间设置 `HTTP_PROXY`、`HTTPS_PROXY`、`ALL_PROXY`、`NO_PROXY` 和 `NODE_USE_ENV_PROXY`，并传入 Chromium 的 `--proxy-server` 与 loopback bypass 参数。Windows 环境变量名不区分大小写，因此使用小写变量名读取时等价。ChatGPT 后续创建的内置 Codex 等子进程会继承同一代理环境。激活完成后会立即恢复程序包调试状态，不修改系统代理和用户环境变量。

启动前关闭进程时，只匹配 `OpenAI.Codex` Store 包族身份，并覆盖更新前后的所有版本；不会终止 VS Code 扩展或其他位置的 Codex 进程。安装位置只从当前用户已注册的主程序包解析，不会误用仅完成 Stage 的待更新版本。
