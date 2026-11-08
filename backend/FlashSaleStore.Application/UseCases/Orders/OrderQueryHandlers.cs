using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Orders;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Enums;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed class GetMyOrderHistoryQueryHandler(IOrderQueries queries) : IRequestHandler<GetMyOrderHistoryQuery, Result<PagedResult<OrderSummaryDto>>>
{
    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(GetMyOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        OrderStatus? status = request.Status is null ? null : Enum.Parse<OrderStatus>(request.Status, true);
        return Result.Success(await queries.GetCustomerHistoryAsync(request.UserId, status, request.PageIndex, request.PageSize, cancellationToken));
    }
}
public sealed class GetMyOrderDetailQueryHandler(IOrderQueries queries) : IRequestHandler<GetMyOrderDetailQuery, Result<OrderDetailDto>>
{
    public async Task<Result<OrderDetailDto>> Handle(GetMyOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var detail = await queries.GetCustomerDetailAsync(request.OrderId, request.UserId, cancellationToken);
        return detail is null ? Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.NotFound("Order", request.OrderId)) : Result.Success(detail);
    }
}
public sealed class GetAdminOrdersQueryHandler(IOrderQueries queries) : IRequestHandler<GetAdminOrdersQuery, Result<PagedResult<OrderSummaryDto>>>
{
    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        OrderStatus? status = request.Status is null ? null : Enum.Parse<OrderStatus>(request.Status, true);
        var filter = new FlashSaleStore.Application.Abstractions.Queries.OrderAdminFilter(request.SearchUser, status, request.MinTotalPrice, request.MaxTotalPrice, string.Equals(request.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase), request.PageIndex, request.PageSize);
        return Result.Success(await queries.GetAdminPageAsync(filter, cancellationToken));
    }
}
public sealed class GetAdminOrderDetailQueryHandler(IOrderQueries queries) : IRequestHandler<GetAdminOrderDetailQuery, Result<StoreOrderDetailDto>>
{
    public async Task<Result<StoreOrderDetailDto>> Handle(GetAdminOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var detail = await queries.GetAdminDetailAsync(request.OrderId, cancellationToken);
        return detail is null ? Result.Failure<StoreOrderDetailDto>(CommerceUseCaseErrors.NotFound("Order", request.OrderId)) : Result.Success(detail);
    }
}
