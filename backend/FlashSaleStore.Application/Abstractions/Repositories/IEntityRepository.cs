namespace FlashSaleStore.Application.Abstractions.Repositories;

/// <summary>Basic entity persistence operations. Query screens use dedicated query interfaces instead.</summary>
public interface IEntityRepository<TEntity, in TKey>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
}
