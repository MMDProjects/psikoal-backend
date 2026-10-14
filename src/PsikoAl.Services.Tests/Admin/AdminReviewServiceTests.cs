using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Admin;

public sealed class AdminReviewServiceTests
{
    private readonly AdminTestDoubles _doubles = new(AdminRoles.Moderator);
    private readonly IReviewRepository _reviews = Substitute.For<IReviewRepository>();
    private readonly AdminReviewService _service;

    public AdminReviewServiceTests()
    {
        _doubles.UnitOfWork.Reviews.Returns(_reviews);
        _service = new AdminReviewService(_doubles.UnitOfWork, _doubles.AdminGuard, Substitute.For<INotificationService>());
    }

    [Fact]
    public async Task RejectAsync_UnpublishesAnApprovedReviewWithItsReason()
    {
        var review = GivenReview(ReviewStatuses.Approved);

        await _service.RejectAsync(_doubles.Admin.AuthUserId, review.Id, "kişisel veri", CancellationToken.None);

        Assert.Equal(ReviewStatuses.Rejected, review.Status);
        Assert.Equal("kişisel veri", review.RejectionReason);
        var audit = Assert.Single(_doubles.AddedAuditLogs);
        Assert.Equal(AuditActions.ReviewUnpublish, audit.Action);
        Assert.Equal("kişisel veri", audit.Reason);
    }

    [Fact]
    public async Task RejectAsync_KeepsTheRejectActionForPendingReviews()
    {
        var review = GivenReview(ReviewStatuses.Pending);

        await _service.RejectAsync(_doubles.Admin.AuthUserId, review.Id, "spam", CancellationToken.None);

        Assert.Equal("admin.review_reject", Assert.Single(_doubles.AddedAuditLogs).Action);
    }

    private Review GivenReview(string status)
    {
        var review = new Review { Id = Guid.NewGuid(), Comment = "Yorum", Rating = 5, Status = status };
        _reviews.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        return review;
    }
}
