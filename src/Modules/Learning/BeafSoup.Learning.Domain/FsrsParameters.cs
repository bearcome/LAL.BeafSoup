namespace BeafSoup.Learning;

/// <summary>FSRS 参数（ADR-003：算法状态与参数经 IFsrsParameterProvider 隔离，升级时可迁移/重算）。
/// 权重取 FSRS-4.5 参考实现默认值；Hard/Easy 系数为首版实现默认（0.85/1.15），待优化器校准后随 SchedulingVersion 迁移。</summary>
public sealed record FsrsParameters(
    IReadOnlyList<double> Weights,
    double HardFactor,
    double EasyBonus,
    int SchedulingVersion)
{
    public double InitialStability(int grade) => Weights[grade - 1];
    public double InitialDifficultyBase => Weights[4];
    public double InitialDifficultySpread => Weights[5];
    public double DifficultyDrift => Weights[6];
    public double MeanReversion => Weights[7];
    public double StabilityGainScale => Weights[8];
    public double StabilityGainDecay => Weights[9];
    public double StabilityGainExponent => Weights[10];
    public double LapseMultiplier => Weights[11];
    public double LapseDifficultyDecay => Weights[12];
    public double LapseStabilityPower => Weights[13];
    public double LapseRetrievabilityWeight => Weights[14];

    public static FsrsParameters ReferenceDefaults { get; } = new(
        [0.4072, 1.1829, 3.1262, 15.4722, 7.2102, 0.5316, 1.0651, 0.0234, 0.9319,
         0.0007, 0.0002, 0.0007, 0.0000, 0.0000, 0.0014],
        HardFactor: 0.85,
        EasyBonus: 1.15,
        SchedulingVersion: 1);
}

/// <summary>FSRS 参数提供方：按学习配置（目标记忆率等）给出参数；首发使用参考默认。</summary>
public interface IFsrsParameterProvider
{
    FsrsParameters GetParameters(double targetRetentionRate);
}

public sealed class DefaultFsrsParameterProvider : IFsrsParameterProvider
{
    public FsrsParameters GetParameters(double targetRetentionRate) => FsrsParameters.ReferenceDefaults;
}
