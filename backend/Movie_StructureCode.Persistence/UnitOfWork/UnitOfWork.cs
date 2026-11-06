using System.Data;
using System.Data.Common;
using Movie_StructureCode.Application.Abstractions.Data;

namespace Movie_StructureCode.Persistence.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private IDbConnection? _connection;
    private IDbTransaction? _currentTransaction;
    private bool _disposed;

    public UnitOfWork(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = _connectionFactory.CreateConnection();
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }
            }
            return _connection;
        }
    }

    public IDbTransaction? CurrentTransaction => _currentTransaction;
    public bool HasActiveTransaction => _currentTransaction != null;

    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        if (_connection == null)
        {
            _connection = _connectionFactory.CreateConnection();
        }

        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is DbConnection dbConn)
            {
                await dbConn.OpenAsync(cancellationToken);
            }
            else
            {
                _connection.Open();
            }
        }

        _currentTransaction = _connection.BeginTransaction();
        return _currentTransaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            if (_currentTransaction is DbTransaction dbTrans)
            {
                await dbTrans.CommitAsync(cancellationToken);
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

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            return;
        }

        try
        {
            if (_currentTransaction is DbTransaction dbTrans)
            {
                await dbTrans.RollbackAsync(cancellationToken);
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

    public async Task ExecuteTransactionAsync(
        Func<IDbTransaction, Task> action,
        CancellationToken cancellationToken = default)
    {
        var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            await action(transaction);
            await CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<T> ExecuteTransactionAsync<T>(
        Func<IDbTransaction, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(transaction);
            await CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
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

        if (_currentTransaction is DbTransaction dbTrans)
        {
            await dbTrans.DisposeAsync();
        }
        else
        {
            _currentTransaction?.Dispose();
        }

        if (_connection is DbConnection dbConn)
        {
            await dbConn.DisposeAsync();
        }
        else
        {
            _connection?.Dispose();
        }

        _disposed = true;
    }
}
