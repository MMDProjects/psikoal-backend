using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface INotificationRepository : IRepository<Notification, Guid>
{
    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);

    Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);
}
