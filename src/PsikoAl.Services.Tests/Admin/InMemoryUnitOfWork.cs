using Microsoft.EntityFrameworkCore;
using PsikoAl.Data;
using PsikoAl.Data.Repositories;

namespace PsikoAl.Services.Tests.Admin;

internal static class InMemoryUnitOfWork
{
    public static (UnitOfWork UnitOfWork, AppDbContext DbContext) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        return (new UnitOfWork(dbContext), dbContext);
    }
}
