namespace PsikoAl.Common.Dtos.Review.Create;

public sealed record CreateReviewDto(int Rating, string Comment, string? SessionType);
