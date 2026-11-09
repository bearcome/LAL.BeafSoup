# 数据库 Schema（第 2 阶段）

> 供应商与策略见 [ADR-005](adr/ADR-005-sqlite-authority-database.md)。本文是内容与本地化数据层的表结构事实来源；学习/账户/考试表在第 3、4 阶段落地时补充。

## 总体约定

- 一个 SQLite 数据库文件；每模块一个 `DbContext`、一套 EF 迁移，迁移历史表按模块隔离（`__EFMigrationsHistory_{Localization|Vocabulary|Content}`）。
- 跨模块引用（词形 ID、语义键）**不在数据库层建外键**，由种子/发布管线的领域校验保证一致性。
- 枚举一律存字符串（可读、可搜索）；`de-DE`/`zh-CN` 等语言代号存 BCP-47 标签原文。
- 有序集合一律以 `Ordinal` 列保序。

## Localization 模块（`LocalizationDbContext`）

### `semantic_keys` — 语义键注册表

| 列 | 类型 | 说明 |
|---|---|---|
| `Key` | TEXT PK | 不透明稳定标识符，如 `word.coffee`（≤128） |
| `Domain` | TEXT | `vocabulary` / `content` / `ui` / `exam` |
| `CreatedAtUtc` | TEXT | 注册时间 |
| `IsDeprecated` | INTEGER | 废弃标记；键永禁止改名与物理删除 |

### `localized_texts` — 本地化文本

| 列 | 类型 | 说明 |
|---|---|---|
| `Key` | TEXT PK, FK→semantic_keys（逻辑外键） | 同一语义键 |
| `LanguageCode` | TEXT PK | `de-DE`/`zh-CN`/… |
| `Text` | TEXT | 该语言版本全文 |
| `Revision` | INTEGER | 每次文本变更递增，初始 1；标注绑定此修订号 |

主键 `(Key, LanguageCode)`：一个键可存 N 种语言，同键同语言仅一行。

### `voice_role_configs` — 音色角色配置

| 列 | 类型 | 说明 |
|---|---|---|
| `LanguageCode` | TEXT PK | |
| `Role` | TEXT PK | `Male`/`Female`/`Child` |
| `AssetFileToken` | TEXT | 稳定文件名片段（如 `Max`），非 Azure 语音 id |
| `TtsVoiceName` | TEXT | Azure 语音名 |

每语言必须恰好三角色（领域层 `EnsureCompleteSet` 校验）。

## Vocabulary 模块（`VocabularyDbContext`）

### `lexical_concepts` — 语义概念（跨语言聚合根）

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 Guid（由 concept 键确定性生成） |
| `ConceptKey` | TEXT UNIQUE | 语义键，命名空间 `concept.*`，如 `concept.drink` |
| `PartOfSpeech` | TEXT | Noun/Verb/Adjective/…（concept 级） |
| `Kind` | TEXT | `Word`/`Phrase`；短语的 lemma 用 `_` 连接词与词 |

### `lexemes` — 词元（每概念每语言 0..n 个）

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 Guid（由 concept+语言+lemma 确定性生成） |
| `ConceptId` | TEXT FK | 级联删除 |
| `LanguageCode` | TEXT | `de-DE`/`zh-CN`/… |
| `Ordinal` | INTEGER | 同 (概念, 语言) 内保序 |
| `Lemma` | TEXT | 原形检索词（短语含 `_`） |
| `DisplayWord` | TEXT NULL | 展示文本（名词含冠词，如 `der Kaffee`）；NULL = 同 Lemma |
| `IsPrimary` | INTEGER | 每个 (概念, 语言) 恰一行 = 1（领域校验） |
| `Register` | TEXT | 语体：Neutral/Elevated/Colloquial/Common/Literary |
| `Article` | TEXT NULL | `Der`/`Die`/`Das`，仅名词 |
| `Cefr` | TEXT | A1–C2 |

### `lexeme_tags`

主键 `(LexemeId, Ordinal)`；`Tag` ≤64。词元的受控标签，保序。

### `paradigm_cells` — 范式槽位（形态）

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 Guid（由 concept+语言+lemma+范式+槽位确定性生成；形态掌握度、句子标注引用它，跨读写必须稳定） |
| `LexemeId` | TEXT FK | 级联删除 |
| `ParadigmKey` | TEXT，索引 | 范式名，如 `indicative_present`/`declension`（检索用） |
| `SlotOrdinal` | INTEGER | 槽位序号；槽位顺序即人称/数或格/数的固定约定 |
| `Surface` | TEXT | 表层形式文本（词汇表直接持有，不走 localized_texts） |

## Content 模块（`ContentDbContext`）

### `sentences` — 句子

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 slug（如 `s-000001`） |
| `Key` | TEXT | 句子语义键；全文只存于 `localized_texts` |

### `sentence_tokenizations` — 分词结果

主键 `(SentenceId, LanguageCode)`；`TextRevision` INTEGER 绑定 `localized_texts.Revision`。

### `sentence_tokens` — 分词片段

主键 `(SentenceId, LanguageCode, Ordinal)`；`Start`/`Length` 为 Unicode scalar 索引（零基），`Kind`（Word/Whitespace/Punctuation/NonLexicalSpeech），`FormCellId` NULL 或引用 `paradigm_cells.Id`（逻辑外键）。片段按序、无重叠、完整覆盖全文（领域构造校验）。

### `sentence_span_links` — 短语标注

自增 `Id` PK；`SentenceId`+`LanguageCode`+`TextRevision` 索引；`StartTokenIndex`+`TokenCount` 指向词元序号；`LexicalEntryKey` 指向词汇条目语义键。修订失配即失效。

### `dialogues` / `dialogue_lines`

`dialogues.Id` PK。`dialogue_lines` 主键 `(DialogueId, Ordinal)`；`SpeakerLabel` 仅展示、绝不进 TTS；`SentenceId` 逻辑引用 `sentences.Id`；`VoiceRole` 为角色枚举字符串。

### `essays` / `essay_sentences`

短文 = 有序句子列表；`essay_sentences` 主键 `(EssayId, Ordinal)`。音频由句子资产前端拼接，无独立文件。

### `courses` / `skill_units` / `lessons` / `lesson_exercises`

- `courses`：`Id` PK + `TitleKey`。
- `skill_units`：`Id` PK，`CourseId` FK 级联，`Ordinal` 课程内保序，`TitleKey`。
- `lessons`：`Id` PK，`SkillUnitId` FK 级联，`Ordinal` 单元内保序，`TitleKey`。
- `lesson_exercises`：主键 `(LessonId, Ordinal)`；`ExerciseInstanceId` Guid + `ExerciseTypeKey`（题型注册表键）；习题负载由 Exercises 模块（第 4 阶段）校验。

## Identity 模块（`IdentityDbContext`，第 3 阶段）

### `user_accounts` — 用户账户

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 UserId |
| `Status` | TEXT | `Active`/`Disabled`/`Deleted`（删除为标记删除，注册事实保留） |
| `RegisteredAtUtc` | TEXT | UTC 注册时间 |
| `IsTest` | INTEGER | 测试账户（排除出累计注册数与有效账户数） |

### `user_identity_links` — 身份关联

主键 `(Provider, ProviderUserId)`；`UserId` 索引。登录身份在提供方范围内唯一并映射到 UserId；同一账户可挂多个提供方。

### `user_credentials` — 凭据散列（第 3 阶段，password 提供方）

主键 `(Provider, ProviderUserId)`；`Hash` 为 PBKDF2-SHA256 散列（格式 `v1.<迭代>.<salt>.<hash>`）。凭据绝不进入领域对象或日志；提供方边界见 ADR-006。

### `user_registered_events` — 注册事实事件

`EventId` PK：服务端成功接受的 `UserRegistered` 事件，按稳定事件 ID 去重；累计注册数的权威来源。

### `user_sessions` — 服务端会话记录

`SessionId` PK；`UserId` 索引；`LastHeartbeatAtUtc` 索引。在线判据 = `LastHeartbeatAtUtc + LeaseTtl`（ADR-007）；`AccumulatedOnlineSeconds` 按 `min(心跳间隔, MaxTimingWindow)` 累计，超窗不追补；`EndedAtUtc`/`EndReason`（`Ended`/`Revoked`，租约过期为派生态不落库）。每账户最多一个有效交互会话（新登录顶替旧会话）。

## Learning 模块（`LearningDbContext`，第 4 阶段）

### `learner_profiles` — 学习配置

`Id` PK；`UserId` 索引；`BaseLanguage`/`TargetLanguage`（BCP-47 原文）；`TargetRetentionRate` REAL；`ThemePreference`/`InflectionDisplay`（`Common`/`Full`）/`InspectorTrigger`（`Click`/`Hover`）。

### `review_cards` — FSRS 复习卡

主键 `(ProfileId, ConceptId)`：学习单位是概念，词元/形态不重复建卡；`Status`（`New`/`Learning`/`Review`/`Relearning`）；`DueUtc` 索引（到期队列）；`Stability`/`Difficulty` REAL；`SchedulingVersion` INTEGER（参数升级迁移用，ADR-003）。

### `form_mastery` — 形态掌握度

主键 `(ProfileId, CellId)`；`CorrectCount`/`IncorrectCount`；`LastSeenAtUtc`。仅明确关联 `ParadigmCellId` 的判分结果更新。

### `learning_activities` — 学习活动事实

`Id` PK；`(ProfileId, DedupeKey)` 唯一索引（重试/重放幂等）；`Type`（`GradedAnswer`/`ReviewSubmitted`/`LessonCompleted`）；`IsCorrect`（答错不奖励经验，但不扣已得奖励）；`ExerciseInstanceId`/`LessonId` 可空。

### `learner_progress_states` — 游戏化状态

`ProfileId` PK；`Hearts`（上限 5，自动恢复 1 心/30 分钟，下限 0）；`LastHeartChangeAtUtc`；`LastActivityDateUtc`；`DayStreak`（UTC 连续日口径）。

## Exercises 模块（`ExercisesDbContext`，第 4 阶段）

### `exercise_instances` — 习题实例

`Id` PK（由种子习题键确定性生成）；`TypeKey`（题型注册表键，索引）；`PayloadVersion` INTEGER；`PayloadJson`（版本化负载，下发前经 handler 校验，未知键/版本明确失败）；`Difficulty` INTEGER；`HintKey` 可空。课时引用见 Content 模块 `lesson_exercises`。

## 运维

- 迁移由 `src/BeafSoup.DbMigrator` 显式执行：`migrate`（仅迁移）、`seed`（迁移 + 空库填充，`--force` 替换内容表）、`reset`（删库重建）。应用运行期不自动迁移。
- 备份：先 `PRAGMA wal_checkpoint(TRUNCATE);` 再复制数据库文件（见 ADR-005）。
