using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class Match
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }

    public Guid AcceptedOfferId { get; set; }

    public Guid ClientId { get; set; }

    public Guid ExpertId { get; set; }

    public string Status { get; set; } = MatchStatuses.Active;

    public DateTimeOffset? ClientReleasedAt { get; set; }

    public DateTimeOffset? ExpertReleasedAt { get; set; }

    public bool ReleasedByAdmin { get; set; }

    public string? ReleaseReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Listing? Listing { get; set; }

    public Offer? AcceptedOffer { get; set; }

    public Profile? Client { get; set; }

    public Expert? Expert { get; set; }
}
