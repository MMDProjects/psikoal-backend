namespace PsikoAl.Data.Repositories.Abstractions;

// Commit edilmeden dispose edilen transaction geri alınır.
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
