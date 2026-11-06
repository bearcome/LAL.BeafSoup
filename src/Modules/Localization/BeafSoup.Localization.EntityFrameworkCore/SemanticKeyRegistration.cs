namespace BeafSoup.Localization.EntityFrameworkCore;

/// <summary>语义键注册表行：键是不透明稳定标识符，domain 标记所属领域，废弃用标记代替改名/删除。</summary>
public sealed class SemanticKeyRegistration
{
    public string Key { get; set; } = "";
    public string Domain { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public bool IsDeprecated { get; set; }
}
