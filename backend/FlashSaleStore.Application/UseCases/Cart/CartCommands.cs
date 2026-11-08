using FlashSaleStore.Application.Queries.Cart;
using FlashSaleStore.Contract.Abstractions.Shared;
using MediatR;

namespace FlashSaleStore.Application.UseCases.Cart;

public sealed record GetMyCartQuery(Guid UserId) : IRequest<Result<CartDto>>;
public sealed record AddCartItemCommand(Guid UserId, Guid ProductId, int Quantity) : IRequest<Result<CartDto>>;
public sealed record SetCartItemQuantityCommand(Guid UserId, Guid ProductId, int Quantity) : IRequest<Result<CartDto>>;
public sealed record RemoveCartItemCommand(Guid UserId, Guid ProductId) : IRequest<Result<CartDto>>;
