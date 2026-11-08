using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;

namespace FlashSaleStore.Application.Abstractions.Queries;

public interface ICategoryQueries
{
    Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminCategoryDto>> GetAdminPageAsync(CategoryAdminFilter filter, CancellationToken cancellationToken = default);
    Task<AdminCategoryDto?> GetAdminByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
