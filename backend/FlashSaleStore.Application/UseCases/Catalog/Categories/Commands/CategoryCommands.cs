using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Categories.Commands;

public sealed record CreateCategoryCommand(string Name, string? Description)
    : IRequest<Result<AdminCategoryDto>>;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Description)
    : IRequest<Result<AdminCategoryDto>>;

public sealed record BatchUpdateCategoryStatusCommand(Guid[] Ids, string Status)
    : IRequest<Result<BatchStatusDto>>;
