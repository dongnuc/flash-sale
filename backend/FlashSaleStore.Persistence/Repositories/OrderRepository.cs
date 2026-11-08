using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class OrderRepository : SqlRepositoryBase, IOrderRepository
{
    public OrderRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Order>("""
            SELECT Id, UserId, TotalAmount, Status, IdempotencyKey, IdempotencyRequestHash, ExpiresAt,
                   ReceiverName, ReceiverPhone, ShippingAddress, Province, District, Ward, AddressLine, Note, CreatedAt, UpdatedAt
            FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public Task<Order?> GetByIdForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Order>("""
            SELECT Id, UserId, TotalAmount, Status, IdempotencyKey, IdempotencyRequestHash, ExpiresAt,
                   ReceiverName, ReceiverPhone, ShippingAddress, Province, District, Ward, AddressLine, Note, CreatedAt, UpdatedAt
            FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @OrderId AND UserId = @UserId;
            """, new { OrderId = orderId, UserId = userId }, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetExpiredUnpaidAsync(DateTime utcNow, int batchSize, CancellationToken cancellationToken = default) =>
        await QueryAsync<Order>("""
            SELECT TOP (@BatchSize) Id, UserId, TotalAmount, Status, IdempotencyKey, IdempotencyRequestHash, ExpiresAt,
                   ReceiverName, ReceiverPhone, ShippingAddress, Province, District, Ward, AddressLine, Note, CreatedAt, UpdatedAt
            FROM dbo.Orders
            WHERE Status = @PendingStatus AND ExpiresAt IS NOT NULL AND ExpiresAt <= @UtcNow
            ORDER BY ExpiresAt, Id;
            """, new { BatchSize = batchSize, PendingStatus = OrderStatus.Pending, UtcNow = utcNow }, cancellationToken);

    public Task<int> TryExpireAsync(Order order, DateTime utcNow, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Orders
            SET Status = @ExpiredStatus, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND Status = @PendingStatus AND ExpiresAt IS NOT NULL AND ExpiresAt <= @UtcNow;
            """, new { order.Id, order.UpdatedAt, UtcNow = utcNow, PendingStatus = OrderStatus.Pending, ExpiredStatus = OrderStatus.Expired }, cancellationToken);

    public Task<int> InsertAsync(Order entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.Orders
                (Id, UserId, TotalAmount, Status, IdempotencyKey, IdempotencyRequestHash, ExpiresAt,
                ReceiverName, ReceiverPhone, ShippingAddress, Province, District, Ward, AddressLine, Note, CreatedAt, UpdatedAt)
            VALUES
                (@Id, @UserId, @TotalAmount, @Status, @IdempotencyKey, @IdempotencyRequestHash, @ExpiresAt,
                 @ReceiverName, @ReceiverPhone, @ShippingAddress, @Province, @District, @Ward, @AddressLine, @Note, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(Order entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Orders
            SET TotalAmount = @TotalAmount, Status = @Status, IdempotencyKey = @IdempotencyKey,
                IdempotencyRequestHash = @IdempotencyRequestHash, ExpiresAt = @ExpiresAt,
                ReceiverName = @ReceiverName, ReceiverPhone = @ReceiverPhone,
                ShippingAddress = @ShippingAddress, Province = @Province, District = @District, Ward = @Ward,
                AddressLine = @AddressLine, Note = @Note, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, entity, cancellationToken);
}
