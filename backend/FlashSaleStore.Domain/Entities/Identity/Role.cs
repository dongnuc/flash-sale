using FlashSaleStore.Domain.Entities;

namespace FlashSaleStore.Domain.Entities.Identity;

/// <summary>Maps to dbo.Roles.</summary>
public sealed class Role
{
    public Guid Id { get; set; } = BaseEntity.NewId();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? ConcurrencyStamp { get; set; }
}
