# 架构总览 — DDD 模块化单体

## 目标

- **高度灵活、可扩展**，参考 ABP vNext 的模式（模块化、DDD 分层、仓储/规约抽象），但不硬性依赖 ABP 框架，除非所有者决定引入。
- 新增一门语言 = 数据 + 配置工作（见 `localization-data-design.md`）。
- 新增一种题型 = 实现一个接口 + 注册，课程引擎零改动。
- 每个用户只允许一个客户端在线，不可以同时登陆n和客户端


## 分层结构（参考 ABP，每个模块内六项目）
每个业务模块（Lexicon / LearningPath / Vocabulary / Exam / SentenceInteraction）在
Modules/{Module}/ 下拥有以下项目，依赖方向严格单向：

- {Module}.Domain.Shared：枚举、常量、错误码。不依赖任何项目。所有其他项目可引用。
- {Module}.Domain：聚合、实体、值对象、领域服务、仓储接口、领域事件。
  只引用 Domain.Shared。禁止引用 Application、Contracts、EF Core、ASP.NET Core。
- {Module}.Application.Contracts：应用服务接口 + DTO + 查询对象。
  只引用 Domain.Shared。**禁止引用 Domain**（这是 Blazor.Client 能安全引用的前提）。
- {Module}.Application：应用服务实现、DTO ↔ 领域映射。
  引用 Domain + Contracts。禁止引用 EntityFrameworkCore、ASP.NET Core。
- {Module}.EntityFrameworkCore：DbContext + 仓储实现 + 映射配置。
  引用 Domain + Domain.Shared。禁止引用 Application、Contracts。
- {Module}.HttpApi： API 端点。只引用 Contracts。

## 模块间规则
- 模块间零项目引用，**唯一例外**：可以引用对方模块的 .Application.Contracts。
- 禁止引用其他模块的 .Domain 或 .EntityFrameworkCore。
- 模块间状态变更走领域事件；查询走对方 Contracts 接口。

## Blazor WASM 的物理分离
- {Module}.Blazor.Host：纯 WASM 客户端。
  **只允许引用各模块的 .Application.Contracts 和 .Domain.Shared**。
  禁止引用 .Application、.Domain、.EntityFrameworkCore。
- {Module}.HttpApi.Host：唯一组装 Application + EntityFrameworkCore 的宿主。
- Blazor.Host 通过 HttpClient 调用 HttpApi， ***ApiClient(接口调用实现层) 不直接调用应用服务实现。
- Blazor.Shared 实现Page、Component、I***ApiClient(接口调用接口层)等

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
| `Exercises` | 题型注册表 + 五种题型；判分与反馈 |
| `Audio` | 按路径约定解析音频资产；播放抽象；TTS 批量离线工具 |
| `Learning` | SRS 调度器、学习进度、连胜/红心、复习队列 |
| `Exam` | 模拟官方考试、计时、分数段位 |
| `Theming` | 亮/暗主题令牌、响应式原语 |

共享内核：`Shared`（通用值对象如 `LanguageCode`、`VoiceRole`、语义键 `ContentKey`）。

## 关键模式（借鉴 ABP vNext）

- **模块插件**：每个模块暴露 `ConfigureServices` + `OnApplicationInitialization`；宿主负责组合。
- **仓储抽象**：领域定义 `IRepository<TAggregate>`；由 EF Core 实现。查询规约（specification）在应用层组合。
- **领域服务**：承载跨聚合逻辑（如 `InflectionResolver` 形态解析、`ExerciseGrader` 判分）。
- **领域事件**：处理副作用（如 `SentenceCompleted` → 更新 SRS 计划）。
- **工作单元（UoW）**：包裹每个用例（应用层）。

## 部署

- Blazor **WASM 独立部署**：产物为纯静态文件，可部署在任何静态服务器（IIS/nginx/对象存储/GitHub Pages）。
- 音频同样是按 URL 约定寻址的静态文件（见 `audio-asset-spec.md`）——应用只拼 URL，不做音频代理，第 3–5 阶段完全不需要后端。
- 本地优先（local-first）持久化方案待定（CONTEXT.md Q1）；架构已通过仓储接口将其隔离，后续 ADR 落地无需重写。
