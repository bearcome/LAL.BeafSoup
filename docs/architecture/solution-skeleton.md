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
