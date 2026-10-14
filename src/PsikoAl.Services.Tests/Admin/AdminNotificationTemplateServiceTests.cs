using System.Text.Json;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Services.Tests.Admin;

public sealed class AdminNotificationTemplateServiceTests
{
    [Fact]
    public async Task UpdateAsync_WritesOldAndNewValuesToTheAuditLog()
    {
        var doubles = new AdminTestDoubles();
        var templates = Substitute.For<INotificationTemplateRepository>();
        doubles.UnitOfWork.NotificationTemplates.Returns(templates);
        var template = new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Type = NotificationTypes.System,
            Title = "Eski başlık",
            Body = "Eski gövde",
            IsActive = true,
        };
        templates.GetByTypeAsync(NotificationTypes.System, Arg.Any<CancellationToken>()).Returns(template);
        var service = new AdminNotificationTemplateService(doubles.UnitOfWork, doubles.AdminGuard);

        await service.UpdateAsync(
            doubles.Admin.AuthUserId,
            NotificationTypes.System,
            new UpdateAdminNotificationTemplateDto("Yeni başlık", "Yeni gövde", null, true, false, true, true),
            CancellationToken.None);

        var audit = Assert.Single(doubles.AddedAuditLogs);
        Assert.Equal(AuditActions.NotificationTemplateUpdate, audit.Action);
        Assert.Equal(NotificationTypes.System, audit.EntityId);
        Assert.Equal("Eski başlık", ReadTitle(audit.OldValue));
        Assert.Equal("Yeni başlık", ReadTitle(audit.NewValue));
        await doubles.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static string? ReadTitle(string? json)
    {
        using var document = JsonDocument.Parse(json ?? "{}");
        return document.RootElement.GetProperty("title").GetString();
    }
}
