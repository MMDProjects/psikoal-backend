using PsikoAl.Common.Dtos.Offer;
using PsikoAl.Common.Dtos.Offer.Create;

namespace PsikoAl.Services.Abstractions;

public interface IOfferService
{
    Task<OfferDto> CreateAsync(Guid expertUserId, CreateOfferDto request, CancellationToken cancellationToken);

    Task<OfferListResult> ListMyAsync(Guid expertUserId, string? status, CancellationToken cancellationToken);

    Task<OfferListResult> ListForListingAsync(Guid listingId, Guid viewerUserId, CancellationToken cancellationToken);

    Task<OfferDto> GetByIdAsync(Guid offerId, Guid viewerUserId, CancellationToken cancellationToken);

    Task<OfferDto> AcceptAsync(Guid clientUserId, Guid offerId, CancellationToken cancellationToken);

    Task<OfferDto> RejectAsync(Guid clientUserId, Guid offerId, CancellationToken cancellationToken);

    Task<OfferDto> WithdrawAsync(Guid expertUserId, Guid offerId, CancellationToken cancellationToken);
}
