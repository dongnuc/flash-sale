using System.Data;
using Dapper;
using Movie_StructureCode.Application.Abstractions.Data;
using Movie_StructureCode.Contract.Abstractions.Shared;

namespace Movie_StructureCode.Persistence.Sql;

public sealed class SqlExecutor : ISqlExecutor
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlExecutor(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            var results = await transaction.Connection!.QueryAsync<T>(command);
            return results.AsList();
        }

        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(sql, param, null, commandTimeout, commandType, cancellationToken: cancellationToken);
        var res = await connection.QueryAsync<T>(cmd);
        return res.AsList();
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await transaction.Connection!.QueryFirstOrDefaultAsync<T>(command);
        }

        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(sql, param, null, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<T>(cmd);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await transaction.Connection!.QuerySingleOrDefaultAsync<T>(command);
        }

        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(sql, param, null, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(cmd);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await transaction.Connection!.ExecuteAsync(command);
        }

        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(sql, param, null, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(cmd);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await transaction.Connection!.ExecuteScalarAsync<T>(command);
        }

        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(sql, param, null, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<T>(cmd);
    }

    public async Task<PagedResult<T>> QueryPagedAsync<T>(
        string baseSql,
        string orderBy,
        object? param = null,
        int pageIndex = 1,
        int pageSize = 10,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        pageIndex = pageIndex < 1 ? PagedResult<T>.DefaultPageIndex : pageIndex;
        pageSize = pageSize < 1 ? PagedResult<T>.DefaultPageSize : (pageSize > PagedResult<T>.UperPageSize ? PagedResult<T>.UperPageSize : pageSize);
        var offset = (pageIndex - 1) * pageSize;

        var countSql = $"SELECT COUNT(1) FROM ({baseSql}) AS PagedCountQuery";
        var pagedSql = $"{baseSql} ORDER BY {orderBy} OFFSET @PagedOffset ROWS FETCH NEXT @PagedPageSize ROWS ONLY";

        var dynamicParams = new DynamicParameters();
        if (param != null)
        {
            dynamicParams.AddDynamicParams(param);
        }
        dynamicParams.Add("PagedOffset", offset);
        dynamicParams.Add("PagedPageSize", pageSize);

        var totalCount = await ExecuteScalarAsync<int>(countSql, dynamicParams, transaction, commandTimeout, cancellationToken: cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<T>(new List<T>(), pageIndex, pageSize, 0);
        }

        var items = await QueryAsync<T>(pagedSql, dynamicParams, transaction, commandTimeout, cancellationToken: cancellationToken);
        return new PagedResult<T>(items.ToList(), pageIndex, pageSize, totalCount);
    }

    public Task<PagedResult<T>> QueryPagedAsync<T>(
        string baseSql,
        string orderBy,
        IPagedRequest pagedRequest,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return QueryPagedAsync<T>(
            baseSql,
            orderBy,
            param,
            pagedRequest.PageIndex,
            pagedRequest.PageSize,
            transaction,
            commandTimeout,
            cancellationToken);
    }

    public async Task<PagedResult<T>> QueryPagedCustomAsync<T>(
        string querySql,
        string countSql,
        object? param = null,
        int pageIndex = 1,
        int pageSize = 10,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        pageIndex = pageIndex < 1 ? PagedResult<T>.DefaultPageIndex : pageIndex;
        pageSize = pageSize < 1 ? PagedResult<T>.DefaultPageSize : (pageSize > PagedResult<T>.UperPageSize ? PagedResult<T>.UperPageSize : pageSize);

        var totalCount = await ExecuteScalarAsync<int>(countSql, param, transaction, commandTimeout, cancellationToken: cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<T>(new List<T>(), pageIndex, pageSize, 0);
        }

        var items = await QueryAsync<T>(querySql, param, transaction, commandTimeout, cancellationToken: cancellationToken);
        return new PagedResult<T>(items.ToList(), pageIndex, pageSize, totalCount);
    }

    public async Task<PagedResult<T>> QueryPagedMultipleAsync<T>(
        string combinedSql,
        object? param = null,
        int pageIndex = 1,
        int pageSize = 10,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        pageIndex = pageIndex < 1 ? PagedResult<T>.DefaultPageIndex : pageIndex;
        pageSize = pageSize < 1 ? PagedResult<T>.DefaultPageSize : (pageSize > PagedResult<T>.UperPageSize ? PagedResult<T>.UperPageSize : pageSize);

        if (transaction != null)
        {
            var command = new CommandDefinition(combinedSql, param, transaction, commandTimeout, cancellationToken: cancellationToken);
            using var multi = await transaction.Connection!.QueryMultipleAsync(command);
            var totalCount = await multi.ReadFirstAsync<int>();
            var items = (await multi.ReadAsync<T>()).ToList();
            return new PagedResult<T>(items, pageIndex, pageSize, totalCount);
        }

        using var connection = _connectionFactory.CreateConnection();
        var cmd = new CommandDefinition(combinedSql, param, null, commandTimeout, cancellationToken: cancellationToken);
        using var multiConn = await connection.QueryMultipleAsync(cmd);
        var total = await multiConn.ReadFirstAsync<int>();
        var data = (await multiConn.ReadAsync<T>()).ToList();
        return new PagedResult<T>(data, pageIndex, pageSize, total);
    }
}
