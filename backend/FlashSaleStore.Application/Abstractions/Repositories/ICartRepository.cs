using FlashSaleStore.Domain.Entities.Cart;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface ICartRepository : IEntityRepository<Cart, Guid>
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
