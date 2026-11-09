namespace BeafSoup.Identity;

/// <summary>口令散列抽象（安全敏感路径的边界）：实现位于 HttpApi 基础设施层，Application 不依赖 ASP.NET Core。</summary>
public interface IPasswordHashingService
{
    string Hash(string userId, string password);
    bool Verify(string userId, string hash, string password);
}
