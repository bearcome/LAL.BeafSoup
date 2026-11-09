using BeafSoup.SharedKernel;

namespace BeafSoup.Learning;

/// <summary>FSRS-4.5 调度核心（纯函数，时间/参数全部注入）：记忆保留率 R(t,S)、难度迁移与稳定性更新。</summary>
public static class FsrsScheduler
{
    private const double Decay = -0.5;
    private const double Factor = 19.0 / 81.0;

    /// <summary>记忆保留率：R(t,S) = (1 + FACTOR·t/S)^DECAY，t 为距上次复习的天数。</summary>
    public static double Retrievability(double elapsedDays, double stability)
    {
        if (stability <= 0)
            return 0;
        return Math.Pow(1 + Factor * elapsedDays / stability, Decay);
    }

    /// <summary>初始难度：D₀(G) = clamp(w₄ − e^{w₅·(G−1)} + 1, 1, 10)。</summary>
    public static double InitialDifficulty(int grade, FsrsParameters p)
        => Math.Clamp(p.InitialDifficultyBase - Math.Exp(p.InitialDifficultySpread * (grade - 1)) + 1, 1, 10);

    /// <summary>复习后难度：向初始难度均值回归，按评分漂移（难升易降）。</summary>
    public static double NextDifficulty(double difficulty, int grade, FsrsParameters p)
    {
        var drift = p.DifficultyDrift * (3 - grade);
        var moved = difficulty + drift * (10 - difficulty) / 9;
        return p.MeanReversion * InitialDifficulty(4, p) + (1 - p.MeanReversion) * moved;
    }

    /// <summary>复习后稳定性：Again 用遗忘分支；Hard/Good/Easy 用增益分支并按系数调整。</summary>
    public static double NextStability(double difficulty, double stability, double retrievability, int grade, FsrsParameters p)
    {
        if (grade == 1)
        {
            var lapsed = p.LapseMultiplier
                * Math.Pow(difficulty, -p.LapseDifficultyDecay)
                * (Math.Pow(stability, 1 - p.LapseStabilityPower) - 1)
                * Math.Exp((1 - retrievability) * p.LapseRetrievabilityWeight);
            return Math.Max(lapsed, 0.1);
        }
        var gained = stability
            * (1 + Math.Exp(p.StabilityGainScale)
                 * (11 - difficulty)
                 * Math.Pow(stability, -p.StabilityGainDecay)
                 * (Math.Exp((1 - retrievability) * p.StabilityGainExponent) - 1));
        return grade switch
        {
            2 => gained * p.HardFactor,
            4 => gained * p.EasyBonus,
            _ => gained,
        };
    }

    /// <summary>执行一次评级：更新 FSRS 状态与到期时间（now 注入，领域层不取系统时间）。</summary>
    public static void ApplyReview(FsrsCardState state, int grade, DateTime nowUtc, FsrsParameters p)
    {
        if (grade is < 1 or > 4)
            throw new DomainException(LearningErrorCodes.InvalidRating, "评级必须在 1（Again）到 4（Easy）之间。");

        if (state.Status is FsrsCardStatus.New)
        {
            state.Stability = p.InitialStability(grade);
            state.Difficulty = InitialDifficulty(grade, p);
        }
        else
        {
            var elapsedDays = Math.Max(0, (nowUtc - state.DueUtc).TotalDays + state.Stability);
            var retrievability = Retrievability(elapsedDays, state.Stability);
            state.Stability = NextStability(state.Difficulty, state.Stability, retrievability, grade, p);
            state.Difficulty = NextDifficulty(state.Difficulty, grade, p);
        }

        // 间隔 < 1 天留在学习态；达到 1 天毕业为复习态；Again 一律进入（重）学习态
        state.Status = grade switch
        {
            1 => state.Status is FsrsCardStatus.Review ? FsrsCardStatus.Relearning : FsrsCardStatus.Learning,
            _ => state.Stability >= 1 ? FsrsCardStatus.Review : FsrsCardStatus.Learning,
        };
        state.DueUtc = nowUtc.AddDays(state.Stability);
        state.SchedulingVersion = p.SchedulingVersion;
    }
}
