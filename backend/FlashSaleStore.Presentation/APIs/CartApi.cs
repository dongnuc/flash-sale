using System.Security.Claims;
using Carter;
using FlashSaleStore.Application.UseCases.Cart;
using FlashSaleStore.Presentation.Abstractions;
using FlashSaleStore.Presentation.Contracts.Commerce;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlashSaleStore.Presentation.APIs;

public sealed class CartApi : ApiEndpoint, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("my-cart").MapGroup("/api/v{version:apiVersion}/me/cart").HasApiVersion(1).RequireAuthorization();
        group.MapGet("/", GetCart).WithName("GetMyCart").WithTags("Cart");
        group.MapPost("/items", AddItem).WithName("AddCartItem").WithTags("Cart");
        group.MapPatch("/items/{productId:guid}", SetQuantity).WithName("SetCartItemQuantity").WithTags("Cart");
        group.MapDelete("/items/{productId:guid}", RemoveItem).WithName("RemoveCartItem").WithTags("Cart");
    }

    private static Guid GetUserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
    private static async Task<IResult> GetCart(ClaimsPrincipal user, ISender sender, CancellationToken ct) => Handle(await sender.Send(new GetMyCartQuery(GetUserId(user)), ct));
    private static async Task<IResult> AddItem(CartItemRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) => Handle(await sender.Send(new AddCartItemCommand(GetUserId(user), request.ProductId, request.Quantity), ct));
    private static async Task<IResult> SetQuantity(Guid productId, SetCartItemQuantityRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct) => Handle(await sender.Send(new SetCartItemQuantityCommand(GetUserId(user), productId, request.Quantity), ct));
    private static async Task<IResult> RemoveItem(Guid productId, ClaimsPrincipal user, ISender sender, CancellationToken ct) => Handle(await sender.Send(new RemoveCartItemCommand(GetUserId(user), productId), ct));
}
