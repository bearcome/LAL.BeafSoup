using System.Net;
using System.Net.Http.Json;
using BeafSoup.Identity;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Api.Tests;

/// <summary>账户生命周期验收（ADR-008）：邮箱验证、密码找回、注销、数据导出与保留期。</summary>
[Collection("api")]
public sealed class AccountLifecycleApiTests(ApiTestFixture fixture)
{
    private static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task 注册待验证_未验证登录被拒_验证后取得会话()
    {
        var client = fixture.CreateClient();
        var email = NewEmail("verify-flow");

        var registration = await fixture.RegisterRawAsync(client, email);
        registration.VerificationRequired.Should().BeTrue();
        registration.Tokens.Should().BeNull();

        var login = await client.PostAsJsonAsync("/api/identity/login",
            new { email, password = "passw0rd-xyz" });
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var code = await fixture.GetLatestCodeAsync(client, email, "EmailVerification");
        var verify = await client.PostAsJsonAsync("/api/identity/email/verify", new { email, code });
        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = (await verify.Content.ReadFromJsonAsync<AuthTokensDto>())!;
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        ApiTestFixture.Authenticate(client, tokens);

        var account = await client.GetFromJsonAsync<AccountProfileDto>("/api/identity/account");
        account!.EmailVerified.Should().BeTrue();
        account.Email.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task 验证码一次性_重复使用被拒()
    {
        var client = fixture.CreateClient();
        var email = NewEmail("verify-once");
        await fixture.RegisterRawAsync(client, email);
        var code = await fixture.GetLatestCodeAsync(client, email, "EmailVerification");

        var first = await client.PostAsJsonAsync("/api/identity/email/verify", new { email, code });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/identity/email/verify", new { email, code });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task 密码找回对任意邮箱返回相同结果()
    {
        var client = fixture.CreateClient();
        var existing = NewEmail("forgot-existing");
        await fixture.RegisterAsync(client, existing);

        var known = await client.PostAsJsonAsync("/api/identity/password/forgot", new { email = existing });
        var unknown = await client.PostAsJsonAsync("/api/identity/password/forgot",
            new { email = NewEmail("forgot-missing") });

        known.StatusCode.Should().Be(HttpStatusCode.Accepted);
        unknown.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // 仅存在的账户收到重置码
        var messages = await client.GetFromJsonAsync<List<ApiTestFixture.OutboxEmailDto>>("/api/dev/email-outbox");
        messages!.Should().Contain(m => m.To == existing.ToLowerInvariant() && m.Kind == "PasswordReset");
    }

    [Fact]
    public async Task 密码重置后旧会话与旧密码失效()
    {
        var client = fixture.CreateClient();
        var email = NewEmail("reset");
        var oldTokens = await fixture.RegisterAsync(client, email);

        await client.PostAsJsonAsync("/api/identity/password/forgot", new { email });
        var code = await fixture.GetLatestCodeAsync(client, email, "PasswordReset");
        var reset = await client.PostAsJsonAsync("/api/identity/password/reset",
            new { email, code, newPassword = "new-passw0rd" });
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 旧会话（注册时签发）已被吊销
        var oldSessionClient = fixture.CreateClient();
        ApiTestFixture.Authenticate(oldSessionClient, oldTokens);
        (await oldSessionClient.GetAsync("/api/identity/account")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        // 旧密码不可再用，新密码可登录
        var oldLogin = await client.PostAsJsonAsync("/api/identity/login", new { email, password = "passw0rd-xyz" });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var newLogin = await client.PostAsJsonAsync("/api/identity/login", new { email, password = "new-passw0rd" });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task 数据导出只包含本人数据()
    {
        var clientA = fixture.CreateClient();
        var emailA = NewEmail("export-a");
        var tokensA = await fixture.RegisterAsync(clientA, emailA);
        ApiTestFixture.Authenticate(clientA, tokensA);

        var clientB = fixture.CreateClient();
        var emailB = NewEmail("export-b");
        var tokensB = await fixture.RegisterAsync(clientB, emailB);
        ApiTestFixture.Authenticate(clientB, tokensB);

        var export = await clientA.GetFromJsonAsync<AccountExportDto>("/api/identity/account/export");
        export!.Account.Email.Should().Be(emailA.ToLowerInvariant());
        export.Learning.Profiles.Should().NotBeEmpty();
        export.Learning.Profiles.Should().OnlyContain(p => p.BaseLanguage == "zh-CN" && p.TargetLanguage == "de-DE");
        var serialized = System.Text.Json.JsonSerializer.Serialize(export);
        serialized.Should().NotContain(emailB.ToLowerInvariant());
        serialized.Should().NotContain(tokensB.UserId.ToString());
    }

    [Fact]
    public async Task 注销需要密码_注销后删除个人数据且可重新注册()
    {
        var client = fixture.CreateClient();
        var email = NewEmail("delete");
        var tokens = await fixture.RegisterAsync(client, email);
        ApiTestFixture.Authenticate(client, tokens);

        var wrong = await client.PostAsJsonAsync("/api/identity/account/delete", new { password = "wrong-password" });
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var deleted = await client.PostAsJsonAsync("/api/identity/account/delete", new { password = "passw0rd-xyz" });
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 注销后：旧会话失效、学习数据不可再访问
        var staleClient = fixture.CreateClient();
        ApiTestFixture.Authenticate(staleClient, tokens);
        (await staleClient.GetAsync("/api/identity/account")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 同一邮箱可重新注册（个人标识已移除）
        var reused = await fixture.RegisterRawAsync(fixture.CreateClient(), email);
        reused.VerificationRequired.Should().BeTrue();
        reused.Email.Should().Be(email.ToLowerInvariant());
    }
}
