using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Identity;

public class UserAccountTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AttachIdentityLink_DuplicateLink_Throws()
    {
        var account = UserAccount.Create(Guid.NewGuid(), Now);
        account.AttachIdentityLink(new UserIdentityLink("password", "a@b.c"));
        var act = () => account.AttachIdentityLink(new UserIdentityLink("password", "a@b.c"));
        act.Should().Throw<DomainException>().Where(e => e.Code == IdentityErrorCodes.DuplicateIdentityLink);
    }

    [Fact]
    public void AttachIdentityLink_DifferentProvider_Allowed()
    {
        var account = UserAccount.Create(Guid.NewGuid(), Now);
        account.AttachIdentityLink(new UserIdentityLink("password", "a@b.c"));
        account.AttachIdentityLink(new UserIdentityLink("oidc-google", "a@b.c"));
        account.IdentityLinks.Should().HaveCount(2);
    }

    [Fact]
    public void Disable_ThenEnsureActive_Throws()
    {
        var account = UserAccount.Create(Guid.NewGuid(), Now);
        account.Disable();
        var act = () => account.EnsureActive();
        act.Should().Throw<DomainException>().Where(e => e.Code == IdentityErrorCodes.AccountDisabled);
    }
}

public class UserSessionTests
{
    private const long LeaseTtl = 90;
    private const long MaxWindow = 300;
    private static readonly DateTime T0 = new(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void HeartbeatWithinWindow_AccumulatesElapsed()
    {
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), T0);
        session.RecordHeartbeat(T0.AddSeconds(30), MaxWindow);
        session.RecordHeartbeat(T0.AddSeconds(60), MaxWindow);
        session.AccumulatedOnlineSeconds.Should().Be(60);
        session.IsOnline(T0.AddSeconds(60 + LeaseTtl - 1), LeaseTtl).Should().BeTrue();
    }

    [Fact]
    public void HeartbeatGapBeyondMaxWindow_DoesNotBackfillMissingTime()
    {
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), T0);
        session.RecordHeartbeat(T0.AddSeconds(30), MaxWindow);
        // 间隔 1000 秒 ≫ 最大计时窗口 300 秒：只计 300，不追补缺失时段
        session.RecordHeartbeat(T0.AddSeconds(1030), MaxWindow);
        session.AccumulatedOnlineSeconds.Should().Be(30 + 300);
    }

    [Fact]
    public void LeaseExpired_NotOnline_AndExpiryDoesNotExtendLease()
    {
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), T0);
        session.RecordHeartbeat(T0.AddSeconds(30), MaxWindow);
        var afterLease = T0.AddSeconds(30 + LeaseTtl + 1);
        session.IsOnline(afterLease, LeaseTtl).Should().BeFalse();
        // 过期心跳不能延长租约：即使 now 之后再查也不改变判据
        session.IsOnline(afterLease.AddHours(1), LeaseTtl).Should().BeFalse();
    }

    [Fact]
    public void RevokedSession_NotOnline_AndFurtherHeartbeatThrows()
    {
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), T0);
        session.End(T0.AddSeconds(30), SessionEndReason.Revoked, MaxWindow);
        session.IsOnline(T0.AddSeconds(30), LeaseTtl).Should().BeFalse();
        var act = () => session.RecordHeartbeat(T0.AddSeconds(40), MaxWindow);
        act.Should().Throw<DomainException>().Where(e => e.Code == IdentityErrorCodes.SessionEnded);
        session.AccumulatedOnlineSeconds.Should().Be(30);
    }

    [Fact]
    public void EndCountsFinalIntervalUnderWindowCap()
    {
        var session = UserSession.Start(Guid.NewGuid(), Guid.NewGuid(), T0);
        session.End(T0.AddSeconds(10_000), SessionEndReason.Ended, MaxWindow);
        session.AccumulatedOnlineSeconds.Should().Be(MaxWindow);
    }
}
