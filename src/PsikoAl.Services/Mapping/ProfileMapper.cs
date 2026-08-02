using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Data.Entities;

namespace PsikoAl.Services.Mapping;

public static class ProfileMapper
{
    public static AuthUserDto ToAuthUserDto(Profile profile)
        => new(
            profile.Id,
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.Role,
            profile.IsVerified,
            profile.AvatarUrl,
            profile.CreatedAt,
            profile.Phone,
            profile.City,
            profile.ShareEmail,
            profile.SharePhone,
            profile.ShareLocation);
}
