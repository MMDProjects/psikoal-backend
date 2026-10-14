using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos;
using PsikoAl.Common.Exceptions;

namespace PsikoAl.Api.Middleware;

public sealed class DomainExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

            // `message` alanı eskiden hata KODUNUN kendisiydi. Mobil onu olduğu gibi
            // ekrana bastığı için kullanıcı şifre ekranında "AUTH_INVALID_CREDENTIALS",
            // ilan ekranında "LISTING_MAX_ACTIVE_EXCEEDED" görüyordu. Kod makineye,
            // mesaj kullanıcıya ait: `code` sözleşme gereği aynen korunuyor —
            // mobil onu dallanma için kullanıyor — `message` artık Türkçe.
            await context.Response.WriteAsJsonAsync(
                new ApiErrorDto(
                    exception.ErrorKey,
                    ErrorMessagesTr.For(exception.ErrorKey),
                    exception.Field),
                context.RequestAborted);
        }
    }
}
