using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;
using FlashSaleStore.Contract.Abstractions.Shared;

namespace FlashSaleStore.Persistence.Repositories;

internal abstract class SqlRepositoryBase
{
    private readonly ISqlExecutor _sql;
    private readonly ISqlTransactionAccessor _transactionAccessor;

    protected SqlRepositoryBase(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
    {
        _sql = sql;
        _transactionAccessor = transactionAccessor;
    }

    protected Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters, CancellationToken cancellationToken) =>
        _sql.QueryAsync<T>(sql, parameters, _transactionAccessor.CurrentTransaction, cancellationToken: cancellationToken);

    protected Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken) =>
        _sql.QuerySingleOrDefaultAsync<T>(sql, parameters, _transactionAccessor.CurrentTransaction, cancellationToken: cancellationToken);

    protected Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters, CancellationToken cancellationToken) =>
        _sql.QueryFirstOrDefaultAsync<T>(sql, parameters, _transactionAccessor.CurrentTransaction, cancellationToken: cancellationToken);

    protected Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken) =>
        _sql.ExecuteAsync(sql, parameters, _transactionAccessor.CurrentTransaction, cancellationToken: cancellationToken);

    protected Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters, CancellationToken cancellationToken) =>
        _sql.ExecuteScalarAsync<T>(sql, parameters, _transactionAccessor.CurrentTransaction, cancellationToken: cancellationToken);

    protected Task<PagedResult<T>> QueryPagedAsync<T>(string baseSql, string orderBy, object? parameters,
        int pageIndex, int pageSize, CancellationToken cancellationToken) =>
        _sql.QueryPagedAsync<T>(baseSql, orderBy, parameters, pageIndex, pageSize,
            _transactionAccessor.CurrentTransaction, cancellationToken: cancellationToken);
}
