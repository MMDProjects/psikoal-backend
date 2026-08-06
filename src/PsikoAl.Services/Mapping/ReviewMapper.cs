using PsikoAl.Common.Dtos.Review;
using PsikoAl.Common.Presentation;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class ReviewMapper
{
    public static ReviewDto ToReviewDto(Review review)
        => new(
            review.Id,
            review.ExpertId,
            review.Rating,
            review.Comment,
            review.SessionType,
            review.CreatedAt,
            RelativeTimeTr.From(review.CreatedAt));
}
