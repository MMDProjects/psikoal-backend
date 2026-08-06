using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class ExpertRepository(AppDbContext dbContext)
    : Repository<Expert, Guid>(dbContext), IExpertRepository
{
    public Task<Expert?> GetWithProfileAsync(Guid id, CancellationToken cancellationToken)
        => DbContext.Experts
            .Include(expert => expert.Profile)
            .FirstOrDefaultAsync(expert => expert.Id == id, cancellationToken);

    public Task<int> CountApprovedBySpecializationAsync(string specialization, CancellationToken cancellationToken)
        => DbContext.Experts
            .Where(expert => expert.Status == ExpertStatuses.Approved && expert.Specializations.Contains(specialization))
            .CountAsync(cancellationToken);
}
