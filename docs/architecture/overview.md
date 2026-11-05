# 架构总览 — DDD 模块化单体

## 目标

- **高度灵活、可扩展**，参考 ABP vNext 的模式（模块化、DDD 分层、仓储/规约抽象），但不硬性依赖 ABP 框架，除非所有者决定引入。
- 已有语言处理器覆盖的新语言 = 数据 + 配置工作；新的书写系统或形态规则须先通过语言处理器扩展并验证（见 `localization-data-design.md`）。
- 新增一种题型 = 实现一个接口 + 注册，课程引擎零改动。
- 普通有效注册用户初期可使用全部学习功能；账户数据隔离、身份验证及管理员操作保护始终生效。
- 每个账户的并发交互会话数由服务端策略控制，初始默认最多一个。


## 分层结构（参考 ABP，每个模块内六项目）
每个业务模块（Localization / Vocabulary / Content / Exercises / Audio / Learning / Exam / Identity / UsageAnalytics）在
Modules/{Module}/ 下拥有以下项目，依赖方向严格单向：

- {Module}.Domain.Shared：本模块枚举、常量、错误码，只引用 SharedKernel。
- {Module}.Domain：聚合、实体、值对象、领域服务、仓储接口、领域事件。
  只引用 SharedKernel 与本模块 Domain.Shared。禁止引用其他模块 Domain、Application、EF Core、ASP.NET Core。
- {Module}.Application.Contracts：应用服务接口 + DTO + 查询对象。
  只引用 SharedKernel 与本模块 Domain.Shared。**禁止引用任何 Domain**（这是 Blazor.Client 能安全引用的前提）。
- {Module}.Application：应用服务实现、DTO ↔ 领域映射。
  引用本模块 Domain + Contracts。禁止引用 EntityFrameworkCore、ASP.NET Core。
- {Module}.EntityFrameworkCore：DbContext + 仓储实现 + 映射配置。
  只引用本模块 Domain + Domain.Shared。禁止引用 Application、Contracts。
- {Module}.HttpApi： API 端点。只引用 Contracts。

## 模块间规则
- 模块间零项目引用，**唯一例外**：可以引用对方模块的 .Application.Contracts。
- 禁止引用其他模块的 .Domain 或 .EntityFrameworkCore。
- 模块间状态变更走领域事件；查询走对方 Contracts 接口。

## Blazor WASM 的物理分离
- `BeafSoup.Blazor.Client`：纯 WASM 客户端。
  **只允许引用各模块的 .Application.Contracts 和 .Domain.Shared**。
  禁止引用 .Application、.Domain、.EntityFrameworkCore。
- `BeafSoup.HttpApi.Host`：唯一组合服务端 Application、HttpApi 与 EntityFrameworkCore 模块的宿主。
- Blazor 客户端通过 HttpClient 调用 API；`ApiClient` 只实现 Contracts 中定义的客户端接口，不直接调用应用服务实现。
- `BeafSoup.Blazor.Shared` 存放页面和组件，不包含领域或应用业务逻辑。

## 数据访问
- 仓储接口在 Domain，实现在 EntityFrameworkCore。
- Application 层只依赖仓储接口，**禁止注入 DbContext**。
- Domain 层禁止出现任何 EF Core 特性或基类。

## 模块划分（垂直切片，每个模块内含自己的分层）

| 模块 | 职责 |
|---|---|
| `Localization` | 语义键 ↔ N 语言版本；基础语言解释查询 |
| `Vocabulary` | 词元（原型）/ 形态变化 / 词与句的互链 |
| `Content` | 课程、技能单元、课时、句子、对话、短文（由句子拼接） |
| `Exercises` | 题型注册表 + 六种题型；判分与反馈 |
| `Audio` | 按路径约定解析音频资产；播放抽象；TTS 批量离线工具 |
| `Learning` | SRS 调度器、学习进度、连胜/红心、复习队列 |
| `Exam` | 模拟官方考试、计时、分数段位 |
| `Identity` | 注册账户、会话与服务端身份/授权策略边界 |
| `UsageAnalytics` | 注册数、在线用户/会话数、在线时长只读投影 |
| `Theming` | 亮/暗主题令牌、响应式原语（客户端支持库，不是 DDD 业务模块） |

共享内核：`BeafSoup.SharedKernel`（通用值对象如 `LanguageCode`、`VoiceRole`、`SemanticKey`）。

## 关键模式（借鉴 ABP vNext）

- **模块插件**：每个模块暴露 `ConfigureServices` + `OnApplicationInitialization`；宿主负责组合。
- **仓储抽象**：领域定义 `IRepository<TAggregate>`；由 EF Core 实现。查询规约（specification）在应用层组合。
- **领域服务**：承载跨聚合逻辑（如 `InflectionResolver` 形态解析、`ExerciseGrader` 判分）。
- **领域事件**：处理副作用（如 `ExerciseAnswered` → 更新学习事实与适用的 SRS 卡；注册/会话事件 → 更新统计投影）。统计投影不得反向成为用户或学习状态的权威来源。
- **工作单元（UoW）**：包裹每个用例（应用层）。

## 部署

- Blazor **WASM 独立部署**：客户端产物仍为纯静态文件，可部署在任意静态服务器；“独立”指不由 ASP.NET Core 托管客户端，不代表整个产品没有 API 服务。
- 音频按 URL 约定作为静态文件（见 `audio-asset-spec.md`），客户端不做音频代理。
- 注册、跨设备可信身份、集中注册数及在线统计必须使用独立部署的 `HttpApi.Host` 与服务端存储；服务端不得信任客户端上报的注册、心跳时间或累计时长。
- 账户、学习配置、学习活动、复习状态和考试记录均以 API 服务端持久化为权威来源；浏览器存储仅为可丢弃缓存，首发不支持离线提交学习结果（ADR-004）。
- 具体数据库供应商、身份提供方和备份策略须经对应 ADR 批准后实施，不得由实现者自行选型。
