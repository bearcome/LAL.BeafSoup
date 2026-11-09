using System.Security.Claims;
using BeafSoup.SharedKernel;

namespace BeafSoup.Identity;

/// <summary>从已验证的 ClaimsPrincipal 提取会话/用户标识（sid/sub claim）；身份主体只来自服务端验证过的令牌，不接受客户端任意指定。</summary>
public static class AuthenticatedPrincipal
{
    public static Guid RequireSessionId(ClaimsPrincipal principal)
    {
        var sid = principal.FindFirst("sid")?.Value;
        if (sid is null || !Guid.TryParse(sid, out var sessionId))
            throw new DomainException(IdentityErrorCodes.SessionNotFound, "令牌缺少有效会话标识。");
        return sessionId;
    }

    public static Guid RequireUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
            throw new DomainException(IdentityErrorCodes.InvalidCredentials, "令牌缺少有效用户标识。");
        return userId;
    }
}
