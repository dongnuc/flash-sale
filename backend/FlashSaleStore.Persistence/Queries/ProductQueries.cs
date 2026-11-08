using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Catalog;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Persistence.Repositories;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Queries;

internal sealed class ProductQueries : SqlRepositoryBase, IProductQueries
{
    public ProductQueries(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public async Task<PagedResult<ProductCardDto>> GetBestSellingAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
        MapPage(await QueryPagedAsync<ProductRow>(CustomerProductsSql, "SoldQuantity DESC, CreatedAt DESC, Id", null, pageIndex, pageSize, cancellationToken));

    public async Task<PagedResult<ProductCardDto>> GetNewestAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
        MapPage(await QueryPagedAsync<ProductRow>(CustomerProductsSql, "CreatedAt DESC, Id DESC", null, pageIndex, pageSize, cancellationToken));

    public async Task<PagedResult<ProductCardDto>> GetFeaturedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default) =>
        MapPage(await QueryPagedAsync<ProductRow>(CustomerProductsSql + " AND p.IsFeatured = 1", "CreatedAt DESC, Id DESC", null, pageIndex, pageSize, cancellationToken));

    public async Task<IReadOnlyList<ProductCardDto>> GetTopFlashSaleAsync(CancellationToken cancellationToken = default)
    {
        const string sql = CustomerProductsSql + " AND sale.FlashSaleId IS NOT NULL ORDER BY DiscountPercent DESC, p.Id OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY";
        var rows = await QueryAsync<ProductRow>(sql,
            null, cancellationToken);
        return rows.Select(MapCard).ToArray();
    }

    public async Task<PagedResult<ProductCardDto>> GetFlashSalePageAsync(FlashSaleProductFilter filter, CancellationToken cancellationToken = default)
    {
        const string sql = CustomerProductsSql + " AND sale.FlashSaleId IS NOT NULL AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)";
        var orderBy = filter.SortDiscountAscending ? "DiscountPercent ASC, Id" : "DiscountPercent DESC, Id";
        var page = await QueryPagedAsync<ProductRow>(sql, orderBy, new { filter.CategoryId },
            filter.PageIndex, filter.PageSize, cancellationToken);
        return MapPage(page);
    }

    public async Task<PagedResult<ProductCardDto>> GetCatalogPageAsync(ProductCatalogFilter filter, CancellationToken cancellationToken = default)
    {
        const string sql = CustomerProductsSql + " AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)";
        var page = await QueryPagedAsync<ProductRow>(sql, "SoldQuantity DESC, CreatedAt DESC, Id",
            new { filter.CategoryId }, filter.PageIndex, filter.PageSize, cancellationToken);
        return MapPage(page);
    }

    public async Task<ProductDetailDto?> GetDetailAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<ProductRow>(CustomerProductsSql + " AND p.Id = @ProductId",
            new { ProductId = productId }, cancellationToken);
        if (row is null) return null;

        var card = MapCard(row);
        return new ProductDetailDto
        {
            Id = card.Id,
            Name = card.Name,
            ThumbnailUrl = card.ThumbnailUrl,
            Category = card.Category,
            RegularPrice = card.RegularPrice,
            SellingPrice = card.SellingPrice,
            IsFlashSale = card.IsFlashSale,
            FlashSaleId = card.FlashSaleId,
            DiscountPercent = card.DiscountPercent,
            ProductQuantity = card.ProductQuantity,
            FlashSaleQuantityRemaining = card.FlashSaleQuantityRemaining,
            SoldQuantity = card.SoldQuantity,
            Description = row.Description,
            Images = Array.Empty<string>(),
            Attributes = Array.Empty<ProductAttributeDto>()
        };
    }

    public async Task<AdminProductDto?> GetAdminByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<AdminProductRow>(AdminProductSelectSql + " WHERE p.Id = @ProductId;",
            new { ProductId = productId }, cancellationToken);
        return row is null ? null : MapAdminProduct(row);
    }

    public async Task<PagedResult<AdminProductDto>> GetAdminPageAsync(ProductAdminFilter filter, CancellationToken cancellationToken = default)
    {
        const string sql = AdminProductSelectSql + """
            WHERE (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
              AND (@IsActive IS NULL OR p.IsActive = @IsActive)
              AND (@Search IS NULL OR p.Name LIKE N'%' + @Search + N'%')
              AND (@MinPrice IS NULL OR COALESCE(sale.SalePrice, p.Price) >= @MinPrice)
              AND (@MaxPrice IS NULL OR COALESCE(sale.SalePrice, p.Price) <= @MaxPrice)
            """;

        var parameters = new
        {
            filter.CategoryId,
            filter.IsActive,
            Search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            filter.MinPrice,
            filter.MaxPrice
        };
        var page = await QueryPagedAsync<AdminProductRow>(sql, "CreatedAt DESC, Id DESC", parameters,
            filter.PageIndex, filter.PageSize, cancellationToken);
        var items = page.Items.Select(MapAdminProduct).ToList();
        return new PagedResult<AdminProductDto>(items, page.PageIndex, page.PageSize, page.TotalCount);
    }

    private static PagedResult<ProductCardDto> MapPage(PagedResult<ProductRow> page) =>
        new(page.Items.Select(MapCard).ToList(), page.PageIndex, page.PageSize, page.TotalCount);

    private static ProductCardDto MapCard(ProductRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        ThumbnailUrl = row.ThumbnailUrl,
        Category = new CategorySummaryDto { Id = row.CategoryId, Name = row.CategoryName },
        RegularPrice = row.RegularPrice,
        SellingPrice = row.SellingPrice,
        IsFlashSale = row.FlashSaleId.HasValue,
        FlashSaleId = row.FlashSaleId,
        DiscountPercent = row.DiscountPercent,
        ProductQuantity = row.ProductQuantity,
        FlashSaleQuantityRemaining = row.FlashSaleQuantityRemaining,
        SoldQuantity = row.SoldQuantity
    };

    private static AdminProductDto MapAdminProduct(AdminProductRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Description = row.Description,
        CategoryId = row.CategoryId,
        CategoryName = row.CategoryName,
        RegularPrice = row.RegularPrice,
        SellingPrice = row.SellingPrice,
        DiscountPercent = row.DiscountPercent,
        Quantity = row.Quantity,
        IsFeatured = row.IsFeatured,
        Status = row.IsActive ? "ACTIVE" : "INACTIVE",
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt
    };

    private const string AdminProductSelectSql = """
        SELECT p.Id, p.Name, p.Description, p.CategoryId, c.Name AS CategoryName,
               p.Price AS RegularPrice,
               COALESCE(sale.SalePrice, p.Price) AS SellingPrice,
               CAST(CASE WHEN sale.FlashSaleId IS NULL THEN 0
                    ELSE (p.Price - sale.SalePrice) / NULLIF(p.Price, 0) * 100 END AS decimal(9,2)) AS DiscountPercent,
               p.StockQuantity AS Quantity, p.IsFeatured, p.IsActive, p.CreatedAt, p.UpdatedAt
        FROM dbo.Products AS p
        INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId
        OUTER APPLY
        (
            SELECT TOP (1) fsi.FlashSaleId, fsi.SalePrice
            FROM dbo.FlashSaleItems AS fsi
            INNER JOIN dbo.FlashSales AS fs ON fs.Id = fsi.FlashSaleId
            WHERE fsi.ProductId = p.Id AND fs.Status = 2
              AND fs.StartsAt <= SYSUTCDATETIME() AND SYSUTCDATETIME() < fs.EndsAt
              AND fsi.SoldQuantity < fsi.SaleQuantity
            ORDER BY (p.Price - fsi.SalePrice) / NULLIF(p.Price, 0) DESC, fs.StartsAt DESC, fsi.Id
        ) AS sale
        """;

    private const string CustomerProductsSql = """
        SELECT p.Id, p.Name, CAST(NULL AS nvarchar(2048)) AS ThumbnailUrl, p.Description,
               p.CategoryId, c.Name AS CategoryName, p.Price AS RegularPrice,
               COALESCE(sale.SalePrice, p.Price) AS SellingPrice,
               sale.FlashSaleId,
               CAST(CASE WHEN sale.FlashSaleId IS NULL THEN 0
                    ELSE (p.Price - sale.SalePrice) / NULLIF(p.Price, 0) * 100 END AS decimal(9,2)) AS DiscountPercent,
               p.StockQuantity AS ProductQuantity,
               COALESCE(sale.RemainingQuantity, 0) AS FlashSaleQuantityRemaining,
               COALESCE(sold.SoldQuantity, 0) AS SoldQuantity,
               p.CreatedAt
        FROM dbo.Products AS p
        INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId AND c.IsActive = 1
        OUTER APPLY
        (
            SELECT TOP (1) fsi.FlashSaleId, fsi.SalePrice,
                   fsi.SaleQuantity - fsi.SoldQuantity AS RemainingQuantity
            FROM dbo.FlashSaleItems AS fsi
            INNER JOIN dbo.FlashSales AS fs ON fs.Id = fsi.FlashSaleId
            WHERE fsi.ProductId = p.Id AND fs.Status = 2
              AND fs.StartsAt <= SYSUTCDATETIME() AND SYSUTCDATETIME() < fs.EndsAt
              AND fsi.SoldQuantity < fsi.SaleQuantity
            ORDER BY (p.Price - fsi.SalePrice) / NULLIF(p.Price, 0) DESC, fs.StartsAt DESC, fsi.Id
        ) AS sale
        LEFT JOIN
        (
            SELECT od.ProductId, SUM(od.Quantity) AS SoldQuantity
            FROM dbo.OrderDetails AS od
            INNER JOIN dbo.Orders AS o ON o.Id = od.OrderId
            WHERE o.Status IN (1, 2, 3)
            GROUP BY od.ProductId
        ) AS sold ON sold.ProductId = p.Id
        WHERE p.IsActive = 1
        """;

    private sealed class ProductRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal RegularPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public Guid? FlashSaleId { get; set; }
        public decimal DiscountPercent { get; set; }
        public int ProductQuantity { get; set; }
        public int FlashSaleQuantityRemaining { get; set; }
        public int SoldQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class AdminProductRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal RegularPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public int Quantity { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
