using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IPushTokenRepository : IRepository<PushToken, Guid>
{
    Task<PushToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    Task<List<string>> GetTokensForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task DeleteByTokenAsync(string token, CancellationToken cancellationToken);
}
