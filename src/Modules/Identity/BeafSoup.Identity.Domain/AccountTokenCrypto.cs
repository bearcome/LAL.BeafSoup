using System.Security.Cryptography;
using System.Text;

namespace BeafSoup.Identity;

/// <summary>一次性验证码的生成与散列：6 位数字、随机盐、SHA-256；比较使用固定时间算法。</summary>
public static class AccountTokenCrypto
{
    public static string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string GenerateSalt() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string ComputeHash(string salt, string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}|{code}")));

    public static bool Matches(string salt, string code, string expectedHash)
    {
        if (string.IsNullOrEmpty(code))
            return false;
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}|{code}"));
        return CryptographicOperations.FixedTimeEquals(
            actual, Convert.FromHexString(expectedHash));
    }
}
