using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Payments;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class PaymentRepository : SqlRepositoryBase, IPaymentRepository
{
    public PaymentRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Payment>("""
            SELECT Id, OrderId, UserId, Amount, Provider, Status, RequiresManualRefund, PayOsOrderCode,
                   PayOsPaymentLinkId, ProviderTransactionId, CheckoutUrl, QrCode, FailureReason,
                   CreatedAt, UpdatedAt, PaidAt, FailedAt, ExpiredAt, CancelledAt
            FROM dbo.Payments
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        QueryFirstOrDefaultAsync<Payment>("""
            SELECT TOP (1) Id, OrderId, UserId, Amount, Provider, Status, RequiresManualRefund, PayOsOrderCode,
                   PayOsPaymentLinkId, ProviderTransactionId, CheckoutUrl, QrCode, FailureReason,
                   CreatedAt, UpdatedAt, PaidAt, FailedAt, ExpiredAt, CancelledAt
            FROM dbo.Payments
            WHERE OrderId = @OrderId
            ORDER BY CreatedAt DESC, Id DESC;
            """, new { OrderId = orderId }, cancellationToken);

    public Task<Payment?> GetByProviderTransactionIdAsync(string providerTransactionId, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Payment>("""
            SELECT Id, OrderId, UserId, Amount, Provider, Status, RequiresManualRefund, PayOsOrderCode,
                   PayOsPaymentLinkId, ProviderTransactionId, CheckoutUrl, QrCode, FailureReason,
                   CreatedAt, UpdatedAt, PaidAt, FailedAt, ExpiredAt, CancelledAt
            FROM dbo.Payments
            WHERE ProviderTransactionId = @ProviderTransactionId;
            """, new { ProviderTransactionId = providerTransactionId }, cancellationToken);

    public Task<Payment?> GetByPayOsOrderCodeAsync(long payOsOrderCode, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Payment>("""
            SELECT Id, OrderId, UserId, Amount, Provider, Status, RequiresManualRefund, PayOsOrderCode,
                   PayOsPaymentLinkId, ProviderTransactionId, CheckoutUrl, QrCode, FailureReason,
                   CreatedAt, UpdatedAt, PaidAt, FailedAt, ExpiredAt, CancelledAt
            FROM dbo.Payments WHERE PayOsOrderCode = @PayOsOrderCode;
            """, new { PayOsOrderCode = payOsOrderCode }, cancellationToken);

    public Task<int> InsertAsync(Payment entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.Payments
                (Id, OrderId, UserId, Amount, Provider, Status, RequiresManualRefund, PayOsOrderCode,
                 PayOsPaymentLinkId, ProviderTransactionId, CheckoutUrl, QrCode, FailureReason,
                 CreatedAt, UpdatedAt, PaidAt, FailedAt, ExpiredAt, CancelledAt)
            VALUES
                (@Id, @OrderId, @UserId, @Amount, @Provider, @Status, @RequiresManualRefund, @PayOsOrderCode,
                 @PayOsPaymentLinkId, @ProviderTransactionId, @CheckoutUrl, @QrCode, @FailureReason,
                 @CreatedAt, @UpdatedAt, @PaidAt, @FailedAt, @ExpiredAt, @CancelledAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(Payment entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Payments
            SET Status = @Status, RequiresManualRefund = @RequiresManualRefund,
                PayOsPaymentLinkId = @PayOsPaymentLinkId, ProviderTransactionId = @ProviderTransactionId,
                CheckoutUrl = @CheckoutUrl, QrCode = @QrCode, FailureReason = @FailureReason,
                UpdatedAt = @UpdatedAt, PaidAt = @PaidAt, FailedAt = @FailedAt,
                ExpiredAt = @ExpiredAt, CancelledAt = @CancelledAt
            WHERE Id = @Id;
            """, entity, cancellationToken);

    public Task<int> SaveCheckoutInformationAsync(Payment payment, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Payments
            SET PayOsPaymentLinkId = @PayOsPaymentLinkId, CheckoutUrl = @CheckoutUrl,
                QrCode = @QrCode, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND Status = 0;
            """, payment, cancellationToken);
}
