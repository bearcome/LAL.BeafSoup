# ADR-007：在线心跳、会话租约与统计口径参数

- 状态：**已接受**
- 日期：2026-11-09

## 背景

CONTEXT.md Q5 要求在第 3 阶段前决定在线心跳/租约参数。账户与统计规范规定：在线状态由 `LastHeartbeatAt + SessionLeaseTtl` 判断；心跳周期、租约时长、最大计时窗口均为服务端配置；服务端使用 UTC；间隔超过最大计时窗口不追补；在线用户数按不同 `UserId` 去重；统计不得依赖 WebSocket。

## 决策

初始参数（服务端配置，键见 `UsageAnalytics` 宿主配置节）：

| 参数 | 初始值 | 含义 |
|---|---|---|
| `HeartbeatInterval` | 30 秒 | 客户端心跳发送周期（提示值，服务端不依赖客户端守时） |
| `SessionLeaseTtl` | 90 秒 | 租约时长：在线判据 = `LastHeartbeatAtUtc + Ttl > now` |
| `MaxTimingWindow` | 300 秒 | 时长累计的最大心跳间隔：超出部分不追补 |
| 时区 | UTC | 全部统计口径使用服务端 UTC；客户端时间戳不参与 |

- 心跳由服务端在接受时以服务端时钟更新 `LastHeartbeatAtUtc` 并累计在线时长（本次间隔按 `min(间隔, MaxTimingWindow)` 计入）。
- 会话结束以租约过期收敛；客户端崩溃/断网不发送结束事件。
- 在线用户数 = 在线租约内不同 `UserId` 数；在线会话数 = 租约内不同 `SessionId` 数。
- 累计注册数/有效账户数来自 Identity 注册事实与账户状态（排除测试账户）；停用不减少累计注册数。

## 后果

- 参数可在服务端配置调整，无需发版；修改口径属统计定义变更，须同步更新 `docs/features/account-and-usage-statistics.md`。
- 在线时长是"心跳覆盖时长估算"，不得宣传为精确专注时长（规范原文要求）。
