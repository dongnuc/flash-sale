namespace FlashSaleStore.Infrastructure.Payments;

public sealed class PayOsOptions
{
    public string? ClientId { get; set; }
    public string? ApiKey { get; set; }
    public string? ChecksumKey { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
}
