namespace BeafSoup.Learning.EntityFrameworkCore;

public sealed class LearnerProfileRow
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string BaseLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    public double TargetRetentionRate { get; set; }
    public string ThemePreference { get; set; } = "";
    public string InflectionDisplay { get; set; } = "";
    public string InspectorTrigger { get; set; } = "";
}

public sealed class ReviewCardRow
{
    public string ProfileId { get; set; } = "";
    public string ConceptId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime DueUtc { get; set; }
    public double Stability { get; set; }
    public double Difficulty { get; set; }
    public int SchedulingVersion { get; set; }
}

public sealed class FormMasteryRow
{
    public string ProfileId { get; set; } = "";
    public string CellId { get; set; } = "";
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

public sealed class LearningActivityRow
{
    public string Id { get; set; } = "";
    public string DedupeKey { get; set; } = "";
    public string ProfileId { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public string? ExerciseInstanceId { get; set; }
    public string? LessonId { get; set; }
    public bool IsCorrect { get; set; }
}

public sealed class LearnerProgressStateRow
{
    public string ProfileId { get; set; } = "";
    public int Hearts { get; set; }
    public DateTime LastHeartChangeAtUtc { get; set; }
    public DateTime? LastActivityDateUtc { get; set; }
    public int DayStreak { get; set; }
}
