using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Assessments;

// GET /assessment/results/{id} eskiden kimliksiz ve sahiplik kontrolsüzdü: id'yi bilen
// herkes başkasının depresyon/kaygı sonucunu okuyabiliyordu (KVKK özel nitelikli veri).
public sealed class AssessmentResultAccessTests
{
    [Fact]
    public async Task Owner_can_read_own_result()
    {
        using var database = new TestDatabase();
        var owner = await database.AddProfileAsync();
        var result = await database.AddAssessmentResultAsync(owner.Id);
        var service = new AssessmentService(database.UnitOfWork);

        var dto = await service.GetResultAsync(result.Id, owner.Id, CancellationToken.None);

        Assert.Equal(result.Id, dto.Id);
    }

    [Fact]
    public async Task Another_user_gets_not_found_for_an_owned_result()
    {
        using var database = new TestDatabase();
        var owner = await database.AddProfileAsync();
        var stranger = await database.AddProfileAsync();
        var result = await database.AddAssessmentResultAsync(owner.Id);
        var service = new AssessmentService(database.UnitOfWork);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.GetResultAsync(result.Id, stranger.Id, CancellationToken.None));

        Assert.Equal(ErrorKeys.AssessmentResultNotFound, exception.ErrorKey);
    }

    [Fact]
    public async Task Anonymous_viewer_gets_not_found_for_an_owned_result()
    {
        using var database = new TestDatabase();
        var owner = await database.AddProfileAsync();
        var result = await database.AddAssessmentResultAsync(owner.Id);
        var service = new AssessmentService(database.UnitOfWork);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.GetResultAsync(result.Id, viewerUserId: null, CancellationToken.None));

        Assert.Equal(ErrorKeys.AssessmentResultNotFound, exception.ErrorKey);
    }

    [Fact]
    public async Task Anonymous_result_stays_readable_by_id()
    {
        using var database = new TestDatabase();
        var result = await database.AddAssessmentResultAsync(userId: null);
        var service = new AssessmentService(database.UnitOfWork);

        var dto = await service.GetResultAsync(result.Id, viewerUserId: null, CancellationToken.None);

        Assert.Equal(result.Id, dto.Id);
    }
}
