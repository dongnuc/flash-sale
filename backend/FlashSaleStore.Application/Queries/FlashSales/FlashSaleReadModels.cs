namespace FlashSaleStore.Application.Queries.FlashSales;

public sealed class FlashSaleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<FlashSaleItemDto> Items { get; set; } = Array.Empty<FlashSaleItemDto>();
}

public sealed class FlashSaleItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal RegularPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int FlashSaleQuantity { get; set; }
    public int RemainingQuantity { get; set; }
}

public sealed class PaymentIntentDto
{
    public Guid PaymentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? QrCode { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime ExpiresAt { get; set; }
}

public sealed class BatchStatusDto
{
    public int RequestedCount { get; set; }
    public int UpdatedCount { get; set; }
    public IReadOnlyList<Guid> UpdatedIds { get; set; } = Array.Empty<Guid>();
    public string Status { get; set; } = string.Empty;
}
