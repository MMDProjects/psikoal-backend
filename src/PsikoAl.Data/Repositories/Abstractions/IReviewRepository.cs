using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IReviewRepository : IRepository<Review, Guid>
{
    Task<bool> ExistsForMatchAsync(Guid clientId, Guid matchId, CancellationToken cancellationToken);

    Task<double> GetRatingAsync(Guid expertId, CancellationToken cancellationToken);

    Task<int> GetReviewCountAsync(Guid expertId, CancellationToken cancellationToken);
}
