using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.FlashSales;

/// <summary>Maps to dbo.FlashSaleItems.</summary>
public sealed class FlashSaleItem : BaseEntity
{
    public Guid FlashSaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal SalePrice { get; private set; }
    public int SaleQuantity { get; private set; }
    public int SoldQuantity { get; private set; }
    public int? LimitPerUser { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public int RemainingQuantity => Math.Max(0, SaleQuantity - SoldQuantity);

    public static FlashSaleItem Create(Guid flashSaleId, Guid productId, decimal finalPrice,
        decimal regularPrice, int saleQuantity, int totalProductQuantity, int? limitPerUser)
    {
        if (flashSaleId == Guid.Empty || productId == Guid.Empty)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Reference.Required", "FlashSale và sản phẩm phải được xác định.");
        }

        var item = new FlashSaleItem { FlashSaleId = flashSaleId, ProductId = productId };
        item.UpdateOffer(finalPrice, regularPrice, saleQuantity, totalProductQuantity, limitPerUser);
        item.MarkUpdated(item.CreatedAt);
        return item;
    }

    public void UpdateOffer(decimal finalPrice, decimal regularPrice, int saleQuantity,
        int totalProductQuantity, int? limitPerUser)
    {
        ValidateOffer(finalPrice, regularPrice, saleQuantity, totalProductQuantity, limitPerUser);

        if (saleQuantity < SoldQuantity)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Quantity.BelowReserved", "Không thể giảm quota thấp hơn số lượng đã được giữ hoặc bán.", DomainFailureKind.Conflict);
        }

        SalePrice = finalPrice;
        SaleQuantity = saleQuantity;
        LimitPerUser = limitPerUser;
        MarkUpdated();
    }

    public decimal CalculateDiscountPercent(decimal regularPrice)
    {
        if (regularPrice <= 0)
        {
            throw new DomainRuleViolationException("Product.Price.Invalid", "Giá thường phải lớn hơn 0 để tính phần trăm giảm.");
        }

        if (SalePrice > regularPrice)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Price.ExceedsRegularPrice", "Giá FlashSale không được cao hơn giá thường.");
        }

        return (regularPrice - SalePrice) / regularPrice * 100m;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Reservation.Invalid", "Số lượng giữ trong FlashSale phải lớn hơn 0.");
        }

        if (quantity > RemainingQuantity)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Quota.Insufficient", "Quota FlashSale còn lại không đủ.", DomainFailureKind.Conflict);
        }

        SoldQuantity += quantity;
        MarkUpdated();
    }

    public void EnsureUserLimit(int requestedQuantity, int quantityAlreadyPurchasedOrReserved)
    {
        if (requestedQuantity <= 0 || quantityAlreadyPurchasedOrReserved < 0)
        {
            throw new DomainRuleViolationException("FlashSaleItem.UserLimit.QuantityInvalid", "Số lượng mua hoặc số lượng đã mua không hợp lệ.");
        }

        if (LimitPerUser is not null &&
            (long)requestedQuantity + quantityAlreadyPurchasedOrReserved > LimitPerUser.Value)
        {
            throw new DomainRuleViolationException("FlashSaleItem.UserLimit.Exceeded", "Số lượng vượt giới hạn mua FlashSale của khách hàng.", DomainFailureKind.Conflict);
        }
    }

    public void Release(int quantity)
    {
        if (quantity <= 0 || quantity > SoldQuantity)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Reservation.InvalidRelease", "Số lượng hoàn quota không hợp lệ.");
        }

        SoldQuantity -= quantity;
        MarkUpdated();
    }

    private static void ValidateOffer(decimal finalPrice, decimal regularPrice, int saleQuantity,
        int totalProductQuantity, int? limitPerUser)
    {
        if (finalPrice <= 0)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Price.Invalid", "Giá bán cuối cùng phải lớn hơn 0.");
        }

        if (regularPrice <= 0 || finalPrice >= regularPrice)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Price.InvalidDiscount", "Giá FlashSale phải thấp hơn giá thường.");
        }

        if (saleQuantity <= 0)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Quantity.Invalid", "Quota FlashSale phải lớn hơn 0.");
        }

        if (totalProductQuantity <= 0 || saleQuantity >= totalProductQuantity)
        {
            throw new DomainRuleViolationException("FlashSaleItem.Quantity.MustBeBelowProductStock", "Quota FlashSale ban đầu phải nhỏ hơn tổng tồn kho sản phẩm.");
        }

        if (limitPerUser is <= 0)
        {
            throw new DomainRuleViolationException("FlashSaleItem.LimitPerUser.Invalid", "Giới hạn mua mỗi khách phải lớn hơn 0 hoặc để trống.");
        }
    }
}
