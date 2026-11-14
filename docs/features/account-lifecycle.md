# 账户生命周期与合规

> 需求 14 的延伸（见 [feature-catalog.md](feature-catalog.md)），决策见 [ADR-008](../architecture/adr/ADR-008-account-lifecycle-and-retention.md)。

## 邮箱验证

- 注册后自动发送 6 位验证码（30 分钟有效、一次性、散列存储、最多 5 次错误尝试）。
- 部署开关 `Identity:RequireVerifiedEmailForLogin`（默认 `true`）：启用时注册不签发令牌，未验证登录返回 `Identity:EmailNotVerified`（HTTP 403）。
- 客户端：注册成功进入 `/verify-email?email=...`；未验证登录在登录页内直接提供“重发验证码”和验证入口。
- 发件：`IEmailSender` 抽象；仓库内仅提供开发实现 `DevelopmentEmailSender`（Development/Testing 环境），生产必须由宿主接入真实邮件服务。
- 开发取码：`GET /api/dev/email-outbox`（仅 Development/Testing）返回内存发件箱，便于本地与自动化测试。

## 密码找回

1. `/forgot-password` 提交邮箱 → 服务端始终返回 202（不泄露账户是否存在）。
2. 账户存在且有效时发送重置验证码（与验证码共用令牌机制）。
3. `/reset-password?email=...` 输入验证码与新密码 → 设置新密码、吊销全部会话、标记邮箱已验证、返回成功并引导重新登录。

## 账户注销

- 设置页“危险区域”：重新输入密码确认后调用 `POST /api/identity/account/delete`。
- 服务端删除身份关联、凭据、令牌、会话与全部学习数据；账户行标记为 `Deleted`，仅保留匿名注册事实。
- 注销后同一邮箱可重新注册；旧令牌立即失效。客户端清空本地缓存并回到首页。

## 数据导出

- 设置页“导出我的数据”调用 `GET /api/identity/account/export`，浏览器下载 JSON 文件。
- 内容：账户基本信息、学习配置、学习活动、复习卡、形态掌握度。不含凭据散列、令牌、会话标识与他人数据。

## 隐私政策与服务条款

- 路由 `/privacy`、`/terms`；标题与正文来自本地化数据层（`ui.legal.*`），支持中/德/英三语。
- 隐私政策列出数据类别、保留期（见 ADR-008 §5）、注销与导出方式。
- 注册页底部与设置页提供入口。

## 数据保留期清理

- 命令：`dotnet run --project src/BeafSoup.DbMigrator -- purge --connection "..."`（幂等）。
- 删除：结束时间早于 90 天的会话记录、早于 7 天的已使用/过期账户令牌。
- 不删除：有效会话、未使用令牌、学习活动与复习状态（账户存活期间必须保留，否则进度无法重建）。
- 生产环境需用定时任务调度该命令。

## 客户端页面

| 路由 | 用途 |
|---|---|
| `/verify-email?email=` | 输入验证码完成验证并自动登录 |
| `/forgot-password` | 申请密码重置 |
| `/reset-password?email=&code=` | 设置新密码（可从邮件链接直接带入） |
| `/privacy`、`/terms` | 隐私政策与服务条款 |
| `/settings` | 账户状态、重发验证、数据导出、注销 |

## 安全约束

- 验证码与重置码只以散列存储；接口响应不回显验证码。
- 登录/找回接口对不存在的账户返回与存在账户相同的外部响应。
- 注销要求重新输入密码；导出与注销都只作用于当前令牌对应的账户。
