namespace FlashSaleStore.Domain.Enums;

/// <summary>Values map to dbo.Orders.Status (tinyint).</summary>
public enum OrderStatus : byte
{
    Pending = 0,
    Paid = 1,
    Shipping = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5
}
