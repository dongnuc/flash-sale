using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;

namespace FlashSaleStore.Application.Abstractions.Queries;

public interface IFlashSaleQueries
{
    Task<PagedResult<FlashSaleDto>> GetAdminPageAsync(FlashSaleAdminFilter filter, CancellationToken cancellationToken = default);
    Task<FlashSaleDto?> GetByIdAsync(Guid flashSaleId, CancellationToken cancellationToken = default);
}
