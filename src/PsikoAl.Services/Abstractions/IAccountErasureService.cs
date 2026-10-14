namespace PsikoAl.Services.Abstractions;

public interface IAccountErasureService
{
    Task EraseAsync(Guid userId, CancellationToken cancellationToken);
}
