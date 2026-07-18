# GProxyT Microsoft Store 发布资料

本文档是 GProxyT Microsoft Store 产品身份、提交参数和商店链接的项目内记录。银行账户、税表签名和登录凭据不进入仓库。

## 产品

| 字段 | 值 |
| --- | --- |
| 产品名称 | GProxyT |
| 发布者显示名称 | LaiqiInfo |
| Store ID | `9P3M0G0LP9ZD` |
| Microsoft Store URL | <https://apps.microsoft.com/detail/9P3M0G0LP9ZD> |
| Store 协议链接 | `ms-windows-store://pdp/?productid=9P3M0G0LP9ZD` |
| 中国区目标零售价 | ¥0.99 |
| 类别 | 实用工具与工具 |
| 官网 | <https://next.firco.cn/gproxyt> |
| 隐私政策 | <https://next.firco.cn/gproxyt/privacy> |
| 使用条款 | <https://next.firco.cn/gproxyt/terms> |
| 支持页面 | <https://next.firco.cn/gproxyt> |

## 程序包身份

| Manifest 字段 | 值 |
| --- | --- |
| `Package/Identity/Name` | `LaiqiInfo.GProxyT` |
| `Package/Identity/Publisher` | `CN=F5A5F8E6-B2BB-4B41-9DFE-7079CA6B44A4` |
| `Package/Properties/PublisherDisplayName` | `LaiqiInfo` |
| Package Family Name | `LaiqiInfo.GProxyT_1a3eb8madkayp` |
| Package SID | `S-1-15-2-2612711666-364666383-1667947384-294833611-3515809450-1857838861-3452201325` |
| MSA 应用 ID | `ef53563c-6314-43b2-8cc0-8471d94316ba` |

`packaging/Package.appxmanifest` 是程序包身份的可执行单一真相源；本表用于 Partner Center 核对，不建立第二个生成入口。

## 首次商店提交

| 字段 | 值 |
| --- | --- |
| Partner Center Submission ID | `1152921505701431722` |
| 提交日期 | 2026-07-17 |
| 应用版本 | `1.2.0-stable` |
| Store 程序包版本 | `1.2.0.0` |
| 程序包 | `GProxyT_1.2.0.0_x64.msix` |
| 架构 | x64 |
| IARC 年龄分级 | 3+ |
| 发布方式 | 认证通过后自动发布 |
| 提交时状态 | 正在认证 |

首次提交包含四张中文桌面截图、完整信任能力说明、开机启动说明、隐私政策和认证测试步骤。商店产品文案维护在 `store-listing.zh-CN.md`，认证人员说明维护在 `certification-notes.zh-CN.md`。

## 1.6.0 重新提交

| 字段 | 值 |
| --- | --- |
| Partner Center Submission ID | `1152921505701431722` |
| 更新日期 | 2026-07-18 |
| 应用版本 | `1.6.0-stable` |
| Store 程序包版本 | `1.6.0.0` |
| 程序包 | `GProxyT_1.6.0.0_x64.msix` |
| 架构 | x64 |
| Store 一览语言 | 20 |
| 程序包语言 | 20 |

本次更新使用 GProxyT 自有品牌图标替换第三方产品图像，应用顶栏使用同一图标的纯黑剪影。Store 一览仅保留 `store-assets/zh-CN/01-main-light.png` 这一张当前版本桌面截图，并复用于 20 个语言页面。

## 收款与税务

Microsoft Store China Seller ID 为 `95335590`。W-8BEN-E、企业银行转账资料、销售收益和广告收益的 CNY 分配已于 2026-07-17 提交，提交后状态为“挂起的 Microsoft 验证”。Partner Center 最长可能需要 48 小时同步验证结果。

仓库只记录流程状态，不保存统一社会信用代码、联系人、银行账号、联行号、税务签名或账户凭据。

## 发布维护

1. 修改 `src/Gproxyt/Gproxyt.csproj` 中的语义版本。
2. 运行 `./test.ps1`。
3. 运行 `./store-build.ps1` 生成 MSIX。
4. 使用本文件核对 Partner Center 产品与程序包身份。
5. 使用 `store-listing.zh-CN.md`、`certification-notes.zh-CN.md` 和 `store-assets/zh-CN` 更新提交资料。
6. 认证通过后记录新的 Submission ID、程序包版本和提交日期。
