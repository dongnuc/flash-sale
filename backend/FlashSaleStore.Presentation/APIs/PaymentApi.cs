using System.Security.Claims;
using Carter;
using FlashSaleStore.Application.UseCases.Orders;
using FlashSaleStore.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlashSaleStore.Presentation.APIs;

public sealed class PaymentApi : ApiEndpoint, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var customer = app.NewVersionedApi("my-order-payment").MapGroup("/api/v{version:apiVersion}/me/orders").HasApiVersion(1).RequireAuthorization();
        customer.MapPost("/{orderId:guid}/payment", CreatePayment).WithName("CreateOrderPayment").WithTags("Payments").Produces(StatusCodes.Status201Created);
        var webhooks = app.NewVersionedApi("payment-webhooks").MapGroup("/api/v{version:apiVersion}/payments/webhooks").HasApiVersion(1);
        webhooks.MapPost("/{provider}", ProcessWebhook).WithName("ProcessPaymentWebhook").WithTags("Payments");
    }
    private static Guid UserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
    private static async Task<IResult> CreatePayment(Guid orderId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        Handle(await sender.Send(new CreatePaymentCommand(UserId(user), orderId), ct), StatusCodes.Status201Created);
    private static async Task<IResult> ProcessWebhook(string provider, HttpRequest request, ISender sender, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        var rawBody = await reader.ReadToEndAsync(ct);
        return Handle(await sender.Send(new ProcessPaymentWebhookCommand(provider, rawBody), ct));
    }
}
