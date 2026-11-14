using System.Net.Http.Json;
using System.Text;
using BeafSoup.Identity;
using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.Exercises.EntityFrameworkCore;
using BeafSoup.Identity.EntityFrameworkCore;
using BeafSoup.Learning.EntityFrameworkCore;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.UsageAnalytics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BeafSoup.Api.Tests;

/// <summary>API 集成测试夹具：独立 SQLite 库 + 测试 JWT 密钥；按 ADR-005 由测试显式迁移（应用运行期不自动迁移）。</summary>
public sealed class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestSigningKey = "api-test-signing-key-with-enough-entropy-0123456789abcdef";
    private readonly string _databasePath;

    public ApiTestFixture()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"beafsoup-api-tests-{Guid.NewGuid():N}.db");
        ConnectionString = $"Data Source={_databasePath}";
        // 环境变量先于宿主构建确定（CreateDefaultConfiguration 读取），覆盖 appsettings.json
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("Presence__LeaseTtlSeconds", "90");
        Environment.SetEnvironmentVariable("Presence__MaxTimingWindowSeconds", "300");
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");

    public async Task InitializeAsync()
    {
        // 测试夹具替代 DbMigrator 显式迁移（ADR-005）
        await using (var context = new LocalizationDbContext(Options<LocalizationDbContext>("__EFMigrationsHistory_Localization")))
            await context.Database.MigrateAsync();
        await using (var context = new IdentityDbContext(Options<IdentityDbContext>("__EFMigrationsHistory_Identity")))
            await context.Database.MigrateAsync();
        await using (var context = new VocabularyDbContext(Options<VocabularyDbContext>("__EFMigrationsHistory_Vocabulary")))
            await context.Database.MigrateAsync();
        await using (var context = new ContentDbContext(Options<ContentDbContext>("__EFMigrationsHistory_Content")))
            await context.Database.MigrateAsync();
        await using (var context = new LearningDbContext(Options<LearningDbContext>("__EFMigrationsHistory_Learning")))
            await context.Database.MigrateAsync();
        await using (var context = new ExercisesDbContext(Options<ExercisesDbContext>("__EFMigrationsHistory_Exercises")))
            await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", null);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", null);
        // 释放 SQLite 连接池占用后再删除库文件
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private DbContextOptions<T> Options<T>(string historyTable) where T : DbContext
        => new DbContextOptionsBuilder<T>()
            .UseSqlite(ConnectionString, sqlite => sqlite.MigrationsHistoryTable(historyTable))
            .Options;

    /// <summary>
    /// 注册并（默认）完成邮箱验证：启用验证要求时经开发发件箱取码验证，返回可用的会话令牌。
    /// verify=false 时返回未验证账户的原始注册结果，供生命周期测试使用。
    /// </summary>
    public async Task<AuthTokensDto> RegisterAsync(
        HttpClient client, string email, Guid? registrationEventId = null, bool isTest = false, bool verify = true)
    {
        var result = await RegisterRawAsync(client, email, registrationEventId, isTest);
        if (result.Tokens is not null)
            return result.Tokens;
        if (!verify)
            throw new InvalidOperationException("注册需要邮箱验证，但调用方要求不验证。");
        var code = await GetLatestCodeAsync(client, email, "EmailVerification");
        var verifyResponse = await client.PostAsJsonAsync("/api/identity/email/verify", new { email, code });
        verifyResponse.EnsureSuccessStatusCode();
        return (await verifyResponse.Content.ReadFromJsonAsync<AuthTokensDto>())!;
    }

    /// <summary>原始注册结果（可能为待验证）：用于验证要求开关与注册幂等测试。</summary>
    public async Task<RegistrationResultDto> RegisterRawAsync(
        HttpClient client, string email, Guid? registrationEventId = null, bool isTest = false)
    {
        var response = await client.PostAsJsonAsync("/api/identity/register", new
        {
            email,
            password = "passw0rd-xyz",
            baseLanguage = (string?)null,
            targetLanguage = (string?)null,
            registrationEventId = registrationEventId ?? Guid.NewGuid(),
            isTestAccount = isTest,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegistrationResultDto>())!;
    }

    /// <summary>从开发发件箱读取该邮箱最近一封指定用途邮件的 6 位验证码（仅 Testing 环境可用）。</summary>
    public async Task<string> GetLatestCodeAsync(HttpClient client, string email, string kind)
    {
        var messages = await client.GetFromJsonAsync<List<OutboxEmailDto>>("/api/dev/email-outbox") ?? [];
        var message = messages
            .LastOrDefault(m => string.Equals(m.To, email.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
                && m.Kind == kind)
            ?? throw new InvalidOperationException($"发件箱中没有 {email} 的 {kind} 邮件。");
        var match = System.Text.RegularExpressions.Regex.Match(message.Body, @"\b\d{6}\b");
        return match.Success
            ? match.Value
            : throw new InvalidOperationException($"邮件正文中没有 6 位验证码：{message.Body}");
    }

    /// <summary>开发发件箱条目（仅测试断言使用）。</summary>
    public sealed record OutboxEmailDto(string To, string Subject, string Body, string Kind, DateTime CreatedAtUtc);

    public static void Authenticate(HttpClient client, AuthTokensDto tokens)
        => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

    /// <summary>签发带统计权限的"运营"令牌（复用已注册账户的活动会话 sid）：验证统计权限默认拒绝普通用户、按稳定标识开放。</summary>
    public static string MintStatisticsOperatorToken(AuthTokensDto sessionTokens)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "BeafSoup",
            Audience = "BeafSoup.Client",
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = sessionTokens.UserId.ToString(),
                ["sid"] = sessionTokens.SessionId.ToString(),
                ["permission"] = UsageAnalyticsPermissions.StatisticsPolicy,
            },
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static void AuthenticateAsStatisticsOperator(HttpClient client, AuthTokensDto sessionTokens)
        => client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", MintStatisticsOperatorToken(sessionTokens));
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiTestFixture>;
