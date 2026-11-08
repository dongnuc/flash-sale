using System.Security.Claims;
using Carter;
using FlashSaleStore.Application.UseCases.Orders;
using FlashSaleStore.Presentation.Abstractions;
using FlashSaleStore.Presentation.Contracts.Commerce;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlashSaleStore.Presentation.APIs;

public sealed class OrderApi : ApiEndpoint, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var customer = app.NewVersionedApi("my-orders").MapGroup("/api/v{version:apiVersion}/me/orders").HasApiVersion(1).RequireAuthorization();
        customer.MapGet("/", GetHistory).WithName("GetMyOrderHistory").WithTags("Orders");
        customer.MapGet("/{orderId:guid}", GetDetail).WithName("GetMyOrderDetail").WithTags("Orders");
        customer.MapPost("/", Create).WithName("CreateOrder").WithTags("Orders").Produces(StatusCodes.Status201Created);

        var admin = app.NewVersionedApi("admin-orders").MapGroup("/api/v{version:apiVersion}/admin/orders").HasApiVersion(1).RequireAuthorization("StoreOwner");
        admin.MapGet("/", GetAdminPage).WithName("GetAdminOrders").WithTags("Admin Orders");
        admin.MapGet("/{orderId:guid}", GetAdminDetail).WithName("GetAdminOrderDetail").WithTags("Admin Orders");
    }
    private static Guid UserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
    private static async Task<IResult> GetHistory(ClaimsPrincipal user, ISender sender, CancellationToken ct, string? status = null, int pageIndex = 1, int pageSize = 10) => Handle(await sender.Send(new GetMyOrderHistoryQuery(UserId(user), status, pageIndex, pageSize), ct));
    private static async Task<IResult> GetDetail(Guid orderId, ClaimsPrincipal user, ISender sender, CancellationToken ct) => Handle(await sender.Send(new GetMyOrderDetailQuery(UserId(user), orderId), ct));
    private static async Task<IResult> Create(CreateOrderRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) => Handle(await sender.Send(new CreateOrderCommand(UserId(user), request.PaymentMethod, request.DeliveryAddress?.ToInput(request.Note)), ct), StatusCodes.Status201Created);
    private static async Task<IResult> GetAdminPage(ISender sender, CancellationToken ct, string? searchUser = null, string? status = null, decimal? minTotalPrice = null, decimal? maxTotalPrice = null, string? sortDirection = "DESC", int pageIndex = 1, int pageSize = 10) => Handle(await sender.Send(new GetAdminOrdersQuery(searchUser, status, minTotalPrice, maxTotalPrice, sortDirection, pageIndex, pageSize), ct));
    private static async Task<IResult> GetAdminDetail(Guid orderId, ISender sender, CancellationToken ct) => Handle(await sender.Send(new GetAdminOrderDetailQuery(orderId), ct));
}
