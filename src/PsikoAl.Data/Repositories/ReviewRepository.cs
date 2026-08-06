using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class ReviewRepository(AppDbContext dbContext)
    : Repository<Review, Guid>(dbContext), IReviewRepository
{
    public Task<bool> ExistsForMatchAsync(Guid clientId, Guid matchId, CancellationToken cancellationToken)
        => DbContext.Reviews.AnyAsync(
            review => review.ClientId == clientId && review.MatchId == matchId,
            cancellationToken);

    public async Task<double> GetRatingAsync(Guid expertId, CancellationToken cancellationToken)
    {
        var rating = await DbContext.ExpertRatings.AsNoTracking()
            .Where(expertRating => expertRating.ExpertId == expertId)
            .Select(expertRating => (double?)expertRating.Rating)
            .FirstOrDefaultAsync(cancellationToken);
        return rating ?? 0;
    }

    public async Task<int> GetReviewCountAsync(Guid expertId, CancellationToken cancellationToken)
    {
        var count = await DbContext.ExpertRatings.AsNoTracking()
            .Where(expertRating => expertRating.ExpertId == expertId)
            .Select(expertRating => (int?)expertRating.ReviewCount)
            .FirstOrDefaultAsync(cancellationToken);
        return count ?? 0;
    }
}
