using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Cart;

/// <summary>Maps to dbo.CartItems.</summary>
public sealed class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    public static CartItem Create(Guid cartId, Guid productId, int quantity, int availableStock)
    {
        if (cartId == Guid.Empty || productId == Guid.Empty)
        {
            throw new DomainRuleViolationException("CartItem.Reference.Required", "Giỏ hàng và sản phẩm phải được xác định.");
        }

        ValidateQuantity(quantity, availableStock);
        return new CartItem { CartId = cartId, ProductId = productId, Quantity = quantity };
    }

    public void ChangeQuantity(int quantity, int availableStock)
    {
        ValidateQuantity(quantity, availableStock);
        Quantity = quantity;
        MarkUpdated();
    }

    private static void ValidateQuantity(int quantity, int availableStock)
    {
        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("CartItem.Quantity.Invalid", "Số lượng sản phẩm phải lớn hơn 0.");
        }

        if (availableStock < 0 || quantity > availableStock)
        {
            throw new DomainRuleViolationException("CartItem.Stock.Insufficient", "Tồn kho khả dụng không đủ cho số lượng trong giỏ.", DomainFailureKind.Conflict);
        }
    }
}
