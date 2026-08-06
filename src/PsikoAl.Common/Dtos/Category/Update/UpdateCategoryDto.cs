namespace PsikoAl.Common.Dtos.Category.Update;

public sealed record UpdateCategoryDto(
    string? Name,
    string? Icon,
    string? Summary,
    string? Description,
    string? BlogTag,
    string? AssessmentCategory,
    bool? IsActive,
    int? SortOrder);
