using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Catalog.Categories.Queries;

public sealed class GetAdminCategoriesQueryValidator : AbstractValidator<GetAdminCategoriesQuery>
{
    public GetAdminCategoriesQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
