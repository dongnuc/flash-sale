using FlashSaleStore.Domain.Entities.Cart;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface ICartItemRepository : IEntityRepository<CartItem, Guid>
{
    Task<IReadOnlyList<CartItem>> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<CartItem?> GetByCartAndProductAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(Guid cartItemId, CancellationToken cancellationToken = default);
}
