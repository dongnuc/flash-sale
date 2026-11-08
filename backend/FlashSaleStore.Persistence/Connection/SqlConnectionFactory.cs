using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using FlashSaleStore.Persistence.Options;

namespace FlashSaleStore.Persistence.Connection;

internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IOptions<DatabaseOptions> databaseOptions)
    {
        _connectionString = databaseOptions.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new ArgumentException("Database connection string has not been configured in DatabaseOptions.");
        }
    }

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
