using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Catalog.Products.Queries;

public sealed class GetBestSellingProductsQueryValidator : AbstractValidator<GetBestSellingProductsQuery>
{
    public GetBestSellingProductsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetNewestProductsQueryValidator : AbstractValidator<GetNewestProductsQuery>
{
    public GetNewestProductsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetFeaturedProductsQueryValidator : AbstractValidator<GetFeaturedProductsQuery>
{
    public GetFeaturedProductsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetFlashSaleProductsQueryValidator : AbstractValidator<GetFlashSaleProductsQuery>
{
    public GetFlashSaleProductsQueryValidator()
    {
        RuleFor(query => query.CategoryId).Must(id => id is null || id != Guid.Empty);
        RuleFor(query => query.SortByDiscount)
            .Must(value => value is not null && (value.Equals("ASC", StringComparison.OrdinalIgnoreCase) || value.Equals("DESC", StringComparison.OrdinalIgnoreCase)));
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetProductCatalogQueryValidator : AbstractValidator<GetProductCatalogQuery>
{
    public GetProductCatalogQueryValidator()
    {
        RuleFor(query => query.CategoryId).Must(id => id is null || id != Guid.Empty);
        RuleFor(query => query.SortBy)
            .Must(value => value is not null && value.Equals("bestSelling", StringComparison.OrdinalIgnoreCase));
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetProductDetailQueryValidator : AbstractValidator<GetProductDetailQuery>
{
    public GetProductDetailQueryValidator() => RuleFor(query => query.ProductId).NotEmpty();
}

public sealed class GetAdminProductsQueryValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsQueryValidator()
    {
        RuleFor(query => query.CategoryId).Must(id => id is null || id != Guid.Empty);
        RuleFor(query => query.MinPrice).GreaterThanOrEqualTo(0).When(query => query.MinPrice.HasValue);
        RuleFor(query => query.MaxPrice).GreaterThanOrEqualTo(0).When(query => query.MaxPrice.HasValue);
        RuleFor(query => query)
            .Must(query => !query.MinPrice.HasValue || !query.MaxPrice.HasValue || query.MinPrice <= query.MaxPrice)
            .WithMessage("minPrice phải nhỏ hơn hoặc bằng maxPrice.");
        RuleFor(query => query.Status)
            .Must(status => status is null || status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase) || status.Equals("INACTIVE", StringComparison.OrdinalIgnoreCase));
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
