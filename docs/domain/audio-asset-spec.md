# 音频资产规范

> 需求 5：音频用 Azure TTS（免费额度预算）预生成，以静态文件存储，遵循严格命名规则。

## 路径约定（规范性）

基础路径：`{assetBase}/audio/`

### 单词音频

```
{audio}/de-DE/{article}-{lemma}-{assetFileToken}.mp3
示例：audio/de-DE/der-Kaffee-Max.mp3
```

- `article`：名词冠词小写（`der`/`die`/`das`）；无冠词名词、动词、形容词等去掉 `{article}-`。
- `lemma`：词典原形，空格转 `-`，变音符号保留（`ä/ö/ü/ß`）。
- `{assetFileToken}`：从 `VoiceRoleConfig` 读取的稳定文件名片段（如 `Max` / `Mia` / `Lars`），不是 UI 显示名或 Azure 语音 id。修改该值视为资产路径变更。

### 句子音频

```
{audio}/de-DE/{sentenceId}-{assetFileToken}.mp3
示例：audio/de-DE/s-004217-Max.mp3
```

- `sentenceId`：Sentence 实体的稳定 id（英文 slug 或数字，不含语言前缀，语言由目录区分）。
- 短文（essay）音频**无独立文件**——播放时在前端拼接其句子文件。

## 音色角色

- 每种语言恰好配置 **3 个角色**：男 / 女 / 儿童（`Male` / `Female` / `Child`）。
- 每个对话行携带一个 `VoiceRole`；普通句子的音频请求也必须明确角色。该语言所有音频只使用 `Male`/`Female`/`Child` 三个已配置角色（需求 5）。
- 角色 → 文件名片段及 Azure 语音的映射存于 `VoiceRoleConfig`（见 `localization-data-design.md`）；显示名与文件名片段分离，Azure 语音 id 绝不泄露到文件名或组件中。

## 生成管线（离线，第 2 阶段）

1. 内容数据（句子含每行角色 + **TTS 文本**字段）→ 批量控制台工具（`BeafSoup.Audio.Generation`）。
2. 工具渲染 SSML → 调用 Azure TTS → 按约定写文件；幂等（已存在则跳过，`--force` 重生成）。
3. Azure 密钥只存在于仓库根 `secret.json`（已 git 忽略，结构 `AzureTTS: { Endpoint, ApiKey, Region }`），由工具运行时绑定；非机密配置（输出目录、CEFR→语速映射、角色韵律覆盖）在工具的 `appsettings.json`。WASM 应用永不直接调用 Azure。
4. 工具命令：`validate`（只校验种子数据，不触网）、`generate --dry-run`（打印资产清单）、`generate`（实际合成并写 `manifest.json`）。

## 内容硬规则

1. **说话人名绝不朗读**。对话行的 TTS 文本仅为该行内容；`Max:` / `Kari:` 是展示元数据，合成前剥离（需求 8，见 `exercise-types.md`"对话填空"一节）。
2. **对话填空播放**须用**补全后**的完整对话（如题目 `Max: ____! Kari: Guten Tag!` 播放 `Guten Tag! Guten Tag!`，每行按各自角色合成）。
   - `{sentenceId}-{assetFileToken}.mp3`
3. 标点规范化、数字展开（`7` → `sieben`）、按 CEFR 等级调节 SSML 语速，均在生成期完成；应用端原样播放。
4. **名词单词音频带冠词朗读**：名词的 TTS 文本为 `冠词 + 空格 + 词元`（如 `der Kaffee`），冠词取该名词的定冠词（`der/die/das`，小写拼入）；无冠词名词（如仅用复数的名词）及动词、形容词等其他词性仅朗读词元本身。

## 变更策略

文件改名或改约定 = 破坏性变更：须在同一次提交中更新本规范、领域服务 `AudioPathBuilder`，并重跑生成管线。
TTS 文本规则变化（如"内容硬规则"第 4 条）虽不改路径，但使既有单词资产内容过期：须重跑生成管线并对受影响资产使用 `--force` 重生成。
