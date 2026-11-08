using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Orders;

/// <summary>Maps to dbo.OrderDetails and keeps the product name and price snapshot.</summary>
public sealed class OrderDetail : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid? FlashSaleItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    public static OrderDetail CreateSnapshot(
        Guid orderId,
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice,
        Guid? flashSaleItemId)
    {
        if (orderId == Guid.Empty || productId == Guid.Empty)
        {
            throw new DomainRuleViolationException("OrderDetail.Reference.Required", "Đơn hàng và sản phẩm phải được xác định.");
        }

        var normalizedProductName = productName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedProductName) || normalizedProductName.Length > 200)
        {
            throw new DomainRuleViolationException("OrderDetail.ProductName.Invalid", "Tên sản phẩm trong snapshot phải có từ 1 đến 200 ký tự.");
        }

        if (quantity <= 0 || unitPrice <= 0)
        {
            throw new DomainRuleViolationException("OrderDetail.Values.Invalid", "Số lượng và đơn giá của dòng đơn phải lớn hơn 0.");
        }

        decimal lineTotal;
        try
        {
            lineTotal = checked(quantity * unitPrice);
        }
        catch (OverflowException)
        {
            throw new DomainRuleViolationException("OrderDetail.LineTotal.Overflow", "Thành tiền của dòng đơn vượt giới hạn cho phép.");
        }

        return new OrderDetail
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = normalizedProductName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = lineTotal,
            FlashSaleItemId = flashSaleItemId
        };
    }
}
