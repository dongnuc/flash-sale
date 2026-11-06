using System.Data;

namespace Movie_StructureCode.Application.Abstractions.Data;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? CurrentTransaction { get; }
    bool HasActiveTransaction { get; }

    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);

    Task ExecuteTransactionAsync(
        Func<IDbTransaction, Task> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteTransactionAsync<T>(
        Func<IDbTransaction, Task<T>> action,
        CancellationToken cancellationToken = default);
}
