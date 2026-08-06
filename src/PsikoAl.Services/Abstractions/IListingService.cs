using PsikoAl.Common.Dtos.Listing;
using PsikoAl.Common.Dtos.Listing.Create;

namespace PsikoAl.Services.Abstractions;

public sealed record ListingFeedFilters(
    IReadOnlyList<string>? Specialization,
    IReadOnlyList<string>? SessionType,
    decimal? BudgetMin,
    decimal? BudgetMax,
    string? Sort);

public sealed record ListingListResult(IReadOnlyList<ListingDto> Data, int Total, int? ActiveCount);

public interface IListingService
{
    Task<ListingDto> CreateAsync(Guid clientUserId, CreateListingDto request, CancellationToken cancellationToken);

    Task<ListingListResult> ListFeedAsync(ListingFeedFilters filters, CancellationToken cancellationToken);

    Task<ListingListResult> ListMyAsync(Guid clientUserId, string? status, CancellationToken cancellationToken);

    Task<ListingDto> GetByIdAsync(Guid listingId, Guid viewerUserId, CancellationToken cancellationToken);

    Task<ListingDto> CloseAsync(Guid clientUserId, Guid listingId, CancellationToken cancellationToken);
}
