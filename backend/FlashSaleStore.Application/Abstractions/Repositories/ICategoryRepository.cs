using FlashSaleStore.Domain.Entities.Catalog;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface ICategoryRepository : IEntityRepository<Category, Guid>
{
    Task<Category?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
