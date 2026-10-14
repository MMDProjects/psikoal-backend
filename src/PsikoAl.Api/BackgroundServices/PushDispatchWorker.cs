using PsikoAl.Services.Notifications;

namespace PsikoAl.Api.BackgroundServices;

// Push giden kutusunu (IPushOutbox) boşaltır. Her öğe kendi scope'unda gönderilir; bir gönderimin hatası
// (Expo kapalı, zaman aşımı) loglanır ve sıradakini engellemez.
public sealed partial class PushDispatchWorker(
    IPushOutbox outbox,
    IServiceScopeFactory scopeFactory,
    ILogger<PushDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dispatch in outbox.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<PushDispatcher>();
                await dispatcher.DispatchAsync(dispatch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogDispatchFailed(logger, exception, dispatch.UserId);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push bildirimi gönderilemedi (userId: {UserId})")]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception, Guid userId);
}
