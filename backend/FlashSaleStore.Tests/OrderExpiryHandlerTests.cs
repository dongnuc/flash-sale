using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Application.UseCases.Orders;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Domain.Entities.Payments;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Services;
using Xunit;

namespace FlashSaleStore.Tests;

public sealed class OrderExpiryHandlerTests
{
    [Fact]
    public async Task Expiry_handler_releases_stock_and_sale_quota_once_and_expires_payment()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var product = Product.Create(Guid.NewGuid(), "Widget", null, 100m, 10);
        var sale = FlashSale.Create("Sale", now.AddHours(-2), now.AddHours(2));
        var saleItem = FlashSaleItem.Create(sale.Id, product.Id, 70m, product.Price, 5, 10, null);
        product.ReserveStock(3);
        saleItem.Reserve(2);
        var order = Order.Create(userId);
        order.SetShippingInformation("Customer", "0900000000", "Address");
        order.AddLine(new OrderLineAllocation(product.Id, saleItem.Id, 2, 70m), product.Name);
        order.AddLine(new OrderLineAllocation(product.Id, null, 1, 100m), product.Name);
        order.FinalizeCheckout(true);
        var payment = Payment.Create(order.Id, userId, order.TotalAmount, 123456789);
        var unitOfWork = new DirectUnitOfWork();
        var orderRepository = new ExpiringOrderRepository(order);
        var productRepository = new ExpiringProductRepository(product);
        var saleItemRepository = new ExpiringFlashSaleItemRepository(saleItem);
        var paymentRepository = new ExpiringPaymentRepository(payment);
        var handler = new ExpireUnpaidOrdersCommandHandler(orderRepository, new ExpiringOrderDetailsRepository(order), paymentRepository,
            new OrderInventoryReleaser(productRepository, saleItemRepository), unitOfWork);

        var first = await handler.Handle(new(order.ExpiresAt!.Value, 10), CancellationToken.None);
        var second = await handler.Handle(new(order.ExpiresAt.Value.AddMinutes(1), 10), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value!.CancelledCount);
        Assert.Equal(1, first.Value.RestockedCount);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(0, saleItem.SoldQuantity);
        Assert.Equal(OrderStatus.Expired, order.Status);
        Assert.Equal(PaymentStatus.Expired, payment.Status);
        Assert.Equal(0, second.Value!.ScannedCount);
        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(0, saleItem.SoldQuantity);
    }

    private sealed class DirectUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
    private sealed class ExpiringOrderRepository(Order entity) : IOrderRepository
    {
        private bool _transitioned;
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(id == entity.Id ? entity : null);
        public Task<Order?> GetByIdForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<Order?>(null);
        public Task<IReadOnlyList<Order>> GetExpiredUnpaidAsync(DateTime utcNow, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Order>>(!_transitioned && entity.ExpiresAt <= utcNow && entity.Status == OrderStatus.Pending ? [entity] : []);
        public Task<int> TryExpireAsync(Order order, DateTime utcNow, CancellationToken cancellationToken = default)
        {
            if (_transitioned || order.Id != entity.Id || order.ExpiresAt > utcNow) return Task.FromResult(0);
            _transitioned = true;
            return Task.FromResult(1);
        }
        public Task<int> InsertAsync(Order value, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> UpdateAsync(Order value, CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
    private sealed class ExpiringOrderDetailsRepository(Order entity) : IOrderDetailRepository
    {
        public Task<OrderDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(entity.Details.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<OrderDetail>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<OrderDetail>>(entity.Details.ToArray());
        public Task<int> InsertAsync(OrderDetail value, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> InsertRangeAsync(IReadOnlyCollection<OrderDetail> values, CancellationToken cancellationToken = default) => Task.FromResult(values.Count);
    }
    private sealed class ExpiringProductRepository(Product entity) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(id == entity.Id ? entity : null);
        public Task<Product?> GetByNormalizedNameAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<Product?>(null);
        public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Product>>(ids.Contains(entity.Id) ? [entity] : []);
        public Task<int> GetQuantityHeldByPendingOrdersAsync(Guid productId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> InsertAsync(Product value, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> UpdateAsync(Product value, CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
    private sealed class ExpiringFlashSaleItemRepository(FlashSaleItem entity) : IFlashSaleItemRepository
    {
        public Task<FlashSaleItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<FlashSaleItem?>(id == entity.Id ? entity : null);
        public Task<IReadOnlyList<FlashSaleItem>> GetByFlashSaleIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FlashSaleItem>>([]);
        public Task<IReadOnlyList<FlashSaleItem>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FlashSaleItem>>(ids.Contains(entity.Id) ? [entity] : []);
        public Task<FlashSaleItem?> GetByFlashSaleAndProductAsync(Guid flashSaleId, Guid productId, CancellationToken cancellationToken = default) => Task.FromResult<FlashSaleItem?>(null);
        public Task<IReadOnlyList<FlashSaleSchedule>> GetSchedulesByProductIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FlashSaleSchedule>>([]);
        public Task<int> GetPurchasedOrReservedQuantityAsync(Guid flashSaleItemId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> InsertAsync(FlashSaleItem value, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> UpdateAsync(FlashSaleItem value, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
    private sealed class ExpiringPaymentRepository(Payment entity) : IPaymentRepository
    {
        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Payment?>(id == entity.Id ? entity : null);
        public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) => Task.FromResult<Payment?>(orderId == entity.OrderId ? entity : null);
        public Task<Payment?> GetByProviderTransactionIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Payment?>(null);
        public Task<Payment?> GetByPayOsOrderCodeAsync(long code, CancellationToken cancellationToken = default) => Task.FromResult<Payment?>(null);
        public Task<int> SaveCheckoutInformationAsync(Payment payment, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> InsertAsync(Payment value, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<int> UpdateAsync(Payment value, CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
