using Movie_StructureCode.Contract.Abstractions.Shared;

namespace Movie_StructureCode.Domain.Abstractions;

public interface IRepositoryBase<TEntity, in TId> where TEntity : class
{
    Task<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TEntity>> FindPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default);
}
