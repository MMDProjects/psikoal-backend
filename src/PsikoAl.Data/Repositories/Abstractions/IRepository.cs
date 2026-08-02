namespace PsikoAl.Data.Repositories.Abstractions;

public interface IRepository<TEntity, in TKey>
    where TEntity : class
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Delete(TEntity entity);
}
