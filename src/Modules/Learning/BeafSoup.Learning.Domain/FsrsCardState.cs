namespace BeafSoup.Learning;

public enum FsrsCardStatus { New, Learning, Review, Relearning }

/// <summary>FSRS 卡片状态：类型仅属于 Learning 模块（程序集边界强制），不向其他模块暴露调度内部状态。</summary>
public sealed class FsrsCardState
{
    public FsrsCardStatus Status { get; internal set; } = FsrsCardStatus.New;
    public DateTime DueUtc { get; internal set; } = DateTime.UtcNow;
    public double Stability { get; internal set; }
    public double Difficulty { get; internal set; }
    public int SchedulingVersion { get; internal set; } = 1;

    /// <summary>供持久化还原使用。</summary>
    public static FsrsCardState Restore(FsrsCardStatus status, DateTime dueUtc, double stability, double difficulty, int schedulingVersion)
        => new()
        {
            Status = status,
            DueUtc = dueUtc,
            Stability = stability,
            Difficulty = difficulty,
            SchedulingVersion = schedulingVersion,
        };
}
