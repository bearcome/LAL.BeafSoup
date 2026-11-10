# 解决方案骨架（规划目标，第 1 阶段产出）

> 本文使用下方唯一的模块化项目树。
路径中的 `{Module}`、`{Layer}` 和逗号分隔的层名称均为模板缩写，不是字面目录；例如 `Modules/Vocabulary/BeafSoup.Vocabulary.Domain/`。每个模块各自创建独立项目。
```
LAL.BeafSoup.sln
├── src/
│   ├── BeafSoup.SharedKernel/          # LanguageCode、VoiceRole、SemanticKey 等稳定共享类型
│   ├── Modules/
│   │   ├── Localization/{Localization}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Vocabulary/{Vocabulary}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Content/{Content}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Exercises/{Exercises}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Audio/{Audio}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Learning/{Learning}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Exam/{Exam}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   ├── Identity/{Identity}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   │   └── UsageAnalytics/{UsageAnalytics}.{Domain.Shared,Domain,Application.Contracts,Application,EntityFrameworkCore,HttpApi}/
│   ├── BeafSoup.HttpApi.Host/          # 组合服务端模块，不承载业务规则
│   ├── BeafSoup.Blazor.Client/         # 独立部署的静态 WASM 客户端
│   ├── BeafSoup.Blazor.Shared/         # 页面与组件，只依赖 Contracts/Domain.Shared
│   ├── BeafSoup.Blazor.Theming/        # 客户端主题与响应式支持库
│   └── BeafSoup.Audio.Generation/      # Azure TTS 离线生成工具
├── test/
│   ├── BeafSoup.Architecture.Tests/    # 模块依赖方向与程序集边界
│   ├── Modules/{Module}.Domain.Tests/  # 纯领域不变量
│   ├── Modules/{Module}.Application.Tests/
│   ├── BeafSoup.Api.Tests/             # 授权、持久化与 API 集成测试
│   └── BeafSoup.Browser.Tests/         # 核心端到端验收场景
└── docs/（即本目录）
```

## 脚手架规则

- 全项目启用 Nullable + 隐式 using；`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`。
- 每个模块的 Domain 只允许引用 `SharedKernel` 与本模块 `Domain.Shared`；不得引用其他模块 Domain 或基础设施。由架构测试强制约束。
- 模块间只通过对方 `Application.Contracts` 查询；状态变更通过稳定版本化领域事件，不通过跨模块对象引用。
- `HttpApi.Host` 是服务端组合入口；`Blazor.Client` 是唯一 WASM 入口，不引用 Domain、Application 或基础设施。
- 初始阶段只创建当前阶段所需模块项目，不提前生成未实现模块的空壳。
- 注释用中文；标识符用英文。

## 第 1 阶段实际落地（2026-11）

- 解决方案采用 .NET 11 新格式 `LAL.BeafSoup.slnx`。
- 仅创建 SharedKernel 与 Localization/Vocabulary/Content/Audio/Learning 的 `Domain.Shared` + `Domain`；Exercises 仅 `Domain.Shared`（题型注册键常量）。
- Application/EntityFrameworkCore/HttpApi 空壳未创建；Exam/Identity/UsageAnalytics 模块整体留待各自阶段。
- 另建 `src/BeafSoup.Audio.Generation` 离线 TTS 工具（允许引用各模块 Domain，不进入 WASM 产物）。

## 第 2 阶段实际落地（2026-11）

- 按 ADR-005 创建 Localization/Vocabulary/Content 的 `EntityFrameworkCore` 项目：每模块独立 `DbContext` 与迁移（独立迁移历史表），共享一个 SQLite 数据库文件；持久化行 + 显式映射器（`{Module}PersistenceMapper`），不直接把领域对象交给 EF 物化。
- 领域侧仅为持久化还原增加恢复工厂（`LocalizedText.Restore`、`LexicalConcept.Restore`、`Lexeme.Restore`），并由调用方按领域规则确定性生成概念/词元/范式槽位 Id，保证 `ParadigmCellId` 跨读写稳定。
- 另建 `src/BeafSoup.DbMigrator` 控制台（migrate / seed / reset / export-dictionary）与三个持久化测试项目、`test/BeafSoup.DbMigrator.Tests`。
- 已知坑：本仓库 .NET 11 RC SDK 的 `dotnet sln add` 对 `.slnx` 会误写条目，新增项目需手工编辑 `.slnx`。

## 第 3 阶段实际落地（2026-11）

- 新建 Identity / UsageAnalytics 模块：Identity 为完整六层（Domain.Shared/Domain/Application.Contracts/Application/EntityFrameworkCore/HttpApi）；UsageAnalytics 无 EF 层（在线/时长为 Identity 权威事实之上的只读投影，计算即投影、天然可重建）。
- Localization 补 Application.Contracts/Application/HttpApi（UI 文案与内容标题目录，`ui` + `content` 两个 domain）。
- 新建 `BeafSoup.HttpApi.Host`（唯一组合宿主：组合各模块 MVC Controller、JWT Bearer + sid 会话活性校验、统计权限默认拒绝、CORS、领域异常 → 稳定错误码）。
- 新建 `BeafSoup.Blazor.Client`（WASM 独立入口）/ `BeafSoup.Blazor.Shared`（页面、组件、ApiClient——只实现 Contracts 接口）/ `BeafSoup.Blazor.Theming`（主题令牌 + 主题服务）。WASM 兼容引用使用 `Microsoft.AspNetCore.Components.Web(.Authorization)` 包（不用 FrameworkReference，否则 browser-wasm RID 冲突）。
- 新增 NuGet 依赖（变更理由）：`Microsoft.AspNetCore.Authentication.JwtBearer`（令牌校验属安全敏感路径，不自研；与既有 Microsoft 系依赖同发布列车 10.0.14）、`Microsoft.IdentityModel.JsonWebTokens`（JWT 签发）、`Microsoft.AspNetCore.Mvc.Testing`（API 集成测试宿主，标准测试设施）、`Microsoft.AspNetCore.Components.Web[.Authorization]`（WASM 客户端库引用）。口令散列未引依赖：PBKDF2-SHA256 直接用 BCL `Rfc2898DeriveBytes`。
- 已知坑（沿用第 2 阶段）：`dotnet sln add` 对 `.slnx` 误写条目，新增项目手工编辑解决方案文件。

## 第 4 阶段实际落地（2026-11）

- Exercises 补 Domain（题型注册表 + 六种 handler + 版本化负载 + 复习关联声明）与 Application.Contracts/Application/EntityFrameworkCore/HttpApi；判分为服务端权威（负载声明的答案引用/可接受词形/正确顺序，绝不按显示文本推断）。
- Learning 补 Application.Contracts/Application/EntityFrameworkCore/HttpApi；Domain 增加 FSRS-4.5 调度核心（`FsrsScheduler`，参数经 `IFsrsParameterProvider` 隔离，Hard/Easy 系数为首版实现默认 0.85/1.15 待优化器校准）与 `LearnerProgressState`（红心/连胜）。
- Content 补 Application.Contracts/Application/HttpApi（课程树 + 课时习题引用，负载由 Exercises 按实例供给）。
- `LearningActivity` 增加 `IsCorrect`；`LearnerProfile` 增加弹层偏好（`InflectionDisplay`/`InspectorTrigger`）。
- Blazor.Shared 学习闭环：技能树、课时流程（逐题显示、即时判分、答对自动推进、结束总结）、六种题型组件、点词弹层（概念词典 K-V 缓存客户端）、背词模块（到期队列 + 新卡入队 + FSRS 评级）。
- 种子管线扩展：`ui-texts.json`（界面文案）、`exercises.json`（六题型示例习题，入库前经注册表全量校验）、课程/单元/课时标题本地化；`DbMigrator` 增加 Learning/Exercises 迁移与习题入库；词典导出含词元音频 URL。
- 测试：Exercises/Learning 领域测试、API 集成测试（`test/BeafSoup.Api.Tests`，WebApplicationFactory：学习闭环、授权边界 404、幂等重放、复习队列、统计口径）；架构测试扩展至全部新模块与 Blazor.Shared 引用边界。
