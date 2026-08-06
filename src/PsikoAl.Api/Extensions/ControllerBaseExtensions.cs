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

    // Auth gerektirmeyen uçlarda (örn. assessment/submit) opsiyonel kullanıcı bağlamı içindir —
    // token varsa okunur, yoksa null döner; [Authorize] gibi 401 fırlatmaz.
    public static Guid? CurrentUserIdOrNull(this ControllerBase controller)
    {
        var value = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    public static string CurrentUserEmail(this ControllerBase controller)
        => controller.User.FindFirstValue(ClaimTypes.Email)
            ?? throw new DomainException(ErrorKeys.AuthUserNotFound);
}
