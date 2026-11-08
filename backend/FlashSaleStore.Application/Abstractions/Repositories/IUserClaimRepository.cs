using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

public interface IUserClaimRepository
{
    Task<UserClaim?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserClaim>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <returns>The identity key assigned by SQL Server.</returns>
    Task<int> InsertAsync(UserClaim entity, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(UserClaim entity, CancellationToken cancellationToken = default);
}
