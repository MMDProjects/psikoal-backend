using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface IListingRepository : IRepository<Listing, Guid>
{
    Task<Listing?> GetWithClientAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountActiveByClientAsync(Guid clientId, CancellationToken cancellationToken);
}
