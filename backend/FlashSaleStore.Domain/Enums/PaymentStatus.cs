namespace FlashSaleStore.Domain.Enums;

/// <summary>Values map to dbo.Payments.Status (tinyint).</summary>
public enum PaymentStatus : byte
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Expired = 3,
    Cancelled = 4
}
