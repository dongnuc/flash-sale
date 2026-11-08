using System.Data;
using System.Data.Common;
using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Persistence.Connection;

namespace FlashSaleStore.Persistence.UnitOfWork;

internal sealed class UnitOfWork : IUnitOfWork, ISqlTransactionAccessor, IDisposable, IAsyncDisposable
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _currentTransaction;
    private bool _disposed;

    public UnitOfWork(ISqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    IDbTransaction? ISqlTransactionAccessor.CurrentTransaction => _currentTransaction;

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await BeginAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await BeginAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task BeginAsync(CancellationToken cancellationToken)
    {
        if (_currentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already in progress in this unit of work.");
        }

        _connection ??= _connectionFactory.CreateConnection();
        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                _connection.Open();
            }
        }

        _currentTransaction = _connection.BeginTransaction();
    }

    private async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_currentTransaction is null) throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            if (_currentTransaction is DbTransaction transaction)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                _currentTransaction.Commit();
            }
        }
        finally
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }

    private async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_currentTransaction is null) return;

        try
        {
            if (_currentTransaction is DbTransaction transaction)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                _currentTransaction.Rollback();
            }
        }
        finally
        {
            _currentTransaction.Dispose();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _currentTransaction?.Dispose();
        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_currentTransaction is DbTransaction transaction)
        {
            await transaction.DisposeAsync();
        }
        else
        {
            _currentTransaction?.Dispose();
        }

        if (_connection is DbConnection connection)
        {
            await connection.DisposeAsync();
        }
        else
        {
            _connection?.Dispose();
        }

        _disposed = true;
    }
}
