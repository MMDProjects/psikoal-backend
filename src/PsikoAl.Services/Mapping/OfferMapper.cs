using PsikoAl.Common.Dtos.Offer;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class OfferMapper
{
    public static OfferDto ToOfferDto(Offer offer, bool viewerIsListingOwner, double expertRating)
    {
        var listingDto = offer.Listing is { } listing ? ToListingEmbedDto(listing, viewerIsListingOwner) : null;
        var expertDto = offer.Expert is { Profile: { } profile } expert
            ? new OfferExpertDto(
                expert.Id,
                $"{profile.FirstName} {profile.LastName}",
                expert.Title,
                ExpertMapper.InitialsOf(profile.FirstName, profile.LastName),
                profile.AvatarUrl,
                expertRating)
            : null;

        return new OfferDto(
            offer.Id,
            offer.ListingId,
            offer.ExpertId,
            offer.Title,
            offer.Price,
            offer.Description,
            offer.SessionType,
            offer.Status,
            offer.MatchId,
            offer.CreatedAt,
            RelativeTimeTr.From(offer.CreatedAt),
            listingDto,
            expertDto);
    }

    private static OfferListingDto ToListingEmbedDto(Listing listing, bool viewerIsListingOwner)
    {
        var client = listing.Client;
        var fullName = client is null ? "Danışan" : $"{client.FirstName} {client.LastName}";
        var clientDisplayName = viewerIsListingOwner ? fullName : NameMasker.Mask(fullName);
        var clientDto = client is null
            ? null
            : new OfferListingClientDto(client.Id, ExpertMapper.InitialsOf(client.FirstName, client.LastName), client.AvatarUrl);

        return new OfferListingDto(listing.Id, listing.Title, listing.ClientId, listing.City, clientDisplayName, clientDto);
    }
}
