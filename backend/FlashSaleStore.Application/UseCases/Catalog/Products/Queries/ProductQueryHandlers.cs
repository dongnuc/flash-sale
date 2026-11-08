using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Application.UseCases.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Products.Queries;

public sealed class GetTopFlashSaleProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetTopFlashSaleProductsQuery, Result<IReadOnlyList<ProductCardDto>>>
{
    public async Task<Result<IReadOnlyList<ProductCardDto>>> Handle(GetTopFlashSaleProductsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await queries.GetTopFlashSaleAsync(cancellationToken));
}

public sealed class GetBestSellingProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetBestSellingProductsQuery, Result<PagedResult<ProductCardDto>>>
{
    public async Task<Result<PagedResult<ProductCardDto>>> Handle(GetBestSellingProductsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await queries.GetBestSellingAsync(request.PageIndex, request.PageSize, cancellationToken));
}

public sealed class GetNewestProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetNewestProductsQuery, Result<PagedResult<ProductCardDto>>>
{
    public async Task<Result<PagedResult<ProductCardDto>>> Handle(GetNewestProductsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await queries.GetNewestAsync(request.PageIndex, request.PageSize, cancellationToken));
}

public sealed class GetFeaturedProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetFeaturedProductsQuery, Result<PagedResult<ProductCardDto>>>
{
    public async Task<Result<PagedResult<ProductCardDto>>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken) =>
        Result.Success(await queries.GetFeaturedAsync(request.PageIndex, request.PageSize, cancellationToken));
}

public sealed class GetFlashSaleProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetFlashSaleProductsQuery, Result<PagedResult<ProductCardDto>>>
{
    public async Task<Result<PagedResult<ProductCardDto>>> Handle(GetFlashSaleProductsQuery request, CancellationToken cancellationToken)
    {
        var filter = new FlashSaleProductFilter(
            request.CategoryId,
            SortDiscountAscending: string.Equals(request.SortByDiscount, "ASC", StringComparison.OrdinalIgnoreCase),
            request.PageIndex,
            request.PageSize);
        return Result.Success(await queries.GetFlashSalePageAsync(filter, cancellationToken));
    }
}

public sealed class GetProductCatalogQueryHandler(IProductQueries queries)
    : IRequestHandler<GetProductCatalogQuery, Result<PagedResult<ProductCardDto>>>
{
    public async Task<Result<PagedResult<ProductCardDto>>> Handle(GetProductCatalogQuery request, CancellationToken cancellationToken) =>
        Result.Success(await queries.GetCatalogPageAsync(
            new ProductCatalogFilter(request.CategoryId, request.PageIndex, request.PageSize), cancellationToken));
}

public sealed class GetProductDetailQueryHandler(IProductQueries queries)
    : IRequestHandler<GetProductDetailQuery, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> Handle(GetProductDetailQuery request, CancellationToken cancellationToken)
    {
        var product = await queries.GetDetailAsync(request.ProductId, cancellationToken);
        return product is null
            ? Result.Failure<ProductDetailDto>(CatalogUseCaseErrors.ProductNotFound(request.ProductId))
            : Result.Success(product);
    }
}

public sealed class GetAdminProductsQueryHandler(IProductQueries queries)
    : IRequestHandler<GetAdminProductsQuery, Result<PagedResult<AdminProductDto>>>
{
    public async Task<Result<PagedResult<AdminProductDto>>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        bool? isActive = request.Status?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => true,
            "INACTIVE" => false,
            _ => null
        };

        var filter = new ProductAdminFilter(
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Search,
            isActive,
            request.PageIndex,
            request.PageSize);
        return Result.Success(await queries.GetAdminPageAsync(filter, cancellationToken));
    }
}
