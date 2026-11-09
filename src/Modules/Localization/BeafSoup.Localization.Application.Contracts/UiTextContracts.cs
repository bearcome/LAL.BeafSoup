using BeafSoup.SharedKernel;

namespace BeafSoup.Localization;

/// <summary>UI 文案查询结果：键 → 各语言文本（缺失语言由客户端按回退链处理）。</summary>
public sealed record UiTextCatalogDto(
    string Domain,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> TextsByKeyAndLanguage);

public interface IUiTextCatalogQuery
{
    Task<UiTextCatalogDto> GetCatalogAsync(string domain, CancellationToken cancellationToken);
}
