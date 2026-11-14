namespace BeafSoup.Learning;

// ---------- 学习者数据权利：导出与注销（ADR-008） ----------

public sealed record LearnerProfileExportDto(
    Guid ProfileId,
    string BaseLanguage,
    string TargetLanguage,
    double TargetRetentionRate,
    string ThemePreference,
    string InflectionDisplay,
    string InspectorTrigger);

public sealed record LearningActivityExportDto(
    Guid ActivityId,
    string Type,
    string DedupeKey,
    Guid? ExerciseInstanceId,
    Guid? LessonId,
    bool IsCorrect,
    DateTime OccurredAtUtc);

public sealed record ReviewCardExportDto(
    Guid ConceptId,
    string Status,
    DateTime DueUtc,
    double Stability,
    double Difficulty,
    int SchedulingVersion);

public sealed record FormMasteryExportDto(
    Guid CellId,
    int CorrectCount,
    int IncorrectCount,
    DateTime? LastSeenAtUtc);

/// <summary>本人学习数据快照：只包含该 UserId 名下的学习配置与学习事实，不含他人数据。</summary>
public sealed record LearnerDataExportDto(
    IReadOnlyList<LearnerProfileExportDto> Profiles,
    IReadOnlyList<LearningActivityExportDto> Activities,
    IReadOnlyList<ReviewCardExportDto> ReviewCards,
    IReadOnlyList<FormMasteryExportDto> FormMastery);

/// <summary>数据导出查询（实现在 EntityFrameworkCore；只读）。</summary>
public interface ILearnerDataExportQuery
{
    Task<LearnerDataExportDto> ExportAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>账户数据擦除：删除该用户全部学习配置及其学习事实（账户注销，ADR-008）。</summary>
public interface IAccountDataEraser
{
    Task EraseAsync(Guid userId, CancellationToken cancellationToken);
}
