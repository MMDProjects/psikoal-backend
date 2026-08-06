using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Api.Extensions;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Dtos.Category.Create;
using PsikoAl.Common.Dtos.Category.Update;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("admin/categories")]
[Authorize(Policy = "Admin")]
public sealed class AdminCategoriesController(IAdminCategoryService adminCategoryService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminCategoryListItemDto>> List(CancellationToken cancellationToken)
        => adminCategoryService.ListAllAsync(cancellationToken);

    [HttpPost]
    public Task<CategoryDto> Create(CreateCategoryDto request, CancellationToken cancellationToken)
        => adminCategoryService.CreateAsync(this.CurrentUserId(), request, cancellationToken);

    [HttpPatch("{id:guid}")]
    public Task<CategoryDto> Update(Guid id, UpdateCategoryDto request, CancellationToken cancellationToken)
        => adminCategoryService.UpdateAsync(this.CurrentUserId(), id, request, cancellationToken);
}
