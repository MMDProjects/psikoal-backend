namespace PsikoAl.Common.Dtos.Review.Create;

public sealed record CreateReviewDto(Guid MatchId, int Rating, string Comment, string? SessionType);
