namespace FlashSaleStore.Domain.Entities.Identity;

/// <summary>Maps to dbo.UserRoles; the primary key is (UserId, RoleId).</summary>
public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
