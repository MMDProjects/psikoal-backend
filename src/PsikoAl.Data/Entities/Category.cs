namespace PsikoAl.Data.Entities;

public sealed class Category
{
    public Guid Id { get; set; }

    public required string Slug { get; set; }

    public required string Name { get; set; }

    public required string Icon { get; set; }

    public required string Summary { get; set; }

    public required string Description { get; set; }

    public required string BlogTag { get; set; }

    public string? AssessmentCategory { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
