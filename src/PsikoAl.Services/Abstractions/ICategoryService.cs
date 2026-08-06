using PsikoAl.Common.Dtos.Category;

namespace PsikoAl.Services.Abstractions;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListActiveAsync(CancellationToken cancellationToken);

    Task<CategoryDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    /// Uzmanlık sözlüğünün tek kaynağı categories tablosudur (docs/ADMIN_PANEL_REQUIREMENTS.md §2.6).
    Task<bool> AllActiveNamesExistAsync(IEnumerable<string> names, CancellationToken cancellationToken);
}
