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
            await context.Response.WriteAsJsonAsync(
                new ApiErrorDto(exception.ErrorKey, exception.ErrorKey, exception.Field),
                context.RequestAborted);
        }
    }
}
