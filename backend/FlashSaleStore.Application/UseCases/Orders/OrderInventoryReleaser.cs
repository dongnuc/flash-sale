using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed class OrderInventoryReleaser(IProductRepository products, IFlashSaleItemRepository flashSaleItems)
{
    public async Task ReleaseAsync(Order order, CancellationToken cancellationToken)
    {
        var plan = order.CreateInventoryReleasePlan();
        foreach (var release in plan.ProductStock)
        {
            var product = await products.GetByIdAsync(release.ProductId, cancellationToken)
                ?? throw new DomainRuleViolationException("Product.NotFound", $"Không tìm thấy sản phẩm '{release.ProductId}' khi hoàn kho.");
            product.ReleaseStock(release.Quantity);
            if (await products.UpdateAsync(product, cancellationToken) != 1)
                throw new DomainRuleViolationException("Order.InventoryRelease.Conflict", "Tồn kho sản phẩm đã thay đổi; hoàn kho sẽ được retry.", DomainFailureKind.Conflict);
        }
        foreach (var release in plan.FlashSaleQuota)
        {
            var item = await flashSaleItems.GetByIdAsync(release.FlashSaleItemId, cancellationToken)
                ?? throw new DomainRuleViolationException("FlashSaleItem.NotFound", $"Không tìm thấy quota FlashSale '{release.FlashSaleItemId}' khi hoàn kho.");
            item.Release(release.Quantity);
            if (await flashSaleItems.UpdateAsync(item, cancellationToken) != 1)
                throw new DomainRuleViolationException("Order.FlashSaleRelease.Conflict", "Quota FlashSale đã thay đổi; hoàn kho sẽ được retry.", DomainFailureKind.Conflict);
        }
    }
}
