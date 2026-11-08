using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Services;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IFlashSaleItemRepository : IEntityRepository<FlashSaleItem, Guid>
{
    Task<IReadOnlyList<FlashSaleItem>> GetByFlashSaleIdAsync(Guid flashSaleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlashSaleItem>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<FlashSaleItem?> GetByFlashSaleAndProductAsync(Guid flashSaleId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlashSaleSchedule>> GetSchedulesByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
    Task<int> GetPurchasedOrReservedQuantityAsync(Guid flashSaleItemId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(Guid flashSaleItemId, CancellationToken cancellationToken = default);
}
