namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminReviewListItemDto(
    Guid Id,
    Guid ExpertId,
    string ExpertName,
    string ClientName,
    int Rating,
    string Comment,
    string Status,
    string? RejectionReason,
    DateTimeOffset CreatedAt);
