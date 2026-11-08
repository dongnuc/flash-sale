using System.Security.Cryptography;
using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Payments;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Domain.Entities.Payments;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed class CreatePaymentCommandHandler(IOrderRepository orders, IPaymentRepository payments, IPaymentGateway gateway, IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePaymentCommand, Result<PaymentIntentDto>>
{
    private static long _lastOrderCode;

    public async Task<Result<PaymentIntentDto>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        Payment? paymentRecord = null;
        DateTime paymentExpiresAt = default;
        try
        {
            var pending = await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var order = await orders.GetByIdForUserAsync(request.OrderId, request.UserId, ct);
                if (order is null) return (Error: CommerceUseCaseErrors.NotFound("Order", request.OrderId), Payment: (Payment?)null);
                var now = DateTime.UtcNow;
                if (order.Status != OrderStatus.Pending || order.ExpiresAt is null || order.ExpiresAt <= now)
                    return (Error: new Error("Payment.Order.NotPayable", "Đơn hàng không còn trong thời hạn thanh toán."), Payment: (Payment?)null);
                var existing = await payments.GetByOrderIdAsync(order.Id, ct);
                if (existing is { Status: PaymentStatus.Pending }) return (Error: Error.None, Payment: existing);
                var code = NextOrderCode();
                var payment = Payment.Create(order.Id, request.UserId, order.TotalAmount, code);
                if (await payments.InsertAsync(payment, ct) != 1) throw new DomainRuleViolationException("Payment.CreateFailed", "Không thể tạo bản ghi thanh toán.");
                return (Error: Error.None, Payment: payment);
            }, cancellationToken);

            if (pending.Error.Code.Length != 0) return Result.Failure<PaymentIntentDto>(pending.Error);
            paymentRecord = pending.Payment!;
            var orderRecord = await orders.GetByIdForUserAsync(request.OrderId, request.UserId, cancellationToken);
            if (orderRecord?.ExpiresAt is null || orderRecord.Status != OrderStatus.Pending || orderRecord.ExpiresAt <= DateTime.UtcNow)
                return Result.Failure<PaymentIntentDto>(new Error("Payment.Order.NotPayable", "Đơn hàng không còn trong thời hạn thanh toán."));
            paymentExpiresAt = orderRecord.ExpiresAt.Value;
            if (paymentRecord.CheckoutUrl is not null) return Result.Success(ToIntent(paymentRecord, paymentExpiresAt));

            var checkout = await gateway.CreateCheckoutAsync(new(paymentRecord.PayOsOrderCode, paymentRecord.Amount, paymentExpiresAt), cancellationToken);
            if (string.IsNullOrWhiteSpace(checkout.PaymentLinkId) || string.IsNullOrWhiteSpace(checkout.CheckoutUrl))
                throw new PaymentGatewayException("Payment.Provider.ResponseInvalid", "PayOS trả về thông tin link thanh toán không hợp lệ.");
            paymentRecord.SetCheckoutInformation(checkout.PaymentLinkId, checkout.CheckoutUrl, checkout.QrCode);
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                if (await payments.SaveCheckoutInformationAsync(paymentRecord, ct) != 1) throw new DomainRuleViolationException("Payment.SaveFailed", "Khoản thanh toán đã hết hạn hoặc thay đổi trước khi lưu link.");
            }, cancellationToken);
            return Result.Success(ToIntent(paymentRecord, paymentExpiresAt));
        }
        catch (PaymentGatewayException ex)
        {
            if (paymentRecord is not null)
            {
                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var current = await payments.GetByIdAsync(paymentRecord.Id, ct);
                    if (current?.Status == PaymentStatus.Pending)
                    {
                        current.MarkFailed(ex.Message, DateTime.UtcNow);
                        await payments.UpdateAsync(current, ct);
                    }
                }, cancellationToken);
            }
            return Result.Failure<PaymentIntentDto>(new Error(ex.Code, ex.Message));
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<PaymentIntentDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }

    private static long NextOrderCode()
    {
        var candidate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + RandomNumberGenerator.GetInt32(0, 1000);
        while (true)
        {
            var previous = Interlocked.Read(ref _lastOrderCode);
            var next = Math.Max(candidate, previous + 1);
            if (Interlocked.CompareExchange(ref _lastOrderCode, next, previous) == previous) return next;
        }
    }

    private static PaymentIntentDto ToIntent(Payment payment, DateTime expiresAt) => new()
    {
        PaymentId = payment.Id, Provider = payment.Provider, CheckoutUrl = payment.CheckoutUrl,
        QrCode = payment.QrCode, Amount = payment.Amount, ExpiresAt = expiresAt
    };
}

public sealed class ProcessPaymentWebhookCommandHandler(IPaymentGateway gateway, IPaymentRepository payments, IOrderRepository orders,
    IOrderDetailRepository details, OrderInventoryReleaser inventoryReleaser, IUnitOfWork unitOfWork)
    : IRequestHandler<ProcessPaymentWebhookCommand, Result<PaymentWebhookAckDto>>
{
    public async Task<Result<PaymentWebhookAckDto>> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        PaymentGatewayWebhook? webhook;
        try { webhook = await gateway.VerifyWebhookAsync(request.Provider, request.RawBody, cancellationToken); }
        catch (PaymentGatewayException ex) { return Result.Failure<PaymentWebhookAckDto>(new Error(ex.Code, ex.Message)); }
        if (webhook is null) return Result.Failure<PaymentWebhookAckDto>(new Error("Payment.Webhook.SignatureInvalid", "Chữ ký webhook không hợp lệ."));

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var payment = await payments.GetByPayOsOrderCodeAsync(webhook.OrderCode, ct);
                if (payment is null) return Result.Failure<PaymentWebhookAckDto>(CommerceUseCaseErrors.NotFound("Payment", Guid.Empty));
                if (payment.Amount != webhook.Amount) return Result.Failure<PaymentWebhookAckDto>(new Error("Payment.Webhook.AmountMismatch", "Số tiền callback không khớp khoản thanh toán."));
                var order = await orders.GetByIdAsync(payment.OrderId, ct);
                if (order is null) return Result.Failure<PaymentWebhookAckDto>(CommerceUseCaseErrors.NotFound("Order", payment.OrderId));
                if (!webhook.IsSuccess)
                {
                    if (payment.Status == PaymentStatus.Pending)
                    {
                        payment.MarkFailed(webhook.FailureReason ?? "Thanh toán thất bại từ PayOS.", webhook.ReceivedAtUtc);
                        if (await payments.UpdateAsync(payment, ct) != 1) throw new DomainRuleViolationException("Payment.SaveFailed", "Không thể cập nhật thanh toán.");
                    }
                    return Result.Success(new PaymentWebhookAckDto(true));
                }

                var canPay = order.Status == OrderStatus.Pending && (order.ExpiresAt is null || order.ExpiresAt > webhook.ReceivedAtUtc);
                if (order.Status == OrderStatus.Pending && !canPay && order.TryExpire(webhook.ReceivedAtUtc))
                {
                    order.LoadDetails(await details.GetByOrderIdAsync(order.Id, ct));
                    if (await orders.TryExpireAsync(order, webhook.ReceivedAtUtc, ct) == 1)
                    {
                        await inventoryReleaser.ReleaseAsync(order, ct);
                        var relatedPayment = await payments.GetByOrderIdAsync(order.Id, ct);
                        if (relatedPayment?.Status == PaymentStatus.Pending && relatedPayment.Id != payment.Id)
                        {
                            relatedPayment.MarkExpired(webhook.ReceivedAtUtc);
                            if (await payments.UpdateAsync(relatedPayment, ct) != 1) throw new DomainRuleViolationException("Payment.SaveFailed", "Không thể hết hạn payment cũ.");
                        }
                    }
                }
                if (canPay) order.MarkPaid(webhook.ReceivedAtUtc);
                payment.MarkPaid(webhook.TransactionId, webhook.ReceivedAtUtc, canPay);
                if (canPay && await orders.UpdateAsync(order, ct) != 1) throw new DomainRuleViolationException("Order.Payment.SaveFailed", "Không thể cập nhật trạng thái đơn hàng.");
                if (await payments.UpdateAsync(payment, ct) != 1) throw new DomainRuleViolationException("Payment.SaveFailed", "Không thể cập nhật trạng thái thanh toán.");
                return Result.Success(new PaymentWebhookAckDto(true, payment.RequiresManualRefund));
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<PaymentWebhookAckDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}
