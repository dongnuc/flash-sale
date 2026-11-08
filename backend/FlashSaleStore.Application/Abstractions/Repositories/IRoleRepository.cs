using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IRoleRepository : IEntityRepository<Role, Guid>
{
    Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
}
