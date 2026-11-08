using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class ProductRepository : SqlRepositoryBase, IProductRepository
{
    public ProductRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Product>("""
            SELECT Id, CategoryId, Name, Description, Price, StockQuantity, IsActive, IsFeatured,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.Products
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public Task<Product?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Product>("""
            SELECT Id, CategoryId, Name, Description, Price, StockQuantity, IsActive, IsFeatured,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.Products
            WHERE UPPER(LTRIM(RTRIM(Name))) = @NormalizedName;
            """, new { NormalizedName = normalizedName }, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<Product>();
        return await QueryAsync<Product>("""
            SELECT Id, CategoryId, Name, Description, Price, StockQuantity, IsActive, IsFeatured,
                   CreatedAt, UpdatedAt, RowVersion
            FROM dbo.Products
            WHERE Id IN @Ids;
            """, new { Ids = ids }, cancellationToken);
    }

    public Task<int> GetQuantityHeldByPendingOrdersAsync(Guid productId, CancellationToken cancellationToken = default) =>
        ExecuteScalarAsync<int>("""
            SELECT COALESCE(SUM(od.Quantity), 0)
            FROM dbo.OrderDetails AS od
            INNER JOIN dbo.Orders AS o ON o.Id = od.OrderId
            WHERE od.ProductId = @ProductId AND o.Status = 0;
            """, new { ProductId = productId }, cancellationToken);

    public Task<int> InsertAsync(Product entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.Products
                (Id, CategoryId, Name, Description, Price, StockQuantity, IsActive, IsFeatured, CreatedAt, UpdatedAt)
            VALUES
                (@Id, @CategoryId, @Name, @Description, @Price, @StockQuantity, @IsActive, @IsFeatured, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(Product entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Products
            SET CategoryId = @CategoryId, Name = @Name, Description = @Description, Price = @Price,
                StockQuantity = @StockQuantity, IsActive = @IsActive, IsFeatured = @IsFeatured, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND RowVersion = @RowVersion;
            """, entity, cancellationToken);
}
