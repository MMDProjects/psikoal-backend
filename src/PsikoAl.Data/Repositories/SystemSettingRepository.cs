using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class SystemSettingRepository(AppDbContext dbContext) : ISystemSettingRepository
{
    public IQueryable<SystemSetting> Query() => dbContext.SystemSettings.AsNoTracking();

    public Task<SystemSetting?> GetAsync(string key, CancellationToken cancellationToken)
        => dbContext.SystemSettings.FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);

    public async Task<int> GetIntAsync(string key, CancellationToken cancellationToken)
    {
        var setting = await GetAsync(key, cancellationToken) ?? throw new DomainException(ErrorKeys.SystemSettingNotFound, key);
        return int.Parse(setting.Value);
    }

    public async Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken)
    {
        var setting = await GetAsync(key, cancellationToken) ?? throw new DomainException(ErrorKeys.SystemSettingNotFound, key);
        return bool.Parse(setting.Value);
    }
}
