using FlashSaleStore.Application.Abstractions.Data;
using FlashSaleStore.Application.Abstractions.Queries;
using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Application.Queries.Cart;
using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Entities.Cart;
using FlashSaleStore.Domain.Exceptions;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Cart;

public sealed class GetMyCartQueryHandler(ICartRepository carts, ICartQueries queries, IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var cart = await carts.GetByUserIdAsync(request.UserId, ct);
                if (cart is null)
                {
                    cart = FlashSaleStore.Domain.Entities.Cart.Cart.Create(request.UserId);
                    if (await carts.InsertAsync(cart, ct) != 1) throw new DomainRuleViolationException("Cart.CreateFailed", "Không thể tạo giỏ hàng.");
                }
                var output = await queries.GetByUserIdAsync(request.UserId, DateTime.UtcNow, ct);
                return Result.Success(output ?? new CartDto { CartId = cart.Id });
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<CartDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}

public sealed class AddCartItemCommandHandler(ICartRepository carts, ICartItemRepository cartItems, IProductRepository products, ICartQueries queries, IUnitOfWork unitOfWork)
    : IRequestHandler<AddCartItemCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var cart = await carts.GetByUserIdAsync(request.UserId, ct);
                if (cart is null)
                {
                    cart = FlashSaleStore.Domain.Entities.Cart.Cart.Create(request.UserId);
                    if (await carts.InsertAsync(cart, ct) != 1) throw new DomainRuleViolationException("Cart.CreateFailed", "Không thể tạo giỏ hàng.");
                }
                var allItems = await cartItems.GetByCartIdAsync(cart.Id, ct);
                cart.LoadItems(allItems);
                var product = await products.GetByIdAsync(request.ProductId, ct);
                if (product is null) return Result.Failure<CartDto>(CommerceUseCaseErrors.NotFound("Product", request.ProductId));
                var existing = allItems.SingleOrDefault(x => x.ProductId == product.Id);
                cart.AddOrIncreaseItem(request.UserId, product, request.Quantity);
                if (existing is null)
                {
                    var created = cart.Items.Single(x => x.ProductId == product.Id);
                    if (await cartItems.InsertAsync(created, ct) != 1) throw new DomainRuleViolationException("Cart.Item.SaveFailed", "Không thể lưu sản phẩm vào giỏ hàng.");
                }
                else if (await cartItems.UpdateAsync(existing, ct) != 1)
                    throw new DomainRuleViolationException("Cart.Item.ConcurrencyConflict", "Dòng giỏ hàng đã thay đổi; hãy thử lại.", DomainFailureKind.Conflict);
                if (await carts.UpdateAsync(cart, ct) != 1) throw new DomainRuleViolationException("Cart.SaveFailed", "Không thể cập nhật giỏ hàng.");
                return Result.Success(await queries.GetByUserIdAsync(request.UserId, DateTime.UtcNow, ct) ?? new CartDto { CartId = cart.Id });
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<CartDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}

public sealed class SetCartItemQuantityCommandHandler(ICartRepository carts, ICartItemRepository cartItems, IProductRepository products, ICartQueries queries, IUnitOfWork unitOfWork)
    : IRequestHandler<SetCartItemQuantityCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(SetCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var cart = await carts.GetByUserIdAsync(request.UserId, ct);
                if (cart is null) return Result.Failure<CartDto>(CommerceUseCaseErrors.NotFound("Cart", request.UserId));
                var allItems = await cartItems.GetByCartIdAsync(cart.Id, ct);
                cart.LoadItems(allItems);
                var product = await products.GetByIdAsync(request.ProductId, ct);
                if (product is null) return Result.Failure<CartDto>(CommerceUseCaseErrors.NotFound("Product", request.ProductId));
                cart.SetItemQuantity(request.UserId, product, request.Quantity);
                var item = allItems.Single(x => x.ProductId == product.Id);
                if (await cartItems.UpdateAsync(item, ct) != 1 || await carts.UpdateAsync(cart, ct) != 1)
                    throw new DomainRuleViolationException("Cart.ConcurrencyConflict", "Giỏ hàng đã thay đổi; hãy tải lại.", DomainFailureKind.Conflict);
                return Result.Success(await queries.GetByUserIdAsync(request.UserId, DateTime.UtcNow, ct) ?? new CartDto { CartId = cart.Id });
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<CartDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}

public sealed class RemoveCartItemCommandHandler(ICartRepository carts, ICartItemRepository cartItems, ICartQueries queries, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCartItemCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var cart = await carts.GetByUserIdAsync(request.UserId, ct);
                if (cart is null) return Result.Failure<CartDto>(CommerceUseCaseErrors.NotFound("Cart", request.UserId));
                var allItems = await cartItems.GetByCartIdAsync(cart.Id, ct);
                cart.LoadItems(allItems);
                cart.RemoveItem(request.UserId, request.ProductId);
                var item = allItems.Single(x => x.ProductId == request.ProductId);
                if (await cartItems.DeleteAsync(item.Id, ct) != 1 || await carts.UpdateAsync(cart, ct) != 1)
                    throw new DomainRuleViolationException("Cart.ConcurrencyConflict", "Giỏ hàng đã thay đổi; hãy tải lại.", DomainFailureKind.Conflict);
                return Result.Success(await queries.GetByUserIdAsync(request.UserId, DateTime.UtcNow, ct) ?? new CartDto { CartId = cart.Id });
            }, cancellationToken);
        }
        catch (DomainRuleViolationException ex) { return Result.Failure<CartDto>(CommerceUseCaseErrors.FromDomain(ex)); }
    }
}
