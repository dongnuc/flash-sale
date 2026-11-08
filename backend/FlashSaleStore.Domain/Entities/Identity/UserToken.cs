namespace FlashSaleStore.Domain.Entities.Identity;

/// <summary>Maps to dbo.UserTokens; the primary key is (UserId, LoginProvider, Name).</summary>
public sealed class UserToken
{
    public Guid UserId { get; set; }
    public string LoginProvider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}
