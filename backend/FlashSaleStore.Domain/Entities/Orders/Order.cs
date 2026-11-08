using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;
using FlashSaleStore.Domain.Services;

namespace FlashSaleStore.Domain.Entities.Orders;

/// <summary>Maps to dbo.Orders.</summary>
public sealed class Order : BaseEntity
{
    private readonly List<OrderDetail> _details = [];

    public Guid UserId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? IdempotencyKey { get; private set; }
    public string? IdempotencyRequestHash { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? ReceiverName { get; private set; }
    public string? ReceiverPhone { get; private set; }
    public string? ShippingAddress { get; private set; }
    public string? Province { get; private set; }
    public string? District { get; private set; }
    public string? Ward { get; private set; }
    public string? AddressLine { get; private set; }
    public string? Note { get; private set; }
    public IReadOnlyCollection<OrderDetail> Details => _details.AsReadOnly();

    public static Order Create(Guid userId, string? idempotencyKey = null, string? requestHash = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleViolationException("Order.UserId.Required", "Khách hàng của đơn hàng không hợp lệ.");
        }

        if (idempotencyKey?.Length > 150 || requestHash?.Length > 256)
        {
            throw new DomainRuleViolationException("Order.Idempotency.Invalid", "Thông tin idempotency vượt quá độ dài cho phép.");
        }

        return new Order
        {
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            IdempotencyRequestHash = requestHash
        };
    }

    public void SetShippingInformation(string receiverName, string receiverPhone, string shippingAddress,
        string? province = null, string? district = null, string? ward = null, string? note = null)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Order.Shipping.Immutable", "Không thể sửa thông tin giao hàng sau khi đơn đã xử lý.", DomainFailureKind.Conflict);
        }

        var name = receiverName?.Trim();
        var phone = receiverPhone?.Trim();
        var address = shippingAddress?.Trim();
        var normalizedProvince = NormalizeOptional(province);
        var normalizedDistrict = NormalizeOptional(district);
        var normalizedWard = NormalizeOptional(ward);
        var normalizedNote = NormalizeOptional(note);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 ||
            string.IsNullOrWhiteSpace(phone) || phone.Length > 30 ||
            string.IsNullOrWhiteSpace(address) || address.Length > 300 ||
            normalizedProvince?.Length > 100 || normalizedDistrict?.Length > 100 || normalizedWard?.Length > 100 || normalizedNote?.Length > 500)
        {
            throw new DomainRuleViolationException("Order.Shipping.Invalid", "Tên người nhận, số điện thoại hoặc địa chỉ giao hàng không hợp lệ.");
        }

        var combinedAddress = string.Join(", ", new[] { normalizedProvince, normalizedDistrict, normalizedWard, address }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (combinedAddress.Length > 500)
        {
            throw new DomainRuleViolationException("Order.Shipping.Invalid", "Địa chỉ giao hàng đầy đủ vượt quá giới hạn lưu trữ.");
        }

        ReceiverName = name;
        ReceiverPhone = phone;
        Province = normalizedProvince;
        District = normalizedDistrict;
        Ward = normalizedWard;
        AddressLine = address;
        Note = normalizedNote;
        ShippingAddress = combinedAddress;
        MarkUpdated();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void FinalizeCheckout(bool isOnlinePayment)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Order.Checkout.StatusInvalid", "Chỉ có thể hoàn tất checkout cho đơn đang chờ xử lý.", DomainFailureKind.Conflict);
        }

        if (_details.Count == 0 || TotalAmount <= 0)
        {
            throw new DomainRuleViolationException("Order.Details.Required", "Đơn hàng phải có ít nhất một dòng hàng và tổng tiền lớn hơn 0.");
        }

        if (string.IsNullOrWhiteSpace(ReceiverName) || string.IsNullOrWhiteSpace(ReceiverPhone) ||
            string.IsNullOrWhiteSpace(ShippingAddress))
        {
            throw new DomainRuleViolationException("Order.Shipping.Required", "Cần có đầy đủ thông tin người nhận và địa chỉ giao hàng.");
        }

        ExpiresAt = isOnlinePayment ? CreatedAt.AddHours(1) : null;
        MarkUpdated();
    }

    public void AddLine(OrderLineAllocation allocation, string productName)
    {
        if (allocation is null)
        {
            throw new DomainRuleViolationException("Order.Line.Required", "Thông tin dòng hàng không hợp lệ.");
        }

        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Order.Details.Immutable", "Không thể thêm dòng hàng sau khi đơn đã xử lý.", DomainFailureKind.Conflict);
        }

        var line = OrderDetail.CreateSnapshot(
            Id,
            allocation.ProductId,
            productName,
            allocation.Quantity,
            allocation.UnitPrice,
            allocation.FlashSaleItemId);

        decimal newTotal;
        try
        {
            newTotal = checked(TotalAmount + line.LineTotal);
        }
        catch (OverflowException)
        {
            throw new DomainRuleViolationException("Order.TotalAmount.Overflow", "Tổng tiền đơn hàng vượt giới hạn cho phép.");
        }

        _details.Add(line);
        TotalAmount = newTotal;
        MarkUpdated();
    }

    public void LoadDetails(IEnumerable<OrderDetail> details)
    {
        if (details is null)
        {
            throw new DomainRuleViolationException("Order.Details.Required", "Danh sách dòng hàng không được để trống.");
        }

        var loadedDetails = details.ToArray();
        if (loadedDetails.Any(detail => detail is null || detail.OrderId != Id || detail.Quantity <= 0))
        {
            throw new DomainRuleViolationException("Order.Details.Invalid", "Các dòng hàng phải thuộc đúng đơn hàng.");
        }

        decimal calculatedTotal;
        try
        {
            calculatedTotal = loadedDetails.Aggregate(0m, (total, detail) => checked(total + detail.LineTotal));
        }
        catch (OverflowException)
        {
            throw new DomainRuleViolationException("Order.TotalAmount.Overflow", "Tổng tiền đơn hàng vượt giới hạn cho phép.");
        }

        if (calculatedTotal != TotalAmount)
        {
            throw new DomainRuleViolationException("Order.TotalAmount.Mismatch", "Tổng tiền không khớp tổng giá trị các dòng hàng.");
        }

        _details.Clear();
        _details.AddRange(loadedDetails);
    }

    public void MarkPaid(DateTime paidAtUtc)
    {
        if (Status == OrderStatus.Paid) return;

        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Order.Payment.StatusInvalid", "Đơn hàng không còn ở trạng thái có thể thanh toán.", DomainFailureKind.Conflict);
        }

        if (ExpiresAt is not null && paidAtUtc >= ExpiresAt.Value)
        {
            throw new DomainRuleViolationException("Order.Payment.DeadlineExceeded", "Đơn hàng đã quá hạn thanh toán.", DomainFailureKind.Conflict);
        }

        Status = OrderStatus.Paid;
        MarkUpdated(paidAtUtc);
    }

    public void StartShipping(DateTime shippedAtUtc)
    {
        if (Status != OrderStatus.Paid)
        {
            throw new DomainRuleViolationException("Order.Shipping.StatusInvalid", "Chỉ đơn đã thanh toán mới được chuyển sang giao hàng.", DomainFailureKind.Conflict);
        }

        Status = OrderStatus.Shipping;
        MarkUpdated(shippedAtUtc);
    }

    public void Complete(DateTime completedAtUtc)
    {
        if (Status != OrderStatus.Shipping)
        {
            throw new DomainRuleViolationException("Order.Completion.StatusInvalid", "Chỉ đơn đang giao mới được hoàn tất.", DomainFailureKind.Conflict);
        }

        Status = OrderStatus.Completed;
        MarkUpdated(completedAtUtc);
    }

    /// <returns>True only when this call changes the order to Cancelled.</returns>
    public bool CancelUnpaid(DateTime cancelledAtUtc)
    {
        if (Status == OrderStatus.Cancelled) return false;

        if (Status != OrderStatus.Pending)
        {
            throw new DomainRuleViolationException("Order.Cancellation.StatusInvalid", "Chỉ đơn chưa thanh toán mới được hủy theo luồng hiện tại.", DomainFailureKind.Conflict);
        }

        Status = OrderStatus.Cancelled;
        MarkUpdated(cancelledAtUtc);
        return true;
    }

    /// <returns>True only for the first successful transition to Expired.</returns>
    public bool TryExpire(DateTime utcNow)
    {
        if (Status != OrderStatus.Pending || ExpiresAt is null || ExpiresAt.Value > utcNow) return false;

        Status = OrderStatus.Expired;
        MarkUpdated(utcNow);
        return true;
    }

    public OrderInventoryReleasePlan CreateInventoryReleasePlan()
    {
        if (Status is not (OrderStatus.Expired or OrderStatus.Cancelled))
        {
            throw new DomainRuleViolationException("Order.InventoryRelease.StatusInvalid", "Chỉ đơn đã hủy hoặc hết hạn mới được hoàn kho.", DomainFailureKind.Conflict);
        }

        try
        {
            var productStock = _details
                .GroupBy(detail => detail.ProductId)
                .Select(group => new ProductStockRelease(group.Key, group.Sum(detail => detail.Quantity)))
                .ToArray();

            var flashSaleQuota = _details
                .Where(detail => detail.FlashSaleItemId.HasValue)
                .GroupBy(detail => detail.FlashSaleItemId!.Value)
                .Select(group => new FlashSaleQuotaRelease(group.Key, group.Sum(detail => detail.Quantity)))
                .ToArray();

            return new OrderInventoryReleasePlan(productStock, flashSaleQuota);
        }
        catch (OverflowException)
        {
            throw new DomainRuleViolationException("Order.InventoryRelease.Overflow", "Số lượng hoàn kho vượt giới hạn cho phép.");
        }
    }
}
