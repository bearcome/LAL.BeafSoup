# ADR-006：身份提供方、令牌与并发会话策略

- 状态：**已接受**
- 日期：2026-11-09

## 背景

CONTEXT.md Q2 要求在第 3 阶段前决定身份提供方，且"API 必须支持替换提供方"。账户规范要求：凭据由身份提供方管理、不进入领域对象；身份关联在提供方范围内唯一并映射到稳定 `UserId`；同一账户可关联多个提供方；每个账户默认最多一个有效交互会话，会话冲突处置方式须在 ADR 中确定。Blazor WASM 独立静态部署（ADR-002），身份与统计只能由独立 API 服务端权威执行（ADR-004）。

候选：

1. 引入第三方/云身份服务（Auth0、Azure AD B2C 等）。
2. 整体引入 ASP.NET Core Identity + Identity UI。
3. 自研轻量账户提供方：密码散列复用 ASP.NET Core Identity 核心的 `PasswordHasher<T>`（共享框架自带，无需新增依赖），令牌用 JWT Bearer；提供方边界以接口隔离。

## 决策

采用方案 3。

- **提供方边界**：`IIdentityProvider` 接口（Identity 模块内）承担"验证凭据 / 创建身份关联"职责；首发实现 `PasswordIdentityProvider`（提供方键 `password`，身份关联 = 规范化邮箱）。后续新增 OAuth/OIDC 提供方 = 新实现 + 复用 `UserIdentityLink` 映射到既有 `UserId`，账户与应用层零改动。
- **密码散列**：`Microsoft.AspNetCore.Identity.PasswordHasher<string>`（PBKDF2-SHA256，共享框架程序集，不新增 NuGet 依赖）；散列仅存于 Identity 模块自己的凭据存储表，绝不进入领域对象、学习内容或日志。
- **令牌**：JWT Bearer（HS256，`sub`=UserId、`sid`=SessionId、`jti` 唯一标识，有效期 12 小时）。令牌校验使用官方 `Microsoft.AspNetCore.Authentication.JwtBearer` 包（新增依赖，理由：令牌校验属安全敏感路径，不自研；与既有 Microsoft.\* 依赖同发布列车）。签名密钥只存在于服务端配置。
- **默认 LearnerProfile**：注册成功后即创建默认学习配置（基础语言/目标语言取注册请求，缺省 `zh-CN` → `de-DE`）。
- **并发会话**：每账户最多一个有效交互会话（沿用架构约束）。会话冲突处置 = **新登录使旧会话失效**（旧令牌因 `sid` 校验失败而拒绝），冲突非错误、不阻止登录。

## 后果

- 替换提供方不影响账户模型与学习数据归属；`UserId` 稳定不变。
- 服务端必须校验每个请求的 `sid` 对应会话仍有效（支持会话吊销）。
- 密钥管理（JWT 签名密钥）进入部署运维清单；遗失密钥 = 全部令牌失效。
