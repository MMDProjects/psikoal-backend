namespace PsikoAl.Data.Entities;

public sealed class AssessmentScoreRule
{
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }

    public int MinScore { get; set; }

    public int MaxScore { get; set; }

    public required string Level { get; set; }

    public required string Summary { get; set; }

    public List<string> Suggestions { get; set; } = [];

    public int SortOrder { get; set; }
}
