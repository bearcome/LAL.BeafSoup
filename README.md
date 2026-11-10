# LAL.BeafSoup

基于 **C# / .NET + Blazor WebAssembly（独立部署）** 的 B/S 架构多语言学习平台。

- **首个语言对**：中文（母语者）学习**德语（de-DE）**。
- **架构**：DDD 领域驱动设计、模块化单体、高度可扩展——设计模式参考 ABP vNext。
- **长期目标**：一个语义/一个单词对应 **N 种语言**；新增一门语言只是"数据 + 模块配置"的工作，而不是重写代码。
- **长期目标**：一个语义/一个单词对应 **N 种语言**；已有语言处理器覆盖时可通过数据与配置扩展，新的书写系统或形态规则须先实现并验证对应处理器。

> 仓库已完成第 1–4 阶段，当前进入第 5 阶段（模拟考试、加固、第二门语言验证）。

## 仓库结构

```
├── AGENTS.md            # AI 智能体工作规则与约束（须首先阅读）
├── CONTEXT.md           # 项目背景：愿景、技术栈、当前阶段、关键决策
├── docs/                # 全部设计文档（架构 / 领域 / 功能 / 规范）
├── media/               # 静态媒体资源（文档配图、示例音频等）
├── src/                 # 解决方案与各模块源码
└── test/                # 领域、持久化、架构与 API 集成测试
```

## 本地启动

前置条件：安装支持 `net11.0` 的 .NET 11 SDK。以下命令块均从**仓库根目录**开始执行；服务端与客户端使用两个独立终端。进入各项目目录后再运行 `dotnet run`，可确保项目内 `appsettings.json`、SQLite 文件和静态资源使用预期路径。

### 1. 首次初始化数据库与客户端词典

```bash
cd src/BeafSoup.DbMigrator
dotnet run -- seed --connection "Data Source=../BeafSoup.HttpApi.Host/beafsoup.db" --seed-dir ../../data/seed
dotnet run -- export-dictionary --connection "Data Source=../BeafSoup.HttpApi.Host/beafsoup.db" --output ../BeafSoup.Blazor.Client/wwwroot/dictionary.json
```

`seed` 会显式执行全部模块迁移并填充示例数据，不会在宿主启动时自动修改数据库（ADR-005）。后续只需要应用新迁移时，执行：

```bash
cd src/BeafSoup.DbMigrator
dotnet run -- migrate --connection "Data Source=../BeafSoup.HttpApi.Host/beafsoup.db"
```

### 2. 启动 API 服务端

```bash
cd src/BeafSoup.HttpApi.Host
dotnet run
```

- API 地址：`http://localhost:5080/`
- 匿名健康检查：`http://localhost:5080/health`

### 3. 启动 Blazor WebAssembly 客户端

在另一个终端中执行：

```bash
cd src/BeafSoup.Blazor.Client
dotnet run
```

浏览器访问 `http://localhost:5280/`。客户端 `wwwroot/appsettings.json` 中的 `ApiBaseUrl` 已指向上述服务端；服务端 `appsettings.json` 的 `Cors:AllowedOrigins` 已允许该客户端开发来源。修改服务端端口时，同步更新宿主 `Urls`、客户端 `ApiBaseUrl` 和宿主 `Properties/launchSettings.json`；修改客户端端口时，同步更新 `Cors:AllowedOrigins` 和客户端 `Properties/launchSettings.json`。

## 文档导航

| 文档 | 内容 |
|---|---|
| [CONTEXT.md](CONTEXT.md) | 项目是什么、技术栈、阶段状态 |
| [AGENTS.md](AGENTS.md) | 所有 AI/人类贡献者必须遵守的规则 |
| [docs/roadmap.md](docs/roadmap.md) | 分阶段交付计划 |
| [docs/architecture/overview.md](docs/architecture/overview.md) | DDD 分层与模块划分 |
| [docs/architecture/solution-skeleton.md](docs/architecture/solution-skeleton.md) | 规划的 .sln / 项目布局 |
| [docs/architecture/adr/](docs/architecture/adr/README.md) | 架构决策记录（ADR） |
| [docs/domain/model.md](docs/domain/model.md) | 领域模型与聚合设计 |
| [docs/domain/localization-data-design.md](docs/domain/localization-data-design.md) | 多语言数据设计（语义键） |
| [docs/domain/audio-asset-spec.md](docs/domain/audio-asset-spec.md) | 音频文件命名、路径与音色规则 |
| [docs/features/feature-catalog.md](docs/features/feature-catalog.md) | 功能总目录（需求 1–14 映射） |
| [docs/features/account-and-usage-statistics.md](docs/features/account-and-usage-statistics.md) | 注册、访问策略、学习数据归属与使用统计 |
| [docs/features/acceptance-scenarios.md](docs/features/acceptance-scenarios.md) | 核心用户流程的 BDD 验收场景 |
| [docs/conventions/coding-conventions.md](docs/conventions/coding-conventions.md) | 命名规范、语言规则（英文设计/中文注释）、验证要求 |
