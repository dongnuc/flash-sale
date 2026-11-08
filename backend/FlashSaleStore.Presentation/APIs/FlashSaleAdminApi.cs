using Carter;
using FlashSaleStore.Application.UseCases.FlashSales.Commands;
using FlashSaleStore.Application.UseCases.FlashSales.Queries;
using FlashSaleStore.Presentation.Abstractions;
using FlashSaleStore.Presentation.Contracts.FlashSales;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlashSaleStore.Presentation.APIs;

public sealed class FlashSaleAdminApi : ApiEndpoint, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("admin-flash-sales").MapGroup("/api/v{version:apiVersion}/admin/flash-sales").HasApiVersion(1).RequireAuthorization("StoreOwner");
        group.MapGet("/", GetPage).WithName("GetAdminFlashSales").WithTags("Admin FlashSale");
        group.MapGet("/{flashSaleId:guid}", GetDetail).WithName("GetAdminFlashSaleDetail").WithTags("Admin FlashSale");
        group.MapPost("/", Create).WithName("CreateFlashSale").WithTags("Admin FlashSale").Produces(StatusCodes.Status201Created);
        group.MapPut("/{flashSaleId:guid}", Update).WithName("UpdateFlashSale").WithTags("Admin FlashSale");
        group.MapPatch("/batch-status", BatchStatus).WithName("BatchUpdateFlashSaleStatus").WithTags("Admin FlashSale");
        group.MapPatch("/{flashSaleId:guid}/start-time", SetStartTime).WithName("SetFlashSaleStartTime").WithTags("Admin FlashSale");
    }
    private static async Task<IResult> GetPage(ISender sender, CancellationToken ct, string? search = null, string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null, int pageIndex = 1, int pageSize = 10) => Handle(await sender.Send(new GetAdminFlashSalesQuery(dateFrom, dateTo, search, status, pageIndex, pageSize), ct));
    private static async Task<IResult> GetDetail(Guid flashSaleId, ISender sender, CancellationToken ct) => Handle(await sender.Send(new GetAdminFlashSaleDetailQuery(flashSaleId), ct));
    private static async Task<IResult> Create(CreateFlashSaleRequest request, ISender sender, CancellationToken ct) => Handle(await sender.Send(new CreateFlashSaleCommand(request.Name, request.StartsAt, request.EndsAt, (request.Items ?? Array.Empty<FlashSaleItemRequest>()).Select(ToInput).ToArray()), ct), StatusCodes.Status201Created);
    private static async Task<IResult> Update(Guid flashSaleId, UpdateFlashSaleRequest request, ISender sender, CancellationToken ct) => Handle(await sender.Send(new UpdateFlashSaleCommand(flashSaleId, request.Name, request.StartsAt, request.EndsAt, (request.Items ?? Array.Empty<FlashSaleItemRequest>()).Select(ToInput).ToArray()), ct));
    private static async Task<IResult> BatchStatus(FlashSaleBatchStatusRequest request, ISender sender, CancellationToken ct) => Handle(await sender.Send(new BatchUpdateFlashSaleStatusCommand(request.Ids ?? Array.Empty<Guid>(), request.Status), ct));
    private static async Task<IResult> SetStartTime(Guid flashSaleId, SetFlashSaleStartTimeRequest request, ISender sender, CancellationToken ct) => Handle(await sender.Send(new SetFlashSaleStartTimeCommand(flashSaleId, request.StartsAt), ct));
    private static FlashSaleItemInput ToInput(FlashSaleItemRequest item) => new(item.ProductId, item.FinalPrice, item.FlashSaleQuantity, item.LimitPerUser);
}
