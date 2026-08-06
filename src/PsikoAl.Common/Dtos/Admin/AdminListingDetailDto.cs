namespace PsikoAl.Common.Dtos.Admin;

public sealed record AdminListingDetailDto(
    Guid Id,
    string ClientFullName,
    string ClientEmail,
    string Title,
    string Description,
    IReadOnlyList<string> Specialization,
    string BudgetLabel,
    string PreferredSessionType,
    string? City,
    string Status,
    string? RejectionReason,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
