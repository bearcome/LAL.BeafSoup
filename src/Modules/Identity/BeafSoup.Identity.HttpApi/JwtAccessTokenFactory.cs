using BeafSoup.Identity;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BeafSoup.Identity;

/// <summary>JWT 配置：Issuer/Audience/签名密钥/有效期；密钥只存在于服务端机密配置。</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "BeafSoup";
    public string Audience { get; set; } = "BeafSoup.Client";
    public string SigningKey { get; set; } = "";
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromHours(12);
}

/// <summary>JWT 签发（HS256，sub=UserId / sid=SessionId / jti 唯一）；实现在 HttpApi 层，与 JwtBearer 校验共用 Microsoft.IdentityModel。</summary>
public sealed class JwtAccessTokenFactory : IAccessTokenFactory
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenFactory(IOptions<JwtOptions> options, TimeProvider timeProvider)
        => (_options, _timeProvider) = (options.Value, timeProvider);

    public (string Token, DateTime ExpiresAtUtc) Create(Guid userId, Guid sessionId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAtUtc = now + _options.Lifetime;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            SigningCredentials = credentials,
            Expires = expiresAtUtc,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                ["sid"] = sessionId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
        };
        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);
        return (token, expiresAtUtc);
    }
}
