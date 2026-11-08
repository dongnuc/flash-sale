using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Cart;

/// <summary>Maps to dbo.Carts.</summary>
public sealed class Cart : BaseEntity
{
    private readonly List<CartItem> _items = [];

    public Cart() { }

    private Cart(Guid userId) => UserId = userId;

    public Guid UserId { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public static Cart Create(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleViolationException("Cart.UserId.Required", "Khách hàng của giỏ hàng không hợp lệ.");
        }

        return new Cart(userId);
    }

    public void LoadItems(IEnumerable<CartItem> items)
    {
        if (items is null)
        {
            throw new DomainRuleViolationException("Cart.Items.Invalid", "Danh sách mặt hàng không hợp lệ.");
        }

        var loadedItems = items.ToArray();
        if (loadedItems.Any(item => item is null || item.CartId != Id || item.Quantity <= 0) ||
            loadedItems.Select(item => item.ProductId).Distinct().Count() != loadedItems.Length)
        {
            throw new DomainRuleViolationException("Cart.Items.Invalid", "Các dòng hàng phải thuộc đúng giỏ và không được trùng sản phẩm.");
        }

        _items.Clear();
        _items.AddRange(loadedItems);
    }

    public void AddOrIncreaseItem(Guid requesterId, Product product, int quantity)
    {
        EnsureOwner(requesterId);
        if (product is null)
        {
            throw new DomainRuleViolationException("Cart.Product.Required", "Sản phẩm cần được xác định.");
        }

        if (!product.IsActive)
        {
            throw new DomainRuleViolationException("Cart.Product.Inactive", "Không thể thêm sản phẩm hiện không được phép bán.", DomainFailureKind.Conflict);
        }

        if (quantity <= 0)
        {
            throw new DomainRuleViolationException("Cart.Quantity.Invalid", "Số lượng thêm vào giỏ phải lớn hơn 0.");
        }

        var currentItem = _items.SingleOrDefault(item => item.ProductId == product.Id);
        if (currentItem is not null)
        {
            var requestedTotal = (long)currentItem.Quantity + quantity;
            if (requestedTotal > int.MaxValue)
            {
                throw new DomainRuleViolationException("Cart.Quantity.Overflow", "Số lượng sản phẩm trong giỏ vượt giới hạn cho phép.");
            }

            currentItem.ChangeQuantity((int)requestedTotal, product.StockQuantity);
        }
        else
        {
            _items.Add(CartItem.Create(Id, product.Id, quantity, product.StockQuantity));
        }

        MarkUpdated();
    }

    public void SetItemQuantity(Guid requesterId, Product product, int quantity)
    {
        EnsureOwner(requesterId);
        if (product is null)
        {
            throw new DomainRuleViolationException("Cart.Product.Required", "Sản phẩm cần được xác định.");
        }

        if (!product.IsActive)
        {
            throw new DomainRuleViolationException("Cart.Product.Inactive", "Sản phẩm hiện không được phép bán.", DomainFailureKind.Conflict);
        }

        var item = _items.SingleOrDefault(cartItem => cartItem.ProductId == product.Id);
        if (item is null)
        {
            throw new DomainRuleViolationException("Cart.Item.NotFound", "Sản phẩm không tồn tại trong giỏ hàng.", DomainFailureKind.NotFound);
        }

        item.ChangeQuantity(quantity, product.StockQuantity);
        MarkUpdated();
    }

    public void RemoveItem(Guid requesterId, Guid productId)
    {
        EnsureOwner(requesterId);

        var item = _items.SingleOrDefault(cartItem => cartItem.ProductId == productId);
        if (item is null)
        {
            throw new DomainRuleViolationException("Cart.Item.NotFound", "Sản phẩm không tồn tại trong giỏ hàng.", DomainFailureKind.NotFound);
        }

        _items.Remove(item);
        MarkUpdated();
    }

    public void Clear(Guid requesterId)
    {
        EnsureOwner(requesterId);
        if (_items.Count == 0) return;

        _items.Clear();
        MarkUpdated();
    }

    private void EnsureOwner(Guid requesterId)
    {
        if (requesterId != UserId)
        {
            throw new DomainRuleViolationException("Cart.AccessDenied", "Khách hàng chỉ được thao tác trên giỏ hàng của mình.", DomainFailureKind.Forbidden);
        }
    }
}
