using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Products.Commands;

public sealed record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal RegularPrice,
    int Quantity,
    bool IsFeatured,
    string Status) : IRequest<Result<AdminProductDto>>;

public sealed record UpdateProductCommand(
    Guid ProductId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal RegularPrice,
    int Quantity,
    bool IsFeatured,
    string Status) : IRequest<Result<AdminProductDto>>;

public sealed record BatchUpdateProductStatusCommand(Guid[] Ids, string Status)
    : IRequest<Result<BatchStatusDto>>;
