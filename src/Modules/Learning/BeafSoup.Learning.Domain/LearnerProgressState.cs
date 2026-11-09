namespace BeafSoup.Learning;

/// <summary>学习游戏化状态（按 LearnerProfileId 唯一）：红心/连胜的权威状态；经验与课时完成仍由学习活动推导。</summary>
public sealed class LearnerProgressState
{
    public const int MaxHearts = 5;

    private readonly TimeSpan _heartRegenPeriod;

    public Guid LearnerProfileId { get; }
    public int Hearts { get; private set; }
    public DateTime LastHeartChangeAtUtc { get; private set; }
    public DateTime? LastActivityDateUtc { get; private set; }
    public int DayStreak { get; private set; }

    private LearnerProgressState(Guid learnerProfileId, TimeSpan heartRegenPeriod)
        => (LearnerProfileId, _heartRegenPeriod) = (learnerProfileId, heartRegenPeriod);

    public static LearnerProgressState Create(Guid learnerProfileId, DateTime nowUtc, TimeSpan? heartRegenPeriod = null)
        => new(learnerProfileId, heartRegenPeriod ?? TimeSpan.FromMinutes(30))
        {
            Hearts = MaxHearts,
            LastHeartChangeAtUtc = nowUtc,
        };

    /// <summary>供持久化还原使用。</summary>
    public static LearnerProgressState Restore(Guid learnerProfileId, int hearts, DateTime lastHeartChangeAtUtc,
        DateTime? lastActivityDateUtc, int dayStreak, TimeSpan? heartRegenPeriod = null)
        => new(learnerProfileId, heartRegenPeriod ?? TimeSpan.FromMinutes(30))
        {
            Hearts = hearts,
            LastHeartChangeAtUtc = lastHeartChangeAtUtc,
            LastActivityDateUtc = lastActivityDateUtc,
            DayStreak = dayStreak,
        };

    /// <summary>答错扣心：先按时间恢复，再扣 1（下限 0）；红心为 0 时不再为负。</summary>
    public void RecordMistake(DateTime nowUtc)
    {
        ApplyRegen(nowUtc);
        if (Hearts > 0)
            Hearts--;
        LastHeartChangeAtUtc = nowUtc;
    }

    /// <summary>记录一次学习活动日：今天已有活动不变；昨天延续连胜；否则重置为 1（UTC 口径，ADR-007 时区决策）。</summary>
    public void RecordActivity(DateTime nowUtc)
    {
        ApplyRegen(nowUtc);
        var today = nowUtc.Date;
        DayStreak = LastActivityDateUtc?.Date switch
        {
            null => 1,
            var last when last == today => DayStreak,
            var last when last == today.AddDays(-1) => DayStreak + 1,
            _ => 1,
        };
        LastActivityDateUtc = today;
    }

    /// <summary>自动恢复：每过一个恢复周期 +1 心，上限 MaxHearts。</summary>
    public void ApplyRegen(DateTime nowUtc)
    {
        if (Hearts >= MaxHearts)
            return;
        var elapsed = nowUtc - LastHeartChangeAtUtc;
        var recovered = (int)(elapsed / _heartRegenPeriod);
        if (recovered > 0)
        {
            Hearts = Math.Min(MaxHearts, Hearts + recovered);
            LastHeartChangeAtUtc = LastHeartChangeAtUtc.AddTicks(recovered * _heartRegenPeriod.Ticks);
        }
    }
}
