using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public sealed class AuditLogRepository(AppDbContext dbContext)
    : Repository<AuditLog, long>(dbContext), IAuditLogRepository;
