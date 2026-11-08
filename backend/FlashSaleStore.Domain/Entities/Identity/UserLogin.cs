namespace FlashSaleStore.Domain.Entities.Identity;

/// <summary>Maps to dbo.UserLogins; the primary key is (LoginProvider, ProviderKey).</summary>
public sealed class UserLogin
{
    public string LoginProvider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string? ProviderDisplayName { get; set; }
    public Guid UserId { get; set; }
}
