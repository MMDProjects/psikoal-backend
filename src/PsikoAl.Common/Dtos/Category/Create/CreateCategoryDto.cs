namespace PsikoAl.Common.Dtos.Category.Create;

public sealed record CreateCategoryDto(
    string Slug,
    string Name,
    string Icon,
    string Summary,
    string Description,
    string BlogTag,
    string? AssessmentCategory);
