using FluentValidation;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator() { RuleFor(x => x.UserId).NotEmpty(); RuleFor(x => x.OrderId).NotEmpty(); }
}
public sealed class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookCommandValidator() { RuleFor(x => x.Provider).NotEmpty().MaximumLength(50); RuleFor(x => x.RawBody).NotEmpty(); }
}
