using FlashSaleStore.Application.UseCases.Orders;
using MediatR;

namespace FlashSaleStore.API.BackgroundServices;

public sealed class OrderExpiryBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<OrderExpiryBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batchSize = Math.Clamp(configuration.GetValue("BackgroundJobs:OrderExpiry:BatchSize", 100), 1, 1000);
        var interval = TimeSpan.FromSeconds(Math.Clamp(configuration.GetValue("BackgroundJobs:OrderExpiry:IntervalSeconds", 30), 5, 3600));
        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ExpireUnpaidOrdersCommand(DateTime.UtcNow, batchSize), stoppingToken);
                if (result.IsSuccess && result.Value is { ScannedCount: > 0 } report)
                    logger.LogInformation("Expired unpaid orders: scanned={Scanned}, cancelled={Cancelled}, restocked={Restocked}, failed={Failed}", report.ScannedCount, report.CancelledCount, report.RestockedCount, report.FailedCount);
                else if (result.IsFailure)
                    logger.LogError("Order expiry worker rejected the batch: {Code} {Message}", result.Error.Code, result.Error.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Order expiry worker run failed."); }
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
