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
- `lemma`：词典原形，空格转 `-`；短语的词间 `_` 同样在文件名中转 `-`（如 `guten_Tag` → `guten-Tag`）；变音符号保留（`ä/ö/ü/ß`）。
- 单词音频为**每个词元**（所有已配置音色角色的语言，含非主词元）各生成一份。
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

1. 内容数据（句子的每行角色 + **TTS 文本**字段；词元的 lemma/冠词/语体）→ 批量控制台工具（`BeafSoup.Audio.Generation`）。
2. 工具渲染 SSML → 调用 Azure TTS → 按约定写文件；幂等（已存在则跳过，`--force` 重生成）。
3. Azure 密钥只存在于仓库根 `secret.json`（已 git 忽略，结构 `AzureTTS: { Endpoint, ApiKey, Region }`），由工具运行时绑定；非机密配置（输出目录、CEFR→语速映射、角色韵律覆盖）在工具的 `appsettings.json`。WASM 应用永不直接调用 Azure。
4. 工具命令：`validate`（只校验种子数据，不触网）、`generate --dry-run`（打印资产清单）、`generate`（实际合成并写 `manifest.json`）。

### 产物位置与访问

- 生成产物直接写入 Blazor 客户端静态目录：`src/BeafSoup.Blazor.Client/wwwroot/media/audio/`（`de-DE/*.mp3` + `manifest.json`），随客户端静态部署发布；输出目录由工具 `appsettings.json` 的 `outputDirectory` 指定。
- 数据中存储的音频地址（如词典与习题载荷中的 `audioUrl`）一律为本规范定义的**相对路径**（`{language}/{name}.mp3`）；客户端经 `wwwroot/appsettings.json` 的 `MediaBaseUrl`（默认 `media/audio/`）拼接为可访问 URL，部署到 CDN 或独立域名时只改该配置。
- `MediaBaseUrl` 支持三种形式：应用相对路径（跟随 `<base href>` 的子路径部署）、以 `/` 开头的站点根路径、带协议的 CDN 绝对地址。统一由 `MediaUrlResolver` 解析，`AudioButton` 不得另行拼接。
- 播放桥在 JavaScript 内吸收浏览器拒绝播放及加载失败，`AudioButton` 恢复按钮并显示本地化错误；不得把音频 Promise 拒绝留成未处理异常。
- 对话填空的 `-revealed-` 补全版资产缺失时，客户端回退到同句子的完整句音频（基础句音频本身就是补全后的台词，且不含说话人标签）；生成管线仍应补齐 revealed 资产，回退只用于避免播放中断。
- 应用端统一经 `AudioButton` 组件解析基路径，任何组件不得自行拼接音频 URL。

## 内容硬规则

1. **说话人名绝不朗读**。对话行的 TTS 文本仅为该行内容；`Max:` / `Kari:` 是展示元数据，合成前剥离（需求 8，见 `exercise-types.md`"对话填空"一节）。
2. **对话填空播放**须用**补全后**的完整对话（如题目 `Max: ____! Kari: Guten Tag!` 播放 `Guten Tag! Guten Tag!`，每行按各自角色合成）。
   - `{sentenceId}-{assetFileToken}.mp3`
3. 标点规范化、数字展开（`7` → `sieben`）、按 CEFR 等级调节 SSML 语速，均在生成期完成；应用端原样播放。
4. **名词单词音频带冠词朗读**：名词的 TTS 文本为 `冠词 + 空格 + 词元`（如 `der Kaffee`），冠词取该名词的定冠词（`der/die/das`，小写拼入）；无冠词名词（如仅用复数的名词）及动词、形容词等其他词性仅朗读词元本身。
5. **短语按词朗读**：短语的 TTS 文本将词间 `_` 还原为空格（如 `guten_Tag` → `guten Tag`）。

## 变更策略

文件改名或改约定 = 破坏性变更：须在同一次提交中更新本规范、领域服务 `AudioPathBuilder`，并重跑生成管线。
TTS 文本规则变化（如"内容硬规则"第 4 条）虽不改路径，但使既有单词资产内容过期：须重跑生成管线并对受影响资产使用 `--force` 重生成。
