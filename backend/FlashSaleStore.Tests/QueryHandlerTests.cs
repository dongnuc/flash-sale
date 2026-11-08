using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Application.Queries.Orders;
using FlashSaleStore.Application.UseCases.FlashSales.Queries;
using FlashSaleStore.Application.UseCases.Orders;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Enums;
using Xunit;

namespace FlashSaleStore.Tests;

public sealed class QueryHandlerTests
{
    [Fact]
    public async Task Customer_history_and_detail_handlers_keep_the_user_scope()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var queries = new FakeOrderQueries(userId, orderId);
        var historyHandler = new GetMyOrderHistoryQueryHandler(queries);
        var detailHandler = new GetMyOrderDetailQueryHandler(queries);

        var history = await historyHandler.Handle(new(userId, "Pending", 2, 5), CancellationToken.None);
        var detail = await detailHandler.Handle(new(userId, orderId), CancellationToken.None);
        var otherUserDetail = await detailHandler.Handle(new(Guid.NewGuid(), orderId), CancellationToken.None);

        Assert.True(history.IsSuccess);
        Assert.Equal(OrderStatus.Pending, queries.LastHistoryStatus);
        Assert.Equal(2, queries.LastPageIndex);
        Assert.True(detail.IsSuccess);
        Assert.True(otherUserDetail.IsFailure);
        Assert.Equal("Order.NotFound", otherUserDetail.Error.Code);
    }

    [Fact]
    public async Task Admin_order_and_flash_sale_queries_validate_filters_before_accessing_storage()
    {
        var orderQueries = new FakeOrderQueries(Guid.NewGuid(), Guid.NewGuid());
        var orderHandler = new GetAdminOrdersQueryHandler(orderQueries);
        var orders = await orderHandler.Handle(new("customer", "Completed", 100m, 900m, "ASC", 1, 10), CancellationToken.None);
        Assert.True(orders.IsSuccess);
        Assert.Equal(OrderStatus.Completed, orderQueries.LastAdminFilter!.Status);
        Assert.True(orderQueries.LastAdminFilter.SortTotalAscending);

        var salesQueries = new FakeFlashSaleQueries();
        var salesHandler = new GetAdminFlashSalesQueryHandler(salesQueries);
        var invalid = await salesHandler.Handle(new(null, null, null, "999", 1, 10), CancellationToken.None);
        Assert.True(invalid.IsFailure);
        Assert.Equal(0, salesQueries.CallCount);
        var valid = await salesHandler.Handle(new(null, null, "weekend", "Scheduled", 1, 10), CancellationToken.None);
        Assert.True(valid.IsSuccess);
        Assert.Equal(FlashSaleStatus.Scheduled, salesQueries.LastFilter!.Status);
    }

    private sealed class FakeOrderQueries(Guid ownedUserId, Guid ownedOrderId) : IOrderQueries
    {
        public OrderStatus? LastHistoryStatus { get; private set; }
        public int LastPageIndex { get; private set; }
        public OrderAdminFilter? LastAdminFilter { get; private set; }

        public Task<PagedResult<OrderSummaryDto>> GetCustomerHistoryAsync(Guid userId, OrderStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            LastHistoryStatus = status;
            LastPageIndex = pageIndex;
            return Task.FromResult(new PagedResult<OrderSummaryDto>([], pageIndex, pageSize, 0));
        }
        public Task<OrderDetailDto?> GetCustomerDetailAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrderDetailDto?>(orderId == ownedOrderId && userId == ownedUserId ? new OrderDetailDto { OrderId = orderId } : null);
        public Task<PagedResult<OrderSummaryDto>> GetAdminPageAsync(OrderAdminFilter filter, CancellationToken cancellationToken = default)
        {
            LastAdminFilter = filter;
            return Task.FromResult(new PagedResult<OrderSummaryDto>([], filter.PageIndex, filter.PageSize, 0));
        }
        public Task<StoreOrderDetailDto?> GetAdminDetailAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoreOrderDetailDto?>(orderId == ownedOrderId ? new StoreOrderDetailDto { OrderId = orderId } : null);
    }

    private sealed class FakeFlashSaleQueries : IFlashSaleQueries
    {
        public int CallCount { get; private set; }
        public FlashSaleAdminFilter? LastFilter { get; private set; }
        public Task<PagedResult<FlashSaleDto>> GetAdminPageAsync(FlashSaleAdminFilter filter, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastFilter = filter;
            return Task.FromResult(new PagedResult<FlashSaleDto>([], filter.PageIndex, filter.PageSize, 0));
        }
        public Task<FlashSaleDto?> GetByIdAsync(Guid flashSaleId, CancellationToken cancellationToken = default) => Task.FromResult<FlashSaleDto?>(null);
    }
}
