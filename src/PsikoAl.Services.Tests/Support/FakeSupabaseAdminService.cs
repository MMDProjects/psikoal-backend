using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Support;

public sealed class FakeSupabaseAdminService : ISupabaseAdminService
{
    public Guid UserIdToCreate { get; set; } = Guid.NewGuid();

    public List<Guid> BannedUserIds { get; } = [];

    public List<Guid> UnbannedUserIds { get; } = [];

    public Task<Guid> CreateConfirmedUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        CancellationToken cancellationToken)
        => Task.FromResult(UserIdToCreate);

    public Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task BanUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        BannedUserIds.Add(userId);
        return Task.CompletedTask;
    }

    public Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        UnbannedUserIds.Add(userId);
        return Task.CompletedTask;
    }
}
