using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Application.UseCases.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Categories.Queries;

public sealed class GetActiveCategoriesQueryHandler(ICategoryQueries queries)
    : IRequestHandler<GetActiveCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetActiveCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await queries.GetActiveAsync(cancellationToken);
        return Result.Success(categories);
    }
}

public sealed class GetAdminCategoriesQueryHandler(ICategoryQueries queries)
    : IRequestHandler<GetAdminCategoriesQuery, Result<PagedResult<AdminCategoryDto>>>
{
    public async Task<Result<PagedResult<AdminCategoryDto>>> Handle(GetAdminCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await queries.GetAdminPageAsync(
            new CategoryAdminFilter(request.Search, request.PageIndex, request.PageSize), cancellationToken);
        return Result.Success(categories);
    }
}
