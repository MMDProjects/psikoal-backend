namespace PsikoAl.Data.Entities;

public sealed class AssessmentQuestion
{
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }

    public required string Text { get; set; }

    public required string Type { get; set; }

    public int SortOrder { get; set; }

    // Ham JSON: [{"id","text","value"}] — AnswerOption listesine mapper'da çözülür.
    public string Options { get; set; } = "[]";
}
