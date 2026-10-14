using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos;

namespace PsikoAl.Client.Services;

public static class AdminErrorMessages
{
    public const string NotAnAdmin = "Bu hesabın yönetici yetkisi yok.";
    public const string Forbidden = "Bu işlem için yetkiniz yok.";
    public const string SessionExpired = "Oturumunuz sona erdi. Tekrar giriş yapın.";
    public const string ApiUnreachable = "Sunucuya ulaşılamıyor. Biraz sonra tekrar deneyin.";
    public const string LoginFailed = "Giriş başarısız. Bilgilerinizi kontrol edin.";

    public static async Task<string> FromResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return SessionExpired;
            case HttpStatusCode.Forbidden:
                return Forbidden;
            case HttpStatusCode.TooManyRequests:
                return ErrorMessagesTr.For(ErrorKeys.AuthRateLimited);
            case HttpStatusCode.BadRequest:
                return ErrorMessagesTr.For(ErrorKeys.ValidationFailed);
        }

        if ((int)response.StatusCode >= 500)
        {
            return ErrorMessagesTr.For(ErrorKeys.InternalError);
        }

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(cancellationToken);
            return string.IsNullOrWhiteSpace(error?.Message)
                ? ErrorMessagesTr.For(ErrorKeys.InternalError)
                : error.Message;
        }
        catch (JsonException)
        {
            return ErrorMessagesTr.For(ErrorKeys.InternalError);
        }
    }
}
