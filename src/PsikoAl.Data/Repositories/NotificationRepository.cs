using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class NotificationRepository(AppDbContext dbContext)
    : Repository<Notification, Guid>(dbContext), INotificationRepository
{
    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
        => DbContext.Notifications.CountAsync(notification => notification.UserId == userId && !notification.Read, cancellationToken);

    public Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken)
        => DbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.Read)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.Read, true), cancellationToken);
}
