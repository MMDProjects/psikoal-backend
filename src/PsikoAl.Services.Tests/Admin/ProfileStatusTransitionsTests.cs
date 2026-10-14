using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;

namespace PsikoAl.Services.Tests.Admin;

public sealed class ProfileStatusTransitionsTests
{
    [Theory]
    [InlineData(ProfileStatuses.Active, ProfileStatuses.Frozen)]
    [InlineData(ProfileStatuses.Frozen, ProfileStatuses.Active)]
    public void EnsureAdminCanChange_AllowsFreezeAndUnfreeze(string current, string target)
    {
        var exception = Record.Exception(() => ProfileStatusTransitions.EnsureAdminCanChange(current, target));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ProfileStatuses.Active)]
    [InlineData(ProfileStatuses.Frozen)]
    public void EnsureAdminCanChange_NeverTouchesADeletedAccount(string target)
    {
        var exception = Assert.Throws<DomainException>(
            () => ProfileStatusTransitions.EnsureAdminCanChange(ProfileStatuses.Deleted, target));

        Assert.Equal(ErrorKeys.AccountDeletedImmutable, exception.ErrorKey);
    }

    [Theory]
    [InlineData(ProfileStatuses.Active, ProfileStatuses.Active)]
    [InlineData(ProfileStatuses.Frozen, ProfileStatuses.Frozen)]
    [InlineData(ProfileStatuses.Active, ProfileStatuses.Deleted)]
    [InlineData(ProfileStatuses.Frozen, ProfileStatuses.Deleted)]
    public void EnsureAdminCanChange_RejectsEveryOtherTransition(string current, string target)
    {
        var exception = Assert.Throws<DomainException>(
            () => ProfileStatusTransitions.EnsureAdminCanChange(current, target));

        Assert.Equal(ErrorKeys.AccountStatusTransitionInvalid, exception.ErrorKey);
    }
}
