using FlashSaleStore.Domain.Entities.Identity;

namespace FlashSaleStore.Application.Abstractions.Repositories;

/// <summary>User-role links have no mutable fields; assigning or removing a role is the domain operation.</summary>
public interface IUserRoleRepository
{
    Task<IReadOnlyList<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRole>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<int> InsertAsync(UserRole entity, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
