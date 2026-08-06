namespace PsikoAl.Common.Dtos.Category;

public sealed record CategoryDto(
    Guid Id,
    string Slug,
    string Name,
    string Icon,
    string Summary,
    string Description,
    string BlogTag,
    string? AssessmentCategory);
