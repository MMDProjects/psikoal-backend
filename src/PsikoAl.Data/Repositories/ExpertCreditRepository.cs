using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class ExpertCreditRepository(AppDbContext dbContext) : IExpertCreditRepository
{
    public Task<int?> GetBalanceAsync(Guid expertId, CancellationToken cancellationToken)
        => dbContext.Experts
            .AsNoTracking()
            .Where(expert => expert.Id == expertId)
            .Select(expert => (int?)expert.CreditBalance)
            .FirstOrDefaultAsync(cancellationToken);

    // ToListAsync bilinçli: FirstOrDefault sorguyu alt sorguya sarar ve
    // UPDATE ... RETURNING alt sorgu olarak çalışmaz.
    public async Task<int?> TryApplyDeltaAsync(Guid expertId, int delta, CancellationToken cancellationToken)
    {
        var balances = await dbContext.Database
            .SqlQuery<int>(
                $"""
                update public.experts
                   set credit_balance = credit_balance + {delta}
                 where id = {expertId}
                   and credit_balance + {delta} >= 0
                returning credit_balance as "Value"
                """)
            .ToListAsync(cancellationToken);

        return balances.Count == 1 ? balances[0] : null;
    }
}
