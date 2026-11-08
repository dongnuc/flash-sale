using FlashSaleStore.Domain.Entities.Orders;

namespace FlashSaleStore.Application.Abstractions.Repositories;

/// <summary>Order detail prices are immutable snapshots, so this repository intentionally has no update operation.</summary>
public interface IOrderDetailRepository
{
    Task<OrderDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDetail>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(OrderDetail entity, CancellationToken cancellationToken = default);
    Task<int> InsertRangeAsync(IReadOnlyCollection<OrderDetail> entities, CancellationToken cancellationToken = default);
}
