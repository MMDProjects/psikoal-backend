using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Middleware;

// Sözleşme §4: dondurulmuş/silinmiş kullanıcı geçerli token'la gelirse 401
// ACCOUNT_INACTIVE. Yalnız yetki isteyen uçlarda koşar: anonim uçlar (login, refresh,
// forgot/reset-password, testler) mobil istemcinin eski token'ı başlıkta taşımasına
// rağmen çalışmaya devam etmeli — login'in kendi AUTH_ACCOUNT_FROZEN yanıtı var.
public sealed class AccountStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAccountStatusService accountStatus)
    {
        if (RequiresAuthorization(context)
            && context.User.Identity?.IsAuthenticated == true
            && Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            && !await accountStatus.IsActiveAsync(userId, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new ApiErrorDto(ErrorKeys.AccountInactive, ErrorMessagesTr.For(ErrorKeys.AccountInactive)),
                context.RequestAborted);
            return;
        }

        await next(context);
    }

    private static bool RequiresAuthorization(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        return endpoint is not null
            && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null
            && endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;
    }
}
