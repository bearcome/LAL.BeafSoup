using System.Net;
using System.Net.Http.Json;
using BeafSoup.Identity;
using BeafSoup.UsageAnalytics;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Api.Tests;

[Collection("api")]
public sealed class IdentityAndStatisticsApiTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Register_ThenHeartbeat_ThenStatistics_OnlineCountedByLease()
    {
        var client = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(client, "learner-a@example.com");
        ApiTestFixture.Authenticate(client, tokens);

        var heartbeat = await client.PostAsync("/api/presence/heartbeat", null);
        heartbeat.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var operatorClient = fixture.CreateClient();
        ApiTestFixture.AuthenticateAsStatisticsOperator(operatorClient, tokens);
        var statistics = await operatorClient.GetFromJsonAsync<OnlineStatisticsSnapshot>("/api/usage-analytics/statistics");
        statistics.Should().NotBeNull();
        statistics!.RegisteredTotal.Should().Be(1);
        statistics.ActiveAccounts.Should().Be(1);
        statistics.OnlineUsers.Should().Be(1);
        statistics.OnlineSessions.Should().Be(1);
    }

    [Fact]
    public async Task Register_ReplayedSameEvent_DoesNotDuplicateCount()
    {
        var client = fixture.CreateClient();
        var eventId = Guid.NewGuid();
        await fixture.RegisterAsync(client, "learner-b@example.com", eventId);
        // 客户端超时重试：同一 RegistrationEventId 重放
        await fixture.RegisterAsync(client, "learner-b@example.com", eventId);

        var operatorClient = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(operatorClient, "operator@example.com");
        ApiTestFixture.AuthenticateAsStatisticsOperator(operatorClient, tokens);
        var before = (await operatorClient.GetFromJsonAsync<OnlineStatisticsSnapshot>("/api/usage-analytics/statistics"))!;
        await fixture.RegisterAsync(operatorClient, $"replay-{Guid.NewGuid():N}@example.com");
        var after = (await operatorClient.GetFromJsonAsync<OnlineStatisticsSnapshot>("/api/usage-analytics/statistics"))!;
        // 同一事件重放两次 + 一次新注册 → 累计注册数净增 1（重放不重复计数）
        (after.RegisteredTotal - before.RegisteredTotal).Should().Be(1);
    }

    [Fact]
    public async Task Login_RevokesPreviousSession_SingleSessionPolicy()
    {
        var client = fixture.CreateClient();
        await fixture.RegisterAsync(client, "learner-c@example.com");
        var first = await LoginAsync(client, "learner-c@example.com");
        var second = await LoginAsync(client, "learner-c@example.com");

        var oldSessionClient = fixture.CreateClient();
        ApiTestFixture.Authenticate(oldSessionClient, first);
        (await oldSessionClient.PostAsync("/api/presence/heartbeat", null)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        var newSessionClient = fixture.CreateClient();
        ApiTestFixture.Authenticate(newSessionClient, second);
        (await newSessionClient.PostAsync("/api/presence/heartbeat", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StatisticsEndpoint_RegularUser_ForbiddenByDefault()
    {
        var client = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(client, "regular@example.com");
        ApiTestFixture.Authenticate(client, tokens);
        (await client.GetAsync("/api/usage-analytics/statistics")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StatisticsEndpoint_StatisticsPermission_Allowed()
    {
        var client = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(client, "operator-b@example.com");
        var operatorClient = fixture.CreateClient();
        ApiTestFixture.AuthenticateAsStatisticsOperator(operatorClient, tokens);
        (await operatorClient.GetAsync("/api/usage-analytics/statistics")).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestAccounts_ExcludedFromRegisteredTotal()
    {
        var client = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(client, $"probe-{Guid.NewGuid():N}@example.com");
        var operatorClient = fixture.CreateClient();
        ApiTestFixture.AuthenticateAsStatisticsOperator(operatorClient, tokens);
        var before = (await operatorClient.GetFromJsonAsync<OnlineStatisticsSnapshot>("/api/usage-analytics/statistics"))!;
        // 注册一个测试账户 + 一个真实账户：只有真实账户计入累计注册数
        await fixture.RegisterAsync(operatorClient, $"test-bot-{Guid.NewGuid():N}@example.com", isTest: true);
        await fixture.RegisterAsync(operatorClient, $"real-{Guid.NewGuid():N}@example.com");
        var after = (await operatorClient.GetFromJsonAsync<OnlineStatisticsSnapshot>("/api/usage-analytics/statistics"))!;
        (after.RegisteredTotal - before.RegisteredTotal).Should().Be(1);
    }

    [Fact]
    public async Task Health_Anonymous_ReturnsOk()
    {
        var client = fixture.CreateClient();
        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnauthenticatedRequest_RejectedByFallbackPolicy()
    {
        var client = fixture.CreateClient();
        (await client.PostAsync("/api/presence/heartbeat", null)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<AuthTokensDto> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/identity/login", new { email, password = "passw0rd-xyz" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthTokensDto>())!;
    }
}
