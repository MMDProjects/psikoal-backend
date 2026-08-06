namespace PsikoAl.Common.Constants;

public static class NotificationTypes
{
    public const string OfferReceived = "OFFER_RECEIVED";
    public const string OfferAccepted = "OFFER_ACCEPTED";
    public const string ListingExpiring = "LISTING_EXPIRING";
    public const string ListingApproved = "LISTING_APPROVED";
    public const string ListingRejected = "LISTING_REJECTED";
    public const string ExpertApproved = "EXPERT_APPROVED";
    public const string ExpertRejected = "EXPERT_REJECTED";
    public const string ReviewApproved = "REVIEW_APPROVED";
    public const string ReviewRejected = "REVIEW_REJECTED";
    public const string System = "SYSTEM";

    public static readonly IReadOnlyList<string> All =
    [
        OfferReceived, OfferAccepted, ListingExpiring, ListingApproved, ListingRejected,
        ExpertApproved, ExpertRejected, ReviewApproved, ReviewRejected, System,
    ];
}
