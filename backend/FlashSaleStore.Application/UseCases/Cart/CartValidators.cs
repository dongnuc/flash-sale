using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Cart;

public sealed class GetMyCartQueryValidator : AbstractValidator<GetMyCartQuery>
{
    public GetMyCartQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}
public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.ProductId).NotEmpty(); RuleFor(x => x.Quantity).GreaterThan(0); }
}
public sealed class SetCartItemQuantityCommandValidator : AbstractValidator<SetCartItemQuantityCommand>
{
    public SetCartItemQuantityCommandValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.ProductId).NotEmpty(); RuleFor(x => x.Quantity).GreaterThan(0); }
}
public sealed class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemCommandValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.ProductId).NotEmpty(); }
}
