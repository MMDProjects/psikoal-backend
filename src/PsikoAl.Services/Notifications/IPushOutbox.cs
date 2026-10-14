namespace PsikoAl.Services.Notifications;

public interface IPushOutbox
{
    bool TryEnqueue(PushDispatch dispatch);

    IAsyncEnumerable<PushDispatch> ReadAllAsync(CancellationToken cancellationToken);
}
