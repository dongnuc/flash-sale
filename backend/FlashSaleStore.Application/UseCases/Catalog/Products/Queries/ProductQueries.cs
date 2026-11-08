using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Products.Queries;

public sealed record GetTopFlashSaleProductsQuery : IRequest<Result<IReadOnlyList<ProductCardDto>>>;

public sealed record GetBestSellingProductsQuery(int PageIndex = 1, int PageSize = 10)
    : IRequest<Result<PagedResult<ProductCardDto>>>;

public sealed record GetNewestProductsQuery(int PageIndex = 1, int PageSize = 10)
    : IRequest<Result<PagedResult<ProductCardDto>>>;

public sealed record GetFeaturedProductsQuery(int PageIndex = 1, int PageSize = 10)
    : IRequest<Result<PagedResult<ProductCardDto>>>;

public sealed record GetFlashSaleProductsQuery(
    Guid? CategoryId = null,
    string SortByDiscount = "DESC",
    int PageIndex = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<ProductCardDto>>>;

public sealed record GetProductCatalogQuery(
    Guid? CategoryId = null,
    string SortBy = "bestSelling",
    int PageIndex = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<ProductCardDto>>>;

public sealed record GetProductDetailQuery(Guid ProductId) : IRequest<Result<ProductDetailDto>>;

public sealed record GetAdminProductsQuery(
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Search = null,
    string? Status = null,
    int PageIndex = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<AdminProductDto>>>;
