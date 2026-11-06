# 路线图

## 第 0 阶段 — 骨架与文档（当前）

- [x] 仓库骨架：`src/`、`test/`、`docs/`、`media/`
- [x] `README.md`、`CONTEXT.md`、`AGENTS.md`、`.gitignore`
- [x] 架构文档（DDD 分层、规划解决方案布局、ADR 决策记录）
- [x] 领域文档（领域模型、多语言数据设计、音频资产规范）
- [x] 功能规格（总目录、题型、背词模块、点词弹层、UI 主题与响应式、账号与统计、BDD 场景）
- [x] 编码规范
- [ ] 所有者评审与确认

## 第 1 阶段 — 解决方案骨架与领域核心

- [x] 按 `docs/architecture/solution-skeleton.md` 创建 `.sln` 与项目布局（实际为 .NET 11 新格式 `.slnx`）
- [x] 领域层：按 `docs/domain/model.md` 实现聚合与值对象（不含存储、不含 UI；仅建各模块 Domain.Shared + Domain）
- [x] 领域不变量单元测试（歧义词形检索、翻译修订导致的标注失效、Lemma 级 SRS 卡与形态掌握度、语义键唯一性、音频路径生成）
- [x] 架构测试：模块依赖方向（NetArchTest）
- [x] CI：`dotnet build` 零警告 + `dotnet test`
- 附带回环产物：`BeafSoup.Audio.Generation` 离线工具 + `data/seed/` 每种数据 3 条测试数据 + `media/audio/` 18 个生成资产（原计划第 2 阶段的管线部分，提前落地；数据库 Schema 仍属第 2 阶段）

## 第 2 阶段 — 内容与本地化数据层

- [x] 数据库供应商与备份/迁移策略 ADR（[ADR-005](architecture/adr/ADR-005-sqlite-authority-database.md)，SQLite）
- [x] 按 `docs/domain/localization-data-design.md` 与 `docs/architecture/database-schema.md` 建立数据库 Schema（Localization/Vocabulary/Content 三模块 DbContext + EF 迁移）
- [x] 内容入库管线：`BeafSoup.DbMigrator`（migrate / seed / reset）+ 持久化往返集成测试
- [x] 离线音频生成管线，按 `docs/domain/audio-asset-spec.md` 执行（第 1 阶段提前落地，本轮同步名词带冠词朗读规则）；Azure TTS 免费额度；de-DE 配置 3 个音色角色
- [ ] 中文学德语完整课程数据（当前为每种数据 3 条的种子测试数据，正式内容填充列入后续迭代）

## 第 3 阶段 — Blazor WASM 外壳

- 独立部署 WASM 应用、模块加载、路由
- 亮/暗主题（`docs/features/ui-theming-responsive.md`）、响应式布局
- 本地化管道（界面文案全部来自数据层）
- 接入独立 `HttpApi.Host`、账户注册/身份验证与使用统计；遵循 `docs/features/account-and-usage-statistics.md`。

## 第 4 阶段 — 学习功能

- 课程引擎 + 题型注册表；落地 `docs/features/exercise-types.md` 中的六种题型（含对话填空音频规则）
- 点词弹层（`docs/features/word-inspector-popover.md`）
- 带 SRS 的背单词模块（`docs/features/vocabulary-trainer.md`）
- 多邻国式学习闭环（`docs/features/feature-catalog.md`）
- 学习配置、学习活动、FSRS 卡片、形态掌握度及考试记录均通过 API 按账户隔离持久化。

## 第 5 阶段 — 考试、加固、第二门语言

- 模拟考试模块（歌德/telc 风格）
- 性能与体验打磨、移动端验收
- 验证语言扩展承诺：以已有处理器支持的第二门语言完成纯数据/配置接入；未覆盖的语言处理能力必须先实现并通过专属测试，不能降级为错误但静默的通用规则。

## 里程碑验收

每个阶段以 `AGENTS.md` § 3 的质量门禁结束，外加该阶段功能文档中列出的专项检查。
