using Microsoft.AspNetCore.Mvc;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var categories = await categoryService.ListActiveAsync(cancellationToken);
        return Ok(new { data = categories, meta = new { page = 1, total = categories.Count, perPage = categories.Count } });
    }

    [HttpGet("{slug}")]
    public Task<CategoryDetailDto> GetBySlug(string slug, CancellationToken cancellationToken)
        => categoryService.GetBySlugAsync(slug, cancellationToken);
}
