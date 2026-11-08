using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.FlashSales.Commands;

public sealed record FlashSaleItemInput(Guid ProductId, decimal FinalPrice, int FlashSaleQuantity, int? LimitPerUser = null);
public sealed record CreateFlashSaleCommand(string Name, DateTime StartsAt, DateTime EndsAt, IReadOnlyList<FlashSaleItemInput> Items) : IRequest<Result<FlashSaleDto>>;
public sealed record UpdateFlashSaleCommand(Guid FlashSaleId, string Name, DateTime StartsAt, DateTime EndsAt, IReadOnlyList<FlashSaleItemInput> Items) : IRequest<Result<FlashSaleDto>>;
public sealed record BatchUpdateFlashSaleStatusCommand(Guid[] Ids, string Status) : IRequest<Result<BatchStatusDto>>;
public sealed record SetFlashSaleStartTimeCommand(Guid FlashSaleId, DateTime StartsAt) : IRequest<Result<FlashSaleStartTimeDto>>;
public sealed record FlashSaleStartTimeDto(Guid FlashSaleId, DateTime StartsAt);
