using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Dtos.Category;
using PsikoAl.Common.Dtos.Category.Create;
using PsikoAl.Common.Dtos.Category.Update;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class AdminCategoryService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : IAdminCategoryService
{
    public async Task<IReadOnlyList<AdminCategoryListItemDto>> ListAllAsync(CancellationToken cancellationToken)
        => await unitOfWork.Categories.Query()
            .OrderBy(category => category.SortOrder)
            .Select(category => new AdminCategoryListItemDto(
                category.Id,
                category.Slug,
                category.Name,
                category.IsActive,
                category.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateAsync(
        Guid actorAuthUserId,
        CreateCategoryDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);

        if (await unitOfWork.Categories.GetBySlugAsync(request.Slug, cancellationToken) is not null)
        {
            throw new DomainException(ErrorKeys.CategorySlugAlreadyExists, "slug");
        }

        var category = new Category
        {
            Slug = request.Slug,
            Name = request.Name,
            Icon = request.Icon,
            Summary = request.Summary,
            Description = request.Description,
            BlogTag = request.BlogTag,
            AssessmentCategory = request.AssessmentCategory,
        };

        await unitOfWork.Categories.AddAsync(category, cancellationToken);
        await AddAuditAsync(actor.Id, "admin.category_create", category.Slug, null, JsonSerializer.Serialize(request), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CategoryMapper.ToCategoryDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(
        Guid actorAuthUserId,
        Guid categoryId,
        UpdateCategoryDto request,
        CancellationToken cancellationToken)
    {
        var actor = await GetRequiredActorAsync(actorAuthUserId, cancellationToken);
        var category = await unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.CategoryNotFound);

        var oldValue = JsonSerializer.Serialize(CategoryMapper.ToCategoryDto(category));

        if (request.Name is not null)
        {
            category.Name = request.Name;
        }

        if (request.Icon is not null)
        {
            category.Icon = request.Icon;
        }

        if (request.Summary is not null)
        {
            category.Summary = request.Summary;
        }

        if (request.Description is not null)
        {
            category.Description = request.Description;
        }

        if (request.BlogTag is not null)
        {
            category.BlogTag = request.BlogTag;
        }

        if (request.AssessmentCategory is not null)
        {
            category.AssessmentCategory = request.AssessmentCategory;
        }

        if (request.IsActive.HasValue)
        {
            category.IsActive = request.IsActive.Value;
        }

        if (request.SortOrder.HasValue)
        {
            category.SortOrder = request.SortOrder.Value;
        }

        await AddAuditAsync(
            actor.Id,
            "admin.category_update",
            category.Slug,
            oldValue,
            JsonSerializer.Serialize(CategoryMapper.ToCategoryDto(category)),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CategoryMapper.ToCategoryDto(category);
    }

    private async Task<AdminUser> GetRequiredActorAsync(Guid actorAuthUserId, CancellationToken cancellationToken)
        => await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

    private async Task AddAuditAsync(
        Guid adminUserId,
        string action,
        string entityId,
        string? oldValue,
        string newValue,
        CancellationToken cancellationToken)
        => await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = adminUserId,
                ActorType = AuditActorTypes.Admin,
                Action = action,
                EntityType = "category",
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
            },
            cancellationToken);
}
