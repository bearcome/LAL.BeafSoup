namespace BeafSoup.Identity;

/// <summary>账户状态：有效 / 停用 / 已删除（删除为标记删除，保留注册事实）。</summary>
public enum UserAccountStatus { Active, Disabled, Deleted }
