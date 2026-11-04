# 编码规范

> 需求 12 与全仓质量规则。AGENTS.md § 1 是规范性摘要；本文件是细则。

## 语言规则

| 产出物 | 语言 |
|---|---|
| 标识符、项目、文件、目录、数据库列、语义键、API 路由 | 英文 |
| 代码注释与 XML 文档注释 | **中文** |
| 文档正文与标题 | 中文（其中代码元素名仍为英文） |
| 提交信息 | 英文，Conventional Commits（`feat:`、`docs:`、`fix:`、`refactor:`、`test:`、`chore:`） |

## C# 风格

- 启用可空引用类型 + 隐式 using；`TreatWarningsAsErrors = true`。
- 类型/成员 `PascalCase`，私有字段 `_camelCase`，局部变量/参数 `camelCase`；接口加 `I` 前缀。
- 值对象为带参数守卫的不可变 record；实体暴露行为方法，禁止公开 setter。
- 异步方法加 `Async` 后缀；CancellationToken 全程传递。
- 领域逻辑禁止 `DateTime.Now` / `Guid.NewGuid()`——时间/ID 抽象注入（可测试性）。

## Blazor/UI

- 一组件一文件；渲染期间参数不可变；事件回调用 `EventCallback<T>`。
- 所有展示文字来自本地化数据层（见 `localization-data-design.md`）——用户可见文案零硬编码。
- 颜色/间距只使用主题令牌（见 `ui-theming-responsive.md`）。

## 测试

- xUnit；命名模式 `Method_Condition_Expected`。
- 领域测试不得触碰 EF Core、JS interop 或网络。
- 判分、音频路径约定、形态解析必须有黄金测试覆盖边界（变音符号、冠词形式、对话补全规则）。

## 数据与资产

- 音频文件名严格遵循 `audio-asset-spec.md`；修改该规范须同一次提交完成迁移。
- 语义键：小写点分命名，绝不从任何语言文本推导。

## Git 卫生

- `main` 受保护；功能分支开发，PR 由所有者评审。
- 绝不提交机密（`.env`、`secrets.json` 已 git 忽略）；Azure 密钥只存在于音频生成工具的本地配置。
- 提交中不得包含构建产物、日志或 `TestResults/`——任何任务结束前检查 `git status`。
