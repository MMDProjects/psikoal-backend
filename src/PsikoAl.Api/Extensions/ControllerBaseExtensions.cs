using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;

namespace PsikoAl.Api.Extensions;

public static class ControllerBaseExtensions
{
    public static Guid CurrentUserId(this ControllerBase controller)
    {
        var value = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new DomainException(ErrorKeys.AuthUserNotFound);
    }

    public static string CurrentUserEmail(this ControllerBase controller)
        => controller.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new DomainException(ErrorKeys.AuthUserNotFound);
}
