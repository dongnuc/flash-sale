using FlashSaleStore.Domain.Entities.Orders;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IOrderRepository : IEntityRepository<Order, Guid>
{
    Task<Order?> GetByIdForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetExpiredUnpaidAsync(DateTime utcNow, int batchSize, CancellationToken cancellationToken = default);
    Task<int> TryExpireAsync(Order order, DateTime utcNow, CancellationToken cancellationToken = default);
}
