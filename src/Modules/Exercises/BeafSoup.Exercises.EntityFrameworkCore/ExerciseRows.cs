namespace BeafSoup.Exercises.EntityFrameworkCore;

public sealed record ExerciseInstanceRow
{
    public string Id { get; set; } = "";
    public string TypeKey { get; set; } = "";
    public int PayloadVersion { get; set; }
    public string PayloadJson { get; set; } = "";
    public int Difficulty { get; set; }
    public string? HintKey { get; set; }
}
