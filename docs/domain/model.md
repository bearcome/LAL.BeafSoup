# 领域模型

> 第 1 阶段的聚合与值对象设计。类型名用英文；代码注释将用中文。

本模型定义领域身份、聚合边界与跨模块引用；持久化结构由实现阶段的 ADR 与 schema 设计确定。

## 建模规则

- 领域标识符使用稳定 ID；跨模块只保存对方的 ID 或契约，不建立跨模块对象导航。
- 多语言文本统一引用 `SemanticKey`，并按 `LanguageCode` 查询 `LocalizedText`；领域对象不复制翻译文本。
- 核心业务状态必须由明确类型和不变量表达。扩展数据仅用于可选、带命名空间和 `schemaVersion` 的非核心元数据；必须由所属模块校验，不能承载权限、进度、判分等核心状态，也不能存凭据或访问令牌。

## 共享内核（Shared Kernel）

| 类型 | 种类 | 说明 |
|---|---|---|
| `LanguageCode` | 值对象 | BCP-47 语言标签，如 `de-DE`、`zh-CN`、`en-US`。统一大小写规范化。 |
| `VoiceRole` | 值对象 | 每语言三个音色角色 `Male`/`Female`/`Child`；每角色映射该语言的一个 Azure TTS 语音名（见 `audio-asset-spec.md`）。 |



## 词汇模块（Vocabulary）

- **`Lemma`**（聚合根）：一种特定语言中的词典词义/词元，如德语 `Kaffee`。包含 `LanguageCode`、词形语义键、词性、名词冠词、CEFR 等级及受控标签；基础语言解释是同一语义键的本地化版本，不在 `Lemma` 上单独存文本或 URL。不同词义或同形异义词使用不同 `LemmaId`。
- **`InflectionForm`**（`Lemma` 下实体）：该词元的一个表层形式，包括词典原形本身；关联表层文本语义键及一个或多个语法分析（格、数、时态、人称等）。
- **匹配规则**：规范化表层形式是按语言配置的检索键，不是全局唯一键；检索可返回多个 `Lemma`/`InflectionForm` 候选。句子标注保存人工或上下文消歧后的目标。不能仅凭表层字符串强制唯一归属。
- **学习标识**：每个可复习词形都有稳定 `InflectionFormId`；词元原形也建模为一个 `InflectionForm`，避免同一原形同时生成词元卡和形态卡。

## 内容模块（Content）

- **`Course` → `SkillUnit`（技能树节点）→ `Lesson`**（需求 1）；学习者进度不写回课程定义。
- **`Sentence`**（实体）：引用句子 `SemanticKey`；本地化全文只存于 `LocalizedText`。每种语言独立拥有带文本修订号的 `SentenceTokenization`。每个 `SentenceToken` 以零基 Unicode scalar 起点/长度索引全文，不复制原文；词、空白、标点和非词汇发音片段按序无重叠且完整覆盖全文。词元可无词汇链接，或链接一个已消歧的 `InflectionFormId`。
- **短语标注**：`SentenceSpanLink { sentenceId, languageCode, startTokenIndex, tokenCount, lexicalEntryRef }` 表达跨词元短语；允许词元级与短语级链接重叠。索引绑定语言和 `LocalizedText` 修订号，仅适用于该版本；全文变更时旧标注失效，必须重新校验或生成。
- **`Dialogue`**（实体）：有序 `DialogueLine { speakerLabel（仅展示）, sentenceId, voiceRole }` 列表；说话人标签永不进入 TTS 输入。每行的音频文本来自对应语言版本。
- **`Essay`**（短文）：有序 `sentenceId` 列表；音频由句子资产拼接，不生成独立 TTS 文件。
- **`LessonContent`**：有序 `ExerciseInstance` 引用；题型由注册表键解析，具体题目负载由对应 handler 校验。

## 习题模块（Exercises）

- **`ExerciseInstance`**：题型注册表键、题目负载版本、提示数据和难度；`IExerciseHandler` 负责负载校验、呈现契约及判分。新增题型通过注册扩展，不修改课程编排器。
- 六个题型注册键：`WordDefinitionMcq`、`SentenceClozeMcq`、`SentenceClozeInput`、`SentenceTranslationOrder`、`DialogueListeningMcq`、`DialogueCloze`。`DialogueCloze` 的版本化负载明确指定 `responseMode`（选择或输入）；`MockExam` 是 Exam 模块的整卷编排，不是题型键。

## 音频模块（Audio）

- **`AudioAsset`**（值对象）：由内容 ID、语言、资产类型及 `VoiceRole` 解析为不可变路径；`AudioPathBuilder` 与 `audio-asset-spec.md` 负责命名。词元和句子只引用资产身份，不保存可变的 `audio_url` 或单一 `role_name`。

## 学习模块（Learning）

- **`LearnerProfile`**（聚合）：属于一个 `UserId`，包含基础语言、目标语言、目标记忆率和主题偏好；同一账户可拥有多个语言学习配置，配置 ID 稳定且不可跨账户访问。
- **`ReviewCard`**（实体）：以（`LearnerProfileId`, `LemmaId`）为唯一键；保存 FSRS 状态与调度版本。该卡的学习单位是词元，不为每个屈折形式重复创建调度卡。
- **`FormMastery`**（实体/投影）：以（`LearnerProfileId`, `InflectionFormId`）为键，单独记录各形态的作答表现。可明确关联词形的练习更新其掌握度，并同时将结果用于所属词元的 SRS 评级；泛理解题不推断词形掌握度。
- **`LearningActivity`**（不可变事实）：记录已判分作答、复习及课时完成等学习行为，含账户/学习配置 ID、稳定事件 ID、UTC 时间和必要的题目关联；去重键保证重试不会重复计分。
- **`ProgressRecord`**（只读投影）：按 `LearnerProfileId` 汇总经验值、连胜、红心/体力及单元完成度；唯一事实来源是已接受且去重的学习活动，不作为独立写入的聚合，也不能跨用户共享。

## 考试模块（Exam）

- **`ExamPaper`**（聚合根）：引用版本化分节与题目、时间限制、等级（`A1`–`C1`）及分数映射；`ExamAttempt` 属于单一 `UserId`/`LearnerProfileId`，答案和成绩不可写入共享试卷定义。

## 身份与使用统计模块（Identity / UsageAnalytics）

- **`UserAccount`**（聚合根）：稳定 `UserId`、账户状态及 UTC 注册时间。登录身份由可替换的身份提供方管理，并通过独立身份关联映射到 `UserId`；同一账户可关联多个提供方，提供方凭据不进入领域对象或日志。
- **权限策略**：初期每个有效注册账户默认可使用全部普通学习功能，不设置普通用户之间的功能等级；账户数据的所有权校验始终启用。权限按稳定权限标识建模，由服务端策略判定；后续可引入角色/策略映射。管理员与统计查询权限不包含在普通学习权限中，默认拒绝。
- **`UserSession`**（服务端会话记录）：记录 `SessionId`、`UserId`、开始时间、最后一次有效心跳和会话结束/过期状态。客户端时间不作为统计依据；在线用户按有效会话中的不同 `UserId` 去重。
- **使用统计**：注册总数、有效账户数、在线账户数、在线会话数和累计/分时段在线时长是 Identity/UsageAnalytics 的只读投影，不是学习域事实。统计事件以服务端接受的账户、会话和学习活动为来源。

## 跨模块领域事件

- `UserRegistered` → UsageAnalytics 更新注册统计投影。
- `SessionHeartbeatAccepted` / `SessionEnded` → UsageAnalytics 更新在线与时长投影；由服务端产生。
- `ExerciseAnswered(graded)` / `ReviewSubmitted(graded)` → Learning 记录去重后的学习事实并更新适用的复习卡与进度。
- `LessonCompleted` → 更新单元完成度；不能单独触发 FSRS 调度。
