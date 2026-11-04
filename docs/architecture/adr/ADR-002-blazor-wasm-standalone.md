# ADR-002：Blazor WebAssembly 独立部署

- 状态：**已接受**
- 日期：2026-11-04

## 背景

需求 10 要求独立部署。候选：Blazor Server、Blazor WASM 托管模式（ASP.NET Core 宿主）、Blazor WASM 独立模式（纯静态文件）。

## 决策

采用 **Blazor WASM 独立模式**：客户端发布产物仅为静态文件，可部署于任意静态服务器（IIS/nginx/对象存储/GitHub Pages），不由 ASP.NET Core 托管。音频仍按 URL 约定作为静态文件寻址（见 `audio-asset-spec.md`）。

## 后果

- **优点**：客户端可独立静态托管，内容/音频分发与 API 生命周期解耦；可离线使用（后续可演进为 PWA）。
- **缺点与边界**：静态客户端不能安全持有密钥，也不能权威统计注册用户、在线用户或时长。注册与集中统计需要独立的身份/API 服务和服务端持久化；Azure TTS 密钥仍只存在于离线生成工具本地配置。学习数据的本地优先和跨设备同步策略见 `CONTEXT.md` Q1、Q2。
- 需求 13（桌面 + 手机响应式）属 UI 关注点，WASM 天然适配。
