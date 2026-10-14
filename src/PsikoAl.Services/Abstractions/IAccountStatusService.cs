namespace PsikoAl.Services.Abstractions;

public interface IAccountStatusService
{
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken);

    void Invalidate(Guid userId);
}
