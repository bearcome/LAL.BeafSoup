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

## 运维

- 迁移由 `src/BeafSoup.DbMigrator` 显式执行：`migrate`（仅迁移）、`seed`（迁移 + 空库填充，`--force` 替换内容表）、`reset`（删库重建）。应用运行期不自动迁移。
- 备份：先 `PRAGMA wal_checkpoint(TRUNCATE);` 再复制数据库文件（见 ADR-005）。
