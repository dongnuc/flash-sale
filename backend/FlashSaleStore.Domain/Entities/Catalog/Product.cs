using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Catalog;

/// <summary>Maps to dbo.Products.</summary>
public sealed class Product : BaseEntity
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public string NormalizedName => Name.Trim().ToUpperInvariant();

    public static Product Create(Guid categoryId, string name, string? description,
        decimal regularPrice, int totalStockQuantity, bool isFeatured = false)
    {
        var product = new Product();
        product.UpdateDetails(categoryId, name, description);
        product.ChangePrice(regularPrice);
        product.SetStock(totalStockQuantity, 0);
        product.IsFeatured = isFeatured;
        product.MarkUpdated(product.CreatedAt);
        return product;
    }

    public void UpdateDetails(Guid categoryId, string name, string? description)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainRuleViolationException("Product.CategoryId.Required", "Danh mục sản phẩm không hợp lệ.");
        }

        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainRuleViolationException("Product.Name.Required", "Tên sản phẩm không được để trống.");
        }

        if (normalizedName.Length > 200)
        {
            throw new DomainRuleViolationException("Product.Name.MaxLength", "Tên sản phẩm không được vượt quá 200 ký tự.");
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 1000)
        {
            throw new DomainRuleViolationException("Product.Description.MaxLength", "Mô tả sản phẩm không được vượt quá 1000 ký tự.");
        }

        CategoryId = categoryId;
        Name = normalizedName;
        Description = normalizedDescription;
        MarkUpdated();
    }

    public void ChangePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new DomainRuleViolationException("Product.Price.Invalid", "Giá sản phẩm phải lớn hơn 0.");
        }

        if (Price == price) return;

        Price = price;
        MarkUpdated();
    }

    /// <summary>Sets physical stock while preserving units held by pending orders.</summary>
    public void SetStock(int physicalQuantity, int quantityHeldByPendingOrders)
    {
        if (physicalQuantity < 0 || quantityHeldByPendingOrders < 0)
        {
            throw new DomainRuleViolationException("Product.Stock.Invalid", "Tồn kho và số lượng đang được giữ không được âm.");
        }

        if (physicalQuantity < quantityHeldByPendingOrders)
        {
            throw new DomainRuleViolationException("Product.Stock.BelowReserved", "Không thể giảm tồn kho thấp hơn số lượng đang được đơn chờ xử lý giữ.", DomainFailureKind.Conflict);
        }

        StockQuantity = physicalQuantity - quantityHeldByPendingOrders;
        MarkUpdated();
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Product.Stock.InvalidReservation", "Số lượng giữ phải lớn hơn 0.");
        }

        if (StockQuantity < quantity)
        {
            throw new DomainRuleViolationException("Product.Stock.Insufficient", "Sản phẩm không còn đủ tồn kho.", DomainFailureKind.Conflict);
        }

        StockQuantity -= quantity;
        MarkUpdated();
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Product.Stock.InvalidRelease", "Số lượng hoàn kho phải lớn hơn 0.");
        }

        if (StockQuantity > int.MaxValue - quantity)
        {
            throw new DomainRuleViolationException("Product.Stock.Overflow", "Số lượng tồn kho vượt giới hạn cho phép.");
        }

        StockQuantity += quantity;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;

        IsActive = isActive;
        MarkUpdated();
    }

    public void SetFeatured(bool isFeatured)
    {
        if (IsFeatured == isFeatured) return;

        IsFeatured = isFeatured;
        MarkUpdated();
    }
}
