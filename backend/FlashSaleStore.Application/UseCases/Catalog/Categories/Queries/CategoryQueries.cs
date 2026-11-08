using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Catalog.Categories.Queries;

public sealed record GetActiveCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;

public sealed record GetAdminCategoriesQuery(
    string? Search = null,
    int PageIndex = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<AdminCategoryDto>>>;
