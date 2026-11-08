using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Application.UseCases;

internal static class CommerceUseCaseErrors
{
    public static Error NotFound(string entity, Guid id) => new($"{entity}.NotFound", $"Không tìm thấy {entity.ToLowerInvariant()} '{id}'.");
    public static Error Invalid(string code, string message) => new(code, message);
    public static Error FromDomain(DomainRuleViolationException exception) => new(exception.Code, exception.Message);
}
