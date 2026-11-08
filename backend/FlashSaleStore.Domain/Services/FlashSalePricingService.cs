using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Services;

public sealed record OrderLineAllocation(
    Guid ProductId,
    Guid? FlashSaleItemId,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>Allocates order quantity between remaining FlashSale quota and regular inventory.</summary>
public sealed class FlashSalePricingService
{
    public IReadOnlyList<OrderLineAllocation> Allocate(
        Product product,
        FlashSale? flashSale,
        FlashSaleItem? flashSaleItem,
        int requestedQuantity,
        DateTime utcNow)
    {
        if (product is null)
        {
            throw new DomainRuleViolationException("Product.Required", "Sản phẩm cần được xác định.");
        }

        if (requestedQuantity <= 0)
        {
            throw new DomainRuleViolationException("Order.Quantity.Invalid", "Số lượng đặt phải lớn hơn 0.");
        }

        if (!product.IsActive)
        {
            throw new DomainRuleViolationException("Product.Inactive", "Sản phẩm hiện không được phép bán.", DomainFailureKind.Conflict);
        }

        if (product.StockQuantity < requestedQuantity)
        {
            throw new DomainRuleViolationException("Product.Stock.Insufficient", "Tổng tồn kho sản phẩm không đủ.", DomainFailureKind.Conflict);
        }

        var isSaleApplicable = flashSale is not null &&
                               flashSaleItem is not null &&
                               flashSaleItem.FlashSaleId == flashSale.Id &&
                               flashSaleItem.ProductId == product.Id &&
                               flashSale.IsEffectiveAt(utcNow);

        if (!isSaleApplicable || flashSaleItem!.RemainingQuantity == 0)
        {
            return Array.AsReadOnly(new[]
            {
                new OrderLineAllocation(product.Id, null, requestedQuantity, product.Price)
            });
        }

        flashSaleItem.CalculateDiscountPercent(product.Price);
        var saleQuantity = Math.Min(requestedQuantity, flashSaleItem.RemainingQuantity);
        var regularQuantity = requestedQuantity - saleQuantity;
        var allocations = new List<OrderLineAllocation>(regularQuantity > 0 ? 2 : 1)
        {
            new(product.Id, flashSaleItem.Id, saleQuantity, flashSaleItem.SalePrice)
        };

        if (regularQuantity > 0)
        {
            allocations.Add(new OrderLineAllocation(product.Id, null, regularQuantity, product.Price));
        }

        return allocations.AsReadOnly();
    }
}
