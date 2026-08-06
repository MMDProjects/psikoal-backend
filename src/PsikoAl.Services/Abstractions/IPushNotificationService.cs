namespace PsikoAl.Services.Abstractions;

public interface IPushNotificationService
{
    // Gönderim sonrası artık geçersiz (DeviceNotRegistered) olan tokenları döner —
    // çağıran taraf bu tokenları push_tokens'tan siler.
    Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        string? dataJson,
        CancellationToken cancellationToken);
}
