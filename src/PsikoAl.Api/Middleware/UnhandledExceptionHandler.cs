using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos;

namespace PsikoAl.Api.Middleware;

// Beklenmeyen hata eskiden gövdesiz 500 dönüyordu; istemci ekrana ham "Request failed"
// basıyordu. Artık sözleşmedeki ApiErrorDto döner, ayrıntı yalnız logda kalır ve
// log ile yanıt aynı traceId'yi taşır (destek talebinde eşleştirmek için).
public sealed partial class UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(logger, traceId);
            return true;
        }

        LogUnhandled(logger, exception, traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.Headers["trace-id"] = traceId;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiErrorDto(ErrorKeys.InternalError, ErrorMessagesTr.For(ErrorKeys.InternalError)),
            cancellationToken);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "İstemci isteği iptal etti (traceId: {TraceId})")]
    private static partial void LogRequestAborted(ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Beklenmeyen hata (traceId: {TraceId})")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string traceId);
}
