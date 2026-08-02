namespace PsikoAl.Client.Services;

public sealed class AdminSessionState
{
    public string? AccessToken { get; private set; }

    public string? Email { get; private set; }

    public bool IsAuthenticated => AccessToken is not null;

    public event Action? Changed;

    public void SignIn(string accessToken, string email)
    {
        AccessToken = accessToken;
        Email = email;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        AccessToken = null;
        Email = null;
        Changed?.Invoke();
    }
}
