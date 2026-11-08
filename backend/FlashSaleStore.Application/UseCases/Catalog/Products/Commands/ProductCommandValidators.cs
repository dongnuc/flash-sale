using FlashSaleStore.Application.UseCases.Catalog;
using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Catalog.Products.Commands;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
        RuleFor(command => command.RegularPrice).GreaterThan(0);
        RuleFor(command => command.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Status).Must(status => CatalogUseCaseErrors.TryParseActiveStatus(status, out _))
            .WithMessage("Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.");
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(1000);
        RuleFor(command => command.RegularPrice).GreaterThan(0);
        RuleFor(command => command.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Status).Must(status => CatalogUseCaseErrors.TryParseActiveStatus(status, out _))
            .WithMessage("Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.");
    }
}

public sealed class BatchUpdateProductStatusCommandValidator : AbstractValidator<BatchUpdateProductStatusCommand>
{
    public BatchUpdateProductStatusCommandValidator()
    {
        RuleFor(command => command.Ids).NotNull().NotEmpty();
        RuleFor(command => command.Ids).Must(ids => ids is not null && ids.All(id => id != Guid.Empty));
        RuleFor(command => command.Ids).Must(ids => ids is not null && ids.Distinct().Count() == ids.Length)
            .WithMessage("Danh sách ID không được chứa phần tử trùng lặp.");
        RuleFor(command => command.Status).Must(status => CatalogUseCaseErrors.TryParseActiveStatus(status, out _))
            .WithMessage("Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.");
    }
}
