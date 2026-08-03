using Microsoft.EntityFrameworkCore;
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
}
