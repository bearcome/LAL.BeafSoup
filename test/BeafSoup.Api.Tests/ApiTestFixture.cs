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

    public async Task<AuthTokensDto> RegisterAsync(HttpClient client, string email, Guid? registrationEventId = null, bool isTest = false)
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
        return (await response.Content.ReadFromJsonAsync<AuthTokensDto>())!;
    }

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
