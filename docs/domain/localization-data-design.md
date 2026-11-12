# 多语言数据设计 — 一个语义键，N 种语言

> 需求 11：同一语义在数据表中可存储 N 种语言版本，用一个相同的键标识，用语言代号区分。

## 核心表

```
SemanticKey
  key            PK   如 'word.coffee'、'sentence.greeting.001'、'ui.button.check'
  domain              如 'vocabulary' | 'content' | 'ui' | 'exam'
  created_at

LocalizedText
  key            FK → SemanticKey.key      -- 同一个键
  language_code  FK，如 'de-DE'            -- 用语言代号区分版本
  text                -- 该语言版本的文本
  revision            -- 每次文本变更递增；初始值为 1
  PRIMARY KEY (key, language_code)

VoiceRoleConfig
  language_code  PK part
  role           PK part   -- 'Male' | 'Female' | 'Child'
  asset_file_token    -- 稳定的文件名片段，与 UI 显示名分离
  tts_voice_name      -- Azure 语音名，如 'de-DE-ConradNeural'
```

## 规则

1. **一个语义单元 = 一个键**，永久不变。禁止改名；废弃用标记代替。键是不透明标识符，绝不从任何语言的文本推导。
2. 所有引用表（Sentence、习题提示、界面文案、考试题）只存**键 + 语言代号**，永不存原始文本。
3. 句子的**基础语言解释**就是 `LocalizedText[key, 学习者基础语言]`——点词弹层对句子文案只是一次数据查询，不是特殊内容（需求 6）。**单词/短语的概念解释不走 `LocalizedText`**：词汇表不保存注释语言的任何内容，解释只来自同一语义键对应的基础语言主词元（如 `concept.drink` → 中文主词元 `喝`），数据由概念词典 K-V 缓存按页面供给（见下文"概念词典 K-V 缓存"）。
4. 新增语言清单：注册 `LanguageCode` → 绑定已验证的文本规范化、分词及形态分析能力 → 为 `Male`/`Female`/`Child` 各添加一行 `VoiceRoleConfig` → 补充 `LocalizedText`。若现有处理器不支持该语言，必须先新增并测试相应处理器；仅在处理器已覆盖时才承诺零代码新增语言。
5. 缺失翻译回退顺序：按学习者显式配置的回退链（默认：基础语言 → 英语 → 目标语言）；在编辑工具中显示"未翻译"角标。
6. 更新某个 `(key, language_code)` 的文本必须递增 `revision`。依赖字符/词元位置的标注必须记录该修订号；修订不匹配时标注失效，必须重新校验或生成，禁止按旧位置静默展示。
7. 语义键格式只允许小写字母、数字、点（`.`）和连字符（`-`），点分段非空；**下划线非法**。数据中的范式键（如 `indicative_present`）是领域标识，不是语义键；引用其界面文案时必须先转换为连字符形式（如 `ui.trainer.paradigm.indicative-present`）。整份种子校验因任一非法键失败时，本地化数据不会入库，界面会显示原始语义键。

## 概念词典 K-V 缓存（读模型）

词汇域的词典数据（`LexicalConcept`/`Lexeme`/`ParadigmCell`，见 `model.md`）在发布期被序列化为**语义 id → 词典条目**的 K-V JSON，随站点静态部署，供 Blazor 客户端整体加载进内存字典：

```
键：{语义 id}，如 "concept.drink"
值：{
  "conceptId": "<稳定 Guid>",
  "pos": "Verb",
  "kind": "Word" | "Phrase",
  "{languageCode}": {
    "lexemes": [
      { "lemma": "trinken", "isPrimary": true, "register": "Neutral",
        "forms": { "indicative_present": ["trinke", "trinkst", "trinkt", "trinken", "trinkt", "trinken"], ... } },
      ...
    ]
  },
  ...
}
```

规则：

1. **去冗余**：`displayWord` 仅在不同于 `lemma` 时出现（名词含冠词展示，如 `der Kaffee`）；`article` 仅名词携带；空 `tags`、空 `forms` 省略。词汇表内不再有任何按语言重复的释义字段（`translations` 已废止）。
2. **页面按需供给**：每个页面只获取与本页内容相关的 concept 条目（分片预生成或由服务端按键批量返回）；客户端以语义 id 直接命中，取得全部语言的所有词元与全部形态，进而取基础语言主词元作注释、取目标语言范式表展示形态变化。
3. 短语的 `lemma` 以 `_` 连接词与词（如 `guten_Tag`；无语间边界的书写系统如中文可不使用），与单词区分；TTS 合成时将 `_` 还原为空格，文件命名时将 `_` 转 `-`（见 `audio-asset-spec.md`）。
4. 该 JSON 是由权威数据库导出的**只读投影**，禁止客户端回写；写入永远走种子/内容入库管线。

## 为什么不用资源文件（.resx）

.resx 是按程序集、编译期生效的；而我们的内容是运行时数据（课程以数据形式编写，非开发者也会添加第三门语言）。数据驱动的键表是唯一同时满足"N 语言、一个键"，且**同一机制同时服务界面文案和学习内容**的模型。
