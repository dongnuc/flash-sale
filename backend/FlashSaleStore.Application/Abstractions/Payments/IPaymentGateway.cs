namespace FlashSaleStore.Application.Abstractions.Payments;

public sealed record PaymentGatewayCheckoutRequest(long OrderCode, decimal Amount, DateTime ExpiresAtUtc);
public sealed record PaymentGatewayCheckout(string PaymentLinkId, string? CheckoutUrl, string? QrCode);
public sealed record PaymentGatewayWebhook(long OrderCode, decimal Amount, bool IsSuccess, string TransactionId, DateTime ReceivedAtUtc, string? FailureReason);

public interface IPaymentGateway
{
    Task<PaymentGatewayCheckout> CreateCheckoutAsync(PaymentGatewayCheckoutRequest request, CancellationToken cancellationToken = default);
    Task<PaymentGatewayWebhook?> VerifyWebhookAsync(string provider, string rawBody, CancellationToken cancellationToken = default);
}

public sealed class PaymentGatewayException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
