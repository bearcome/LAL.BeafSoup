namespace BeafSoup.Identity;

/// <summary>注册事实：服务端成功接受的 UserRegistered 事件，按稳定事件 ID 去重；统计口径的权威来源。</summary>
public sealed record UserRegisteredEvent
{
    public Guid EventId { get; }
    public Guid UserId { get; }
    public DateTime OccurredAtUtc { get; }
    public bool IsTest { get; }

    public UserRegisteredEvent(Guid eventId, Guid userId, DateTime occurredAtUtc, bool isTest)
        => (EventId, UserId, OccurredAtUtc, IsTest) = (eventId, userId, occurredAtUtc, isTest);
}
