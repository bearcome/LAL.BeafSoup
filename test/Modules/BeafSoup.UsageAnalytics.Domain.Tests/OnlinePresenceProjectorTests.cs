using BeafSoup.Identity;
using FluentAssertions;
using Xunit;

namespace BeafSoup.UsageAnalytics;

public class OnlinePresenceProjectorTests
{
    private static readonly PresenceLeasePolicy Policy = PresenceLeasePolicy.InitialDefaults;
    private static readonly DateTime Now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Project_SameUserMultipleSessions_OnlineUsersCountedOnce()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var sessions = new[]
        {
            Online(userA, Now.AddSeconds(-10)),
            Online(userA, Now.AddSeconds(-20)),
            // B 的租约已过期：不计在线
            Online(userB, Now.AddSeconds(-Policy.LeaseTtlSeconds - 30)),
            // 已结束会话：不计在线
            new SessionPresenceSnapshot(Guid.NewGuid(), userB, Now.AddSeconds(-5), 100, IsEnded: true),
        };
        var stats = OnlinePresenceProjector.Project(new AccountStatisticsSnapshot(10, 8), sessions, Policy, Now);
        stats.OnlineUsers.Should().Be(1);
        stats.OnlineSessions.Should().Be(2);
        stats.RegisteredTotal.Should().Be(10);
        stats.ActiveAccounts.Should().Be(8);
        // 累计时长包含全部会话（含已结束）
        stats.AccumulatedOnlineSeconds.Should().Be(sessions.Sum(s => s.AccumulatedOnlineSeconds));
    }

    [Fact]
    public void Project_ExpiredLeaseBeyondTtl_NotCounted()
    {
        var user = Guid.NewGuid();
        var sessions = new[] { Online(user, Now.AddSeconds(-Policy.LeaseTtlSeconds - 1)) };
        var stats = OnlinePresenceProjector.Project(new AccountStatisticsSnapshot(1, 1), sessions, Policy, Now);
        stats.OnlineUsers.Should().Be(0);
        stats.OnlineSessions.Should().Be(0);
    }

    private static SessionPresenceSnapshot Online(Guid userId, DateTime lastHeartbeatAtUtc)
        => new(Guid.NewGuid(), userId, lastHeartbeatAtUtc, 30, IsEnded: false);
}
