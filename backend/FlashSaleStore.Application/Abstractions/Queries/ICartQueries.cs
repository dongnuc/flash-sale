using FlashSaleStore.Application.Queries.Cart;

namespace FlashSaleStore.Application.Abstractions.Queries;

public interface ICartQueries
{
    Task<CartDto?> GetByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);
}
