using System.Data;

namespace FlashSaleStore.Persistence.Connection;

internal interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
