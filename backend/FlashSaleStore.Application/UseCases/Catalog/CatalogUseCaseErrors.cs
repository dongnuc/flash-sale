using FlashSaleStore.Contract.Abstractions.Shared;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Application.UseCases.Catalog;

internal static class CatalogUseCaseErrors
{
    public static Error CategoryNotFound(Guid id) => new("Category.NotFound", $"Không tìm thấy category '{id}'.");
    public static Error ProductNotFound(Guid id) => new("Product.NotFound", $"Không tìm thấy product '{id}'.");
    public static Error CategoryNameDuplicate(string name) => new("Category.Name.Duplicate", $"Tên category '{name}' đã tồn tại.");
    public static Error ProductNameDuplicate(string name) => new("Product.Name.Duplicate", $"Tên product '{name}' đã tồn tại.");
    public static Error ProductCategoryNotFound(Guid id) => new("Product.Category.NotFound", $"Không tìm thấy category '{id}' cho product.");
    public static Error ProductConcurrencyConflict(Guid id) => new("Product.ConcurrencyConflict", $"Product '{id}' đã được thay đổi bởi một thao tác khác.");

    public static Error FromDomain(DomainRuleViolationException exception) =>
        new(exception.Code, exception.Message);

    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    public static bool TryParseActiveStatus(string? status, out bool isActive)
    {
        switch (status?.Trim().ToUpperInvariant())
        {
            case "ACTIVE":
                isActive = true;
                return true;
            case "INACTIVE":
                isActive = false;
                return true;
            default:
                isActive = false;
                return false;
        }
    }
}
