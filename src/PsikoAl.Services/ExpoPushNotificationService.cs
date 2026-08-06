using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services;

public sealed class ExpoPushNotificationService(HttpClient httpClient, ILogger<ExpoPushNotificationService> logger)
    : IPushNotificationService
{
    private const int BatchSize = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        string? dataJson,
        CancellationToken cancellationToken)
    {
        if (tokens.Count == 0)
        {
            return [];
        }

        JsonElement? data = dataJson is null ? null : JsonSerializer.Deserialize<JsonElement>(dataJson);
        var invalidTokens = new List<string>();

        foreach (var batch in tokens.Chunk(BatchSize))
        {
            var messages = batch.Select(token => new ExpoPushMessage(token, title, body, data, "default")).ToArray();

            using var response = await httpClient.PostAsJsonAsync(
                "--/api/v2/push/send",
                messages,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Expo push gönderimi başarısız: {Status}", response.StatusCode);
                continue;
            }

            var result = await response.Content.ReadFromJsonAsync<ExpoPushResponse>(JsonOptions, cancellationToken);
            if (result?.Data is null)
            {
                continue;
            }

            for (var i = 0; i < result.Data.Count && i < batch.Length; i++)
            {
                var ticket = result.Data[i];
                if (ticket.Status == "error" && ticket.Details?.Error == "DeviceNotRegistered")
                {
                    invalidTokens.Add(batch[i]);
                }
                else if (ticket.Status == "error")
                {
                    logger.LogWarning("Expo push hatası: {Message}", ticket.Message);
                }
            }
        }

        return invalidTokens;
    }

    private sealed record ExpoPushMessage(string To, string Title, string Body, JsonElement? Data, string Sound);

    private sealed record ExpoPushResponse(List<ExpoPushTicket>? Data);

    private sealed record ExpoPushTicket(string Status, string? Message, ExpoPushTicketDetails? Details);

    private sealed record ExpoPushTicketDetails(string? Error);
}
