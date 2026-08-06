using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class ListingRepository(AppDbContext dbContext)
    : Repository<Listing, Guid>(dbContext), IListingRepository
{
    public Task<Listing?> GetWithClientAsync(Guid id, CancellationToken cancellationToken)
        => DbContext.Listings
            .Include(listing => listing.Client)
            .FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);

    public Task<int> CountActiveByClientAsync(Guid clientId, CancellationToken cancellationToken)
        => DbContext.Listings
            .Where(listing => listing.ClientId == clientId && ListingStatuses.Active.Contains(listing.Status))
            .CountAsync(cancellationToken);
}
