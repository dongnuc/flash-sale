using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;

namespace FlashSaleStore.Application.Abstractions.Queries;

public interface IProductQueries
{
    Task<PagedResult<ProductCardDto>> GetBestSellingAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductCardDto>> GetNewestAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductCardDto>> GetFeaturedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCardDto>> GetTopFlashSaleAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ProductCardDto>> GetFlashSalePageAsync(FlashSaleProductFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductCardDto>> GetCatalogPageAsync(ProductCatalogFilter filter, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetDetailAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminProductDto>> GetAdminPageAsync(ProductAdminFilter filter, CancellationToken cancellationToken = default);
    Task<AdminProductDto?> GetAdminByIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
