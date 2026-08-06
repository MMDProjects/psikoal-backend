using PsikoAl.Data.Entities;

namespace PsikoAl.Data.Repositories.Abstractions;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate, Guid>
{
    Task<NotificationTemplate?> GetByTypeAsync(string type, CancellationToken cancellationToken);
}
