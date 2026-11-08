using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Cart;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class CartRepository : SqlRepositoryBase, ICartRepository
{
    public CartRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Cart>("""
            SELECT Id, UserId, CreatedAt, UpdatedAt
            FROM dbo.Carts
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Cart>("""
            SELECT Id, UserId, CreatedAt, UpdatedAt
            FROM dbo.Carts
            WHERE UserId = @UserId;
            """, new { UserId = userId }, cancellationToken);

    public Task<int> InsertAsync(Cart entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.Carts (Id, UserId, CreatedAt, UpdatedAt)
            VALUES (@Id, @UserId, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(Cart entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Carts SET UserId = @UserId, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, entity, cancellationToken);
}
