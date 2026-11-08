using System.Data;

namespace FlashSaleStore.Persistence.UnitOfWork;

internal interface ISqlTransactionAccessor
{
    IDbTransaction? CurrentTransaction { get; }
}
