using FlashSaleStore.Application.Queries.Orders;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Enums;

namespace FlashSaleStore.Application.Abstractions.Queries;

public interface IOrderQueries
{
    Task<PagedResult<OrderSummaryDto>> GetCustomerHistoryAsync(Guid userId, OrderStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetCustomerDetailAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderSummaryDto>> GetAdminPageAsync(OrderAdminFilter filter, CancellationToken cancellationToken = default);
    Task<StoreOrderDetailDto?> GetAdminDetailAsync(Guid orderId, CancellationToken cancellationToken = default);
}
