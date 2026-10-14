using System.Text.Json;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Admin;
using PsikoAl.Data;
using PsikoAl.Data.Entities;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Admin;

public sealed class AdminNotificationServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AdminUser _admin;
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly AdminNotificationService _service;

    public AdminNotificationServiceTests()
    {
        var (unitOfWork, dbContext) = InMemoryUnitOfWork.Create();
        _dbContext = dbContext;
        _admin = new AdminUser { Id = Guid.NewGuid(), AuthUserId = Guid.NewGuid(), DisplayName = "Süper", Role = AdminRoles.SuperAdmin };

        var adminGuard = Substitute.For<IAdminGuard>();
        adminGuard.GetActiveAdminAsync(_admin.AuthUserId, Arg.Any<CancellationToken>()).Returns(_admin);
        _service = new AdminNotificationService(unitOfWork, adminGuard, _notifications);
    }

    [Fact]
    public async Task PreviewRecipientCountAsync_CountsOnlyActiveProfilesOfTheSegment()
    {
        SeedProfiles();

        var count = await _service.PreviewRecipientCountAsync(SegmentRequest(ProfileRoles.Client), CancellationToken.None);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SendAsync_SkipsFrozenAndDeletedAccountsAndWritesOneAuditEntry()
    {
        var (activeClient, _, _) = SeedProfiles();

        var sent = await _service.SendAsync(_admin.AuthUserId, SegmentRequest(ProfileRoles.Client), CancellationToken.None);

        Assert.Equal(1, sent);
        await _notifications.Received(1).NotifyAsync(
            activeClient,
            NotificationTypes.System,
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            null,
            Arg.Any<CancellationToken>());

        var audit = Assert.Single(_dbContext.AuditLogs);
        Assert.Equal(AuditActions.NotificationSend, audit.Action);
        Assert.Equal(_admin.Id, audit.AdminUserId);
        Assert.Equal("segment:client", audit.EntityId);
        using var newValue = JsonDocument.Parse(audit.NewValue ?? "{}");
        Assert.Equal(1, newValue.RootElement.GetProperty("recipientCount").GetInt32());
    }

    [Fact]
    public async Task SendAsync_DoesNotReachAFrozenUserEvenWhenTargetedDirectly()
    {
        var (_, frozenClient, _) = SeedProfiles();
        var request = new AdminSendNotificationDto("user", frozenClient, null, null, null, "Duyuru", "Metin");

        var sent = await _service.SendAsync(_admin.AuthUserId, request, CancellationToken.None);

        Assert.Equal(0, sent);
        await _notifications.DidNotReceiveWithAnyArgs().NotifyAsync(default, default!, default!, default, default);
    }

    public void Dispose() => _dbContext.Dispose();

    private static AdminSendNotificationDto SegmentRequest(string role)
        => new("segment", null, role, null, null, "Duyuru", "Metin");

    private (Guid Active, Guid Frozen, Guid Deleted) SeedProfiles()
    {
        var active = AddProfile(ProfileRoles.Client, ProfileStatuses.Active);
        var frozen = AddProfile(ProfileRoles.Client, ProfileStatuses.Frozen);
        var deleted = AddProfile(ProfileRoles.Client, ProfileStatuses.Deleted);
        AddProfile(ProfileRoles.Expert, ProfileStatuses.Active);
        _dbContext.SaveChanges();
        return (active, frozen, deleted);
    }

    private Guid AddProfile(string role, string status)
    {
        var profile = new Profile { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@psikoal.test", Role = role, Status = status };
        _dbContext.Profiles.Add(profile);
        return profile.Id;
    }
}
