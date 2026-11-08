using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public sealed record UserLoginKey(string LoginProvider, string ProviderKey);

public interface IUserLoginRepository : IEntityRepository<UserLogin, UserLoginKey>
{
    Task<IReadOnlyList<UserLogin>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
