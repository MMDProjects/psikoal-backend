using PsikoAl.Common.Constants;

namespace PsikoAl.Client.Services;

public sealed class AdminSessionState : IDisposable
{
    public AdminSession? Current { get; private set; }

    public bool IsRestored { get; private set; }

    public string? AccessToken => Current?.AccessToken;

    public string? Email => Current?.Email;

    public string? Role => Current?.Role;

    public bool IsAuthenticated => Current is not null;

    internal SemaphoreSlim RefreshLock { get; } = new(1, 1);

    public event Action? Changed;

    public bool CanAccess(IReadOnlyList<string> allowedRoles)
        => IsAuthenticated && AdminRoleGroups.Allows(allowedRoles, Role);

    public void SignIn(AdminSession session)
    {
        Current = session;
        IsRestored = true;
        Changed?.Invoke();
    }

    public void MarkRestored()
    {
        IsRestored = true;
    }

    public void SignOut()
    {
        Current = null;
        IsRestored = true;
        Changed?.Invoke();
    }

    public void Dispose() => RefreshLock.Dispose();
}
