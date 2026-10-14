using PsikoAl.Common.Constants;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Services.Tests.Admin;

internal sealed class AdminTestDoubles
{
    public AdminTestDoubles(string adminRole = AdminRoles.SuperAdmin)
    {
        Admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            AuthUserId = Guid.NewGuid(),
            DisplayName = "Test Admin",
            Role = adminRole,
        };

        UnitOfWork.AuditLogs.Returns(AuditLogs);
        UnitOfWork.AuditLogs.AddAsync(Arg.Do<AuditLog>(AddedAuditLogs.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        AdminGuard.GetActiveAdminAsync(Admin.AuthUserId, Arg.Any<CancellationToken>()).Returns(Admin);
    }

    public AdminUser Admin { get; }

    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();

    public IAuditLogRepository AuditLogs { get; } = Substitute.For<IAuditLogRepository>();

    public IAdminGuard AdminGuard { get; } = Substitute.For<IAdminGuard>();

    public List<AuditLog> AddedAuditLogs { get; } = [];
}
