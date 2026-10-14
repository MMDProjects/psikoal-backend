using System.Text.Json;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Admin;

public sealed class AdminUserServiceTests
{
    private readonly AdminTestDoubles _doubles = new(AdminRoles.Moderator);
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly ISupabaseAdminService _supabaseAdmin = Substitute.For<ISupabaseAdminService>();
    private readonly IAccountStatusService _accountStatus = Substitute.For<IAccountStatusService>();
    private readonly AdminUserService _service;

    public AdminUserServiceTests()
    {
        _doubles.UnitOfWork.Profiles.Returns(_profiles);
        _service = new AdminUserService(_doubles.UnitOfWork, _supabaseAdmin, _doubles.AdminGuard, _accountStatus);
    }

    [Fact]
    public async Task FreezeUserAsync_BansTheAccountAndWritesAnAuditEntry()
    {
        var profile = GivenProfile(ProfileStatuses.Active);

        await _service.FreezeUserAsync(_doubles.Admin.AuthUserId, profile.Id, "spam", CancellationToken.None);

        Assert.Equal(ProfileStatuses.Frozen, profile.Status);
        await _supabaseAdmin.Received(1).BanUserAsync(profile.Id, Arg.Any<CancellationToken>());
        var audit = Assert.Single(_doubles.AddedAuditLogs);
        Assert.Equal("admin.user_freeze", audit.Action);
        Assert.Equal(_doubles.Admin.Id, audit.AdminUserId);
        Assert.Equal("spam", audit.Reason);
        Assert.Equal(ProfileStatuses.Active, ReadStatus(audit.OldValue));
        Assert.Equal(ProfileStatuses.Frozen, ReadStatus(audit.NewValue));
        await _doubles.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnfreezeUserAsync_ReactivatesAFrozenAccount()
    {
        var profile = GivenProfile(ProfileStatuses.Frozen);

        await _service.UnfreezeUserAsync(_doubles.Admin.AuthUserId, profile.Id, null, CancellationToken.None);

        Assert.Equal(ProfileStatuses.Active, profile.Status);
        await _supabaseAdmin.Received(1).UnbanUserAsync(profile.Id, Arg.Any<CancellationToken>());
        Assert.Equal("admin.user_unfreeze", Assert.Single(_doubles.AddedAuditLogs).Action);
    }

    [Fact]
    public async Task UnfreezeUserAsync_NeverReactivatesADeletedAccount()
    {
        var profile = GivenProfile(ProfileStatuses.Deleted);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.UnfreezeUserAsync(_doubles.Admin.AuthUserId, profile.Id, null, CancellationToken.None));

        Assert.Equal(ErrorKeys.AccountDeletedImmutable, exception.ErrorKey);
        Assert.Equal(ProfileStatuses.Deleted, profile.Status);
        await _supabaseAdmin.DidNotReceiveWithAnyArgs().UnbanUserAsync(default, default);
        Assert.Empty(_doubles.AddedAuditLogs);
        await _doubles.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task FreezeUserAsync_RejectsAnAlreadyFrozenAccount()
    {
        var profile = GivenProfile(ProfileStatuses.Frozen);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => _service.FreezeUserAsync(_doubles.Admin.AuthUserId, profile.Id, null, CancellationToken.None));

        Assert.Equal(ErrorKeys.AccountStatusTransitionInvalid, exception.ErrorKey);
        await _supabaseAdmin.DidNotReceiveWithAnyArgs().BanUserAsync(default, default);
    }

    private Profile GivenProfile(string status)
    {
        var profile = new Profile { Id = Guid.NewGuid(), Email = "kisi@psikoal.test", Status = status };
        _profiles.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        return profile;
    }

    private static string? ReadStatus(string? json)
        => json is null ? null : JsonDocument.Parse(json).RootElement.GetProperty("status").GetString();
}
