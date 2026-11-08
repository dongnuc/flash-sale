using FlashSaleStore.Application.Abstractions.Repositories;
using FlashSaleStore.Domain.Entities.Settings;
using FlashSaleStore.Persistence.Sql;
using FlashSaleStore.Persistence.UnitOfWork;

namespace FlashSaleStore.Persistence.Repositories;

internal sealed class SystemSettingRepository : SqlRepositoryBase, ISystemSettingRepository
{
    public SystemSettingRepository(ISqlExecutor sql, ISqlTransactionAccessor transactionAccessor)
        : base(sql, transactionAccessor) { }

    public Task<SystemSetting?> GetByKeyAsync(string settingKey, CancellationToken cancellationToken = default) =>
        QuerySingleOrDefaultAsync<SystemSetting>("""
            SELECT SettingKey, SettingValue, Description, UpdatedAt, UpdatedByUserId
            FROM dbo.SystemSettings
            WHERE SettingKey = @SettingKey;
            """, new { SettingKey = settingKey }, cancellationToken);

    public Task<int> InsertAsync(SystemSetting entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description, UpdatedAt, UpdatedByUserId)
            VALUES (@SettingKey, @SettingValue, @Description, @UpdatedAt, @UpdatedByUserId);
            """, entity, cancellationToken);

    public Task<int> UpdateAsync(SystemSetting entity, CancellationToken cancellationToken = default) =>
        ExecuteAsync("""
            UPDATE dbo.SystemSettings
            SET SettingValue = @SettingValue, Description = @Description, UpdatedAt = @UpdatedAt, UpdatedByUserId = @UpdatedByUserId
            WHERE SettingKey = @SettingKey;
            """, entity, cancellationToken);
}
