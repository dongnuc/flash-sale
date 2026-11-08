using FlashSaleStore.Domain.Entities.Payments;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IPaymentRepository : IEntityRepository<Payment, Guid>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByProviderTransactionIdAsync(string providerTransactionId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByPayOsOrderCodeAsync(long payOsOrderCode, CancellationToken cancellationToken = default);
    Task<int> SaveCheckoutInformationAsync(Payment payment, CancellationToken cancellationToken = default);
}
