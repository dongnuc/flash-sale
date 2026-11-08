using System.Globalization;
using System.Text;
using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Persistence.Repositories;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Queries;

internal sealed class CategoryQueries : SqlRepositoryBase, ICategoryQueries
{
    public CategoryQueries(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<CategoryRow>("""
            SELECT Id, Name
            FROM dbo.Categories
            WHERE IsActive = 1
            ORDER BY Name, Id;
            """, null, cancellationToken);
        return rows.Select(row => new CategoryDto { Id = row.Id, Name = row.Name, Slug = CreateSlug(row.Name) }).ToArray();
    }

    public async Task<PagedResult<AdminCategoryDto>> GetAdminPageAsync(CategoryAdminFilter filter, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, Description, IsActive
            FROM dbo.Categories
            WHERE (@Search IS NULL OR Name LIKE N'%' + @Search + N'%')
            """;
        var page = await QueryPagedAsync<CategoryRow>(sql, "Name ASC, Id ASC",
            new { Search = NormalizeSearch(filter.Search) }, filter.PageIndex, filter.PageSize, cancellationToken);
        var items = page.Items.Select(MapAdminCategory).ToList();
        return new PagedResult<AdminCategoryDto>(items, page.PageIndex, page.PageSize, page.TotalCount);
    }

    public async Task<AdminCategoryDto?> GetAdminByIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<CategoryRow>("""
            SELECT Id, Name, Description, IsActive
            FROM dbo.Categories
            WHERE Id = @CategoryId;
            """, new { CategoryId = categoryId }, cancellationToken);
        return row is null ? null : MapAdminCategory(row);
    }

    private static AdminCategoryDto MapAdminCategory(CategoryRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Slug = CreateSlug(row.Name),
        Description = row.Description,
        Status = row.IsActive ? "ACTIVE" : "INACTIVE"
    };

    private static string? NormalizeSearch(string? search) => string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private static string CreateSlug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }

        return builder.ToString().Trim('-').Normalize(NormalizationForm.FormC);
    }

    private sealed class CategoryRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
