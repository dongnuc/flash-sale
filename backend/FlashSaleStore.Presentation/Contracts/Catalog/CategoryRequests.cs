namespace FlashSaleStore.Presentation.Contracts.Catalog;

public sealed record CreateCategoryRequest(string Name, string? Description);

public sealed record UpdateCategoryRequest(string Name, string? Description);

public sealed record BatchStatusRequest(Guid[] Ids, string Status);
