using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IRoleClaimRepository
{
    Task<RoleClaim?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleClaim>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    /// <returns>The identity key assigned by SQL Server.</returns>
    Task<int> InsertAsync(RoleClaim entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(RoleClaim entity, CancellationToken cancellationToken = default);
}
