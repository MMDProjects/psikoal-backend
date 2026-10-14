using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace PsikoAl.Client.Services;

// Oturum tarayıcı sekmesinin sessionStorage'ında, Data Protection ile şifreli tutulur:
// sayfa yenilemesi oturumu düşürmez, sekme kapanınca oturum biter. Okuma/yazma JS
// interop gerektirdiği için yalnız interaktif render sırasında çağrılabilir.
public sealed partial class AdminSessionStore(ProtectedSessionStorage storage, ILogger<AdminSessionStore> logger)
{
    private const string StorageKey = "psikoal.admin.session";

    public async Task<AdminSession?> LoadAsync()
    {
        try
        {
            var result = await storage.GetAsync<AdminSession>(StorageKey);
            return result.Success ? result.Value : null;
        }
        catch (CryptographicException exception)
        {
            LogUnreadableSession(exception);
            await ClearAsync();
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    public async Task SaveAsync(AdminSession session)
    {
        try
        {
            await storage.SetAsync(StorageKey, session);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await storage.DeleteAsync(StorageKey);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stored admin session could not be decrypted; it was discarded.")]
    private partial void LogUnreadableSession(Exception exception);
}
