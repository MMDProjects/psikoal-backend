using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Options;

namespace PsikoAl.Services;

public sealed class BrevoEmailService(
    HttpClient httpClient,
    IOptions<BrevoOptions> options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var brevo = options.Value;
        if (string.IsNullOrWhiteSpace(brevo.ApiKey))
        {
            logger.LogWarning("Brevo API key yapılandırılmamış, e-posta gönderilmedi: {Subject} -> {ToEmail}", subject, toEmail);
            return;
        }

        var response = await httpClient.PostAsJsonAsync(
            "v3/smtp/email",
            new
            {
                sender = new { name = brevo.SenderName, email = brevo.SenderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent = htmlBody,
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Brevo e-posta gönderimi başarısız ({Status}): {Body}", response.StatusCode, body);
        }
    }
}
