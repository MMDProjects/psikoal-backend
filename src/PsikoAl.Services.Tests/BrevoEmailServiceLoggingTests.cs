using System.Net;
using Microsoft.Extensions.Logging;
using PsikoAl.Services.Options;

namespace PsikoAl.Services.Tests;

// Log da kişisel veri işleme kaydıdır: alıcı adresi tam yazılmaz, Brevo'nun adresi
// geri yazabilen hata gövdesi ve sağlık bilgisi taşıyabilen konu loglanmaz.
public sealed class BrevoEmailServiceLoggingTests
{
    private const string Recipient = "ayse.yilmaz@gmail.com";
    private const string Subject = "Depresyon ilanınıza teklif";

    [Fact]
    public async Task Failed_send_logs_masked_recipient_only()
    {
        var logger = new CapturingLogger();
        var handler = new StubHandler(HttpStatusCode.BadRequest, $"{{\"message\":\"invalid {Recipient}\"}}");
        var service = new BrevoEmailService(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.test/") },
            Microsoft.Extensions.Options.Options.Create(new BrevoOptions { ApiKey = "key" }),
            logger);

        await service.SendAsync(Recipient, "Ayşe", Subject, "<p>x</p>", CancellationToken.None);

        var message = Assert.Single(logger.Messages);
        Assert.Contains("a***@gmail.com", message, StringComparison.Ordinal);
        Assert.DoesNotContain(Recipient, message, StringComparison.Ordinal);
        Assert.DoesNotContain("Depresyon", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_api_key_logs_masked_recipient_only()
    {
        var logger = new CapturingLogger();
        var service = new BrevoEmailService(
            new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("https://api.brevo.test/") },
            Microsoft.Extensions.Options.Options.Create(new BrevoOptions()),
            logger);

        await service.SendAsync(Recipient, "Ayşe", Subject, "<p>x</p>", CancellationToken.None);

        var message = Assert.Single(logger.Messages);
        Assert.DoesNotContain(Recipient, message, StringComparison.Ordinal);
        Assert.DoesNotContain("Depresyon", message, StringComparison.Ordinal);
    }

    private sealed class CapturingLogger : ILogger<BrevoEmailService>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
