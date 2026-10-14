using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class CreditTransactionRepository(AppDbContext dbContext)
    : Repository<CreditTransaction, Guid>(dbContext), ICreditTransactionRepository
{
    public IQueryable<CreditTransaction> QueryForExpert(Guid expertId)
        => DbContext.CreditTransactions
            .AsNoTracking()
            .Where(transaction => transaction.ExpertId == expertId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ThenByDescending(transaction => transaction.Id);
}
