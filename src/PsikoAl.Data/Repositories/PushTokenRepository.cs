using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class PushTokenRepository(AppDbContext dbContext)
    : Repository<PushToken, Guid>(dbContext), IPushTokenRepository
{
    public Task<PushToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
        => DbContext.PushTokens.FirstOrDefaultAsync(pushToken => pushToken.Token == token, cancellationToken);

    public Task<List<string>> GetTokensForUserAsync(Guid userId, CancellationToken cancellationToken)
        => DbContext.PushTokens
            .Where(pushToken => pushToken.UserId == userId)
            .Select(pushToken => pushToken.Token)
            .ToListAsync(cancellationToken);

    public Task DeleteByTokenAsync(string token, CancellationToken cancellationToken)
        => DbContext.PushTokens.Where(pushToken => pushToken.Token == token).ExecuteDeleteAsync(cancellationToken);
}
