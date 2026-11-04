# 领域模型

> 第 1 阶段的聚合与值对象设计。类型名用英文；代码注释将用中文。
## 所有Domain 扩展键值对 扩展属性
## 共享内核（Shared Kernel）

| 类型 | 种类 | 说明 |
|---|---|---|
| `LanguageCode` | 值对象 | BCP-47 语言标签，如 `de-DE`、`zh-CN`、`en-US`。统一大小写规范化。 |
| `VoiceRole` | 值对象 | 每语言三个音色角色 `Male`/`Female`/`Child`；每角色映射该语言的一个 Azure TTS 语音名（见 `audio-asset-spec.md`）。 |



## 词汇模块（Vocabulary）

- **`Lemma`**（聚合根）：单词词典形式（原型），如 `der Kaffee`。包含：词性、名词冠词、基础语言解释（引用 `SemanticText`）、CEFR 等级、标签、audio_url、role_name、ext。
- **`InflectionForm`**（Lemma 下实体）：任何出现过的表层形态（变格/变位/比较级/复数…），如 `Kaffees`、`trank`、`besser`。包含：语法范畴袋（格/数/时态/人称…）、用例引用。
- **不变量**：每个表层形态唯一映射到一个 `Lemma` + 一套语法范畴；按规范化形式精确匹配检索（对应需求 4、2）。
- **词 ↔ 句互链**：多对多表 `SentenceWord { sentenceId, tokenIndex, surfaceForm → Lemma/InflectionForm }`，支撑句中点词检索（需求 4）。

## 内容模块（Content）

- **`Course` → `SkillUnit`（技能树节点）→ `Lesson`**（需求 1）。
- **`Sentence`**（实体）：有序词元列表（每个词元链接到 `InflectionForm`，或为标点/纯发音成分），`SemanticText` 引用，**每行说话人 `VoiceRole`**（需求 5），音频引用按约定生成。
- **`Dialogue`**（实体）：有序 `DialogueLine { speakerName（仅展示）, Sentence }` 列表——说话人名是**展示元数据，绝不被 TTS 朗读**（需求 8）。
- **`Essay`**（短文）：有序 `Sentence` id 列表——音频由句子音频拼接而成，不做单独 TTS 合成。
- **`LessonContent`**：多态练习题列表，每项引用题型注册表中的题型。

## 习题模块（Exercises）

- **`Exercise`**（聚合）：`ExerciseType` 判别值（注册表键）、提示数据、判分策略、难度。
- 注册表键：`WordDefinitionMcq`、`SentenceClozeMcq`、`SentenceClozeInput`、`DialogueListeningMcq`、`MockExam`（见 `exercise-types.md`）。

## 音频模块（Audio）

- **`AudioAsset`**（值对象）：按 `{base}/audio/{languageCode}/{fileName}` 约定解析出的 URL（见 `audio-asset-spec.md`）；命名规则由领域服务 `AudioPathBuilder` 强制约束，不可变。

## 学习模块（Learning）

- **`LearnerProfile`**（聚合）：基础语言、目标语言、目标记忆率、主题偏好。
- **`ReviewCard`**（实体）：以（学习者, lemmaOrFormId）为键；存储 FSRS 状态（stability、difficulty、到期时间）——见 ADR-003。
- **`ProgressRecord`**（聚合）：经验值、连胜、红心/体力、单元完成度。

## 考试模块（Exam）

- **`ExamPaper`**：引用题型的分节/题目、时间限制、等级（`A1`–`C1`）、按官方考试惯例映射的分数段位。

## 跨模块领域事件

- `SentenceCompleted` → Learning 更新复习计划。
- `ExerciseAnswered(graded)` → ProgressRecord 与 Learning 更新。
