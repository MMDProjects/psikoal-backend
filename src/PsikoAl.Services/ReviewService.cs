using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Review;
using PsikoAl.Common.Dtos.Review.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class ReviewService(IUnitOfWork unitOfWork) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(
        Guid clientUserId,
        Guid expertId,
        CreateReviewDto request,
        CancellationToken cancellationToken)
    {
        var client = await unitOfWork.Profiles.GetByIdAsync(clientUserId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        if (client.Role != ProfileRoles.Client)
        {
            throw new DomainException(ErrorKeys.ReviewClientRoleRequired);
        }

        if (!await unitOfWork.Experts.ExistsAsync(expertId, cancellationToken))
        {
            throw new DomainException(ErrorKeys.ExpertNotFound);
        }

        if (await unitOfWork.Reviews.ExistsForClientAndExpertAsync(clientUserId, expertId, cancellationToken))
        {
            throw new DomainException(ErrorKeys.ReviewAlreadyExists);
        }

        var review = new Review
        {
            ExpertId = expertId,
            ClientId = clientUserId,
            Rating = request.Rating,
            Comment = request.Comment,
            SessionType = request.SessionType,
        };

        await unitOfWork.Reviews.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ReviewMapper.ToReviewDto(review);
    }

    public async Task<IReadOnlyList<ReviewDto>> ListApprovedForExpertAsync(Guid expertId, CancellationToken cancellationToken)
    {
        var reviews = await unitOfWork.Reviews.Query()
            .Where(review => review.ExpertId == expertId && review.Status == ReviewStatuses.Approved)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. reviews.Select(ReviewMapper.ToReviewDto)];
    }
}
