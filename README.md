# LAL.BeafSoup

基于 **C# / .NET + Blazor WebAssembly（独立部署）** 的 B/S 架构多语言学习平台。

- **首个语言对**：中文（母语者）学习**德语（de-DE）**。
- **架构**：DDD 领域驱动设计、模块化单体、高度可扩展——设计模式参考 ABP vNext。
- **长期目标**：一个语义/一个单词对应 **N 种语言**；新增一门语言只是"数据 + 模块配置"的工作，而不是重写代码。
- **长期目标**：一个语义/一个单词对应 **N 种语言**；已有语言处理器覆盖时可通过数据与配置扩展，新的书写系统或形态规则须先实现并验证对应处理器。

> 仓库当前处于 **第 0 阶段——仅有文档与骨架，尚无任何正式代码**。

## 仓库结构

```
├── AGENTS.md            # AI 智能体工作规则与约束（须首先阅读）
├── CONTEXT.md           # 项目背景：愿景、技术栈、当前阶段、关键决策
├── docs/                # 全部设计文档（架构 / 领域 / 功能 / 规范）
├── media/               # 静态媒体资源（文档配图、示例音频等）
├── src/                 # 未来源码目录（按设计目前刻意留空）
└── test/                # 未来测试目录（按设计目前刻意留空）
```

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
