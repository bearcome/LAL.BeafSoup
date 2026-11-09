namespace BeafSoup.Identity;

/// <summary>会话结束原因：客户端退出 / 被新登录顶替 / 租约过期（过期为派生态，不强制落库）。</summary>
public enum SessionEndReason { Ended, Revoked }
