using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Services;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class FlashSaleItemRepository : SqlRepositoryBase, IFlashSaleItemRepository
{
    public FlashSaleItemRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<FlashSaleItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<FlashSaleItem>("""
            SELECT Id, FlashSaleId, ProductId, SalePrice, SaleQuantity, SoldQuantity, LimitPerUser,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.FlashSaleItems
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public async Task<IReadOnlyList<FlashSaleItem>> GetByFlashSaleIdAsync(Guid flashSaleId, CancellationToken cancellationToken = default) =>
        await QueryAsync<FlashSaleItem>("""
            SELECT Id, FlashSaleId, ProductId, SalePrice, SaleQuantity, SoldQuantity, LimitPerUser,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.FlashSaleItems
            WHERE FlashSaleId = @FlashSaleId;
            """, new { FlashSaleId = flashSaleId }, cancellationToken);

    public async Task<IReadOnlyList<FlashSaleItem>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<FlashSaleItem>();
        return await QueryAsync<FlashSaleItem>("""
            SELECT Id, FlashSaleId, ProductId, SalePrice, SaleQuantity, SoldQuantity, LimitPerUser,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.FlashSaleItems
            WHERE Id IN @Ids;
            """, new { Ids = ids }, cancellationToken);
    }

    public Task<FlashSaleItem?> GetByFlashSaleAndProductAsync(Guid flashSaleId, Guid productId, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<FlashSaleItem>("""
            SELECT Id, FlashSaleId, ProductId, SalePrice, SaleQuantity, SoldQuantity, LimitPerUser,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.FlashSaleItems
            WHERE FlashSaleId = @FlashSaleId AND ProductId = @ProductId;
            """, new { FlashSaleId = flashSaleId, ProductId = productId }, cancellationToken);

    public async Task<IReadOnlyList<FlashSaleSchedule>> GetSchedulesByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0) return Array.Empty<FlashSaleSchedule>();
        return await QueryAsync<FlashSaleSchedule>("""
            SELECT fs.Id AS FlashSaleId, fsi.ProductId, fs.StartsAt, fs.EndsAt, fs.Status
            FROM dbo.FlashSaleItems AS fsi WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN dbo.FlashSales AS fs WITH (UPDLOCK, HOLDLOCK) ON fs.Id = fsi.FlashSaleId
            WHERE fsi.ProductId IN @ProductIds;
            """, new { ProductIds = productIds }, cancellationToken);
    }

    public Task<int> GetPurchasedOrReservedQuantityAsync(Guid flashSaleItemId, Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteScalarAsync<int>("""
            SELECT COALESCE(SUM(od.Quantity), 0)
            FROM dbo.OrderDetails AS od
            INNER JOIN dbo.Orders AS o ON o.Id = od.OrderId
            WHERE od.FlashSaleItemId = @FlashSaleItemId
              AND o.UserId = @UserId
              AND o.Status IN (0, 1, 2, 3);
            """, new { FlashSaleItemId = flashSaleItemId, UserId = userId }, cancellationToken);

    public Task<int> InsertAsync(FlashSaleItem entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.FlashSaleItems
                (Id, FlashSaleId, ProductId, SalePrice, SaleQuantity, SoldQuantity, LimitPerUser, CreatedAt, UpdatedAt)
            VALUES
                (@Id, @FlashSaleId, @ProductId, @SalePrice, @SaleQuantity, @SoldQuantity, @LimitPerUser, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(FlashSaleItem entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.FlashSaleItems
            SET SalePrice = @SalePrice, SaleQuantity = @SaleQuantity, SoldQuantity = @SoldQuantity,
                LimitPerUser = @LimitPerUser, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND RowVersion = @RowVersion;
            """, entity, cancellationToken);

    public Task<int> DeleteAsync(Guid flashSaleItemId, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM dbo.FlashSaleItems WHERE Id = @Id;", new { Id = flashSaleItemId }, cancellationToken);
}
