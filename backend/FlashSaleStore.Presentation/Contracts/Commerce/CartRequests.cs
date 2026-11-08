namespace FlashSaleStore.Presentation.Contracts.Commerce;

public sealed record CartItemRequest(Guid ProductId, int Quantity);
public sealed record SetCartItemQuantityRequest(int Quantity);
