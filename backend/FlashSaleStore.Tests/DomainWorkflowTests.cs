using FlashSaleStore.Domain.Entities.Cart;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Domain.Entities.Payments;
using FlashSaleStore.Domain.Enums;
using FlashSaleStore.Domain.Exceptions;
using FlashSaleStore.Domain.Services;
using Xunit;

namespace FlashSaleStore.Tests;

public sealed class DomainWorkflowTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Checkout_pricing_splits_sale_quota_and_regular_quantity()
    {
        var now = DateTime.UtcNow;
        var product = Product.Create(CategoryId, "Widget", null, 100m, 10);
        var sale = FlashSale.Create("Sale", now.AddMinutes(-1), now.AddMinutes(10));
        sale.ChangeStatus(FlashSaleStatus.Active, now);
        var saleItem = FlashSaleItem.Create(sale.Id, product.Id, 70m, product.Price, 2, 10, null);

        var allocations = new FlashSalePricingService().Allocate(product, sale, saleItem, 3, now);

        Assert.Collection(allocations,
            line => { Assert.Equal(2, line.Quantity); Assert.Equal(70m, line.UnitPrice); Assert.Equal(saleItem.Id, line.FlashSaleItemId); },
            line => { Assert.Equal(1, line.Quantity); Assert.Equal(100m, line.UnitPrice); Assert.Null(line.FlashSaleItemId); });
    }

    [Fact]
    public void Exhausted_or_inactive_flash_sale_uses_regular_price()
    {
        var now = DateTime.UtcNow;
        var product = Product.Create(CategoryId, "Widget", null, 100m, 10);
        var sale = FlashSale.Create("Sale", now.AddMinutes(-1), now.AddMinutes(10));
        var saleItem = FlashSaleItem.Create(sale.Id, product.Id, 70m, product.Price, 2, 10, null);

        var inactiveAllocation = new FlashSalePricingService().Allocate(product, sale, saleItem, 1, now);
        sale.ChangeStatus(FlashSaleStatus.Active, now);
        saleItem.Reserve(2);
        var exhaustedAllocation = new FlashSalePricingService().Allocate(product, sale, saleItem, 1, now);

        Assert.Equal(100m, Assert.Single(inactiveAllocation).UnitPrice);
        Assert.Equal(100m, Assert.Single(exhaustedAllocation).UnitPrice);
    }

    [Fact]
    public void Flash_sale_schedule_policy_rejects_overlap_but_accepts_adjacent_windows()
    {
        var start = DateTime.UtcNow;
        var existing = new FlashSaleSchedule(Guid.NewGuid(), Guid.NewGuid(), start, start.AddHours(2), FlashSaleStatus.Scheduled);
        var policy = new FlashSaleSchedulePolicy();

        Assert.Throws<DomainRuleViolationException>(() => policy.EnsureNoOverlap(
            new FlashSaleSchedule(Guid.NewGuid(), existing.ProductId, start.AddHours(1), start.AddHours(3), FlashSaleStatus.Scheduled), [existing]));
        policy.EnsureNoOverlap(new FlashSaleSchedule(Guid.NewGuid(), existing.ProductId, start.AddHours(2), start.AddHours(3), FlashSaleStatus.Scheduled), [existing]);
    }

    [Fact]
    public void Cart_rejects_other_users_and_quantity_above_available_stock()
    {
        var cart = Cart.Create(UserId);
        var product = Product.Create(CategoryId, "Widget", null, 100m, 2);

        var ownerFailure = Assert.Throws<DomainRuleViolationException>(() => cart.AddOrIncreaseItem(Guid.NewGuid(), product, 1));
        Assert.Equal("Cart.AccessDenied", ownerFailure.Code);
        var stockFailure = Assert.Throws<DomainRuleViolationException>(() => cart.AddOrIncreaseItem(UserId, product, 3));
        Assert.Equal("CartItem.Stock.Insufficient", stockFailure.Code);
    }

    [Fact]
    public void Expiry_creates_inventory_release_plan_once_with_sale_quota_split()
    {
        var productId = Guid.NewGuid();
        var saleItemId = Guid.NewGuid();
        var order = Order.Create(UserId);
        order.SetShippingInformation("Customer", "0900000000", "Address");
        order.AddLine(new OrderLineAllocation(productId, saleItemId, 2, 70m), "Widget");
        order.AddLine(new OrderLineAllocation(productId, null, 1, 100m), "Widget");
        order.FinalizeCheckout(isOnlinePayment: true);

        Assert.True(order.TryExpire(order.ExpiresAt!.Value));
        Assert.False(order.TryExpire(order.ExpiresAt.Value.AddMinutes(1)));
        var plan = order.CreateInventoryReleasePlan();

        Assert.Equal(new ProductStockRelease(productId, 3), Assert.Single(plan.ProductStock));
        Assert.Equal(new FlashSaleQuotaRelease(saleItemId, 2), Assert.Single(plan.FlashSaleQuota));
    }

    [Fact]
    public void Late_success_records_payment_for_manual_refund_without_restoring_order()
    {
        var payment = Payment.Create(Guid.NewGuid(), UserId, 240m, 123456789);

        payment.MarkPaid("provider-txn-1", DateTime.UtcNow, orderCanAcceptPayment: false);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.True(payment.RequiresManualRefund);
    }
}
