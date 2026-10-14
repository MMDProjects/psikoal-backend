using PsikoAl.Common.Dtos.Category;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Support;

public sealed class AllCategoriesExistStub : ICategoryService
{
    public Task<IReadOnlyList<CategoryDto>> ListActiveAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<CategoryDto>>([]);

    public Task<CategoryDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool> AllActiveNamesExistAsync(IEnumerable<string> names, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
