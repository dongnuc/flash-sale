using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Queries.Cart;
using FlashSaleStore.Persistence.Repositories;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Queries;

internal sealed class CartQueries : SqlRepositoryBase, ICartQueries
{
    public CartQueries(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor) : base(sql, transactionAccessor) { }

    public async Task<CartDto?> GetByUserIdAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<CartRow>("""
            SELECT c.Id AS CartId, ci.ProductId, p.Name AS ProductName, ci.Quantity, p.Price AS RegularPrice,
                   sale.SalePrice, COALESCE(sale.RemainingQuantity, 0) AS FlashSaleQuantityRemaining
            FROM dbo.Carts AS c
            LEFT JOIN dbo.CartItems AS ci ON ci.CartId = c.Id
            LEFT JOIN dbo.Products AS p ON p.Id = ci.ProductId
            OUTER APPLY
            (
                SELECT TOP (1) fsi.SalePrice, fsi.SaleQuantity - fsi.SoldQuantity AS RemainingQuantity
                FROM dbo.FlashSaleItems AS fsi
                INNER JOIN dbo.FlashSales AS fs ON fs.Id = fsi.FlashSaleId
                WHERE fsi.ProductId = p.Id AND fs.Status = 2
                  AND fs.StartsAt <= @UtcNow AND @UtcNow < fs.EndsAt
                  AND fsi.SoldQuantity < fsi.SaleQuantity
                ORDER BY (p.Price - fsi.SalePrice) / NULLIF(p.Price, 0) DESC, fsi.Id
            ) AS sale
            WHERE c.UserId = @UserId
            ORDER BY ci.CreatedAt, ci.Id;
            """, new { UserId = userId, UtcNow = utcNow }, cancellationToken);

        if (rows.Count == 0) return null;

        var cart = new CartDto { CartId = rows[0].CartId };
        var lines = new List<CartLineDto>();
        foreach (var row in rows.Where(row => row.ProductId.HasValue))
        {
            var quantity = row.Quantity;
            var saleQuantity = row.SalePrice.HasValue
                ? Math.Min(quantity, row.FlashSaleQuantityRemaining)
                : 0;
            var regularQuantity = quantity - saleQuantity;
            var breakdown = new List<CartPriceBreakdownDto>(2);

            if (saleQuantity > 0)
            {
                breakdown.Add(CreateBreakdown("FLASH_SALE", saleQuantity, row.SalePrice!.Value));
            }

            if (regularQuantity > 0)
            {
                breakdown.Add(CreateBreakdown("REGULAR", regularQuantity, row.RegularPrice));
            }

            var lineTotal = breakdown.Sum(item => item.LineTotal);
            lines.Add(new CartLineDto
            {
                ProductId = row.ProductId!.Value,
                ProductName = row.ProductName ?? string.Empty,
                Quantity = quantity,
                PriceBreakdown = breakdown,
                EstimatedLineTotal = lineTotal
            });
        }

        cart.Items = lines;
        cart.TotalQuantity = lines.Sum(line => line.Quantity);
        cart.EstimatedTotal = lines.Sum(line => line.EstimatedLineTotal);
        return cart;
    }

    private static CartPriceBreakdownDto CreateBreakdown(string priceType, int quantity, decimal unitPrice) => new()
    {
        PriceType = priceType,
        Quantity = quantity,
        UnitPrice = unitPrice,
        LineTotal = quantity * unitPrice
    };

    private sealed class CartRow
    {
        public Guid CartId { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal RegularPrice { get; set; }
        public decimal? SalePrice { get; set; }
        public int FlashSaleQuantityRemaining { get; set; }
    }
}
