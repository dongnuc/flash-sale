using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed record CreatePaymentCommand(Guid UserId, Guid OrderId) : IRequest<Result<PaymentIntentDto>>;
public sealed record ProcessPaymentWebhookCommand(string Provider, string RawBody) : IRequest<Result<PaymentWebhookAckDto>>;
public sealed record PaymentWebhookAckDto(bool Received, bool RequiresManualRefund = false);
