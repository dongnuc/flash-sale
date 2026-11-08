namespace FlashSaleStore.Application.Abstractions.Data;

/// <summary>Coordinates atomic use-case writes without exposing SQL connections or transactions.</summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
