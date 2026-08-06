namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminCategoryListItemDto(
    Guid Id,
    string Slug,
    string Name,
    bool IsActive,
    int SortOrder);
