using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Enums;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed record ExpireUnpaidOrdersCommand(DateTime UtcNow, int BatchSize) : IRequest<Result<ExpireOrdersReportDto>>;
public sealed record ExpireOrdersReportDto(int ScannedCount, int CancelledCount, int RestockedCount, int FailedCount, DateTime RunAt);

public sealed class ExpireUnpaidOrdersCommandHandler(IOrderRepository orders, IOrderDetailRepository details, IPaymentRepository payments,
    OrderInventoryReleaser inventoryReleaser, IUnitOfWork unitOfWork)
    : IRequestHandler<ExpireUnpaidOrdersCommand, Result<ExpireOrdersReportDto>>
{
    public async Task<Result<ExpireOrdersReportDto>> Handle(ExpireUnpaidOrdersCommand request, CancellationToken cancellationToken)
    {
        if (request.BatchSize is < 1 or > 1000)
            return Result.Failure<ExpireOrdersReportDto>(CommerceUseCaseErrors.Invalid("OrderExpiry.BatchSize.Invalid", "batchSize phải từ 1 đến 1000."));
        var candidates = await orders.GetExpiredUnpaidAsync(request.UtcNow, request.BatchSize, cancellationToken);
        var cancelled = 0;
        var restocked = 0;
        var failed = 0;
        foreach (var candidate in candidates)
        {
            try
            {
                var processed = await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var order = await orders.GetByIdAsync(candidate.Id, ct);
                    if (order is null || !order.TryExpire(request.UtcNow)) return false;
                    var orderLines = await details.GetByOrderIdAsync(order.Id, ct);
                    order.LoadDetails(orderLines);
                    if (await orders.TryExpireAsync(order, request.UtcNow, ct) != 1) return false;
                    await inventoryReleaser.ReleaseAsync(order, ct);
                    var payment = await payments.GetByOrderIdAsync(order.Id, ct);
                    if (payment?.Status == PaymentStatus.Pending)
                    {
                        payment.MarkExpired(request.UtcNow);
                        if (await payments.UpdateAsync(payment, ct) != 1)
                            throw new InvalidOperationException("Could not mark expired payment.");
                    }
                    return true;
                }, cancellationToken);
                if (processed) { cancelled++; restocked++; }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch
            {
                failed++;
            }
        }
        return Result.Success(new ExpireOrdersReportDto(candidates.Count, cancelled, restocked, failed, request.UtcNow));
    }
}
