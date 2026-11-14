# 路线图

## 第 0 阶段 — 骨架与文档（当前）

- [x] 仓库骨架：`src/`、`test/`、`docs/`（多媒体生成产物现直接输出至 Blazor 客户端 `wwwroot/media/`）
- [x] `README.md`、`CONTEXT.md`、`AGENTS.md`、`.gitignore`
- [x] 架构文档（DDD 分层、规划解决方案布局、ADR 决策记录）
- [x] 领域文档（领域模型、多语言数据设计、音频资产规范）
- [x] 功能规格（总目录、题型、背词模块、点词弹层、UI 主题与响应式、账号与统计、BDD 场景）
- [x] 编码规范
- [ ] 所有者评审与确认

## 第 1 阶段 — 解决方案骨架与领域核心

- [x] 按 `docs/architecture/solution-skeleton.md` 创建 `.sln` 与项目布局（实际为 .NET 11 新格式 `.slnx`）
- [x] 领域层：按 `docs/domain/model.md` 实现聚合与值对象（不含存储、不含 UI；仅建各模块 Domain.Shared + Domain）
- [x] 领域不变量单元测试（歧义词形检索、翻译修订导致的标注失效、概念级 SRS 卡与范式槽位掌握度、语义键唯一性、音频路径生成）
- [x] 架构测试：模块依赖方向（NetArchTest）
- [x] CI：`dotnet build` 零警告 + `dotnet test`
- 附带回环产物：`BeafSoup.Audio.Generation` 离线工具 + `data/seed/` 每种数据 3 条测试数据 + `wwwroot/media/audio/` 18 个生成资产（随客户端静态部署，音频基路径经客户端 `MediaBaseUrl` 配置）（原计划第 2 阶段的管线部分，提前落地；数据库 Schema 仍属第 2 阶段）

## 第 2 阶段 — 内容与本地化数据层

- [x] 数据库供应商与备份/迁移策略 ADR（[ADR-005](architecture/adr/ADR-005-sqlite-authority-database.md)，SQLite）
- [x] 按 `docs/domain/localization-data-design.md` 与 `docs/architecture/database-schema.md` 建立数据库 Schema（Localization/Vocabulary/Content 三模块 DbContext + EF 迁移）
- [x] 内容入库管线：`BeafSoup.DbMigrator`（migrate / seed / reset）+ 持久化往返集成测试
- [x] 离线音频生成管线，按 `docs/domain/audio-asset-spec.md` 执行（第 1 阶段提前落地，本轮同步名词带冠词朗读规则）；Azure TTS 免费额度；de-DE 配置 3 个音色角色
- [ ] 中文学德语完整课程数据（当前为覆盖六种题型与内容库的示例种子：20 概念 / 40 句 / 10 对话 / 10 短文 / 60 习题 / 10 课时；完整课程内容填充列入后续迭代）

## 第 3 阶段 — Blazor WASM 外壳（已完成 2026-11）

- [x] 独立部署 WASM 应用（`BeafSoup.Blazor.Client/.Shared/.Theming`）、路由、模块加载
- [x] 亮/暗主题（`docs/features/ui-theming-responsive.md`）、响应式布局、PWA manifest（无闪烁内联初始化）
- [x] 本地化管道（界面文案与内容标题全部来自数据层，零硬编码）
- [x] 接入独立 `HttpApi.Host`、账户注册/身份验证（JWT + 单会话顶替，ADR-006）与使用统计（心跳租约，ADR-007）；统计权限默认拒绝（稳定权限标识开放）

## 第 4 阶段 — 学习功能（已完成 2026-11）

- [x] 课程引擎 + 题型注册表；落地 `docs/features/exercise-types.md` 中的六种题型（服务端权威判分、对话填空音频规则、负载版本化）
- [x] 点词弹层（概念词典 K-V 缓存，纯覆盖层，见 `docs/features/word-inspector-popover.md`）
- [x] 带 FSRS 的背单词模块（到期队列 + 每日新卡预算 + 概念级卡片 + 形态掌握度，见 `docs/features/vocabulary-trainer.md`）
- [x] 多邻国式学习闭环（逐题显示、即时判分、红心自动恢复、连胜、经验，见 `docs/features/feature-catalog.md`）
- [x] 学习配置、学习活动（幂等重放安全）、FSRS 卡片、形态掌握度经 API 按账户隔离持久化（考试记录随第 5 阶段 Exam 模块）
- [x] 示例种子课覆盖六种题型（`data/seed/exercises.json` + 课程标题本地化）；完整课程内容填充列入后续迭代

## 第 4 阶段后续迭代 — 内容库与音频细化（已完成 2026-11）

- [x] 多行对话按行顺序连续播放（每行各自 `VoiceRole`），修复只播首行的问题；每行可单独播放
- [x] 多角色对话页、短文页、单词表与单词详情页（含词 ↔ 句例句互链、15/20/30 分页、注释显隐）
- [x] 目标语言选项选中即播放读音；可点击词以链接样式提示，支持最长短语匹配
- [x] 种子内容扩充到每种类型至少 10 个实例（20 概念 / 40 句 / 10 对话 / 10 短文 / 60 习题 / 10 课时）并生成全部音频
- [x] 修复习题呈现顺序未按课时编排返回的问题（逐题显示顺序与课程定义一致）

## 第 4 阶段后续迭代 2 — 账户生命周期与完整 A1 课程（已完成 2026-11-14）

- [x] 邮箱验证（6 位一次性验证码、限时、错误尝试上限、频率限制；登录门禁可配置）、密码找回、账户注销（删除个人数据）、数据导出（ADR-008）
- [x] 隐私政策与服务条款页面（本地化数据层），数据保留期清理命令 `DbMigrator purge`（会话 90 天、令牌 7 天）
- [x] 完整 A1 课程内容：5 单元 / 30 课时 / 300 概念 / 600 例句 / 30 对话 / 15 短文 / 180 习题，每概念 ≥2 例句，全部音频按需生成
- [x] 种子校验放宽：短语概念无形态时，句子与习题只记录概念键（词↔句互链仍可用）
- [x] 修复页面刷新/回访后心跳未重启导致会话租约到期被登出的问题（启动即发送一次心跳，回访自动续租）

## 第 5 阶段 — 考试、加固、第二门语言

- 模拟考试模块（歌德/telc 风格）
- 性能与体验打磨、移动端验收
- 验证语言扩展承诺：以已有处理器支持的第二门语言完成纯数据/配置接入；未覆盖的语言处理能力必须先实现并通过专属测试，不能降级为错误但静默的通用规则。

## 里程碑验收

每个阶段以 `AGENTS.md` § 3 的质量门禁结束，外加该阶段功能文档中列出的专项检查。
