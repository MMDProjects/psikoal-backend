using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class MatchRepository(AppDbContext dbContext)
    : Repository<Match, Guid>(dbContext), IMatchRepository
{
    public IQueryable<Match> QueryWithDetails()
        => DbContext.Matches
            .AsNoTracking()
            .Include(match => match.Listing)
            .Include(match => match.AcceptedOffer)
            .Include(match => match.Client)
            .Include(match => match.Expert)
            .ThenInclude(expert => expert!.Profile);

    public Task<Match?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        => DbContext.Matches
            .Include(match => match.Listing)
            .Include(match => match.AcceptedOffer)
            .Include(match => match.Client)
            .Include(match => match.Expert)
            .ThenInclude(expert => expert!.Profile)
            .FirstOrDefaultAsync(match => match.Id == id, cancellationToken);

    public async Task<Guid?> AcceptOfferAsync(Guid offerId, Guid actorClientId, CancellationToken cancellationToken)
    {
        var result = await DbContext.Database
            .SqlQueryRaw<Guid>("select * from public.accept_offer({0}, {1})", offerId, actorClientId)
            .ToListAsync(cancellationToken);
        return result.Count > 0 ? result[0] : null;
    }
}
