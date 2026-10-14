using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;

namespace PsikoAl.Services;

public static class ProfileStatusTransitions
{
    public static void EnsureAdminCanChange(string currentStatus, string targetStatus)
    {
        if (currentStatus == ProfileStatuses.Deleted)
        {
            throw new DomainException(ErrorKeys.AccountDeletedImmutable, "status");
        }

        var isAllowed = (currentStatus, targetStatus) switch
        {
            (ProfileStatuses.Active, ProfileStatuses.Frozen) => true,
            (ProfileStatuses.Frozen, ProfileStatuses.Active) => true,
            _ => false,
        };

        if (!isAllowed)
        {
            throw new DomainException(ErrorKeys.AccountStatusTransitionInvalid, "status");
        }
    }
}
