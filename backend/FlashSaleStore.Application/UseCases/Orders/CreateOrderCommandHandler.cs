using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Application.Queries.Orders;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Entities.Cart;
using FlashSaleStore.Domain.Entities.Catalog;
using FlashSaleStore.Domain.Entities.FlashSales;
using FlashSaleStore.Domain.Entities.Orders;
using FlashSaleStore.Domain.Exceptions;
using FlashSaleStore.Domain.Services;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Orders;

public sealed class CreateOrderCommandHandler(ICartRepository carts, ICartItemRepository cartItems, IProductRepository products,
    IFlashSaleRepository flashSales, IFlashSaleItemRepository saleItems, IOrderRepository orders, IOrderDetailRepository details,
    IOrderQueries queries, IUnitOfWork unitOfWork, FlashSalePricingService pricingService)
    : IRequestHandler<CreateOrderCommand, Result<OrderDetailDto>>
{
    public async Task<Result<OrderDetailDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.PaymentMethod, "ONLINE", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.Invalid("Payment.Method.Unsupported", "Hiện chỉ hỗ trợ phương thức thanh toán ONLINE."));
        if (request.DeliveryAddress is null)
            return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.Invalid("Order.Shipping.Required", "Thông tin giao hàng là bắt buộc."));
        var deliveryAddress = request.DeliveryAddress;
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var cart = await carts.GetByUserIdAsync(request.UserId, ct);
                if (cart is null) return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.Invalid("Cart.Empty", "Giỏ hàng đang trống."));
                var cartLines = await cartItems.GetByCartIdAsync(cart.Id, ct);
                if (cartLines.Count == 0) return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.Invalid("Cart.Empty", "Giỏ hàng đang trống."));

                var productIds = cartLines.Select(item => item.ProductId).Distinct().ToArray();
                var productList = await products.GetByIdsAsync(productIds, ct);
                if (productList.Count != productIds.Length)
                {
                    var found = productList.Select(product => product.Id).ToHashSet();
                    return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.NotFound("Product", productIds.First(id => !found.Contains(id))));
                }
                var productById = productList.ToDictionary(product => product.Id);
                var schedules = await saleItems.GetSchedulesByProductIdsAsync(productIds, ct);
                var saleIds = schedules.Where(x => x.Status != FlashSaleStore.Domain.Enums.FlashSaleStatus.Cancelled).Select(x => x.FlashSaleId).Distinct().ToArray();
                var saleList = new List<FlashSale>();
                foreach (var id in saleIds)
                {
                    var sale = await flashSales.GetByIdAsync(id, ct);
                    if (sale is not null) saleList.Add(sale);
                }

                var now = DateTime.UtcNow;
                var plan = new List<(Product Product, FlashSaleItem? SaleItem, IReadOnlyList<OrderLineAllocation> Lines)>();
                foreach (var cartLine in cartLines)
                {
                    var product = productById[cartLine.ProductId];
                    var activeSchedule = schedules.Where(s => s.ProductId == product.Id && s.StartsAt <= now && now < s.EndsAt && s.Status == FlashSaleStore.Domain.Enums.FlashSaleStatus.Active)
                        .OrderBy(s => s.FlashSaleId).FirstOrDefault();
                    FlashSale? activeSale = activeSchedule is null ? null : saleList.SingleOrDefault(s => s.Id == activeSchedule.FlashSaleId);
                    FlashSaleItem? activeItem = activeSchedule is null ? null : await saleItems.GetByFlashSaleAndProductAsync(activeSchedule.FlashSaleId, product.Id, ct);
                    var allocations = pricingService.Allocate(product, activeSale, activeItem, cartLine.Quantity, now);
                    if (activeItem is not null && activeSale is not null && activeSale.IsEffectiveAt(now))
                    {
                        var saleQuantity = allocations.Where(x => x.FlashSaleItemId == activeItem.Id).Sum(x => x.Quantity);
                        if (saleQuantity > 0)
                        {
                            var alreadyBought = await saleItems.GetPurchasedOrReservedQuantityAsync(activeItem.Id, request.UserId, ct);
                            activeItem.EnsureUserLimit(saleQuantity, alreadyBought);
                            activeItem.Reserve(saleQuantity);
                        }
                    }
                    product.ReserveStock(cartLine.Quantity);
                    plan.Add((product, activeItem, allocations));
                }

                var order = Order.Create(request.UserId);
                order.SetShippingInformation(deliveryAddress.RecipientName, deliveryAddress.Phone,
                    deliveryAddress.AddressLine, deliveryAddress.Province,
                    deliveryAddress.District, deliveryAddress.Ward, deliveryAddress.Note);
                foreach (var line in plan.SelectMany(x => x.Lines))
                    order.AddLine(line, productById[line.ProductId].Name);
                order.FinalizeCheckout(isOnlinePayment: true);

                if (await orders.InsertAsync(order, ct) != 1) throw new DomainRuleViolationException("Order.CreateFailed", "Không thể tạo đơn hàng.");
                if (await details.InsertRangeAsync(order.Details.ToArray(), ct) != order.Details.Count)
                    throw new DomainRuleViolationException("Order.Details.SaveFailed", "Không thể lưu đầy đủ chi tiết đơn hàng.");
                foreach (var product in productList)
                    if (await products.UpdateAsync(product, ct) != 1) throw new DomainRuleViolationException("Order.Stock.ConcurrencyConflict", $"Tồn kho sản phẩm '{product.Id}' vừa thay đổi; toàn bộ đơn đã rollback.", DomainFailureKind.Conflict);
                foreach (var saleItem in plan.Where(x => x.SaleItem is not null).Select(x => x.SaleItem!).DistinctBy(x => x.Id))
                    if (await saleItems.UpdateAsync(saleItem, ct) != 1) throw new DomainRuleViolationException("Order.FlashSaleQuota.Conflict", "Quota FlashSale vừa thay đổi; toàn bộ đơn đã rollback.", DomainFailureKind.Conflict);
                foreach (var cartLine in cartLines)
                    if (await cartItems.DeleteAsync(cartLine.Id, ct) != 1) throw new DomainRuleViolationException("Order.Cart.ClearFailed", "Không thể xóa giỏ hàng sau khi đặt hàng.");
                var output = await queries.GetCustomerDetailAsync(order.Id, request.UserId, ct)
                    ?? throw new InvalidOperationException("Order was inserted but its detail could not be read.");
                return Result.Success(output);
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.FromDomain(ex)); }
        catch (OverflowException) { return Result.Failure<OrderDetailDto>(CommerceUseCaseErrors.Invalid("Order.Amount.Overflow", "Tổng tiền đơn hàng vượt giới hạn cho phép.")); }
    }
}
