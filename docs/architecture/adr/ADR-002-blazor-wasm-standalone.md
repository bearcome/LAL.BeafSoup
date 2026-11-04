# ADR-002：Blazor WebAssembly 独立部署

- 状态：**已接受**
- 日期：2026-11-04

## 背景

需求 10 要求独立部署。候选：Blazor Server、Blazor WASM 托管模式（ASP.NET Core 宿主）、Blazor WASM 独立模式（纯静态文件）。

## 决策

采用 **Blazor WASM 独立模式**：发布产物仅为静态文件，可部署于任意静态服务器（IIS/nginx/对象存储/GitHub Pages）。音频同样是按 URL 约定寻址的静态文件（见 `audio-asset-spec.md`），因此第 3–5 阶段完全不需要后端。

## 后果

- **优点**：托管成本最低；可离线使用（后续可演进为 PWA）；MVP 无服务端需保护。
- **缺点**：无服务端密钥安全区（Azure TTS 密钥只存在于离线生成工具的本地配置）；被迫采用本地优先持久化（浏览器内 SQLite，见 CONTEXT.md Q1）；多设备同步推迟（Q2）。
- 需求 13（桌面 + 手机响应式）属 UI 关注点，WASM 天然适配。
