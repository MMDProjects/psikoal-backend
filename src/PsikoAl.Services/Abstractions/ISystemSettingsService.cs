using PsikoAl.Common.Dtos.Admin;

namespace PsikoAl.Services.Abstractions;

public interface ISystemSettingsService
{
    Task<IReadOnlyList<SystemSettingDto>> ListAsync(CancellationToken cancellationToken);

    Task<SystemSettingDto> UpdateAsync(Guid actorAuthUserId, string key, string value, CancellationToken cancellationToken);
}
