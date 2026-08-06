using PsikoAl.Common.Constants;

namespace PsikoAl.Data.Entities;

public sealed class Listing
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<string> Specialization { get; set; } = [];

    public decimal BudgetMin { get; set; }

    public decimal BudgetMax { get; set; }

    public required string PreferredSessionType { get; set; }

    public string? City { get; set; }

    public Guid? AssessmentResultId { get; set; }

    public int OfferCount { get; set; }

    public string Status { get; set; } = ListingStatuses.PendingApproval;

    public string? RejectionReason { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Profile? Client { get; set; }
}
