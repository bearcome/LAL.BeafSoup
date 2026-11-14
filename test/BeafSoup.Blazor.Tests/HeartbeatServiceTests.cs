using BeafSoup.Blazor.Shared.Services;
using BeafSoup.Identity;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

public sealed class HeartbeatServiceTests
{
    [Fact]
    public async Task 启动即发送一次心跳_页面刷新后立即续租()
    {
        var sessions = new CountingSessions();
        var tokens = new StubTokenStore(new AuthTokensDto("token", DateTime.UtcNow.AddHours(1), Guid.NewGuid(), Guid.NewGuid()));
        await using var heartbeat = new HeartbeatService(
            sessions, new StubConnectivity(), tokens, TimeSpan.FromHours(1));

        heartbeat.Start();

        // 首跳不等待 30 秒周期：启动后应立即发送
        await WaitUntilAsync(() => sessions.Heartbeats >= 1);
        sessions.Heartbeats.Should().Be(1);
    }

    [Fact]
    public async Task 无令牌时启动立即报告会话失效()
    {
        var sessions = new CountingSessions();
        await using var heartbeat = new HeartbeatService(
            sessions, new StubConnectivity(), new StubTokenStore(null), TimeSpan.FromHours(1));
        var expired = 0;
        heartbeat.SessionExpired += () => Interlocked.Increment(ref expired);

        heartbeat.Start();

        await WaitUntilAsync(() => Volatile.Read(ref expired) == 1);
        sessions.Heartbeats.Should().Be(0);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("等待心跳条件超时。");
            await Task.Delay(20);
        }
    }

    private sealed class CountingSessions : ISessionAppService
    {
        private int _heartbeats;
        public int Heartbeats => Volatile.Read(ref _heartbeats);

        public Task RecordHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _heartbeats);
            return Task.CompletedTask;
        }

        public Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task RevokeActiveSessionsExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubTokenStore(AuthTokensDto? tokens) : IAuthTokenStore
    {
        public AuthTokensDto? Tokens { get; } = tokens;
        public bool IsExpired => Tokens is null || Tokens.ExpiresAtUtc <= DateTime.UtcNow;
        public Task StoreAsync(AuthTokensDto value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubConnectivity : IConnectivityState
    {
        public bool ApiAvailable => true;
        public event Action? Changed;
        public void ReportUnavailable() => Changed?.Invoke();
        public void ReportAvailable() => Changed?.Invoke();
    }
}
