using FluentValidation;

namespace FlashSaleStore.Application.UseCases.FlashSales.Commands;

public sealed class CreateFlashSaleCommandValidator : AbstractValidator<CreateFlashSaleCommand>
{
    public CreateFlashSaleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartsAt).Must(x => x.Kind == DateTimeKind.Utc).WithMessage("Thời gian phải có múi giờ UTC (đuôi Z).");
        RuleFor(x => x.EndsAt).Must(x => x.Kind == DateTimeKind.Utc).WithMessage("Thời gian phải có múi giờ UTC (đuôi Z).");
        RuleFor(x => x).Must(x => x.EndsAt > x.StartsAt).WithMessage("endsAt phải sau startsAt.");
        RuleFor(x => x.Items).NotNull().NotEmpty();
        RuleFor(x => x.Items).Must(x => x is not null && x.Select(i => i.ProductId).Distinct().Count() == x.Count).WithMessage("Danh sách sản phẩm không được trùng.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty(); item.RuleFor(x => x.FinalPrice).GreaterThan(0);
            item.RuleFor(x => x.FlashSaleQuantity).GreaterThan(0); item.RuleFor(x => x.LimitPerUser).GreaterThan(0).When(x => x.LimitPerUser.HasValue);
        });
    }
}

public sealed class UpdateFlashSaleCommandValidator : AbstractValidator<UpdateFlashSaleCommand>
{
    public UpdateFlashSaleCommandValidator()
    {
        RuleFor(x => x.FlashSaleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartsAt).Must(x => x.Kind == DateTimeKind.Utc).WithMessage("Thời gian phải có múi giờ UTC (đuôi Z).");
        RuleFor(x => x.EndsAt).Must(x => x.Kind == DateTimeKind.Utc).WithMessage("Thời gian phải có múi giờ UTC (đuôi Z).");
        RuleFor(x => x).Must(x => x.EndsAt > x.StartsAt).WithMessage("endsAt phải sau startsAt.");
        RuleFor(x => x.Items).NotNull().NotEmpty();
        RuleFor(x => x.Items).Must(x => x is not null && x.Select(i => i.ProductId).Distinct().Count() == x.Count).WithMessage("Danh sách sản phẩm không được trùng.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty(); item.RuleFor(x => x.FinalPrice).GreaterThan(0);
            item.RuleFor(x => x.FlashSaleQuantity).GreaterThan(0); item.RuleFor(x => x.LimitPerUser).GreaterThan(0).When(x => x.LimitPerUser.HasValue);
        });
    }
}

public sealed class BatchUpdateFlashSaleStatusCommandValidator : AbstractValidator<BatchUpdateFlashSaleStatusCommand>
{
    public BatchUpdateFlashSaleStatusCommandValidator()
    {
        RuleFor(x => x.Ids).NotNull().NotEmpty();
        RuleFor(x => x.Ids).Must(x => x is not null && x.All(id => id != Guid.Empty) && x.Distinct().Count() == x.Length).WithMessage("IDs phải hợp lệ và không được trùng.");
        RuleFor(x => x.Status).Must(x => Enum.GetNames<FlashSaleStore.Domain.Enums.FlashSaleStatus>().Contains(x, StringComparer.OrdinalIgnoreCase)).WithMessage("Trạng thái FlashSale không hợp lệ.");
    }
}

public sealed class SetFlashSaleStartTimeCommandValidator : AbstractValidator<SetFlashSaleStartTimeCommand>
{
    public SetFlashSaleStartTimeCommandValidator() { RuleFor(x => x.FlashSaleId).NotEmpty(); RuleFor(x => x.StartsAt).Must(x => x.Kind == DateTimeKind.Utc).WithMessage("Thời gian phải có múi giờ UTC (đuôi Z)."); }
}
