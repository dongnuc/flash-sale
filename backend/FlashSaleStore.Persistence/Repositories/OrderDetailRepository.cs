using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class OrderDetailRepository : SqlRepositoryBase, IOrderDetailRepository
{
    public OrderDetailRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<OrderDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<OrderDetail>("""
            SELECT Id, OrderId, FlashSaleItemId, ProductId, ProductName, Quantity, UnitPrice, LineTotal, CreatedAt, UpdatedAt
            FROM dbo.OrderDetails
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public async Task<IReadOnlyList<OrderDetail>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await QueryAsync<OrderDetail>("""
            SELECT Id, OrderId, FlashSaleItemId, ProductId, ProductName, Quantity, UnitPrice, LineTotal, CreatedAt, UpdatedAt
            FROM dbo.OrderDetails
            WHERE OrderId = @OrderId
            ORDER BY CreatedAt, Id;
            """, new { OrderId = orderId }, cancellationToken);

    public Task<int> InsertAsync(OrderDetail entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.OrderDetails
                (Id, OrderId, FlashSaleItemId, ProductId, ProductName, Quantity, UnitPrice, LineTotal, CreatedAt, UpdatedAt)
            VALUES
                (@Id, @OrderId, @FlashSaleItemId, @ProductId, @ProductName, @Quantity, @UnitPrice, @LineTotal, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> InsertRangeAsync(IReadOnlyCollection<OrderDetail> entities, CancellationToken cancellationToken = default)
    {
        if (entities.Count == 0) return Task.FromResult(0);
        const string sql = """
            INSERT INTO dbo.OrderDetails
                (Id, OrderId, FlashSaleItemId, ProductId, ProductName, Quantity, UnitPrice, LineTotal, CreatedAt, UpdatedAt)
            VALUES
                (@Id, @OrderId, @FlashSaleItemId, @ProductId, @ProductName, @Quantity, @UnitPrice, @LineTotal, @CreatedAt, @UpdatedAt);
            """;
        return ExecuteAsync(sql, entities, cancellationToken);
    }
}
