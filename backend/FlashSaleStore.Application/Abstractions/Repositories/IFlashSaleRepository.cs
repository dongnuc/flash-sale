using FlashSaleStore.Domain.Entities.FlashSales;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IFlashSaleRepository : IEntityRepository<FlashSale, Guid>
{
    Task<IReadOnlyList<FlashSale>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
