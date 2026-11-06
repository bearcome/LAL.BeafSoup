using System.Security.Cryptography;
using System.Text;

namespace BeafSoup.Audio.Generation;

/// <summary>由稳定字符串生成确定性 Guid（种子 id 重跑不变）；仅用于标识稳定性，非安全用途。</summary>
internal static class StableId
{
    public static Guid For(string source) => new(MD5.HashData(Encoding.UTF8.GetBytes(source)));
}
