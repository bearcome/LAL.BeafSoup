using System.Text.Json;

namespace BeafSoup.Exercises;

/// <summary>习题负载契约：所有负载必须显式携带版本号（版本不匹配明确失败）。</summary>
public interface IExercisePayload
{
    int Version { get; }
}

/// <summary>
/// 题型处理器：负载校验（发布/加载时明确失败，不静默退化）+ 判分（按题目负载声明的正确答案引用/可接受词形）。
/// 实现 IExerciseHandler 并注册键即可新增题型，课程引擎零改动（ADR-001）。
/// </summary>
public interface IExerciseHandler
{
    string TypeKey { get; }

    /// <summary>支持的负载版本；版本不匹配的负载必须明确失败。</summary>
    int PayloadVersion { get; }

    /// <summary>校验负载：未知模式/不兼容版本/违反题型不变量 → DomainException。</summary>
    void ValidatePayload(JsonElement payload);

    /// <summary>按题型判分惯例判分；不按可能歧义的显示文本推断。</summary>
    ExerciseGrade Grade(JsonElement payload, JsonElement answer);

    /// <summary>负载声明的复习关联；无明确词形/概念关联的题型返回 null（不推断词汇复习）。</summary>
    ReviewLink? TryGetReviewLink(JsonElement payload);
}
