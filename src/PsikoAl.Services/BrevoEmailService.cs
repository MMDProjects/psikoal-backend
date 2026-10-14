using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PsikoAl.Common.Presentation;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Options;

namespace PsikoAl.Services;

public sealed partial class BrevoEmailService(
    HttpClient httpClient,
    IOptions<BrevoOptions> options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var brevo = options.Value;
        if (string.IsNullOrWhiteSpace(brevo.ApiKey))
        {
            LogNotConfigured(logger, EmailMasker.Mask(toEmail));
            return;
        }

        using var response = await httpClient.PostAsJsonAsync(
            "v3/smtp/email",
            new
            {
                sender = new { name = brevo.SenderName, email = brevo.SenderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent = htmlBody,
            },
            cancellationToken);

        // Yanıt gövdesi loglanmaz: Brevo hata metninde alıcı adresini geri yazabiliyor.
        // Konu da yazılmaz — bildirim şablonları ilan başlığı gibi sağlık bilgisi taşıyabilir.
        if (!response.IsSuccessStatusCode)
        {
            LogSendFailed(logger, response.StatusCode, EmailMasker.Mask(toEmail));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Brevo API key yapılandırılmamış, e-posta gönderilmedi -> {MaskedEmail}")]
    private static partial void LogNotConfigured(ILogger logger, string maskedEmail);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Brevo e-posta gönderimi başarısız ({Status}) -> {MaskedEmail}")]
    private static partial void LogSendFailed(ILogger logger, HttpStatusCode status, string maskedEmail);
}
