using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface ISystemSettingRepository
{
    IQueryable<SystemSetting> Query();

    Task<SystemSetting?> GetAsync(string key, CancellationToken cancellationToken);

    Task<int> GetIntAsync(string key, CancellationToken cancellationToken);

    Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken);
}
