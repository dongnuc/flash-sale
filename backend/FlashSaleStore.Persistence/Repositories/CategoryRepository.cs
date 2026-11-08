using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class CategoryRepository : SqlRepositoryBase, ICategoryRepository
{
    public CategoryRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Category>("""
            SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Categories
            WHERE Id = @Id;
            """, new { Id = id }, cancellationToken);

    public Task<Category?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<Category>("""
            SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Categories
            WHERE UPPER(LTRIM(RTRIM(Name))) = @NormalizedName;
            """, new { NormalizedName = normalizedName }, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<Category>();
        return await QueryAsync<Category>("""
            SELECT Id, Name, Description, IsActive, CreatedAt, UpdatedAt
            FROM dbo.Categories
            WHERE Id IN @Ids;
            """, new { Ids = ids }, cancellationToken);
    }

    public Task<int> InsertAsync(Category entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.Categories (Id, Name, Description, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Description, @IsActive, @CreatedAt, @UpdatedAt);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(Category entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.Categories
            SET Name = @Name, Description = @Description, IsActive = @IsActive, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """, entity, cancellationToken);
}
