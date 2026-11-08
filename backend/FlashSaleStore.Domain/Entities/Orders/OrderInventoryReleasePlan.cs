namespace FlashSaleStore.Domain.Entities.Orders;

public sealed record ProductStockRelease(Guid ProductId, int Quantity);

public sealed record FlashSaleQuotaRelease(Guid FlashSaleItemId, int Quantity);

public sealed record OrderInventoryReleasePlan(
    IReadOnlyList<ProductStockRelease> ProductStock,
    IReadOnlyList<FlashSaleQuotaRelease> FlashSaleQuota);
