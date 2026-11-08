using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.FlashSales;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Persistence.Repositories;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Queries;

internal sealed class FlashSaleQueries : SqlRepositoryBase, IFlashSaleQueries
{
    public FlashSaleQueries(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public async Task<PagedResult<FlashSaleDto>> GetAdminPageAsync(FlashSaleAdminFilter filter, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, StartsAt, EndsAt, Status
            FROM dbo.FlashSales
            WHERE (@DateFrom IS NULL OR StartsAt >= @DateFrom)
              AND (@DateTo IS NULL OR StartsAt <= @DateTo)
              AND (@Search IS NULL OR Name LIKE N'%' + @Search + N'%')
              AND (@Status IS NULL OR Status = @Status)
            """;
        var parameters = new
        {
            filter.DateFrom,
            filter.DateTo,
            Search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            filter.Status
        };
        var page = await QueryPagedAsync<FlashSaleRow>(sql, "StartsAt DESC, Id DESC", parameters,
            filter.PageIndex, filter.PageSize, cancellationToken);
        var items = await AddItemsAsync(page.Items, cancellationToken);
        return new PagedResult<FlashSaleDto>(items, page.PageIndex, page.PageSize, page.TotalCount);
    }

    public async Task<FlashSaleDto?> GetByIdAsync(Guid flashSaleId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<FlashSaleRow>("""
            SELECT Id, Name, StartsAt, EndsAt, Status
            FROM dbo.FlashSales
            WHERE Id = @Id;
            """, new { Id = flashSaleId }, cancellationToken);
        if (row is null) return null;

        return (await AddItemsAsync([row], cancellationToken))[0];
    }

    private async Task<List<FlashSaleDto>> AddItemsAsync(IReadOnlyCollection<FlashSaleRow> sales, CancellationToken cancellationToken)
    {
        if (sales.Count == 0) return [];
        var saleIds = sales.Select(sale => sale.Id).ToArray();
        var itemRows = await QueryAsync<FlashSaleItemRow>("""
            SELECT fsi.FlashSaleId, fsi.ProductId, p.Name AS ProductName, p.Price AS RegularPrice,
                   fsi.SalePrice AS FinalPrice,
                   CAST((p.Price - fsi.SalePrice) / NULLIF(p.Price, 0) * 100 AS decimal(9,2)) AS DiscountPercent,
                   fsi.SaleQuantity AS FlashSaleQuantity,
                   fsi.SaleQuantity - fsi.SoldQuantity AS RemainingQuantity
            FROM dbo.FlashSaleItems AS fsi
            INNER JOIN dbo.Products AS p ON p.Id = fsi.ProductId
            WHERE fsi.FlashSaleId IN @SaleIds
            ORDER BY fsi.FlashSaleId, p.Name, fsi.ProductId;
            """, new { SaleIds = saleIds }, cancellationToken);

        return sales.Select(sale => new FlashSaleDto
        {
            Id = sale.Id,
            Name = sale.Name,
            StartsAt = sale.StartsAt,
            EndsAt = sale.EndsAt,
            Status = ((FlashSaleStatus)sale.Status).ToString(),
            Items = itemRows.Where(item => item.FlashSaleId == sale.Id).Select(item => new FlashSaleItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                RegularPrice = item.RegularPrice,
                FinalPrice = item.FinalPrice,
                DiscountPercent = item.DiscountPercent,
                FlashSaleQuantity = item.FlashSaleQuantity,
                RemainingQuantity = item.RemainingQuantity
            }).ToArray()
        }).ToList();
    }

    private sealed class FlashSaleRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public byte Status { get; set; }
    }

    private sealed class FlashSaleItemRow
    {
        public Guid FlashSaleId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal RegularPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public int FlashSaleQuantity { get; set; }
        public int RemainingQuantity { get; set; }
    }
}
