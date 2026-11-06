using System.Data;
using Movie_StructureCode.Application.Abstractions.Data;
using Movie_StructureCode.Contract.Abstractions.Shared;
using Movie_StructureCode.Domain.Abstractions;

namespace Movie_StructureCode.Persistence.Repositories;

public abstract class SqlRepositoryBase<TEntity, TId> : IRepositoryBase<TEntity, TId> where TEntity : class
{
    protected readonly ISqlExecutor SqlExecutor;
    protected readonly ISqlConnectionFactory ConnectionFactory;

    protected SqlRepositoryBase(ISqlExecutor sqlExecutor, ISqlConnectionFactory connectionFactory)
    {
        SqlExecutor = sqlExecutor;
        ConnectionFactory = connectionFactory;
    }

    public abstract Task<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);
    public abstract Task<IReadOnlyList<TEntity>> FindAllAsync(CancellationToken cancellationToken = default);
    public abstract Task<PagedResult<TEntity>> FindPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    public abstract Task<int> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    public abstract Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    public abstract Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default);
}
