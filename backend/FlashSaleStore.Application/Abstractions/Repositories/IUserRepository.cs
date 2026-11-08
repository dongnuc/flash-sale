using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IUserRepository : IEntityRepository<User, Guid>
{
    Task<User?> GetByNormalizedUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
