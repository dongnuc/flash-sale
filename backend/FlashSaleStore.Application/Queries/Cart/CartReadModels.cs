namespace FlashSaleStore.Application.Queries.Cart;

public sealed class CartDto
{
    public Guid CartId { get; set; }
    public IReadOnlyList<CartLineDto> Items { get; set; } = Array.Empty<CartLineDto>();
    public int TotalQuantity { get; set; }
    public decimal EstimatedTotal { get; set; }
}

public sealed class CartLineDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public IReadOnlyList<CartPriceBreakdownDto> PriceBreakdown { get; set; } = Array.Empty<CartPriceBreakdownDto>();
    public decimal EstimatedLineTotal { get; set; }
}

public sealed class CartPriceBreakdownDto
{
    public string PriceType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
