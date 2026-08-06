namespace PsikoAl.Data.Entities;

public sealed class AssessmentResult
{
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }

    public Guid? UserId { get; set; }

    public int Score { get; set; }

    public required string Level { get; set; }

    public required string Summary { get; set; }

    public List<string> Suggestions { get; set; } = [];

    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Assessment? Assessment { get; set; }
}
