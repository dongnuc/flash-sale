namespace FlashSaleStore.Presentation.Contracts.FlashSales;

public sealed record FlashSaleItemRequest(Guid ProductId, decimal FinalPrice, int FlashSaleQuantity, int? LimitPerUser);
public sealed record CreateFlashSaleRequest(string Name, DateTime StartsAt, DateTime EndsAt, IReadOnlyList<FlashSaleItemRequest> Items);
public sealed record UpdateFlashSaleRequest(string Name, DateTime StartsAt, DateTime EndsAt, IReadOnlyList<FlashSaleItemRequest> Items);
public sealed record SetFlashSaleStartTimeRequest(DateTime StartsAt);
public sealed record FlashSaleBatchStatusRequest(Guid[] Ids, string Status);
