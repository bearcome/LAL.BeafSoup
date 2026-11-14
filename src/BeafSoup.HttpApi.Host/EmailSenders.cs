using System.Collections.Concurrent;
using BeafSoup.Identity;

namespace BeafSoup.HttpApi.Host;

/// <summary>开发/测试环境的邮件发件箱记录（内存，不持久化）。</summary>
public sealed record OutboxEmail(string To, string Subject, string Body, string Kind, DateTime CreatedAtUtc);

/// <summary>
/// 开发用邮件发送实现（ADR-008）：把邮件写入进程内发件箱，供开发调试与自动化测试取验证码。
/// 生产环境必须替换为真实邮件服务实现。
/// </summary>
public sealed class DevelopmentEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<OutboxEmail> _messages = new();

    public IReadOnlyList<OutboxEmail> Messages => _messages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(new OutboxEmail(
            message.To, message.Subject, message.TextBody, message.Kind, DateTime.UtcNow));
        return Task.CompletedTask;
    }
}

/// <summary>未配置邮件服务的占位实现：发送时明确失败并由调用方记录错误，绝不静默丢码。</summary>
public sealed class MissingEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        => throw new InvalidOperationException(
            "未配置邮件发送实现（IEmailSender）。生产环境必须注册 SMTP/邮件服务实现后才能启用邮箱验证与密码找回。");
}
