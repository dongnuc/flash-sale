namespace FlashSaleStore.Application.Queries.Catalog;

public sealed class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public sealed class AdminCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class CategorySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public CategorySummaryDto Category { get; set; } = new();
    public decimal RegularPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsFlashSale { get; set; }
    public Guid? FlashSaleId { get; set; }
    public decimal DiscountPercent { get; set; }
    public int ProductQuantity { get; set; }
    public int FlashSaleQuantityRemaining { get; set; }
    public int SoldQuantity { get; set; }
}

public sealed class ProductDetailDto : ProductCardDto
{
    public string? Description { get; set; }
    public IReadOnlyList<string> Images { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ProductAttributeDto> Attributes { get; set; } = Array.Empty<ProductAttributeDto>();
}

public sealed class ProductAttributeDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class AdminProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal RegularPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int Quantity { get; set; }
    public bool IsFeatured { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
