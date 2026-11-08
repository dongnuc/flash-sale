using FlashSaleStore.Application.Queries.Orders;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed record DeliveryAddressInput(string RecipientName, string Phone, string? Province, string? District, string? Ward, string AddressLine, string? Note = null);
public sealed record CreateOrderCommand(Guid UserId, string PaymentMethod, DeliveryAddressInput? DeliveryAddress) : IRequest<Result<OrderDetailDto>>;
public sealed record GetMyOrderHistoryQuery(Guid UserId, string? Status, int PageIndex, int PageSize) : IRequest<Result<PagedResult<OrderSummaryDto>>>;
public sealed record GetMyOrderDetailQuery(Guid UserId, Guid OrderId) : IRequest<Result<OrderDetailDto>>;
public sealed record GetAdminOrdersQuery(string? SearchUser, string? Status, decimal? MinTotalPrice, decimal? MaxTotalPrice, string? SortDirection, int PageIndex, int PageSize) : IRequest<Result<PagedResult<OrderSummaryDto>>>;
public sealed record GetAdminOrderDetailQuery(Guid OrderId) : IRequest<Result<StoreOrderDetailDto>>;
