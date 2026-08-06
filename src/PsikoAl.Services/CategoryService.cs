using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class CategoryService(IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListActiveAsync(CancellationToken cancellationToken)
        => await unitOfWork.Categories.Query()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .Select(category => CategoryMapper.ToCategoryDto(category))
            .ToListAsync(cancellationToken);

    public async Task<CategoryDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var category = await unitOfWork.Categories.GetBySlugAsync(slug, cancellationToken)
            ?? throw new DomainException(ErrorKeys.CategoryNotFound);

        if (!category.IsActive)
        {
            throw new DomainException(ErrorKeys.CategoryNotFound);
        }

        var expertCount = await unitOfWork.Experts.CountApprovedBySpecializationAsync(category.Name, cancellationToken);

        // completedMatchCount Dilim 5'te matches tablosu gelince gerçek değere bağlanacak.
        const int completedMatchCount = 0;

        return CategoryMapper.ToCategoryDetailDto(category, expertCount, completedMatchCount);
    }

    public Task<bool> AllActiveNamesExistAsync(IEnumerable<string> names, CancellationToken cancellationToken)
        => unitOfWork.Categories.AllActiveNamesExistAsync(names, cancellationToken);
}
