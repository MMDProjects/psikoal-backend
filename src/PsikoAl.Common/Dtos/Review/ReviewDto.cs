namespace PsikoAl.Common.Dtos.Review;

public sealed record ReviewDto(
    Guid Id,
    Guid ExpertId,
    int Rating,
    string Comment,
    string? SessionType,
    DateTimeOffset CreatedAt,
    string CreatedAtRelative);
