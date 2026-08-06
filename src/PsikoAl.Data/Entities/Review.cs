using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class Review
{
    public Guid Id { get; set; }

    public Guid ExpertId { get; set; }

    public Guid ClientId { get; set; }

    /// Dilim 5'te matches tablosuna FK + NOT NULL olacak.
    public Guid? MatchId { get; set; }

    public int Rating { get; set; }

    public required string Comment { get; set; }

    public string? SessionType { get; set; }

    public string Status { get; set; } = ReviewStatuses.Pending;

    public string? RejectionReason { get; set; }

    public DateTimeOffset? ModeratedAt { get; set; }

    public Guid? ModeratedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Profile? Client { get; set; }
}
