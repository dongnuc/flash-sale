namespace FlashSaleStore.Domain.Entities.Identity;

/// <summary>Maps to dbo.UserClaims.</summary>
public sealed class UserClaim
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimValue { get; set; }
}
