using FlashSaleStore.Domain.Entities.Catalog;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IProductRepository : IEntityRepository<Product, Guid>
{
    Task<Product?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<int> GetQuantityHeldByPendingOrdersAsync(Guid productId, CancellationToken cancellationToken = default);
}
