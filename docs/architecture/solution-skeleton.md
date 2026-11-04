# 解决方案骨架（规划目标，第 1 阶段产出）

> 目前尚无代码——这是第 1 阶段脚手架必须产出的目标布局。项目名用英文（遵循 AGENTS.md）。

```
LAL.BeafSoup.sln
├── src/
│   ├── BeafSoup.SharedKernel/            # LanguageCode、VoiceRole、ContentKey、参数守卫
│   ├── BeafSoup.Domain/             # 模拟考试聚合、SRS 调度器、进度、连胜、AudioAsset、路径约定、音色角色、题型注册表、Exercise 基类、判分、Course、SkillUnit、Lesson、Sentence、Lemma（词元）、InflectionForm（形态）、词-句互链、SemanticKey、LocalizedText 聚合
│   ├── BeafSoup.Domain.Shared/           # 跨模块领域契约/事件
│   ├── BeafSoup.Application.Contracts/   # DTO + 服务接口（面向 UI）
│   ├── BeafSoup.Application/             # 用例、题型引擎、本地化查询
│   ├── BeafSoup.EntityFrameworkCore/     # 仓储实现、DbContext、迁移
│   ├── BeafSoup.Audio.Generation/        # Azure TTS 离线批量生成工具（控制台）
│   ├── BeafSoup.Blazor.Host/              # Blazor WASM 独立部署宿主 
│   ├── BeafSoup.Blazor.Shared/           # 组件、Page等
│   └── BeafSoup.Blazor.Theming/          # 亮/暗主题令牌、响应式原语
├── test/
│   ├── BeafSoup.Domain.Tests/            # xUnit，纯领域不变量
│   ├── BeafSoup.Application.Tests/
│   ├── BeafSoup.Exercises.Tests/
│   └── BeafSoup.Audio.Tests/             # 路径约定与 SSML 内容规则
└── docs/（即本目录）
```

## 脚手架规则

- 全项目启用 Nullable + 隐式 using；`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`。
- 领域项目只允许引用 `SharedKernel` 与 `Domain.Shared`——第 1 阶段用架构测试强制约束。
- `Blazor` 项目是唯一 WASM 入口；只引用 `Application.Contracts` 与 Theming，不直接引用基础设施。
- 注释用中文；标识符用英文。
