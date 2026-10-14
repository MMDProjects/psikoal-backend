using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Notifications;
using PsikoAl.Services.Tests.Support;

namespace PsikoAl.Services.Tests.Notifications;

// Sözleşme §7: kilit ekranındaki push metni sağlık bilgisi sızdırmaz (uzman adı ve ilan
// başlığı yok); uygulama içi kayıt ayrıntılı kalır; push data yalnız { type, entityId }.
public sealed class PushPrivacyTests
{
    private const string ListingTitle = "Depresyon ve intihar düşünceleri için destek";

    [Fact]
    public async Task Push_text_is_generic_while_in_app_record_stays_detailed()
    {
        using var database = new TestDatabase();
        var client = await database.AddProfileAsync();
        database.Context.NotificationTemplates.Add(new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Type = NotificationTypes.OfferReceived,
            Title = "{expertName} teklif verdi",
            Body = "{listingTitle} ilanınıza yeni teklif",
            InAppEnabled = true,
            PushEnabled = true,
        });
        await database.Context.SaveChangesAsync();
        var outbox = new RecordingOutbox();
        var service = new NotificationService(
            database.UnitOfWork, outbox, new NoopEmailService(), NullLogger<NotificationService>.Instance);
        var listingId = Guid.NewGuid();

        await service.NotifyAsync(
            client.Id,
            NotificationTypes.OfferReceived,
            new Dictionary<string, string> { ["expertName"] = "Dr. Elif Kaya", ["listingTitle"] = ListingTitle },
            JsonSerializer.Serialize(new { listingId }),
            CancellationToken.None);

        var push = Assert.Single(outbox.Dispatches);
        Assert.Equal("İlanınıza yeni bir teklif geldi.", push.Body);
        Assert.DoesNotContain("Elif", push.Title + push.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Depresyon", push.Title + push.Body, StringComparison.Ordinal);

        using var data = JsonDocument.Parse(push.DataJson);
        Assert.Equal(NotificationTypes.OfferReceived, data.RootElement.GetProperty("type").GetString());
        Assert.Equal(listingId.ToString(), data.RootElement.GetProperty("entityId").GetString());
        Assert.Equal(2, data.RootElement.EnumerateObject().Count());

        var inApp = Assert.Single(database.Context.Notifications);
        Assert.Contains(ListingTitle, inApp.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_notification_type_except_admin_broadcast_has_a_generic_push_text()
    {
        foreach (var type in NotificationTypes.All.Where(type => type != NotificationTypes.System))
        {
            var (title, body) = PushContent.TextFor(type, "SIZINTI başlık", "SIZINTI gövde");
            Assert.DoesNotContain("SIZINTI", title + body, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Admin_broadcast_keeps_its_authored_text()
        => Assert.Equal(("Duyuru", "Yeni testler eklendi"), PushContent.TextFor(NotificationTypes.System, "Duyuru", "Yeni testler eklendi"));

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void Data_without_an_id_has_null_entity(string? dataJson)
    {
        using var data = JsonDocument.Parse(PushContent.DataFor(NotificationTypes.System, dataJson));
        Assert.Equal(JsonValueKind.Null, data.RootElement.GetProperty("entityId").ValueKind);
    }

    private sealed class RecordingOutbox : IPushOutbox
    {
        public List<PushDispatch> Dispatches { get; } = [];

        public bool TryEnqueue(PushDispatch dispatch)
        {
            Dispatches.Add(dispatch);
            return true;
        }

        public IAsyncEnumerable<PushDispatch> ReadAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    // Uygulama içi liste ve push aynı hedef kaydı taşımalı: satıra dokunmak ile bildirime
    // dokunmak aynı ekrana gider.
    [Theory]
    [InlineData("{\"offerId\":\"5f0c\"}", "5f0c")]
    [InlineData(null, null)]
    [InlineData("bozuk", null)]
    public void EntityIdFrom_ReadsTheFirstIdOfTheNotificationData(string? dataJson, string? expected)
        => Assert.Equal(expected, PushContent.EntityIdFrom(dataJson));
}
