using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IMatchRepository : IRepository<Match, Guid>
{
    IQueryable<Match> QueryWithDetails();

    Task<Match?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<Guid?> AcceptOfferAsync(Guid offerId, Guid actorClientId, CancellationToken cancellationToken);
}
