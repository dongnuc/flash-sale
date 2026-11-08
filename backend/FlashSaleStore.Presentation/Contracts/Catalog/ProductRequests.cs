namespace FlashSaleStore.Presentation.Contracts.Catalog;

public sealed record CreateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal RegularPrice,
    int Quantity,
    bool IsFeatured,
    string Status);

public sealed record UpdateProductRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal RegularPrice,
    int Quantity,
    bool IsFeatured,
    string Status);
