using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class FlashSaleRepository : SqlRepositoryBase, IFlashSaleRepository
{
    public FlashSaleRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<FlashSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<FlashSale>("""
            SELECT Id, Name, StartsAt, EndsAt, Status, CreatedAt, UpdatedAt
            FROM dbo.FlashSales
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public async Task<IReadOnlyList<FlashSale>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<FlashSale>();
        return await QueryAsync<FlashSale>("""
            SELECT Id, Name, StartsAt, EndsAt, Status, CreatedAt, UpdatedAt
            FROM dbo.FlashSales
            WHERE Id IN @Ids;
            """, new { Ids = ids }, cancellationToken);
    }

    public Task<int> InsertAsync(FlashSale entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.FlashSales (Id, Name, StartsAt, EndsAt, Status, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @StartsAt, @EndsAt, @Status, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(FlashSale entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.FlashSales
            SET Name = @Name, StartsAt = @StartsAt, EndsAt = @EndsAt, Status = @Status, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, entity, cancellationToken);
}
