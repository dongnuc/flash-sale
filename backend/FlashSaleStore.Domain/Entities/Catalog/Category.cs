using FlashSaleStore.Domain.Entities;
using FlashSaleStore.Domain.Exceptions;

namespace FlashSaleStore.Domain.Entities.Catalog;

/// <summary>Maps to dbo.Categories.</summary>
public sealed class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string NormalizedName => Name.Trim().ToUpperInvariant();

    public static Category Create(string name, string? description)
    {
        var category = new Category();
        category.UpdateDetails(name, description);
        category.MarkUpdated(category.CreatedAt);
        return category;
    }

    public void UpdateDetails(string name, string? description)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new DomainRuleViolationException("Category.Name.Required", "Tên danh mục không được để trống.");
        }

        if (normalizedName.Length > 150)
        {
            throw new DomainRuleViolationException("Category.Name.MaxLength", "Tên danh mục không được vượt quá 150 ký tự.");
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 500)
        {
            throw new DomainRuleViolationException("Category.Description.MaxLength", "Mô tả danh mục không được vượt quá 500 ký tự.");
        }

        Name = normalizedName;
        Description = normalizedDescription;
        MarkUpdated();
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive) return;

        IsActive = isActive;
        MarkUpdated();
    }
}
