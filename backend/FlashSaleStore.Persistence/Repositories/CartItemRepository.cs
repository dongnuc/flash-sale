using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Cart;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class CartItemRepository : SqlRepositoryBase, ICartItemRepository
{
    public CartItemRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<CartItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<CartItem>("""
            SELECT Id, CartId, ProductId, Quantity, CreatedAt, UpdatedAt
            FROM dbo.CartItems
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public async Task<IReadOnlyList<CartItem>> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken = default) =>
        await QueryAsync<CartItem>("""
            SELECT Id, CartId, ProductId, Quantity, CreatedAt, UpdatedAt
            FROM dbo.CartItems
            WHERE CartId = @CartId;
            """, new { CartId = cartId }, cancellationToken);

    public Task<CartItem?> GetByCartAndProductAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<CartItem>("""
            SELECT Id, CartId, ProductId, Quantity, CreatedAt, UpdatedAt
            FROM dbo.CartItems
            WHERE CartId = @CartId AND ProductId = @ProductId;
            """, new { CartId = cartId, ProductId = productId }, cancellationToken);

    public Task<int> InsertAsync(CartItem entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.CartItems (Id, CartId, ProductId, Quantity, CreatedAt, UpdatedAt)
            VALUES (@Id, @CartId, @ProductId, @Quantity, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(CartItem entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.CartItems SET Quantity = @Quantity, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, entity, cancellationToken);

    public Task<int> DeleteAsync(Guid cartItemId, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM dbo.CartItems WHERE Id = @Id;", new { Id = cartItemId }, cancellationToken);
}
