using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Dtos.Category.Create;
using PsikoAl.Common.Dtos.Category.Update;

namespace PsikoAl.Services.Abstractions;

public interface IAdminCategoryService
{
    Task<IReadOnlyList<AdminCategoryListItemDto>> ListAllAsync(CancellationToken cancellationToken);

    Task<CategoryDto> CreateAsync(Guid actorAuthUserId, CreateCategoryDto request, CancellationToken cancellationToken);

    Task<CategoryDto> UpdateAsync(Guid actorAuthUserId, Guid categoryId, UpdateCategoryDto request, CancellationToken cancellationToken);
}
