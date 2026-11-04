# 音频资产规范

> 需求 5：音频用 Azure TTS（免费额度预算）预生成，以静态文件存储，遵循严格命名规则。

## 路径约定（规范性）

基础路径：`{assetBase}/audio/`

### 单词音频

```
{audio}/de-DE/{article}-{lemma}-{Voice}.mp3
示例：audio/de-DE/der-Kaffee-Max.mp3
```

- `article`：名词冠词小写（`der`/`die`/`das`）；无冠词名词、动词、形容词等去掉 `{article}-`。
- `lemma`：词典原形，空格转 `-`，变音符号保留（`ä/ö/ü/ß`）。
- `{Voice}`：该语言的音色**角色名**（见"音色角色"）。角色名是每语言可配置的角色显示名（德语角色如 `Max` / `Mia` / `Lars`），不是 Azure 原始语音 id。

### 句子音频

```
{audio}/de-DE/{sentenceId}-{Voice}.mp3
示例：audio/de-DE/s-004217-Max.mp3
```

- `sentenceId`：Sentence 实体的稳定 id（英文 slug 或数字，不含语言前缀，语言由目录区分）。
- 短文（essay）音频**无独立文件**——播放时在前端拼接其句子文件。

## 音色角色

- 每种语言恰好配置 **3 个角色**：男 / 女 / 儿童（`Male` / `Female` / `Child`）。
- 每个句子实体为每行携带一个 `VoiceRole`；该语言所有音频只使用这 3 个已配置角色（需求 5）。
- 角色 → Azure 语音的映射存于 `VoiceRoleConfig`（见 `localization-data-design.md`）；Azure 语音 id 绝不泄露到文件名或组件中。

## 生成管线（离线，第 2 阶段）

1. 内容数据（句子含每行角色 + **TTS 文本**字段）→ 批量控制台工具（`BeafSoup.Audio.Generation`）。
2. 工具渲染 SSML → 调用 Azure TTS → 按约定写文件；幂等（已存在则跳过）。
3. Azure 密钥只存在于工具的本地配置（`.env`，已 git 忽略）；WASM 应用永不直接调用 Azure。

## 内容硬规则

1. **说话人名绝不朗读**。对话行的 TTS 文本仅为该行内容；`Max:` / `Kari:` 是展示元数据，合成前剥离（需求 8，见 `exercise-types.md`"对话填空"一节）。
2. **对话填空播放**须用**补全后**的完整对话（如题目 `Max: ____! Kari: Guten Tag!` 播放 `Guten Tag! Guten Tag!`，每行按各自角色合成）。
   - `{sentenceId}-{Voice}.mp3` 
3. 标点规范化、数字展开（`7` → `sieben`）、按 CEFR 等级调节 SSML 语速，均在生成期完成；应用端原样播放。

## 变更策略

文件改名或改约定 = 破坏性变更：须在同一次提交中更新本规范、领域服务 `AudioPathBuilder`，并重跑生成管线。
