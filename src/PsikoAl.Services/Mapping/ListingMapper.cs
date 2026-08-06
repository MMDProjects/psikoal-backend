using PsikoAl.Common.Dtos.Listing;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class ListingMapper
{
    public static ListingDto ToListingDto(
        Listing listing,
        Profile client,
        bool viewerIsOwner,
        bool viewerHasOffered,
        Guid? viewerOfferId,
        AssessmentResult? assessmentResult = null)
        => new(
            listing.Id,
            listing.ClientId,
            new ListingClientDto(
                client.Id,
                ExpertMapper.InitialsOf(client.FirstName, client.LastName),
                client.AvatarUrl,
                client.CreatedAt),
            viewerIsOwner
                ? $"{client.FirstName} {client.LastName}"
                : NameMasker.Mask($"{client.FirstName} {client.LastName}"),
            listing.Title,
            listing.Description,
            listing.Specialization,
            listing.BudgetMin,
            listing.BudgetMax,
            listing.PreferredSessionType,
            listing.City,
            listing.OfferCount,
            listing.Status,
            BudgetLabelFormatter.Format(listing.BudgetMin, listing.BudgetMax),
            listing.ExpiresAt,
            listing.CreatedAt,
            RelativeTimeTr.From(listing.CreatedAt),
            viewerHasOffered,
            viewerOfferId,
            listing.RejectionReason,
            assessmentResult is null
                ? null
                : new ListingAssessmentResultDto(
                    assessmentResult.Id,
                    assessmentResult.Score,
                    assessmentResult.Level,
                    assessmentResult.Summary,
                    assessmentResult.Assessment?.Title ?? string.Empty));
}
