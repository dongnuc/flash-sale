namespace FlashSaleStore.Application.Queries.Orders;

public class OrderSummaryDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
}

public class OrderDetailDto : OrderSummaryDto
{
    public IReadOnlyList<OrderLineDto> Items { get; set; } = Array.Empty<OrderLineDto>();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public string? Note { get; set; }
    public DeliveryAddressDto DeliveryAddress { get; set; } = new();
}

public sealed class StoreOrderDetailDto : OrderDetailDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal? PaymentAmount { get; set; }
}

public sealed class OrderLineDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PriceType { get; set; } = string.Empty;
    public Guid? FlashSaleId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class DeliveryAddressDto
{
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string AddressLine { get; set; } = string.Empty;
}
