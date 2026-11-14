using BeafSoup.Identity;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Identity.Domain.Tests;

public sealed class AccountLifecycleDomainTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    private static AccountToken NewToken(TimeSpan? lifetime = null)
        => AccountToken.Issue(Guid.NewGuid(), AccountTokenPurpose.EmailVerification,
            tokenHash: "ABCDEF", salt: "c2FsdA==", Now, lifetime ?? TimeSpan.FromMinutes(30));

    [Fact]
    public void 令牌一次性_重复使用被拒()
    {
        var token = NewToken();
        token.Consume(Now);
        var act = () => token.Consume(Now.AddMinutes(1));
        act.Should().Throw<DomainException>().Which.Code.Should().Be(IdentityErrorCodes.InvalidOrExpiredToken);
    }

    [Fact]
    public void 过期令牌不可用()
    {
        var token = NewToken(TimeSpan.FromMinutes(30));
        var act = () => token.Consume(Now.AddMinutes(30));
        act.Should().Throw<DomainException>().Which.Code.Should().Be(IdentityErrorCodes.InvalidOrExpiredToken);
    }

    [Fact]
    public void 错误尝试达到上限即作废()
    {
        var token = NewToken();
        for (var i = 0; i < AccountToken.MaxFailedAttempts; i++)
            token.RegisterFailedAttempt(Now);

        token.FailedAttempts.Should().Be(AccountToken.MaxFailedAttempts);
        token.ConsumedAtUtc.Should().Be(Now);
        var act = () => token.Consume(Now.AddSeconds(1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void 验证码散列比较_仅正确验证码通过()
    {
        var salt = AccountTokenCrypto.GenerateSalt();
        var code = AccountTokenCrypto.GenerateCode();
        code.Should().MatchRegex("^\\d{6}$");

        var hash = AccountTokenCrypto.ComputeHash(salt, code);
        AccountTokenCrypto.Matches(salt, code, hash).Should().BeTrue();
        AccountTokenCrypto.Matches(salt, "000000" == code ? "111111" : "000000", hash).Should().BeFalse();
        AccountTokenCrypto.Matches(salt, "", hash).Should().BeFalse();
    }

    [Fact]
    public void 邮箱验证幂等且保留首次验证时间()
    {
        var account = UserAccount.Create(Guid.NewGuid(), Now);
        account.IsEmailVerified.Should().BeFalse();

        account.MarkEmailVerified(Now.AddMinutes(1));
        account.MarkEmailVerified(Now.AddMinutes(5));

        account.IsEmailVerified.Should().BeTrue();
        account.EmailVerifiedAtUtc.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void 注销移除身份关联_保留匿名账户标识()
    {
        var userId = Guid.NewGuid();
        var account = UserAccount.Create(userId, Now);
        account.AttachIdentityLink(new UserIdentityLink("password", "someone@example.com"));

        account.DetachAllIdentityLinks();
        account.Delete();

        account.IdentityLinks.Should().BeEmpty();
        account.Status.Should().Be(UserAccountStatus.Deleted);
        account.Id.Should().Be(userId); // 匿名注册事实保留
        var act = account.EnsureActive;
        act.Should().Throw<DomainException>(); // 已删除账户不可再用
    }

    [Fact]
    public void 保留期策略拒绝非正天数并给出截止时间()
    {
        var invalid = () => AccountRetentionPolicy.Create(0, 7);
        invalid.Should().Throw<DomainException>();

        var policy = AccountRetentionPolicy.Create(90, 7);
        policy.SessionCutoffUtc(Now).Should().Be(Now.AddDays(-90));
        policy.TokenCutoffUtc(Now).Should().Be(Now.AddDays(-7));
    }
}
