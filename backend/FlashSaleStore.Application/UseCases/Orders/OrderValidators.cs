using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PaymentMethod).Must(x => string.Equals(x, "ONLINE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Hiện chỉ hỗ trợ phương thức thanh toán ONLINE.");
        RuleFor(x => x.DeliveryAddress).NotNull().DependentRules(() =>
        {
            RuleFor(x => x.DeliveryAddress!.RecipientName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DeliveryAddress!.Phone).NotEmpty().MaximumLength(30);
            RuleFor(x => x.DeliveryAddress!.AddressLine).NotEmpty().MaximumLength(300);
            RuleFor(x => x.DeliveryAddress!.Province).MaximumLength(50);
            RuleFor(x => x.DeliveryAddress!.District).MaximumLength(50);
            RuleFor(x => x.DeliveryAddress!.Ward).MaximumLength(50);
            RuleFor(x => x.DeliveryAddress!.Note).MaximumLength(500);
        });
    }
}
public sealed class GetMyOrderHistoryQueryValidator : AbstractValidator<GetMyOrderHistoryQuery>
{
    public GetMyOrderHistoryQueryValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.Status).Must(x => x is null || Enum.GetNames<FlashSaleStore.Domain.Enums.OrderStatus>().Contains(x, StringComparer.OrdinalIgnoreCase)).WithMessage("Trạng thái đơn hàng không hợp lệ."); }
}
public sealed class GetMyOrderDetailQueryValidator : AbstractValidator<GetMyOrderDetailQuery>
{
    public GetMyOrderDetailQueryValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.OrderId).NotEmpty(); }
}
public sealed class GetAdminOrdersQueryValidator : AbstractValidator<GetAdminOrdersQuery>
{
    public GetAdminOrdersQueryValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1); RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(x => x is null || Enum.GetNames<FlashSaleStore.Domain.Enums.OrderStatus>().Contains(x, StringComparer.OrdinalIgnoreCase)).WithMessage("Trạng thái đơn hàng không hợp lệ.");
        RuleFor(x => x.SortDirection).Must(x => x is null || x.Equals("ASC", StringComparison.OrdinalIgnoreCase) || x.Equals("DESC", StringComparison.OrdinalIgnoreCase)).WithMessage("sortDirection chỉ nhận ASC hoặc DESC.");
        RuleFor(x => x).Must(x => x.MinTotalPrice is null || x.MaxTotalPrice is null || x.MinTotalPrice <= x.MaxTotalPrice).WithMessage("minTotalPrice phải nhỏ hơn hoặc bằng maxTotalPrice.");
    }
}
public sealed class GetAdminOrderDetailQueryValidator : AbstractValidator<GetAdminOrderDetailQuery>
{
    public GetAdminOrderDetailQueryValidator() => RuleFor(x => x.OrderId).NotEmpty();
}
