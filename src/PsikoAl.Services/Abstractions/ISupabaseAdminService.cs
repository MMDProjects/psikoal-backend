namespace PsikoAl.Services.Abstractions;

public interface ISupabaseAdminService
{
    Task<Guid> CreateConfirmedUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        CancellationToken cancellationToken);

    Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    Task BanUserAsync(Guid userId, CancellationToken cancellationToken);

    Task UnbanUserAsync(Guid userId, CancellationToken cancellationToken);

    // Kullanıcı GoTrue'da zaten yoksa sessizce geçer (silme akışı tekrar koşulabilir olmalı).
    Task AnonymizeUserAsync(Guid userId, string anonymizedEmail, CancellationToken cancellationToken);
}
