using PsikoAl.Common.Dtos.Category;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class CategoryMapper
{
    public static CategoryDto ToCategoryDto(Category category)
        => new(
            category.Id,
            category.Slug,
            category.Name,
            category.Icon,
            category.Summary,
            category.Description,
            category.BlogTag,
            category.AssessmentCategory);

    public static CategoryDetailDto ToCategoryDetailDto(Category category, int expertCount, int completedMatchCount)
        => new(
            category.Id,
            category.Slug,
            category.Name,
            category.Icon,
            category.Summary,
            category.Description,
            category.BlogTag,
            category.AssessmentCategory,
            expertCount,
            completedMatchCount);
}
