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
| `LemmaKey` | TEXT NULL，索引 | 词形/词条目指向其词原型键（可选） |

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

### `lemmas` — 词元

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 Guid |
| `LanguageCode` | TEXT | |
| `LemmaKey` | TEXT UNIQUE | 词形语义键，全局唯一 |
| `PartOfSpeech` | TEXT | Noun/Verb/Adjective/… |
| `Article` | TEXT NULL | `Der`/`Die`/`Das`，仅名词 |
| `Cefr` | TEXT | A1–C2 |

### `lemma_tags`

主键 `(LemmaId, Ordinal)`；`Tag` ≤64。词元的受控标签，保序。

### `inflection_forms` — 词形

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 `InflectionFormId`（形态掌握度、句子标注引用它，跨读写必须稳定） |
| `LemmaId` | TEXT FK | 级联删除 |
| `Ordinal` | INTEGER | 同词元内保序；原形永远在最前 |
| `SurfaceKey` | TEXT，索引 | 表层文本语义键；原形词形的 SurfaceKey == LemmaKey |

### `form_analyses` — 语法分析

主键 `(FormId, Ordinal)`；`Case`/`Number`/`Person`/`Tense`/`Gender`/`Degree` 均为可空 TEXT（不适用即 NULL）。一个词形至少一行。

## Content 模块（`ContentDbContext`）

### `sentences` — 句子

| 列 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT PK | 稳定 slug（如 `s-000001`） |
| `Key` | TEXT | 句子语义键；全文只存于 `localized_texts` |

### `sentence_tokenizations` — 分词结果

主键 `(SentenceId, LanguageCode)`；`TextRevision` INTEGER 绑定 `localized_texts.Revision`。

### `sentence_tokens` — 分词片段

主键 `(SentenceId, LanguageCode, Ordinal)`；`Start`/`Length` 为 Unicode scalar 索引（零基），`Kind`（Word/Whitespace/Punctuation/NonLexicalSpeech），`InflectionFormId` NULL 或引用 `inflection_forms.Id`（逻辑外键）。片段按序、无重叠、完整覆盖全文（领域构造校验）。

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

## 运维

- 迁移由 `src/BeafSoup.DbMigrator` 显式执行：`migrate`（仅迁移）、`seed`（迁移 + 空库填充，`--force` 替换内容表）、`reset`（删库重建）。应用运行期不自动迁移。
- 备份：先 `PRAGMA wal_checkpoint(TRUNCATE);` 再复制数据库文件（见 ADR-005）。
