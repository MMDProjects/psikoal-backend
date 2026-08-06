using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class NotificationTemplateRepository(AppDbContext dbContext)
    : Repository<NotificationTemplate, Guid>(dbContext), INotificationTemplateRepository
{
    public Task<NotificationTemplate?> GetByTypeAsync(string type, CancellationToken cancellationToken)
        => DbContext.NotificationTemplates.FirstOrDefaultAsync(template => template.Type == type, cancellationToken);
}
