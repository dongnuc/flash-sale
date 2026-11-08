using FlashSaleStore.Domain.Enums;

namespace FlashSaleStore.Application.Abstractions.Queries;

public sealed record CategoryAdminFilter(string? Search, int PageIndex, int PageSize);

public sealed record ProductCatalogFilter(Guid? CategoryId, int PageIndex, int PageSize);

public sealed record ProductAdminFilter(
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Search,
    bool? IsActive,
    int PageIndex,
    int PageSize);

public sealed record FlashSaleProductFilter(
    Guid? CategoryId,
    bool SortDiscountAscending,
    int PageIndex,
    int PageSize);

public sealed record FlashSaleAdminFilter(
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Search,
    FlashSaleStatus? Status,
    int PageIndex,
    int PageSize);

public sealed record OrderAdminFilter(
    string? SearchUser,
    OrderStatus? Status,
    decimal? MinTotalPrice,
    decimal? MaxTotalPrice,
    bool SortTotalAscending,
    int PageIndex,
    int PageSize);
