using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Payments;

/// <summary>Maps to dbo.Payments.</summary>
public sealed class Payment : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Provider { get; private set; } = "PayOS";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public long PayOsOrderCode { get; private set; }
    public string? PayOsPaymentLinkId { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? QrCode { get; private set; }
    public string? FailureReason { get; private set; }
    public bool RequiresManualRefund { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? ExpiredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public static Payment Create(Guid orderId, Guid userId, decimal amount,
        long payOsOrderCode, string provider = "PayOS")
    {
        var normalizedProvider = provider?.Trim();
        if (orderId == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainRuleViolationException("Payment.Reference.Required", "Đơn hàng và khách hàng của thanh toán phải được xác định.");
        }

        if (amount <= 0 || payOsOrderCode <= 0)
        {
            throw new DomainRuleViolationException("Payment.Values.Invalid", "Số tiền và mã đơn thanh toán phải lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(normalizedProvider) || normalizedProvider.Length > 50)
        {
            throw new DomainRuleViolationException("Payment.Provider.Invalid", "Nhà cung cấp thanh toán không hợp lệ.");
        }

        return new Payment
        {
            OrderId = orderId,
            UserId = userId,
            Amount = amount,
            PayOsOrderCode = payOsOrderCode,
            Provider = normalizedProvider
        };
    }

    public void SetCheckoutInformation(string? paymentLinkId, string? checkoutUrl, string? qrCode)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainRuleViolationException("Payment.Checkout.StatusInvalid", "Chỉ có thể cập nhật thông tin checkout khi thanh toán đang chờ.", DomainFailureKind.Conflict);
        }

        if (paymentLinkId?.Length > 255 || checkoutUrl?.Length > 2048 || qrCode?.Length > 2048)
        {
            throw new DomainRuleViolationException("Payment.Checkout.Invalid", "Thông tin checkout vượt quá độ dài cho phép.");
        }

        PayOsPaymentLinkId = paymentLinkId;
        CheckoutUrl = checkoutUrl;
        QrCode = qrCode;
        MarkUpdated();
    }

    /// <summary>
    /// Records gateway success. A late success remains paid and is flagged for manual refund;
    /// the associated order is never restored here.
    /// </summary>
    public void MarkPaid(string providerTransactionId, DateTime paidAtUtc, bool orderCanAcceptPayment)
    {
        var transactionId = providerTransactionId?.Trim();
        if (string.IsNullOrWhiteSpace(transactionId) || transactionId.Length > 100)
        {
            throw new DomainRuleViolationException("Payment.ProviderTransactionId.Invalid", "Mã giao dịch từ cổng thanh toán không hợp lệ.");
        }

        if (Status == PaymentStatus.Paid)
        {
            if (ProviderTransactionId == transactionId) return;

            throw new DomainRuleViolationException("Payment.Transaction.Conflict", "Thanh toán đã được ghi nhận với giao dịch khác.", DomainFailureKind.Conflict);
        }

        Status = PaymentStatus.Paid;
        ProviderTransactionId = transactionId;
        PaidAt = paidAtUtc;
        RequiresManualRefund = !orderCanAcceptPayment;
        FailureReason = null;
        FailedAt = null;
        ExpiredAt = null;
        CancelledAt = null;
        MarkUpdated(paidAtUtc);
    }

    public void MarkFailed(string reason, DateTime failedAtUtc)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainRuleViolationException("Payment.Status.TransitionInvalid", "Chỉ thanh toán đang chờ mới có thể chuyển sang thất bại.", DomainFailureKind.Conflict);
        }

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 500)
        {
            throw new DomainRuleViolationException("Payment.FailureReason.Invalid", "Lý do thất bại phải có tối đa 500 ký tự.");
        }

        Status = PaymentStatus.Failed;
        FailureReason = normalizedReason;
        FailedAt = failedAtUtc;
        MarkUpdated(failedAtUtc);
    }

    public void MarkExpired(DateTime expiredAtUtc)
    {
        if (Status == PaymentStatus.Expired) return;

        if (Status != PaymentStatus.Pending)
        {
            throw new DomainRuleViolationException("Payment.Status.TransitionInvalid", "Chỉ thanh toán đang chờ mới có thể chuyển sang hết hạn.", DomainFailureKind.Conflict);
        }

        Status = PaymentStatus.Expired;
        ExpiredAt = expiredAtUtc;
        MarkUpdated(expiredAtUtc);
    }

    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status == PaymentStatus.Cancelled) return;

        if (Status != PaymentStatus.Pending)
        {
            throw new DomainRuleViolationException("Payment.Status.TransitionInvalid", "Chỉ thanh toán đang chờ mới có thể bị hủy.", DomainFailureKind.Conflict);
        }

        Status = PaymentStatus.Cancelled;
        CancelledAt = cancelledAtUtc;
        MarkUpdated(cancelledAtUtc);
    }
}
