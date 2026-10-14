using System.Threading.Channels;

namespace PsikoAl.Services.Notifications;

// Push gönderimi isteği BEKLETMEZ: teklif/onay isteği Expo'nun yanıtını beklemeden döner,
// gönderim arka plan işçisinde yapılır. Kuyruk sınırlı — Expo uzun süre erişilemezse
// en eski bildirim düşer, bellek büyümez (push zaten en iyi çaba; kayıt uygulama içinde).
public sealed class PushOutbox : IPushOutbox
{
    private const int Capacity = 1000;

    private readonly Channel<PushDispatch> _channel = Channel.CreateBounded<PushDispatch>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    public bool TryEnqueue(PushDispatch dispatch) => _channel.Writer.TryWrite(dispatch);

    public IAsyncEnumerable<PushDispatch> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
