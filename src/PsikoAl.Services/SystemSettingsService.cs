using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class SystemSettingsService(
    IUnitOfWork unitOfWork,
    IAdminGuard adminGuard) : ISystemSettingsService
{
    public async Task<IReadOnlyList<SystemSettingDto>> ListAsync(CancellationToken cancellationToken)
        => await unitOfWork.SystemSettings.Query()
            .OrderBy(setting => setting.Key)
            .Select(setting => new SystemSettingDto(setting.Key, setting.Value, setting.Description, setting.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<SystemSettingDto> UpdateAsync(
        Guid actorAuthUserId,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        var actor = await adminGuard.GetActiveAdminAsync(actorAuthUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.AdminUserNotFound);

        var setting = await unitOfWork.SystemSettings.GetAsync(key, cancellationToken)
            ?? throw new DomainException(ErrorKeys.SystemSettingNotFound, key);

        var oldValue = setting.Value;
        setting.Value = value;
        setting.UpdatedBy = actor.Id;
        setting.UpdatedAt = DateTimeOffset.UtcNow;

        await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                AdminUserId = actor.Id,
                ActorType = AuditActorTypes.Admin,
                Action = "admin.system_setting_update",
                EntityType = "system_setting",
                EntityId = key,
                OldValue = JsonSerializer.Serialize(new { value = oldValue }),
                NewValue = JsonSerializer.Serialize(new { value }),
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new SystemSettingDto(setting.Key, setting.Value, setting.Description, setting.UpdatedAt);
    }
}
