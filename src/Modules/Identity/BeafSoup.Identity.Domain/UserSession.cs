using BeafSoup.SharedKernel;
namespace BeafSoup.Identity;

/// <summary>服务端会话记录：权威心跳与在线时长事实；客户端时间戳不参与口径（ADR-007）。</summary>
public sealed class UserSession
{
    public Guid SessionId { get; }
    public Guid UserId { get; }
    public DateTime StartedAtUtc { get; }
    public DateTime LastHeartbeatAtUtc { get; private set; }
    public long AccumulatedOnlineSeconds { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public SessionEndReason? EndReason { get; private set; }

    private UserSession(Guid sessionId, Guid userId, DateTime startedAtUtc)
        => (SessionId, UserId, StartedAtUtc) = (sessionId, userId, startedAtUtc);

    public static UserSession Start(Guid sessionId, Guid userId, DateTime startedAtUtc)
        => new(sessionId, userId, startedAtUtc) { LastHeartbeatAtUtc = startedAtUtc };

    /// <summary>供持久化还原使用。</summary>
    public static UserSession Restore(Guid sessionId, Guid userId, DateTime startedAtUtc, DateTime lastHeartbeatAtUtc,
        long accumulatedOnlineSeconds, DateTime? endedAtUtc, SessionEndReason? endReason)
        => new(sessionId, userId, startedAtUtc)
        {
            LastHeartbeatAtUtc = lastHeartbeatAtUtc,
            AccumulatedOnlineSeconds = accumulatedOnlineSeconds,
            EndedAtUtc = endedAtUtc,
            EndReason = endReason,
        };

    /// <summary>记录一次服务端接受的心跳：间隔按 min(间隔, 最大计时窗口) 累计，超窗不追补（ADR-007）。</summary>
    public void RecordHeartbeat(DateTime nowUtc, long maxTimingWindowSeconds)
    {
        EnsureActive();
        var deltaSeconds = (long)Math.Max(0, (nowUtc - LastHeartbeatAtUtc).TotalSeconds);
        AccumulatedOnlineSeconds += Math.Min(deltaSeconds, maxTimingWindowSeconds);
        LastHeartbeatAtUtc = nowUtc;
    }

    /// <summary>在线判据：LastHeartbeatAt + 租约 TTL 之后（会话未结束）。过期会话不延长租约。</summary>
    public bool IsOnline(DateTime nowUtc, long leaseTtlSeconds)
        => EndedAtUtc is null && nowUtc <= LastHeartbeatAtUtc.AddSeconds(leaseTtlSeconds);

    public void End(DateTime nowUtc, SessionEndReason reason, long maxTimingWindowSeconds)
    {
        EnsureActive();
        // 结束时刻按规则计入最后一个有效间隔（与心跳同规则，受最大计时窗口约束）
        RecordHeartbeat(nowUtc, maxTimingWindowSeconds);
        EndedAtUtc = nowUtc;
        EndReason = reason;
    }

    public void EnsureActive()
    {
        if (EndedAtUtc is not null)
            throw new DomainException(IdentityErrorCodes.SessionEnded, "会话已结束。");
    }
}
