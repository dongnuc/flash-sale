using FlashSaleStore.Domain.Entities.Settings;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string settingKey, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(SystemSetting entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(SystemSetting entity, CancellationToken cancellationToken = default);
}
