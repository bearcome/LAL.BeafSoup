using BeafSoup.SharedKernel;

namespace BeafSoup.Exercises;

/// <summary>习题实例聚合：题型注册键 + 版本化负载（JSON）+ 难度；负载校验由对应 handler 执行。</summary>
public sealed class ExerciseInstance
{
    public Guid Id { get; }
    public string TypeKey { get; }
    public int PayloadVersion { get; }
    public string PayloadJson { get; }
    public int Difficulty { get; }
    public string? HintKey { get; }

    public ExerciseInstance(Guid id, string typeKey, int payloadVersion, string payloadJson, int difficulty, string? hintKey = null)
    {
        if (payloadVersion < 1)
            throw new DomainException(ExerciseErrorCodes.UnsupportedPayloadVersion, "负载版本必须不小于 1。");
        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "习题负载不能为空。");
        if (difficulty < 1)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "难度必须不小于 1。");
        (Id, TypeKey, PayloadVersion, PayloadJson, Difficulty, HintKey)
            = (id, typeKey.Trim(), payloadVersion, payloadJson, difficulty, hintKey);
    }
}
