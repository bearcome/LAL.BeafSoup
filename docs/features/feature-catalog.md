# 功能总目录

需求（1–14）到功能规格的映射总表。

| # | 需求 | 功能规格 |
|---|---|---|
| 1 | 多邻国全部功能 + 一套语义/单词对应 N 套语言 | [feature-catalog](feature-catalog.md) § 多邻国对标范围；[localization-data-design](../domain/localization-data-design.md) |
| 2 | 背单词模块（智能记忆法，含所有形态变化） | [vocabulary-trainer.md](vocabulary-trainer.md) |
| 3 | 六种题型（释义选择/句子填空选择/输入/翻译排序/对话听力选择/补全对话）+ 模拟考试编排 + 背词 | [exercise-types.md](exercise-types.md) |
| 4 | 句中词/短语 → 原型关联，点击弹层显示基础语言提示 + 常用/全部形态变化（可配置），词 ↔ 句互链 | [word-inspector-popover.md](word-inspector-popover.md) |
| 5 | Azure TTS 免费额度预生成音频；单词/句子路径规则；每语言 3 音色；句子含角色信息 | [audio-asset-spec.md](../domain/audio-asset-spec.md) |
| 6 | 点击单词显示基础语言解释；弹层不改变页面布局 | [word-inspector-popover.md](word-inspector-popover.md) |
| 7 | 亮/暗双主题 | [ui-theming-responsive.md](ui-theming-responsive.md) |
| 8 | 对话填空音频必须播放补全后整句且不含人名 | [exercise-types.md](exercise-types.md) § 对话填空 |
| 9 | DDD 高扩展，参考 ABP vNext | [overview.md](../architecture/overview.md)、ADR-001 |
| 10 | Blazor WASM 独立部署 | ADR-002 |
| 11 | 同语义 N 语言版本，同键 + 语言代号 | [localization-data-design](../domain/localization-data-design.md) |
| 12 | 项目设计英文、注释中文 | [conventions](../conventions/coding-conventions.md)、AGENTS.md § 1 |
| 13 | 响应式（桌面 + 手机） | [ui-theming-responsive](ui-theming-responsive.md) |
| 14 | 用户注册、初期全学习功能开放、账户隔离及注册/在线/时长统计 | [account-and-usage-statistics](account-and-usage-statistics.md) |

## 多邻国对标——范围定义（需求 1）

"多邻国全部功能"解释为学习闭环功能集，而非每个界面的复刻：

- 技能树（单元/章节）、顺序解锁 + 跳级测试
- 课时流程：约 10 题混合六种题型、逐题显示与即时判分、答对后自动进入下一题、课时结束总结
- 连胜、每日目标、红心/体力（自动恢复）、经验值与段位（本地优先版）
- 练习/复习中心：由 SRS 调度器供题（错题驱动 + 到期卡片）
- 商店/装扮、排行榜、社交：**MVP 范围外**，在 `LearnerProfile`/Progress 中预留扩展缝
