namespace Movie_StructureCode.Persistence.Options;

public sealed class DatabaseOptions
{
    public const string ConnectionStringsSection = "ConnectionStrings";
    public const string DefaultConnectionName = "DefaultConnection";

    public string ConnectionString { get; set; } = string.Empty;
}
