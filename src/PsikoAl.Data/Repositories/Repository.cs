using Microsoft.EntityFrameworkCore;
using PsikoAl.Data.Repositories.Abstractions;

namespace PsikoAl.Data.Repositories;

public class Repository<TEntity, TKey>(AppDbContext dbContext) : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected AppDbContext DbContext { get; } = dbContext;

    public IQueryable<TEntity> Query() => DbContext.Set<TEntity>().AsNoTracking();

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken)
        => DbContext.Set<TEntity>().FindAsync([id], cancellationToken).AsTask();

    public async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken)
        => await GetByIdAsync(id, cancellationToken) is not null;

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        => await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

    public void Delete(TEntity entity) => DbContext.Set<TEntity>().Remove(entity);
}
