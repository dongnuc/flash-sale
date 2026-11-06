using System.Data;
using Movie_StructureCode.Contract.Abstractions.Shared;

namespace Movie_StructureCode.Application.Abstractions.Data;

public interface ISqlExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân trang Raw SQL tự động: truyền baseSql và orderBy, hàm sẽ tự động tạo câu lệnh COUNT và áp dụng OFFSET/FETCH.
    /// </summary>
    Task<PagedResult<T>> QueryPagedAsync<T>(
        string baseSql,
        string orderBy,
        object? param = null,
        int pageIndex = 1,
        int pageSize = 10,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân trang Raw SQL tự động với IPagedRequest.
    /// </summary>
    Task<PagedResult<T>> QueryPagedAsync<T>(
        string baseSql,
        string orderBy,
        IPagedRequest pagedRequest,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân trang Raw SQL với 2 câu query riêng biệt: query lấy danh sách trang (querySql) và query đếm tổng số (countSql).
    /// </summary>
    Task<PagedResult<T>> QueryPagedCustomAsync<T>(
        string querySql,
        string countSql,
        object? param = null,
        int pageIndex = 1,
        int pageSize = 10,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phân trang Raw SQL 1 round-trip: combinedSql trả về 2 result set (COUNT trước, DATA sau) sử dụng Dapper QueryMultipleAsync.
    /// </summary>
    Task<PagedResult<T>> QueryPagedMultipleAsync<T>(
        string combinedSql,
        object? param = null,
        int pageIndex = 1,
        int pageSize = 10,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
