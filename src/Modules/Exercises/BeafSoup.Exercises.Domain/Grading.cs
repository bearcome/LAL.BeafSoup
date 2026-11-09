namespace BeafSoup.Exercises;

/// <summary>判分结果：即时反馈（反馈文案键由 UI 层从数据层解析）。</summary>
public sealed record ExerciseGrade(bool Correct, string? FeedbackKey);

/// <summary>选择题选项：OptionId 为负载内稳定实例标识（同一显示文本的重复选项也是不同实例）。</summary>
public sealed record ExerciseOption(string OptionId, string Text);

/// <summary>复习关联：负载声明的 (ParadigmCellId, 概念键)；仅明确关联词形/概念的结果供 SRS 与形态掌握度，泛理解返回 null。</summary>
public sealed record ReviewLink(Guid? CellId, string? ConceptKey);
