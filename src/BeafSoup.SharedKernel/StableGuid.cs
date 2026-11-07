using System.Security.Cryptography;
using System.Text;

namespace BeafSoup.SharedKernel;

/// <summary>由稳定字符串生成确定性 Guid（同一来源永远得到同一 Id，跨读写/重跑种子稳定）；仅用于标识稳定性，非安全用途。</summary>
public static class StableGuid
{
    public static Guid For(string source) => new(MD5.HashData(Encoding.UTF8.GetBytes(source)));
}
