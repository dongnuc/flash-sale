namespace FlashSaleStore.Persistence.Options;

internal sealed class DatabaseOptions
{
    public const string ConnectionStringsSection = "ConnectionStrings";
    public const string DefaultConnectionName = "DefaultConnection";

    public string ConnectionString { get; set; } = string.Empty;
}
