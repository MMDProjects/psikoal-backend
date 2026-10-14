using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface ICreditTransactionRepository : IRepository<CreditTransaction, Guid>
{
    IQueryable<CreditTransaction> QueryForExpert(Guid expertId);
}
