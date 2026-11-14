using BeafSoup.Identity;
using BeafSoup.Identity.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeafSoup.DbMigrator.Tests;

/// <summary>数据保留期清理（ADR-008）：只删除超期的已结束会话与已关闭令牌，幂等。</summary>
public sealed class AccountRetentionTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"retention-{Guid.NewGuid():N}");
    private string _dbPath = "";

    private string ConnectionString => $"Data Source={_dbPath}";

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _dbPath = Path.Combine(_directory, "retention.db");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
        return Task.CompletedTask;
    }

    private IdentityDbContext CreateContext()
        => new(MigrationRunner.CreateOptions<IdentityDbContext>(
            ConnectionString, "__EFMigrationsHistory_Identity"));

    [Fact]
    public async Task 清理只删除超期会话与已关闭令牌_重复执行幂等()
    {
        var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
        var policy = AccountRetentionPolicy.Create(endedSessionRetentionDays: 90, accountTokenRetentionDays: 7);

        await using (var setup = CreateContext())
        {
            await setup.Database.MigrateAsync();
            setup.UserSessions.AddRange(
                EndedSession("old-ended", now.AddDays(-100)),
                EndedSession("recent-ended", now.AddDays(-10)),
                ActiveSession("old-active", now.AddDays(-200)));
            setup.AccountTokens.AddRange(
                Token("old-consumed", now.AddDays(-10), expires: now.AddDays(-9), consumed: now.AddDays(-10)),
                Token("recent-consumed", now.AddDays(-1), expires: now.AddHours(1), consumed: now.AddDays(-1)),
                Token("old-but-open", now.AddDays(-10), expires: now.AddDays(5), consumed: null));
            await setup.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var (sessions, tokens) = await AccountRetentionPurger.PurgeAsync(context, policy, now, CancellationToken.None);
            sessions.Should().Be(1); // 仅 old-ended
            tokens.Should().Be(1);   // 仅 old-consumed（old-but-open 仍未关闭）
        }

        await using (var verify = CreateContext())
        {
            (await verify.UserSessions.Select(s => s.SessionId).ToListAsync())
                .Should().BeEquivalentTo(["recent-ended", "old-active"]);
            (await verify.AccountTokens.Select(t => t.Id).ToListAsync())
                .Should().BeEquivalentTo(["recent-consumed", "old-but-open"]);
        }

        // 幂等：再次清理不删除任何内容
        await using (var again = CreateContext())
        {
            var (sessions, tokens) = await AccountRetentionPurger.PurgeAsync(again, policy, now, CancellationToken.None);
            sessions.Should().Be(0);
            tokens.Should().Be(0);
        }
    }

    private static UserSessionRow EndedSession(string id, DateTime endedAt) => new()
    {
        SessionId = id,
        UserId = Guid.NewGuid().ToString(),
        StartedAtUtc = endedAt.AddMinutes(-10),
        LastHeartbeatAtUtc = endedAt.AddMinutes(-1),
        AccumulatedOnlineSeconds = 540,
        EndedAtUtc = endedAt,
        EndReason = "Ended",
    };

    private static UserSessionRow ActiveSession(string id, DateTime startedAt) => new()
    {
        SessionId = id,
        UserId = Guid.NewGuid().ToString(),
        StartedAtUtc = startedAt,
        LastHeartbeatAtUtc = startedAt,
        AccumulatedOnlineSeconds = 0,
    };

    private static AccountTokenRow Token(string id, DateTime createdAt, DateTime expires, DateTime? consumed) => new()
    {
        Id = id,
        UserId = Guid.NewGuid().ToString(),
        Purpose = "EmailVerification",
        TokenHash = new string('A', 64),
        Salt = "c2FsdA==",
        CreatedAtUtc = createdAt,
        ExpiresAtUtc = expires,
        ConsumedAtUtc = consumed,
    };
}
