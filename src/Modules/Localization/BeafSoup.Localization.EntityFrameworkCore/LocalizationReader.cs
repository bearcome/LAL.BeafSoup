using BeafSoup.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Localization.EntityFrameworkCore;

/// <summary>Localization 读模型实现：跨语义键注册表与文本表取数，按 domain 过滤。</summary>
public sealed class LocalizationReader : ILocalizationReader
{
    private readonly LocalizationDbContext _context;

    public LocalizationReader(LocalizationDbContext context) => _context = context;

    public async Task<IReadOnlyList<(SemanticKey Key, LanguageCode Language, string Text, int Revision)>> GetTextsByDomainAsync(
        string domain, CancellationToken cancellationToken)
    {
        var rows = await (
                from key in _context.SemanticKeys
                join text in _context.LocalizedTexts on key.Key equals text.Key
                where key.Domain == domain && !key.IsDeprecated
                select text)
            .ToListAsync(cancellationToken);
        return rows
            .Select(r => (SemanticKey.Of(r.Key), LanguageCode.Of(r.LanguageCode), r.Text, r.Revision))
            .ToArray();
    }

    public async Task<VoiceRoleConfig?> FindVoiceRoleConfigAsync(LanguageCode language, VoiceRole role, CancellationToken cancellationToken)
    {
        var row = await _context.VoiceRoleConfigs
            .FirstOrDefaultAsync(v => v.LanguageCode == language.Value && v.Role == role.ToString(), cancellationToken);
        return row is null ? null : LocalizationPersistenceMapper.ToDomain(row);
    }
}
