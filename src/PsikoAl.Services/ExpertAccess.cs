using Microsoft.EntityFrameworkCore;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Services;

public static class ExpertAccess
{
    // Uzman satırı yoksa sebebin ROL mü yoksa eksik profil mi olduğunu ayırır: bir
    // DANIŞAN'a "Uzman profili bulunamadı, profilinizi tamamlayın" demek onu var olmayan
    // bir işe yönlendirir. Ek sorgu yalnız HATA yolunda koşar.
    public static async Task<DomainException> MissingExpertErrorAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        string roleRequiredErrorKey,
        CancellationToken cancellationToken)
    {
        var role = await unitOfWork.Profiles.Query()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.Role)
            .FirstOrDefaultAsync(cancellationToken);

        return role is not null && role != ProfileRoles.Expert
            ? new DomainException(roleRequiredErrorKey)
            : new DomainException(ErrorKeys.ExpertNotFound);
    }
}
