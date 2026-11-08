namespace FlashSaleStore.Domain.Enums;

/// <summary>Values map to dbo.FlashSales.Status (tinyint).</summary>
public enum FlashSaleStatus : byte
{
    Draft = 0,
    Scheduled = 1,
    Active = 2,
    Ended = 3,
    Cancelled = 4
}
