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
  lemma_key?     FK → SemanticKey(key)     -- 词条目指向其词原型词条（可选）
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
3. 任何单词/句子的**基础语言解释**就是 `LocalizedText[key, 学习者基础语言]`——点词弹层只是一次数据查询，不是特殊内容（需求 6）。
4. 新增语言清单：注册 `LanguageCode` → 绑定已验证的文本规范化、分词及形态分析能力 → 为 `Male`/`Female`/`Child` 各添加一行 `VoiceRoleConfig` → 补充 `LocalizedText`。若现有处理器不支持该语言，必须先新增并测试相应处理器；仅在处理器已覆盖时才承诺零代码新增语言。
5. 缺失翻译回退顺序：按学习者显式配置的回退链（默认：基础语言 → 英语 → 目标语言）；在编辑工具中显示"未翻译"角标。
6. 更新某个 `(key, language_code)` 的文本必须递增 `revision`。依赖字符/词元位置的标注必须记录该修订号；修订不匹配时标注失效，必须重新校验或生成，禁止按旧位置静默展示。

## 为什么不用资源文件（.resx）

.resx 是按程序集、编译期生效的；而我们的内容是运行时数据（课程以数据形式编写，非开发者也会添加第三门语言）。数据驱动的键表是唯一同时满足"N 语言、一个键"，且**同一机制同时服务界面文案和学习内容**的模型。
