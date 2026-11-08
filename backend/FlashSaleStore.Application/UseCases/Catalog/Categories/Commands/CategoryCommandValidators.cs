using FlashSaleStore.Application.UseCases.Catalog;
using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Catalog.Categories.Commands;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}

public sealed class BatchUpdateCategoryStatusCommandValidator : AbstractValidator<BatchUpdateCategoryStatusCommand>
{
    public BatchUpdateCategoryStatusCommandValidator()
    {
        RuleFor(command => command.Ids).NotNull().NotEmpty();
        RuleFor(command => command.Ids).Must(ids => ids is not null && ids.All(id => id != Guid.Empty));
        RuleFor(command => command.Ids).Must(ids => ids is not null && ids.Distinct().Count() == ids.Length)
            .WithMessage("Danh sách ID không được chứa phần tử trùng lặp.");
        RuleFor(command => command.Status).Must(status => CatalogUseCaseErrors.TryParseActiveStatus(status, out _))
            .WithMessage("Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.");
    }
}
